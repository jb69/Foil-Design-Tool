#!/usr/bin/env python3
"""Fabric cut templates as DXF, from a pasted FeatureScript console log.

Reads the WING_INPUTS (or STAB_INPUTS) line and the UD_CAP_PLIES cut list that Foil mold prints,
rebuilds the developed planform (the outline unrolled along the tip droop) and writes one DXF with:
  - the woven skin outline (both halves when mirrored), grown by the margin
  - one tapered strip per UD ply (per cap - cut each twice), about the 30 % chord line, Cap width % of the local chord wide,
    out to the ply's length from the cut list, grown by the margin
Shapes are laid side by side along X. Units are millimetres (DXF $INSUNITS = 4).

    python3 cut_templates.py console.txt out.dxf [--stab] [--margin 5] [--gap 10] [--cap-width 40]

The same shapes are what Foil mold's "Cut templates" plate carries inside Onshape; this script is for
a shop without Onshape access, working from the log alone. It uses no libraries beyond the standard
library: the DXF is written as plain LWPOLYLINE entities.
"""
import math
import re
import sys


def num(key, text, default=None):
    m = re.search(r"[;:]\s*%s=([-\d.]+)" % re.escape(key), text)
    return float(m.group(1)) if m else default


def word(key, text, default=None):
    m = re.search(r"[;:]\s*%s=([A-Za-z_]+)" % re.escape(key), text)
    return m.group(1) if m else default


def se_scale(r, n):
    # eta at which the superellipse chord factor reaches the end ratio r
    return (1 - r ** n) ** (1.0 / n)


def chord_factor(shape, eta, r, n):
    if shape == "SUPERELLIPSE":
        s = se_scale(r, n)
        v = 1 - (s * eta) ** n
        return max(v, 0.0) ** (1.0 / n)
    return 1 - (1 - r) * eta


def planform(area_m2, ar, shape, r, n):
    # projected area = span * c0 * G, span = sqrt(AR * area)
    N = 400
    G = sum(chord_factor(shape, (i + 0.5) / N, r, n) for i in range(N)) / N
    span = math.sqrt(ar * area_m2)
    c0 = area_m2 / (span * G)
    return span, c0


def polyline(pts, layer):
    out = ["0", "LWPOLYLINE", "8", layer, "90", str(len(pts)), "70", "1"]
    for x, y in pts:
        out += ["10", "%.3f" % x, "20", "%.3f" % y]
    return out


def main(argv):
    if len(argv) < 2:
        sys.exit(__doc__)
    text = open(argv[0], encoding="utf-8").read()
    out_path = argv[1]
    use_stab = "--stab" in argv
    margin = float(argv[argv.index("--margin") + 1]) if "--margin" in argv else 5.0
    gap = float(argv[argv.index("--gap") + 1]) if "--gap" in argv else 10.0
    cap_w = float(argv[argv.index("--cap-width") + 1]) / 100 if "--cap-width" in argv else 0.40

    line = None
    for ln in text.splitlines():
        if ln.startswith("STAB_INPUTS" if use_stab else "WING_INPUTS"):
            line = ln
    if line is None:
        sys.exit("no %s line in the log" % ("STAB_INPUTS" if use_stab else "WING_INPUTS"))
    mold_w = num("capWidthPct", text)
    if mold_w:
        cap_w = mold_w / 100

    area = num("targetArea_cm2", line) / 1e4
    ar = num("aspectRatio", line)
    shape = word("chordShape", line, "SUPERELLIPSE")
    r = num("endRatio", line) if shape == "SUPERELLIPSE" else num("tipRatio", line, 0.4)
    n = num("superN", line, 2.0)
    sweep = num("sweepTip_mm", line, 0.0)
    sweep_exp = num("sweepExp", line, 2.0)
    drop = num("tipDrop_mm", line, 0.0)
    drop_exp = num("dropExp", line, 2.0)
    dihedral = math.radians(num("rootDihedral_deg", line, 0.0))
    align = num("planformAlign", line, 25.0) / 100
    pivot = {"QUARTER_CHORD": 0.25, "LEADING_EDGE": 0.0, "MID_CHORD": 0.5}.get(word("pivot", line, "QUARTER_CHORD"), 0.25)
    cap_len = num("capLength_mm", line, 0.0) if word("tipCap", line, "NONE") != "NONE" else 0.0
    cap_end = num("capEnd", line, 100.0) / 100
    mirror = word("mirror", line, "true") == "true"

    span, c0 = planform(area, ar, shape, r, n)
    span *= 1000
    c0 *= 1000
    h = span / 2
    cap_start = max(0.0, 1 - cap_len / h) if cap_len > 0 else 1.0

    # fine grid along the half span: chord, LE x, developed span
    N = 60
    eta = [i / N for i in range(N + 1)]
    chord, x_le, s_dev = [], [], [0.0]
    z_prev = 0.0
    for i, e in enumerate(eta):
        c = c0 * chord_factor(shape, e, r, n)
        if e > cap_start and cap_len > 0:
            c *= 1 - (1 - cap_end) * (e - cap_start) / max(1 - cap_start, 1e-6)
        z = -drop * e ** drop_exp + h * math.tan(dihedral) * (e - e ** drop_exp)
        if i > 0:
            s_dev.append(s_dev[-1] + math.hypot(h / N, z - z_prev))
        z_prev = z
        x_off = sweep * e ** sweep_exp + (pivot - align) * (c - c0)
        x_le.append(x_off - pivot * c)
        chord.append(c)

    def half_outline(i_end, front, back, stride):
        idx = list(range(0, i_end, stride)) + [i_end]
        sides = ([-i for i in reversed(idx[1:])] + idx) if mirror else idx
        pts = []
        for i in sides:
            a = abs(i)
            v = math.copysign(s_dev[a] + (margin if a == i_end else 0.0), i if i else 1)
            pts.append((front(a), v))
        for i in reversed(sides):
            a = abs(i)
            v = math.copysign(s_dev[a] + (margin if a == i_end else 0.0), i if i else 1)
            pts.append((back(a), v))
        return pts

    shapes = [("skin", half_outline(N, lambda a: x_le[a] - margin, lambda a: x_le[a] + chord[a] + margin, 2))]

    # UD plies from the cut list: length root-to-root -> end station
    plies = re.findall(r"^\s*(\d+),(\d+),(\d+),(\d+)\s*$", text, re.M)
    for k, length, w_root, w_end in plies:
        half_len = float(length) / (2 if mirror else 1)
        i_end = max(1, min(N, min(range(N + 1), key=lambda i: abs(s_dev[i] - half_len))))
        stride = max(1, round(i_end / 10))
        shapes.append(("UD_%02d" % int(k), half_outline(
            i_end,
            lambda a: x_le[a] + 0.3 * chord[a] - cap_w * chord[a] / 2 - margin,
            lambda a: x_le[a] + 0.3 * chord[a] + cap_w * chord[a] / 2 + margin,
            stride)))

    # layout side by side along X
    dxf = ["0", "SECTION", "2", "HEADER", "9", "$INSUNITS", "70", "4", "0", "ENDSEC", "0", "SECTION", "2", "ENTITIES"]
    u_off = 0.0
    for name, pts in shapes:
        u_min = min(p[0] for p in pts)
        u_max = max(p[0] for p in pts)
        dxf += polyline([(p[0] - u_min + u_off, p[1]) for p in pts], name)
        u_off += (u_max - u_min) + gap
    dxf += ["0", "ENDSEC", "0", "EOF"]
    open(out_path, "w").write("\n".join(dxf) + "\n")
    print("wrote %s: skin + %d UD plies, %.0f mm wide layout, margin %.0f mm" % (out_path, len(shapes) - 1, u_off - gap, margin))
    print("the UD list is per cap (build 118+): cut each UD strip twice, one for each face; the skin outline once per skin ply per face")


if __name__ == "__main__":
    main(sys.argv[1:])
