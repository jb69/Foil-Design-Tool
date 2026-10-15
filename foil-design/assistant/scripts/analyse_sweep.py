#!/usr/bin/env python3
"""Summarise one or two Foil performance CSV sweeps.

Usage: python3 analyse_sweep.py run_a.csv [run_b.csv] [--plot out.png]

Accepts raw FeatureScript console output: it finds the header line starting
with 'speed_kmh' and reads the comma-separated rows after it, ignoring anything else.
"""
import sys

REQUIRED = ["speed_kmh", "L_D", "power_W", "drag_N"]


def load(path):
    rows, header = [], None
    with open(path, encoding="utf-8", errors="replace") as f:
        for raw in f:
            line = raw.strip()
            # console lines can carry a prefix before the data
            if "speed_kmh" in line:
                header = line[line.index("speed_kmh"):].split(",")
                continue
            if header is None or "," not in line:
                continue
            parts = line.split(",")
            if len(parts) < len(header):
                # try stripping a console prefix from the first field
                continue
            parts = parts[-len(header):] if len(parts) > len(header) else parts
            try:
                first = float(parts[0].split()[-1])
            except ValueError:
                continue
            row = {}
            for key, val in zip(header, parts):
                try:
                    row[key] = float(val.split()[-1]) if val.strip() else None
                except ValueError:
                    row[key] = None
            row["speed_kmh"] = first
            rows.append(row)
    if not rows:
        sys.exit(f"{path}: no sweep rows found (looked for a header starting with speed_kmh)")
    missing = [k for k in REQUIRED if k not in rows[0]]
    if missing:
        sys.exit(f"{path}: missing columns {missing}")
    return rows


def at(rows, key, v):
    """Linear interpolation of column key at speed v."""
    for a, b in zip(rows, rows[1:]):
        if a["speed_kmh"] <= v <= b["speed_kmh"] and a.get(key) is not None and b.get(key) is not None:
            t = (v - a["speed_kmh"]) / (b["speed_kmh"] - a["speed_kmh"] or 1)
            return a[key] + t * (b[key] - a[key])
    return None


def summary(rows):
    s = {}
    best = max(rows, key=lambda r: r["L_D"])
    s["best L/D"] = f'{best["L_D"]:.1f} at {best["speed_kmh"]:.1f} km/h'
    minp = min(rows, key=lambda r: r["power_W"])
    s["min power"] = f'{minp["power_W"]:.0f} W at {minp["speed_kmh"]:.1f} km/h'
    lo, hi = rows[0], rows[-1]
    s["range"] = f'{lo["speed_kmh"]:.1f}-{hi["speed_kmh"]:.1f} km/h'
    if lo.get("max_local_cl_pct") is not None:
        s["take-off loading"] = f'{lo["max_local_cl_pct"]:.0f}% of CL max at {lo["speed_kmh"]:.1f} km/h'
        over = [r["speed_kmh"] for r in rows if (r.get("max_local_cl_pct") or 0) >= 100]
        if over:
            s["stalled at"] = f"speeds up to {max(over):.1f} km/h"
    if lo.get("trim_cg_mm") is not None and hi.get("trim_cg_mm") is not None:
        s["trim CG"] = f'{lo["trim_cg_mm"]:+.0f} mm at {lo["speed_kmh"]:.0f} km/h -> {hi["trim_cg_mm"]:+.0f} mm at {hi["speed_kmh"]:.0f} km/h'
        s["stance shift"] = f'{abs(lo["trim_cg_mm"] - hi["trim_cg_mm"]):.0f} mm ({"forward" if hi["trim_cg_mm"] < lo["trim_cg_mm"] else "aft"} as speed rises)'
    sm = [r["static_margin_pct"] for r in rows if r.get("static_margin_pct") is not None]
    if sm:
        s["static margin"] = f"{min(sm):.0f}% to {max(sm):.0f}% MAC (lowest at low speed)" if sm[0] <= sm[-1] else f"{min(sm):.0f}% to {max(sm):.0f}% MAC"
    stab = [r["CL_stab"] for r in rows if r.get("CL_stab") is not None]
    if stab:
        s["stab CL"] = f"{min(stab):+.2f} to {max(stab):+.2f}"
    if "drag_induced_N" in rows[0]:
        cross = None
        for a, b in zip(rows, rows[1:]):
            da = a["drag_induced_N"] - a["drag_profile_N"]
            db = b["drag_induced_N"] - b["drag_profile_N"]
            if da >= 0 > db:
                cross = a["speed_kmh"] + (b["speed_kmh"] - a["speed_kmh"]) * da / (da - db)
        s["induced = profile drag"] = f"at {cross:.1f} km/h (induced dominates below)" if cross else (
            "induced dominates throughout" if rows[-1]["drag_induced_N"] > rows[-1]["drag_profile_N"] else "profile dominates throughout")
    alpha = [r["alpha_deg"] for r in rows if r.get("alpha_deg") is not None]
    if alpha:
        s["board attitude"] = f"{alpha[0]:+.1f} deg at {lo['speed_kmh']:.0f} km/h -> {alpha[-1]:+.1f} deg at {hi['speed_kmh']:.0f} km/h"
    # Propulsion group (Foil performance 1.007+): thrust = drag at the powered ride depth
    if rows[0].get("Wh_per_km") is not None:
        best = min(rows, key=lambda r: r["Wh_per_km"])
        s["energy"] = f'lowest {best["Wh_per_km"]:.1f} Wh/km at {best["speed_kmh"]:.0f} km/h ({best["batt_W"]:.0f} W, {best["throttle_pct"]:.0f}% throttle)'
        s["throttle / motor A"] = f'{lo["throttle_pct"]:.0f}% / {lo["motor_A"]:.0f} A at {lo["speed_kmh"]:.0f} km/h -> {hi["throttle_pct"]:.0f}% / {hi["motor_A"]:.0f} A at {hi["speed_kmh"]:.0f} km/h'
        out = [r["speed_kmh"] for r in rows if r.get("in_limits") == 0]
        if out:
            s["beyond the drivetrain"] = f"from {min(out):.0f} km/h (needs more current or throttle than available)"
        cross = None
        for a, b in zip(rows, rows[1:]):
            ga, gb = a["max_thrust_N"] - a["thrust_N"], b["max_thrust_N"] - b["thrust_N"]
            if ga >= 0 > gb:
                cross = a["speed_kmh"] + (b["speed_kmh"] - a["speed_kmh"]) * ga / (ga - gb)
        s["top speed"] = f"{cross:.1f} km/h (full-power thrust = drag)" if cross else f"above {hi['speed_kmh']:.0f} km/h (still {hi['max_thrust_N'] - hi['thrust_N']:.0f} N spare at the top of the sweep)"
    # powered trim (1.008+): thrust couple from a motor pod
    if rows[0].get("static_margin_powered_pct") is not None:
        smp = [(r["static_margin_powered_pct"], r["speed_kmh"]) for r in rows]
        m, v = min(smp)
        s["powered static margin"] = f"{m:.0f}% MAC at {v:.0f} km/h (lowest){'  <- pitch-unstable under power' if m < 0 else '  <- twitchy under power' if m < 5 else ''}"
        neg = [v for sm, v in smp if sm < 5]
        if neg:
            s["under 5% under power"] = f"{min(neg):.0f}-{max(neg):.0f} km/h - see pitch_sweep.py"
        s["powered trim CG"] = f'{lo["trim_cg_powered_mm"]:+.0f} mm -> {hi["trim_cg_powered_mm"]:+.0f} mm (couple {lo["thrust_couple_Nm"]:.1f} -> {hi["thrust_couple_Nm"]:.1f} Nm)'
    return s


def compare(a, b):
    lines = []
    lo = max(a[0]["speed_kmh"], b[0]["speed_kmh"])
    hi = min(a[-1]["speed_kmh"], b[-1]["speed_kmh"])
    if lo >= hi:
        return ["no overlapping speed range"]
    speeds = sorted({round(lo), round((lo + hi) / 2), round(hi)})
    prop = a[0].get("Wh_per_km") is not None and b[0].get("Wh_per_km") is not None
    lines.append(f"{'km/h':>6} {'L/D A':>7} {'L/D B':>7} {'power A':>8} {'power B':>8} {'CG A':>6} {'CG B':>6}"
                 + (f" {'Wh/km A':>8} {'Wh/km B':>8} {'pwrSM A':>8} {'pwrSM B':>8}" if prop else ""))
    for v in speeds:
        v = min(max(v, lo), hi)
        def f(x, fmt):
            return format(x, fmt) if x is not None else "-"
        lines.append(f"{v:6.1f} {f(at(a,'L_D',v),'7.1f')} {f(at(b,'L_D',v),'7.1f')} {f(at(a,'power_W',v),'8.0f')} {f(at(b,'power_W',v),'8.0f')} "
                     f"{f(at(a,'trim_cg_mm',v),'6.0f')} {f(at(b,'trim_cg_mm',v),'6.0f')}"
                     + (f" {f(at(a,'Wh_per_km',v),'8.1f')} {f(at(b,'Wh_per_km',v),'8.1f')} {f(at(a,'static_margin_powered_pct',v),'8.1f')} {f(at(b,'static_margin_powered_pct',v),'8.1f')}" if prop else ""))
    return lines


def plot(runs, names, out):
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        print("matplotlib not available; skipping plot")
        return
    fig, ax = plt.subplots(2, 2, figsize=(10, 7))
    for rows, name in zip(runs, names):
        v = [r["speed_kmh"] for r in rows]
        ax[0][0].plot(v, [r["L_D"] for r in rows], label=name)
        ax[0][1].plot(v, [r["power_W"] for r in rows], label=name)
        if rows[0].get("trim_cg_mm") is not None:
            ax[1][0].plot(v, [r["trim_cg_mm"] for r in rows], label=name)
        if "drag_induced_N" in rows[0]:
            ax[1][1].plot(v, [r["drag_induced_N"] for r in rows], "--", label=f"{name} induced")
            ax[1][1].plot(v, [r["drag_profile_N"] for r in rows], ":", label=f"{name} profile")
    for a, t, y in [(ax[0][0], "L/D", "L/D"), (ax[0][1], "Power", "W"), (ax[1][0], "Trim CG (aft +)", "mm"), (ax[1][1], "Drag split", "N")]:
        a.set_title(t); a.set_xlabel("km/h"); a.set_ylabel(y); a.grid(alpha=0.3); a.legend(fontsize=8)
    fig.tight_layout()
    fig.savefig(out, dpi=120)
    print(f"plot saved to {out}")


def main(argv):
    files, out = [], None
    i = 0
    while i < len(argv):
        if argv[i] == "--plot":
            out = argv[i + 1]
            i += 2
        else:
            files.append(argv[i])
            i += 1
    if not files or len(files) > 2:
        sys.exit(__doc__)
    runs = [load(f) for f in files]
    names = ["A", "B"][: len(runs)]
    for rows, name, f in zip(runs, names, files):
        print(f"== Run {name}: {f}")
        for k, v in summary(rows).items():
            print(f"  {k:24s} {v}")
    if len(runs) == 2:
        print("== A vs B")
        for line in compare(*runs):
            print("  " + line)
    if out:
        plot(runs, names, out)


if __name__ == "__main__":
    main(sys.argv[1:])
