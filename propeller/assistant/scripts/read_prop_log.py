#!/usr/bin/env python3
"""Summarise a log from the Onshape B-Series Propeller features (propLog / propNotice, moldLog / moldNotice,
partModelLog, forgeLog / forgeNotice), pasted into a text file. Finds the lines it knows, ignores the rest.

    python3 read_prop_log.py run_a.txt [run_b.txt]     one log: a summary; two: a side-by-side comparison
"""
import os
import re
import sys

NUM = r"(-?\d+(?:\.\d+)?)"
PATTERNS = [
    # (key, label, regex, group)
    ("build", "build", r"(?:build|Build)\s+(\d+)\b", 1),
    ("D", "diameter, mm", r"\bD " + NUM + r" mm, hub", 1),
    ("hub", "hub at the root plane, mm", r"mm, hub " + NUM + r" mm \(r/R", 1),
    ("pd", "P/D", r"P/D " + NUM + r" ->", 1),
    ("ear", "EAR", r"\bEAR " + NUM + r", Z", 1),
    ("Z", "blades", r"\bEAR [\d.]+, Z (\d+)", 1),
    ("t_root", "root thickness, mm", r"t root " + NUM, 1),
    ("torque", "shaft torque at the limit, Nm", r"shaft torque at the current limit: " + NUM + r" Nm", 1),
    ("noload", "no-load prop rpm", r"no-load prop speed " + NUM + r" rpm", 1),
    ("st_rpm", "static rpm", r"static, at the current limit: " + NUM + r" prop rpm", 1),
    ("st_N", "static thrust, N", r"static, at the current limit: [\d.]+ prop rpm, " + NUM + r" N", 1),
    ("st_kW", "static shaft power, kW", r"N \([\d.]+ kgf\) thrust, " + NUM + r" kW at the shaft", 1),
    ("tip", "static tip speed, m/s", r"tip speed " + NUM + r" m/s", 1),
    ("st_pct", "static, % of no-load", r"\(" + NUM + r" % of no-load speed\)", 1),
    ("ft_V", "full throttle at, km/h", r"full throttle at " + NUM + r" km/h", 1),
    ("ft_N", "full throttle thrust, N", r"full throttle at [\d.]+ km/h: about [\d.]+ prop rpm, J [\d.]+, " + NUM + r" N", 1),
    ("ft_eta", "full throttle efficiency, %", r"full throttle at .*?open-water efficiency " + NUM + r" %", 1),
    ("ft_pct", "full throttle, % of no-load", r"of the [\d.]+ A limit, at " + NUM + r" % of no-load", 1),
    ("cr_drag", "cruise drag, N", r"steady cruise at [\d.]+ km/h against " + NUM + r" N", 1),
    ("cr_rpm", "cruise rpm", r"steady cruise at [\d.]+ km/h against [\d.]+ N of drag: " + NUM + r" prop rpm", 1),
    ("cr_eta", "cruise efficiency, %", r"steady cruise .*?open-water efficiency " + NUM + r" %", 1),
    ("cr_A", "cruise motor current, A", r"motor " + NUM + r" A at [\d.]+ V \(", 1),
    ("cr_thr", "cruise throttle, %", r"V \(" + NUM + r" % throttle\)", 1),
    ("cr_whkm", "cruise Wh/km", NUM + r" Wh/km at the motor terminals", 1),
    ("release", "mold: release outside the well", r"release along the axis outside the well: ([^;|]*)", 1),
    ("sectors", "mold: sectors clear", r"(\d+ of \d+ clear)", 1),
    ("fill", "mold: cavity fill, %", r"makes " + NUM + r" % of the cavity", 1),
    ("lam", "mold: laminate, cm3", r"laminate " + NUM + r" cm3", 1),
    ("cured", "mold: cured mass, g", r"cm3 \(part [^)]*\), " + NUM + r" g cured", 1),
    ("groove", "mold: groove against flash", r"about ([\d.]+ cm3 against [\d.]+ cm3 of flash)", 1),
    ("templates", "mold: cut templates", r"cut templates: (\d+ outlines on a [\d x]+ mm plate)", 1),
]
CAV = re.compile(r"at 0\.7R: " + NUM + r" m/s, q " + NUM + r" kPa, cavitation number " + NUM + r"; blade loading " + NUM + r" kPa, mean CL " + NUM)


def read(path):
    text = open(path, encoding="utf-8", errors="replace").read()
    out = {}
    for key, _, rx, g in PATTERNS:
        m = re.search(rx, text, re.M)
        if m:
            out[key] = m.group(g).strip()
    out["cav"] = [dict(zip(("W", "q", "sigma", "load", "cl"), m.groups())) for m in CAV.finditer(text)]
    flags = []
    for line in text.splitlines():
        s = line.strip()
        if re.search(r"\b(WARNING|NOTE|ERROR|FAILED|THIN|cannot|could not|overlap badly|under 3 mm|not clear)\b", s):
            flags.append(re.sub(r"^\[\w+\]\s*", "", s)[:220])
    out["flags"] = list(dict.fromkeys(flags))
    return out


def verdicts(r):
    v = []
    f = lambda k: float(r[k]) if k in r else None
    if f("st_pct") is not None:
        p = f("st_pct")
        v.append("static at %.0f %% of no-load: %s" % (p, "over-pitched or too large (under 35 %)" if p < 35 else
                 "under-pitched or too small (above no-load)" if p > 100 else "inside the workable band"))
    if f("tip") is not None and f("tip") > 25:
        v.append("tip speed %.1f m/s is over 25: expect ventilation and noise near the surface" % f("tip"))
    if f("ft_pct") is not None and f("ft_pct") >= 95:
        v.append("full throttle at the cruise speed is hard against no-load (%.0f %%): raise P/D or cells" % f("ft_pct"))
    if f("fill") is not None and not 95 <= f("fill") <= 108:
        v.append("the layup fills %.0f %% of the cavity: %s" % (f("fill"), "too many plies" if f("fill") > 108 else "too few plies"))
    labels = ["static", "full throttle", "cruise"]
    for i, c in enumerate(r["cav"]):
        v.append("%s at 0.7R: cavitation number %s, blade loading %s kPa, mean CL %s" % (labels[i] if i < 3 else "point %d" % (i + 1), c["sigma"], c["load"], c["cl"]))
    return v


def show(r, name):
    print("== %s" % name)
    for key, label, _, _ in PATTERNS:
        if key in r:
            print("  %-34s %s" % (label, r[key]))
    for line in verdicts(r):
        print("  - " + line)
    if r["flags"]:
        print("  warnings and notes in the log:")
        for line in r["flags"]:
            print("    " + line)


def compare(a, b, na, nb):
    print("%-34s %18s %18s" % ("", na[:18], nb[:18]))
    for key, label, _, _ in PATTERNS:
        if key in a or key in b:
            print("%-34s %18s %18s" % (label, a.get(key, "-")[:18], b.get(key, "-")[:18]))
    print()
    for r, n in ((a, na), (b, nb)):
        for line in verdicts(r):
            print("%s: %s" % (n, line))


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    logs = [read(p) for p in sys.argv[1:3]]
    if len(logs) == 1:
        show(logs[0], sys.argv[1])
    else:
        compare(logs[0], logs[1], os.path.basename(sys.argv[1]), os.path.basename(sys.argv[2]))
