#!/usr/bin/env python3
"""Stab area / arm / decalage sweep for pitch stability, from a Foil performance console log.

Usage:
  python3 pitch_sweep.py log.txt [--target 5] [--vmin 12] [--vmax 30]
                         [--ratios 0.12:0.35:0.01] [--arms 550:1000:50] [--dec -1:2:0.5]
                         [--pod-dz 0] [--unpowered] [--top 12]

log.txt is the whole FeatureScript console output with Print report on (and Log inputs on in
Foil stabiliser): it needs the 'Wing:' / 'Stab:' / 'Neutral point' lines, PERFORMANCE_INPUTS,
STAB_INPUTS and the CSV. Everything is rebuilt from those numbers - no geometry is recomputed -
and the rebuilt model is checked against the CSV's own trim CG before anything is swept.

What it varies (each relative to the logged design):
  stab area  (as Area ratio = stab area / wing area; same aspect ratio, so the same lift slope)
  Arm        (moves the stab's aerodynamic centre 1:1)
  decalage   (+1 deg = stab incidence 1 deg more negative, or Link decalage +1)
  --pod-dz   (with Propulsion: move the thrust line up/down in mm; the drag change from the
              different ride depth is NOT included, so treat it as a first estimate)

Pitch model = the tool's own trim equations (Foil performance's trimPoint):
  alpha from lift = weight, stab sees alpha - de/da x CLw, trim CG = -M/W about the wing pivot,
  neutral point from the two lift slopes, static margin = (NP - CG)/MAC.
With Propulsion on, the thrust couple from the CSV (thrust_couple_Nm) is kept, plus the extra
thrust needed for any extra stab drag acting at the thrust line height.
"""
import math
import re
import sys

G = 9.80665


def num(pattern, text, cast=float, default=None):
    m = re.search(pattern, text)
    return cast(m.group(1)) if m else default


def kv_line(prefix, text):
    m = re.search(prefix + r":\s*(.*)", text)
    if not m:
        return {}
    out = {}
    for part in m.group(1).split(";"):
        if "=" in part:
            k, v = part.split("=", 1)
            out[k.strip()] = v.strip()
    return out


def load_csv(text):
    lines = text.splitlines()
    for i, ln in enumerate(lines):
        if "speed_kmh" in ln:
            hdr = ln[ln.index("speed_kmh"):].strip().split(",")
            rows = []
            for r in lines[i + 1:]:
                p = r.strip().split(",")
                if len(p) != len(hdr):
                    if rows:
                        break
                    continue
                try:
                    rows.append({h: (float(v) if v.strip() else None) for h, v in zip(hdr, p)})
                except ValueError:
                    break
            return rows
    sys.exit("no CSV sweep found (turn on Print report in Foil performance)")


def naca_cm0(m, p, N=400):
    """Thin-aerofoil quarter-chord moment of a NACA 4-digit mean line (same integral as the tool)."""
    if m <= 0:
        return 0.0
    tot = 0.0
    for i in range(N):
        th = (i + 0.5) * math.pi / N
        x = 0.5 * (1 - math.cos(th))
        dz = 2 * m / p ** 2 * (p - x) if x < p else 2 * m / (1 - p) ** 2 * (p - x)
        tot += dz * (math.cos(2 * th) - math.cos(th)) * math.pi / N
    return 0.5 * tot


def skin(Re):
    return 0.455 / math.log10(Re) ** 2.58


class Model:
    def __init__(self, text):
        self.text = text
        w = re.search(r"Wing: S ([\d.]+) cm2, AR ([\d.]+), CLa ([\d.]+)/rad, CL0 (-?[\d.]+).*?Cm0 (-?[\d.]+), ac (-?[\d.]+) mm", text)
        s = re.search(r"Stab: S ([\d.]+) cm2, AR ([\d.]+), CLa ([\d.]+)/rad, CL0 (-?[\d.]+), ac (-?[\d.]+) mm, downwash de/da ([\d.]+)", text)
        if not w or not s:
            sys.exit("need the 'Wing:' and 'Stab:' lines from the Foil performance report (a stabiliser is required)")
        self.Sw, self.ARw, self.CLaw, self.CL0w, self.cm0w, self.xw = [float(v) for v in w.groups()]
        self.Sw /= 1e4
        self.xw /= 1000
        self.Ss, self.ARs, self.CLas, self.CL0s, self.xs, deda = [float(v) for v in s.groups()]
        self.Ss /= 1e4
        self.xs /= 1000
        self.ke = deda / self.CLaw
        self.xnp_log = num(r"Neutral point (-?[\d.]+) mm", text) / 1000
        perf = kv_line("PERFORMANCE_INPUTS", text)
        stab = kv_line("STAB_INPUTS", text)
        self.perf, self.stab = perf, stab
        self.W = float(perf.get("mass_kg", 75)) * G
        self.rho = float(perf.get("density", 1025))
        self.Mext = float(perf.get("extMoment_Nm", 0))
        T = float(perf.get("waterTemp_C", 20))
        nu = 1.792e-6 / (1 + 0.0337 * T + 0.000221 * T * T)
        self.nu = nu * 1.05 if self.rho > 1010 else nu
        self.arm = float(stab.get("arm_mm", "nan")) / 1000
        self.linked = stab.get("stabLink") == "LINKED"
        self.ratio0 = self.Ss / self.Sw
        camb = float(stab.get("camber", 0)) / 100
        cpos = float(stab.get("camberPos", 40)) / 100
        self.cm0s = naca_cm0(camb, cpos) if stab.get("profile", "NACA4") == "NACA4" else 0.0
        self.macs0 = math.sqrt(self.Ss / self.ARs)          # close enough for Re and the (small) stab Cm term
        self.rows = load_csv(text)
        self.powered = "thrust_couple_Nm" in self.rows[0] and self.rows[0].get("thrust_couple_Nm") is not None
        self.zT = (num(r"thrust line at the pod axis, Z (-?[\d.]+) mm", text) or 0) / 1000
        # wing MAC from the logged static margin, the one quantity the report does not print directly
        r = next(r for r in self.rows if r.get("static_margin_pct"))
        self.mac = (self.xnp_log - r["trim_cg_mm"] / 1000) / (r["static_margin_pct"] / 100)

    def point(self, v, ratio=None, darm=0.0, ddec=0.0, podz=0.0, powered=True):
        ratio = self.ratio0 if ratio is None else ratio
        k = ratio / self.ratio0
        Ss = self.Ss * k
        macs = self.macs0 * math.sqrt(k)
        xs = self.arm + darm + (self.xs - self.arm) * math.sqrt(k)
        CL0s = self.CL0s - self.CLas * math.radians(ddec)
        C1 = CL0s - self.CLas * self.ke * self.CL0w
        C2 = self.CLas * (1 - self.ke * self.CLaw)
        xnp = (self.Sw * self.CLaw * self.xw + Ss * C2 * xs) / (self.Sw * self.CLaw + Ss * C2)
        V = v / 3.6
        q = 0.5 * self.rho * V * V
        al = (self.W / q - self.Sw * self.CL0w - Ss * C1) / (self.Sw * self.CLaw + Ss * C2)
        CLw = self.CL0w + self.CLaw * al
        CLs = CL0s + self.CLas * (al - self.ke * CLw)
        Mo = -q * self.Sw * CLw * self.xw + q * self.Sw * self.mac * self.cm0w - q * Ss * CLs * xs + q * Ss * macs * self.cm0s + self.Mext
        cds = skin(max(V * macs / self.nu, 1e4)) * 2.04 * 1.2 * (1 + 0.38 * CLs ** 2) + CLs ** 2 / (math.pi * self.ARs * 0.95)
        Dstab = q * Ss * cds
        extra = 0.0
        if powered and self.powered:
            row = min(self.rows, key=lambda r: abs(r["speed_kmh"] - v))
            base = self.point(v, powered=False) if (ratio, darm, ddec, podz) != (self.ratio0, 0, 0, 0) else None
            dD = Dstab - base["Dstab"] if base else 0.0
            T = row["thrust_N"] + dD
            extra = row["thrust_couple_Nm"] - dD * self.zT - T * podz
            Mo += extra
        xcg = -Mo / self.W
        return dict(xnp=xnp, xcg=xcg, sm=(xnp - xcg) / self.mac * 100, CLs=CLs, Dstab=Dstab, couple=extra)


def frange(spec):
    a, b, s = [float(x) for x in spec.split(":")]
    out, x = [], a
    while x <= b + 1e-9:
        out.append(round(x, 6))
        x += s
    return out


def main(argv):
    if not argv or argv[0].startswith("-"):
        sys.exit(__doc__)
    opts = {"--target": "5", "--vmin": None, "--vmax": None, "--ratios": None, "--arms": None,
            "--dec": "0:0:1", "--pod-dz": "0", "--top": "12"}
    flags = set()
    i = 1
    while i < len(argv):
        if argv[i] == "--unpowered":
            flags.add("unpowered")
            i += 1
        else:
            opts[argv[i]] = argv[i + 1]
            i += 2
    m = Model(open(argv[0], encoding="utf-8", errors="replace").read())
    powered = m.powered and "unpowered" not in flags
    speeds = [r["speed_kmh"] for r in m.rows]
    vmin = float(opts["--vmin"]) if opts["--vmin"] else speeds[0]
    vmax = float(opts["--vmax"]) if opts["--vmax"] else speeds[-1]
    vs = [v for v in speeds if vmin - 1e-9 <= v <= vmax + 1e-9]
    podz = float(opts["--pod-dz"]) / 1000

    # 1. check the rebuilt model against the log
    err_np = abs(m.point(vs[0], powered=False)["xnp"] - m.xnp_log) * 1000
    err_cg = max(abs(m.point(r["speed_kmh"], powered=False)["xcg"] * 1000 - r["trim_cg_mm"]) for r in m.rows)
    print(f"model check: neutral point within {err_np:.1f} mm, unpowered trim CG within {err_cg:.1f} mm of the CSV "
          f"(wing MAC {m.mac * 1000:.1f} mm)")
    if powered:
        errp = max(abs(m.point(r["speed_kmh"])["xcg"] * 1000 - r["trim_cg_powered_mm"]) for r in m.rows)
        print(f"             powered trim CG within {errp:.1f} mm (thrust line Z {m.zT * 1000:.0f} mm)")
    if err_cg > 2 or err_np > 2:
        print("WARNING: the rebuilt model does not match this log closely - treat the sweep as indicative only")

    def summary(ratio, darm, ddec):
        pts = [m.point(v, ratio, darm, ddec, podz, powered) for v in vs]
        sms = [p["sm"] for p in pts]
        lo = min(range(len(sms)), key=lambda j: sms[j])
        vc = float(m.perf.get("vCruise_kmh", 20))
        dDc = m.point(vc, ratio, darm, ddec, 0, False)["Dstab"] - m.point(vc, powered=False)["Dstab"]
        return dict(sm_min=sms[lo], v_min=vs[lo], shift=(pts[0]["xcg"] - pts[-1]["xcg"]) * 1000,
                    cls=(min(p["CLs"] for p in pts), max(p["CLs"] for p in pts)), dD=dDc)

    kind = "powered" if powered else "unpowered"
    base = summary(m.ratio0, 0, 0)
    print(f"\nlogged design ({kind}, {vmin:g}-{vmax:g} km/h): lowest static margin {base['sm_min']:.1f}% MAC at {base['v_min']:g} km/h, "
          f"stance shift {base['shift']:.0f} mm, stab CL {base['cls'][0]:+.2f} to {base['cls'][1]:+.2f}")
    if podz:
        print(f"pod moved {podz * 1000:+.0f} mm (drag change from the new ride depth not included)")

    ratios = frange(opts["--ratios"]) if opts["--ratios"] else [round(m.ratio0 + d, 3) for d in frange("0:0.20:0.01")]
    arms = frange(opts["--arms"]) if opts["--arms"] else [round(m.arm * 1000 + d) for d in frange("0:300:50")]
    decs = frange(opts["--dec"])
    target = float(opts["--target"])
    hits = []
    for a in arms:
        for d in decs:
            for r in ratios:                      # smallest area that reaches the target at this arm/decalage
                s = summary(r, a / 1000 - m.arm, d)
                if s["sm_min"] >= target:
                    hits.append((r, a, d, s))
                    break
    if not hits:
        print(f"\nno combination in the range reaches {target:g}% MAC {kind} from {vmin:g} km/h - raise --vmin, widen the ranges, "
              f"or (powered) lower the thrust line with --pod-dz")
        return
    hits.sort(key=lambda h: (h[3]["dD"], abs(h[3]["shift"])))
    inc = float(m.stab.get("incidence_deg", "nan"))
    ldec = float(m.stab.get("linkDecalage_deg", "nan"))
    print(f"\nsmallest stab for >= {target:g}% MAC {kind} from {vmin:g} to {vmax:g} km/h, cheapest (least extra drag at cruise) first:")
    print(f"{'Area ratio':>10} {'area cm2':>9} {'Arm mm':>7} {'Link dec':>8} {'Incid.':>7} {'min SM':>7} {'at':>5} {'stance':>7} {'stab CL':>13} {'+drag N':>8}")
    for r, a, d, s in hits[: int(opts["--top"])]:
        print(f"{r:10.2f} {r * m.Sw * 1e4:9.0f} {a:7.0f} {ldec + d:8.1f} {inc - d:7.1f} {s['sm_min']:6.1f}% {s['v_min']:5.0f} "
              f"{s['shift']:6.0f}mm {s['cls'][0]:+6.2f}/{s['cls'][1]:+.2f} {s['dD']:+8.2f}")
    print(f"\n{'Linked' if m.linked else 'Independent'} stab: change {'Area ratio / Arm / Link decalage' if m.linked else 'Area cm^2 / Arm / Incidence'} in Foil stabiliser. "
          "'stance' = trim CG move from the lowest to the highest speed (how far the rider shifts). "
          "Extra drag is the stab only; a longer Arm also lengthens Foil fuselage (not included).")


if __name__ == "__main__":
    main(sys.argv[1:])
