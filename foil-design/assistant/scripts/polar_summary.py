#!/usr/bin/env python3
"""Fit a Foil wing / Foil stabiliser polar summary from XFoil or XFLR5 output.

Give it two polar files - one near your take-off Reynolds number, one near
cruise/max - and it prints the 13 dialog values ready to type into the
Section polar group (Polar summary mode).

Usage:
    python3 polar_summary.py polar_re350k.txt polar_re900k.txt

Each file should be a standard XFoil polar dump (the "alpha CL CD CDp CM ..."
table XFoil's PACC writes, or XFLR5's exported .txt in the same layout) with a
"Re = ..." line somewhere in the header - used to label the point, not relied
on for the actual Re value (pass --re1/--re2 to override or supply it if the
header format doesn't match).

What it computes, per file:
    CLmax   = the highest CL reached before CL turns over (or before alpha
              stall break - the point right before CL stops increasing with
              alpha, which is a safer proxy than the raw column max when the
              polar continues past stall in a non-converged way)
    Cdmin, CL@Cdmin, k = a parabola Cd = Cdmin + k*(CL - CL@Cdmin)^2 least-
              squares fit to the polar's points below stall

And from whichever file's alpha=CL=0 region is best resolved (or both,
averaged): zero-lift angle and lift-curve slope from a linear fit to the
attached-flow part of the CL-alpha curve, and Cm at the same points.
"""
import re
import sys
import math


def parse_polar(path):
    rows = []
    re_val = None
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            m = re.search(r"Re\s*=\s*([\d.]+)\s*e\s*([+-]?\d+)", line, re.I)
            if m:
                re_val = float(m.group(1)) * 10 ** int(m.group(2))
            parts = line.split()
            if len(parts) >= 5:
                try:
                    alpha, cl, cd = float(parts[0]), float(parts[1]), float(parts[2])
                    cm = float(parts[4]) if len(parts) > 4 else None
                    rows.append((alpha, cl, cd, cm))
                except ValueError:
                    continue
    if not rows:
        sys.exit(f"{path}: no data rows found - expected an XFoil/XFLR5 polar table")
    return rows, re_val


def clmax_and_bucket(rows):
    rows = sorted(rows, key=lambda r: r[0])
    # Real XFoil polars are not simply "increasing then decreasing": at large negative alpha
    # there is often negative-stall wiggle, and around moderate Re a laminar separation bubble
    # can cause a local dip and recovery well before the real stall - a "first CL decrease"
    # rule latches onto either of these and badly underestimates CLmax (or even makes it drop
    # with Re, backwards). Two robust criteria instead:
    #   CLmax = the global max CL among rows with alpha >= -1 (real CLmax is always on the
    #   positive-alpha branch for a cambered section; excludes the negative-stall branch).
    #   Drag-bucket fit uses only rows within 2.5x the polar's own minimum CD - robustly
    #   selects the low-drag bucket itself regardless of where stall or wiggles occur, rather
    #   than trying to find the "attached" range by alpha at all.
    clmax = max(r[1] for r in rows if r[0] >= -1)
    cd_min_all = min(r[2] for r in rows)
    attached = [r for r in rows if r[2] <= 2.5 * cd_min_all]
    if len(attached) < 5:
        attached = rows

    # parabola fit Cd = Cdmin + k (CL - CLcd)^2, i.e. Cd = a + b*CL + c*CL^2 with
    # Cdmin = a - b^2/(4c), CLcd = -b/(2c), k = c
    n = len(attached)
    sx = sum(r[1] for r in attached)
    sx2 = sum(r[1] ** 2 for r in attached)
    sx3 = sum(r[1] ** 3 for r in attached)
    sx4 = sum(r[1] ** 4 for r in attached)
    sy = sum(r[2] for r in attached)
    sxy = sum(r[1] * r[2] for r in attached)
    sx2y = sum(r[1] ** 2 * r[2] for r in attached)
    A = [[n, sx, sx2], [sx, sx2, sx3], [sx2, sx3, sx4]]
    b = [sy, sxy, sx2y]
    for k in range(3):
        p = max(range(k, 3), key=lambda i: abs(A[i][k]))
        A[k], A[p] = A[p], A[k]
        b[k], b[p] = b[p], b[k]
        for i in range(k + 1, 3):
            f = A[i][k] / A[k][k]
            for j in range(k, 3):
                A[i][j] -= f * A[k][j]
            b[i] -= f * b[k]
    x = [0, 0, 0]
    for i in range(2, -1, -1):
        x[i] = (b[i] - sum(A[i][j] * x[j] for j in range(i + 1, 3))) / A[i][i]
    a_, b_, c_ = x
    if c_ <= 0:
        # fallback: not enough curvature to fit a bucket - use the min point directly
        best = min(attached, key=lambda r: r[2])
        return clmax, best[2], best[1], 0.012
    clcd = -b_ / (2 * c_)
    cdmin = a_ - b_ ** 2 / (4 * c_)
    return clmax, max(cdmin, 0.001), clcd, c_


def lift_slope(rows):
    # linear fit over the attached, roughly-linear part: |alpha| within a data-driven window
    rows = sorted(rows, key=lambda r: r[0])
    mid = [r for r in rows if -6 <= r[0] <= 6] or rows[: max(3, len(rows) // 2)]
    n = len(mid)
    sx = sum(r[0] for r in mid)
    sy = sum(r[1] for r in mid)
    sxy = sum(r[0] * r[1] for r in mid)
    sx2 = sum(r[0] ** 2 for r in mid)
    denom = n * sx2 - sx ** 2
    if denom == 0:
        return 2 * math.pi, 0.0, 0.0
    slope_per_deg = (n * sxy - sx * sy) / denom
    intercept = (sy - slope_per_deg * sx) / n
    alpha0_deg = -intercept / slope_per_deg
    cla_per_rad = slope_per_deg * 180 / math.pi
    cm_vals = [r[3] for r in mid if r[3] is not None]
    cm0 = sum(cm_vals) / len(cm_vals) if cm_vals else 0.0
    return cla_per_rad, alpha0_deg, cm0


def main(argv):
    if len(argv) < 2:
        sys.exit(__doc__)
    f1, f2 = argv[0], argv[1]
    re1_override = re2_override = None
    for i, a in enumerate(argv):
        if a == "--re1":
            re1_override = float(argv[i + 1]) * 1e6
        if a == "--re2":
            re2_override = float(argv[i + 1]) * 1e6

    rows1, re1 = parse_polar(f1)
    rows2, re2 = parse_polar(f2)
    re1 = re1_override or re1
    re2 = re2_override or re2
    if re1 is None or re2 is None:
        sys.exit("Could not find 'Re = ...' in one of the files - pass --re1 <millions> --re2 <millions>")
    if re1 > re2:
        f1, f2, rows1, rows2, re1, re2 = f2, f1, rows2, rows1, re2, re1

    clmax1, cdmin1, clcd1, k1 = clmax_and_bucket(rows1)
    clmax2, cdmin2, clcd2, k2 = clmax_and_bucket(rows2)
    cla1, a01, cm01 = lift_slope(rows1)
    cla2, a02, cm02 = lift_slope(rows2)
    # prefer the lower-Re file for alpha0/Cm/slope only if it has more attached-flow points;
    # otherwise average - either way this matters far less than the Re-dependent CLmax/Cd terms
    cla = (cla1 + cla2) / 2
    a0 = (a01 + a02) / 2
    cm0 = (cm01 + cm02) / 2

    print(f"Re 1 (M)      {re1/1e6:.3f}   [{f1}]")
    print(f"Re 2 (M)      {re2/1e6:.3f}   [{f2}]")
    print(f"Zero-lift     {a0:.2f} deg")
    print(f"Lift slope    {cla:.3f} /rad")
    print(f"Cm c/4        {cm0:.4f}")
    print(f"CLmax @Re1    {clmax1:.3f}")
    print(f"Cdmin @Re1    {cdmin1:.4f}")
    print(f"CL@Cdmin 1    {clcd1:.3f}")
    print(f"k @Re1        {k1:.4f}")
    print(f"CLmax @Re2    {clmax2:.3f}")
    print(f"Cdmin @Re2    {cdmin2:.4f}")
    print(f"CL@Cdmin 2    {clcd2:.3f}")
    print(f"k @Re2        {k2:.4f}")


if __name__ == "__main__":
    main(sys.argv[1:])
