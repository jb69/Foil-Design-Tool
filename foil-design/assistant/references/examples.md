# Worked diagnosis sessions

Real iterations with the tool, condensed. Copy the approach and the answer style, not the numbers.

## 1. Foil assist that noses in under power

**Paste:** prone wave foil assist, 95 kg. Wing 1000 cm², AR 7.2. Stab linked at 15 % (150 cm²), Arm 620, Link decalage 2. Pod at 600 mm on a 750 mm mast. Take-off typed as 13 km/h.

**Read first:** the take-off row shows `max_local_cl_pct` 113: stall is 13.8, so the rider can't fly at 13. That's an input problem, not a design one: set *Take-off km/h* to 15 (86 % of CL max).

**Diagnosis:** `static_margin_powered_pct` is −16 % at 15 km/h and only turns positive at 19. The pod's thrust line is 610 mm above the wing and about 490 mm above the drag centroid: a 36 Nm nose-down couple. Gliding margin is 30 %, so the foil itself is fine; it's the motor.

**pitch_sweep.py:** with the pod where it is, the smallest fix is a 290 cm² stab on a 920 mm arm, which is absurd for a wave foil. With `--pod-dz -150`, a 180 cm² stab (area ratio 0.18) at 660 mm with 3° decalage reaches +7 %.

**Answer given:**
1. *Pod height* 600 → 550 (or lower if the pod still clears the water at ride depth). Biggest single effect.
2. *Link area ratio* 0.15 → 0.18, *Arm* 620 → 660.
3. *Link decalage* 2 → 3. Cost: the stance shift grows from about 90 to 130 mm; try 2.5 if that's too much on the water.

**Next run:** powered margin −0.7 % at 15, +5.5 % at 16. Neutral at take-off is acceptable for a foil assist, whose motor phase lasts seconds. Lesson: foil assist trades powered stability for a dry pod; pod height is the lever.

## 2. Mast stress peaks in the wrong place

**Paste:** `STIFFNESS: mast side 1 g at the fuselage, plate fixed: F=932 N, deflection at the fuselage 53 mm, cap stress at the plate 198 MPa, worst 243 MPa at 494 mm`. Mast 750 mm, *Thickness* 16, *Top thick* 22, *Thicken from %* 60. Performance says 340 of 750 mm is submerged.

**Diagnosis:** everything is inside the UD allowable with about a factor of two, but the worst stress is not at the plate. The moment grows all the way up, while the section only starts thickening at 60 % height, so the stress peaks just below that. The waterline at ride depth is at 45 % of the height, so everything above it is dry.

**Answer given:**
1. *Thicken from %* 60 → 45 to 50: the build-up starts near the waterline, which flattens the peak and adds stiffness with no wetted thickness.
2. Or *Thickness* 16 → 18, if the mast should also be stiffer lower down. Stiffness goes with thickness squared; the cost is drag along the whole wetted length.
3. Leave *Cap thick %*: thicker caps sit nearer the neutral axis and barely help.

**Also:** say that 1 g of the whole mass sideways is severe; a hard carve is nearer 0.3-0.5 g, so the real flex is a third to a half of the number.

## 3. The stab-to-fuselage seat disappears

**Paste:** `FUSELAGE_SEAT: | no stab seat | Conflicts: the stab's pad plane is above the fuselage centreline, so there is nothing to seat on`.

**Diagnosis:** the stab was relinked to a bigger area; its root got thicker and the pad face moved up. *Height* is the seat's offset from the fuselage centreline, so the fix is one number.

**Answer given:** Foil stabiliser *Height* −4 (pad on top). With the pad underneath the stab it would be +4. Resizing the stab will never move the seat again.

## 4. "The stall speed isn't realistic"

**Context:** a builder says a foil the model stalls at 13.8 km/h would really need about 18.

**Answer given:** agree it's optimistic, thank them for the point, and explain why: clean 2D section, steady flight, no junction or finish losses, no rider margin. Then turn it into a calibration: 18 km/h means an effective CL max of 1.27 × (13.8 / 18)² ≈ 0.75. Ask for one real foil and its no-pump take-off speed, set *Section CL max* (or the polar CL max) to reproduce it, and use that value for every new design. Until then, quote stall as optimistic and compare variants.

## 5. A mold that won't cut

**Paste:** `Could not cut the cavity into the LOWER half. Tip section is 0.7 mm thick at the cap start`.

**Diagnosis:** a boolean through a sliver under a millimetre fails, and couldn't be laid up anyway.

**Answer given:** Foil wing *Min tip* on at 1 mm. If it still fails, *Split stations* up (30 to 50) and *Tip bias* 3 to 4. If both halves fail on a stab that's been moved, check the Foil mold and Foil stabiliser are the same build.

## 6. A production wing modelled from published numbers

**Paste:** AFS Silk V2 950 (superellipse n 1.4, end ratio 0.26, sweep 67 mm, S-curve drop 30 mm, 15 cm² winglets up at 60°, LE tubercles 5 mm at a 59 mm pitch from 15 to 82 %, NACA 4-digit 3 % root with a 1.5 % tip section from 40 %) and a U Carve 130 stab (double taper, Clark Y inverted, root tail 25 mm) at Arm 560 and Incidence −1.5. 85 kg all-up, no mast or fuselage in the Part Studio, take-off 18, cruise 22, max 36. Full console in the release's `examples/silk-v2-u-carve-console.txt`.

**Read first:** `WING_INPUTS` shows `otherDrag_cm2=0` and there is no `MAST_INPUTS` or `FUSELAGE_INPUTS`, so every drag and L/D figure is wing plus stab only; add 10 to 15 N before comparing with a ridden foil. Stall 15 km/h against a take-off of 18: `max_local_cl_pct` 72 at 18, comfortable.

**Diagnosis:** L/D peaks at 20.0 at 19 km/h and is 19.2 at the 22 km/h cruise, but falls to 11.1 at 36. `CL_stab` runs from −0.11 at take-off to −0.43 at 36 km/h and `static_margin_pct` from 41 to 163 %: the stab is carrying a large and growing download, which is stab-induced drag and a trim CG that moves 137 mm forward between take-off and top speed. The stab at −1.5° is doing more than it needs to; the AFS pairing is designed for a smaller angle than that.

**Recommendation:** *Incidence* (Foil stabiliser) from −1.5 to −0.5. Expect the stab CL at 36 km/h to roughly halve, static margin to fall toward 60 to 80 % at cruise, the stance shift to shrink by a third, and drag at 30 km/h and above to fall a few newtons, with the low-speed margin still positive because the stab's own camber (Clark Y, inverted) keeps it downloaded. Second lever if the margin is still excessive: *Area cm²* (Foil stabiliser) 130 to 115. Leave the wing alone; it is a published shape and the check is whether the tool reproduces it, not whether it can be improved.

**Next run:** compare `static_margin_pct` at 18 and 36, `trim_cg_mm` spread, and `L_D` at 30. If the `MAST_PLATE` or mold lines are in the paste, ignore them for this question: they are the build side.
