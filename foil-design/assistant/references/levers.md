# Levers: what each change does

The sensitivity table below was produced with the same maths as the Foil performance feature (lifting line, profile drag build-up, trim). Baseline: 1200 cm^2 wing, AR 7, superellipse n 2.5, 2 % camber at 40 %, 12/10 % thick, incidence 0; 200 cm^2 stab, AR 5.5, symmetric, incidence -1.5 deg, arm 650 mm; 100 kg in seawater, other drag 0, fully turbulent. Columns per speed: body angle (deg), stab CL, L/D, power W, trim CG mm (aft +), static margin % MAC.

| Change | 12 km/h | 20 km/h | 30 km/h | Stance shift 12->30 |
|---|---|---|---|---|
| Baseline | 14.5 / +0.46 / 12.8 / 255 / +40 / 10 | 4.2 / 0.00 / 18.7 / 292 / +15 / 29 | 1.0 / -0.14 / 13.8 / 591 / -35 / 65 | 75 mm |
| Stab incidence -3 (decalage 3) | 14.8 / +0.35 / 12.7 / 258 / +32 / 16 | 4.5 / -0.10 / 18.0 / 303 / -8 / 45 | 1.2 / -0.25 / 13.1 / 623 / -86 / 102 | 118 mm |
| Stab incidence -1 (decalage 1) | 14.5 / +0.50 / 12.9 / 254 / +43 / 8 | 4.2 / +0.04 / 18.9 / 289 / +22 / 23 | 0.9 / -0.10 / 14.0 / 583 / -18 / 53 | 61 mm |
| Stab incidence 0 (decalage 0) | 14.3 / +0.57 / 13.0 / 252 / +49 / 5 | 4.0 / +0.11 / 19.2 / 284 / +38 / 13 | 0.8 / -0.03 / 14.4 / 569 / +16 / 28 | 33 mm |
| Stab incidence +0.5 | 14.3 / +0.60 / 13.0 / 251 / +51 / 3 | 3.9 / +0.15 / 19.4 / 282 / +45 / 7 | 0.7 / 0.00 / 14.5 / 564 / +33 / 16 | 18 mm |
| Wing camber 0 % | 16.3 / +0.61 / 13.0 / 251 / +47 / 6 | 6.0 / +0.15 / 19.4 / 281 / +32 / 16 | 2.8 / +0.01 / 14.5 / 563 / +4 / 37 | 43 mm |
| Wing camber 3 % | 13.7 / +0.39 / 12.7 / 257 / +37 / 13 | 3.3 / -0.07 / 18.2 / 299 / +6 / 35 | 0.1 / -0.21 / 13.4 / 612 / -54 / 79 | 91 mm |
| Wing camber 4 % | 12.8 / +0.31 / 12.6 / 260 / +34 / 15 | 2.5 / -0.14 / 17.7 / 308 / -2 / 41 | -0.8 / -0.29 / 12.8 / 638 / -74 / 93 | 108 mm |
| Wing incidence +2 | 12.8 / +0.32 / 12.6 / 259 / +30 / 18 | 2.5 / -0.14 / 17.7 / 307 / -15 / 51 | -0.7 / -0.28 / 12.9 / 636 / -103 / 115 | 133 mm |
| Stab area 300 cm^2 | 14.1 / +0.44 / 13.2 / 249 / +56 / 17 | 4.2 / 0.00 / 18.0 / 302 / +15 / 46 | 1.1 / -0.13 / 12.9 / 636 / -64 / 104 | 120 mm |
| Arm 800 mm | 14.5 / +0.46 / 12.8 / 255 / +48 / 14 | 4.2 / 0.00 / 18.7 / 292 / +15 / 38 | 1.0 / -0.14 / 13.8 / 591 / -50 / 85 | 98 mm |
| Stab camber -2 % | 14.8 / +0.31 / 12.6 / 260 / +29 / 19 | 4.5 / -0.14 / 17.7 / 308 / -16 / 52 | 1.3 / -0.29 / 12.8 / 638 / -105 / 116 | 134 mm |
| Wing AR 10 (same area) | 13.3 / +0.53 / 16.5 / 198 / +45 / 13 | 3.8 / +0.03 / 21.2 / 257 / +19 / 36 | 0.8 / -0.13 / 14.3 / 571 / -33 / 81 | 78 mm |
| Wing area 1500 cm^2 | 11.4 / +0.32 / 14.8 / 221 / +32 / 8 | 3.1 / -0.05 / 18.4 / 296 / +9 / 23 | 0.4 / -0.16 / 12.3 / 663 / -34 / 51 | 66 mm |

## What the table shows

**Decalage is the stance-shift and speed-stability knob.** More decalage (stab incidence more negative, stab camber negative, or wing incidence up) makes the stab push down harder at speed, so the trim CG moves forward as speed rises: a large stance shift, a foil that strongly resists speeding up, higher static margin, and a little more drag at speed. Less decalage gives a neutral foil that holds any speed with little stance change, but static margin at low speed falls towards zero. There is a decalage at which trim CG barely moves with speed; below it low-speed stability suffers. Most riders sit between.

**Wing incidence does not change stall speed.** It changes board attitude (alpha) almost one-for-one and acts like extra decalage. Use it to level the board at cruise, not to fix take-off.

**Wing camber** lowers the board attitude needed and raises CL max in reality (the tool only reflects CL max through the input), but adds nose-down moment: bigger stance shift and worse high-speed drag. Pair a camber increase with a slightly smaller decalage to keep the stance shift in check.

**Stab area and arm** both raise tail volume and static margin and increase stance shift. Stab area also adds drag at speed (about -1 L/D at 30 km/h for +50 % stab area). This table predates the Foil fuselage feature: with a real fuselage present, a longer arm now also lengthens the fuselage body and its wetted area, adding a little more drag than this table shows - not re-measured with the fuselage in place; run pitch_sweep.py for a specific design.

**Aspect ratio at fixed area** is the big efficiency lever at low and medium speed (+3.7 L/D at 12 km/h, +2.5 at 20 km/h for AR 7 to 10) and nearly neutral at high speed. Costs: a longer, more flexible wing (higher root moment - check the caps), slower roll and less turning.

**Area** trades low-speed performance for high-speed performance. Stall speed scales with 1/sqrt(area).

**Powered designs** add one more pitch lever, the thrust line (Pod height), and change which static margin matters - see propulsion.md "Levers for powered pitch stability". For any stability question on a specific design, run `scripts/pitch_sweep.py` on the console log instead of reading this generic table: it rebuilds that design's trim from the log, checks itself against the CSV, and sweeps stab area, arm and decalage (and, powered, the thrust line height).

## Symptom -> lever quick map

| Symptom | First levers | Watch out for |
|---|---|---|
| Stalls at or near take-off speed | +area; +camber with a higher Section CL max; check CL max input is realistic | More area and camber cost top-end drag |
| Take-off fine but poor glide / pumping | +AR at the same area; slightly less area if induced drag is not dominant | Root moment and cap thickness rise with span |
| Poor top speed / high power at speed | -area, -thickness, -camber, smaller stab | Take-off speed rises |
| Best L/D speed far below cruise | Wing too big or too cambered for that speed: reduce area or camber | |
| Best L/D speed far above cruise | Wing too small for that speed: increase area or AR | |
| Large stance shift (must move feet a lot) | Reduce decalage (stab incidence less negative, or wing incidence down); reduce wing camber | Low-speed static margin falls |
| Twitchy / pitchy at low speed (static margin low at take-off) | More decalage, larger stab or longer arm | Larger stance shift, more drag |
| Board rides nose-up or nose-down at cruise | Wing incidence (and stab incidence by the same amount to keep decalage) | |
| Stab CL near +/-0.8 | Larger stab or adjust stab incidence toward the value that unloads it at the problem speed | |
| Heavy stab download (inverted cambered stab: Clark Y flat side up, *Invert section* on) costing glide and stance shift | Less negative stab incidence (a wedge under the stab's trailing edge on a flat-mount build) | An inverted cambered stab is weak lifting UP, against its camber: an inverted Clark Y stalls upward at CL about 0.35-0.4 (NeuralFoil, Re 0.2-0.3 M, Ncrit 9; 0.6 at Ncrit 1), against +1.3 the right way up. Once the stab starts lifting near take-off, check its CL at the wing's stall against that, not against its CL max; stop at about 60 %. A thinner inverted section pushes down less but stalls upward sooner. A symmetric stab has no weak side and goes further (DIY-3: 3° wedge +17 %, symmetric stab +29 %, designs/jason/diy3-tuned.md) |
| Cavitation warning at max speed | Thinner, less cambered sections; more depth | Low-speed lift |
| Caps too thick or impossible | Thicker root section, wider caps, lower AR or span | Drag, weight |
| Pitch-unstable / twitchy under power (powered static margin low) | Lower Pod height (Foil mast); longer Arm; use the motor only from a higher speed; then stab area. Size with scripts/pitch_sweep.py | Pod height also sets eFoil vs foil assist; stab area adds drag and a big stance shift |
| Stands much further back under power than gliding | Thrust line far above the drag centroid - lower Pod height | See propulsion.md |
| High Wh/km / short range | Ride nearer the best-Wh/km speed; less drag (area, mast depth, pod); higher Prop P/D if throttle is low at cruise | Top speed and static thrust trade against P/D |
| Top speed too low | Higher Prop P/D, more cells, lower Gear ratio (pitch-limited), or more Current lim A (current-limited - check motor_A at the top of the sweep) | Static thrust falls with higher P/D |


## Structure and joint levers

| Want | Lever | Cost |
|---|---|---|
| Stiffer mast | *Thickness* and *Top thick* in Foil mast (stiffness goes with thickness squared); start *Thicken from %* near the waterline | drag only where it is wet |
| Lower mast cap stress at the worst point | lower *Thicken from %*, or more *Thickness* | weight |
| Mast junction shear under the allowable | more *Tab plies*, longer *Cap run-out*, bigger *Root fillet* | layup time |
| Stiffer wing | *Root thick %*, then *Cap width %* and *Cap thick %* | drag, UD to cut |
| Tip too thin to lay up | *Min tip* on, or *End ratio* up | a little tip drag |
| Root too wide for the mount, or a bump at the bolts | *Chord distribution* Double taper with *Break %* 20-30, *Mid ratio* about 0.6 and *Blend %* about 10, and *Root thick %* below *Tip thick %* |
| Tips up or down for tracking or a smaller projected span | *Winglet* Up or Down with *Winglet area*, *Winglet angle*, *Bend radius*; performance credits 0.9 × the height as span | the mold handles it up to 75° but the whole block grows by the winglet height; a gentle upturn is cheaper with *Tip drop* negative |
| Wants the production-style gull front view: level centre, steepest mid-span, level again before rising tips | *Drop shape* S-curve, *Drop from %* 3-10, *Drop to %* 85-95, *Tip drop* for the depth (Silk V2: 3 / 91 / 45 mm); a small *Winglet* Up for the tip rise | the sweep ignores anhedral; the mold block grows with the drop |
| Foil too unstable in roll, hard to pump without precise input, or a smaller size feels twitchy | more anhedral: *Tip drop* or *Root dihedral* negative. Anhedral adds roll stability and self-centres the rider; it does not make a wing turn better. Use a little less as span grows and a little more on the smaller sizes of a family; it is the one change a builder makes when scaling a design (Jonah Lepak) | the wing notice's roll number (build 230) shows the change as degrees of equivalent anhedral, e.g. a 35 mm S-curve on 900 mm is about 5°, 60 mm about 9°; upturned tips take back about half a degree; the feel on the water stays qualitative |
| Wants a looser, quicker-turning foil, or a straighter, more stable one | move the centre of lift relative to the mast: *Sweep tip* up (lift aft, closer to the mast: looser), or the wing forward on the fuselage (straighter, more stable). Sweep's own effect on performance is small; lifted tips pierce and release better in turns | trim CG moves with it |
| Stab too powerful or too weak for a new section | a section with less pitching moment (camber further forward, or reflex) needs less stab; trim the stab's *Area* a few % at a time, as builders trim a few mm off the tips | |
| Tips stall first (max_local_cl_pct high while the root has margin), or wants a flatter, later-stalling tip | *Tip profile* with a lower *Tip camber %* than the root (1-2 against 3-4) and *Blend from %* 40-60; the sweep then checks each station against its own CL max and the trim moves with the aerodynamic twist | a little less lift at the tips at a given angle; the mold follows the blend |
| Wants a softer stall for take-off or turns, like the production wings | *LE tubercles* on, *Tubercle amp* about 4 % of the local chord, *Tubercle pitch* 50-70 mm on a wing, from 15 to 85 % of the half span | geometry only: the sweep will not show the benefit, and regeneration slows; say so |
| Part weight far above a production wing of the same size | *Core* = Foam core, *Core kg/m^3* 80-100 (Corecell M80 85); production wings are 0.6-1.1 kg per 1000 cm² | less stiffness: the caps and skins must carry the bending, so re-check Cap stress and Deflection |
| Square tip on a linear or double taper | *Tip cap* = Rounded + plan, *Cap length* about 10 % of the half span | a little less area at the tip, which the solver puts back in the root chord | the wide root adds a little area near the fuselage; keep the thickness in mm at the bolts above the countersink |
| Fuselage seat wider | *Height* further from the centreline (more negative with the pad on top) | less tube under the flat |
| Stab bolts off the root | *Bolt 1 at* nearer the pivot, shorter *Bolt pitch* | |
