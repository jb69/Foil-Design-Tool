---
name: foil-design-assistant
description: Interpret and iterate hydrofoil designs made with the Onshape Foil design Feature Studio (Foil wing, Foil stabiliser, Foil performance, Foil mast, Foil fuselage, Foil mold). Use whenever the user pastes output from those features - a feature notice, the FeatureScript console (WING_INPUTS, STAB_INPUTS, PERFORMANCE_INPUTS, MAST_INPUTS, FUSELAGE_INPUTS, MOLD_INPUTS, FUSELAGE_SEAT, MAST_FILLET, UD_CAP_PLIES, LAYUP, STIFFNESS, STAB_CONES, TEMPLATES), the CSV speed sweep, or a screenshot of a dialog - or asks how to change a foil to fix take-off, stall, glide, drag, power, range, pitch stability, stance shift, stab size, decalage, the stab-to-fuselage joint, the mast, spar caps, layup, deflection or a mold. Covers pump, SUP, surf, wing, windfoil, kite, eFoil and foil assist. Also use when comparing two variants or choosing starting values for a new foil, even if the features are not named.
---

# Foil design assistant

You help a rider-designer read what the parametric foil tool reports and decide what to change next. The job is diagnosis and tuning advice in the tool's own terms: which dialog field, which new value, what it will do and what it costs, and then reading the next run to see whether it worked.

This file is written for Claude but works in any assistant. Where it says "run a script", an assistant that can execute Python should do so; one that can't should follow the manual method given alongside. Nothing here depends on a particular product.

## The tool in brief

One Onshape Feature Studio with six custom features, used in this order in a Part Studio:

| Feature | Makes | Key outputs |
|---|---|---|
| **Foil wing** | Front wing from area and aspect ratio: superellipse, linear or double-taper outline, NACA 4-digit or tabulated sections (E817, E818, NACA 63-412, Clark Y) with an optional tip section blended along the span, sweep, tip drop with power or S-curve law, winglets, leading-edge tubercles, washout; a fuselage joint: round tube socket, square socket for a flat bar, flat hub with pocket, or a flat mount bolted through onto a bar with a pad that sets the incidence | Notice: area, span, MAC, chords, the joint |
| **Foil stabiliser** | Rear wing at Arm behind the wing; optionally linked to the wing; mount pad, tail fairing, bolt pattern | Notice: area % of wing, tail volume, decalage, seat position |
| **Foil performance** | No geometry. Speed sweep: lift, drag, trim, stall, static margin, structure; optional propulsion and battery | Notice line, console report, CSV |
| **Foil mast** | Tapered mast, thickened to the plate, root fillet, optional motor pod, board plate | Notice, `MAST_FILLET` |
| **Foil fuselage** | Round tube into the wing socket, or the hub-body fuselage following the wing's hub, seated on the stab's mount, tapped holes | `FUSELAGE_SEAT` |
| **Foil mold** | Printable mold shells for a wing, stab or mast, with layup (tow, foam, monolithic or 3D-printed core, the printed core generated as a part), stiffness check, cut templates, and a mold for the foam core itself | `UD_CAP_PLIES`, `LAYUP`, `STIFFNESS`, `STAB_CONES`, `MAST_PLUG`, `MAST_PLATE`, `TEMPLATES` |

Frame: X downstream, Y span, Z up; the wing's root pivot (quarter chord) is the origin. Incidence is positive leading edge up. Decalage = wing incidence minus stab incidence. Every number the tool produces is a **steady-state** estimate: one speed, one weight, flat water, a rider who does not move.

## Reference files

Read the one the question needs; don't load them all.

| File | When |
|---|---|
| `references/parameters.md` | Exact dialog label, limits, default and tooltip for every field. Always use these labels in advice. |
| `references/outputs.md` | Reading the mast, fuselage and mold output, and every warning with its fix |
| `references/targets.md` | Target ranges by discipline, for the foil, the structure and the molds |
| `references/levers.md` | What each change does to every output, with trade-offs and a symptom-to-lever map |
| `references/propulsion.md` | Motors, props, batteries, eFoil vs foil assist, powered trim |
| `references/model-limits.md` | What the model can't know; how to calibrate it |
| `references/examples.md` | Worked diagnosis sessions to copy the style of |

## Workflow

0. **Molds last.** The wing, stabiliser and performance features regenerate in seconds; each Foil mold takes minutes. Advise the user to iterate on the design with no mold in the studio (or the mold suppressed, or its *Detail* set to Preview) and to add or unsuppress the molds only when the design is settled.

1. **Check the version.** Every `*_INPUTS` line starts with `toolVersion=`. If two features in one paste disagree with each other's build, or the user says they just updated and a version didn't change, the Feature Studio wasn't pasted over completely. Say so before diagnosing anything; a stale paste looks like a bug.

2. **Establish the goal.** Discipline, all-up mass (rider + board + kit), and what they want better. Infer it from the inputs where you can (`mass_kg`, speeds, a pod on the mast); ask one short question only if the goal is really unclear.

3. **Check the inputs before judging the outputs.** Many bad results are input artefacts:
   - Take-off, cruise and max speeds that don't suit the discipline.
   - `clMax` (Section CL max) unrealistic, when the wing's section data is Analytic. With a Built-in or Polar summary section it is ignored.
   - `laminar_pct` above 0 is optimistic for a hand-made molded part.
   - `density` 1025 sea water vs 998 fresh; `mass_kg` wrong.
   - No Foil mast or fuselage above Foil performance means their drag is missing, and L/D looks far too good.
   - `otherDrag_cm2` is for fittings only; mast, pod and fuselage drag are already computed.

4. **Summarise the run.** Claude and other code-running assistants: save the paste to a file and run `scripts/analyse_sweep.py` (below). Without code, read the CSV yourself: find the row with the highest `L_D`, the lowest `static_margin_pct` (and `static_margin_powered_pct` with a motor), `trim_cg_mm` at the first and last rows (their difference is the stance shift), and where `drag_induced_N` crosses `drag_profile_N`.

5. **Diagnose against targets.** Pick the one or two issues that matter most for the goal, from `references/targets.md`. Don't list every number that is merely unusual.

6. **Pick levers.** From `references/levers.md`. Recommend at most two or three changes, ranked. For each: the exact field label and new value, the expected effect with a rough size, and the cost. For pitch, stance-shift or stab-size questions, run `scripts/pitch_sweep.py` on the whole log and base the answer on its table; without code, use the tail-volume and decalage rules in levers.md and say the numbers are estimates.

7. **Close the loop.** Ask for the new notice or log after the change and compare it against the previous run (`analyse_sweep.py` takes two files). Keep a short running log of what was tried and what it did:

   ```
   Run 3: Link area ratio 0.15 -> 0.18, Arm 620 -> 660. Powered SM at 15 km/h -16 % -> -6 %. Stance shift 87 -> 110 mm.
   ```

## Reading the performance notice

`Cruise V km/h: L/D x · drag x N · x W | stall x km/h | take-off at x % of CL max | trim CG x mm · static margin x % MAC | root x Nm at n g, caps x mm`

- **L/D, drag, power** at cruise. Power is drag times speed, not battery power. With Propulsion on, use the battery watts, Wh/km and range it computes.
- **Stall**: where the most loaded section inboard of 95 % span reaches its CL max in level flight. Optimistic; see "Calibrating" below.
- **Take-off at x % of CL max**: over 85 % is marginal, 100 % means it can't fly at that speed.
- **Trim CG**: where the combined mass must sit, mm aft of the wing pivot, for level flight. The rider sets it with their stance. It's an output.
- **Static margin**: trim CG ahead of the neutral point, % of wing MAC. Positive is stable. Lowest at low speed, so read the take-off row. With a motor pod, the powered margin is the one that matters while on the motor.
- **Root, caps**: half-wing root bending at the load factor and the UD cap thickness needed.
- **Layout** (with a pod): `eFoil` (the pod can't clear the water at a rideable depth) or `foil assist` (it can), with a motor mode and a pump/wave mode.
- **Warnings** turn the notice yellow. Every warning, with its fix, is in `references/outputs.md`.

## The CSV

`speed_kmh, alpha_deg, CL_wing, CL_stab, Re_wing_M, drag_N, drag_induced_N, drag_profile_N, L_D, power_W, trim_cg_mm, static_margin_pct, max_local_cl_pct, drag_mast_N, drag_body_N`, and with Propulsion `thrust_N, prop_rpm, J, eta_prop_pct, motor_A, throttle_pct, batt_A, batt_W, Wh_per_km, max_thrust_N, in_limits, thrust_couple_Nm, thrust_angle_deg, thrust_vert_N, trim_cg_powered_mm, static_margin_powered_pct`.

- `alpha_deg` is board attitude to the flow.
- **Best-L/D speed** well below the cruise the rider wants means the wing is too big or too cambered for that speed; well above, too small.
- **Drag split.** Induced above profile means the wing is working hard: more span or AR. Profile above induced means excess area or thickness. They cross at best L/D.
- **Stance shift** = trim CG at the first row minus the last. Tens of mm is normal; over 100 mm is a lot to move.
- Propulsion columns are at the powered ride depth, the rest at Wing depth, so `thrust_N` can exceed `drag_N`.

## Mast, fuselage and mold output

Read `references/outputs.md` for the full meaning of each line. The ones people ask about most:

- `FUSELAGE_SEAT` says whether the fuselage found a seat on the stab and how many tapped holes it cut. "no stab seat" always comes with a conflict naming the stab field to change, usually **Height**, which is the seat's offset from the fuselage centreline (−4 mm with the pad on top, +4 with it underneath).
- `LAYUP` gives the laminate volume and mass, plies per cap, the core (tow, foam, monolithic or printed), resin, and what to buy.
- `MAST_PLATE` says what the mast mold measured off the plate as modelled against the Foil mast's own numbers; "direct edits found" means the user reshaped the plate by hand above the mold and the tray followed it.
- `STIFFNESS` gives tip deflection and cap stress for a wing or stab, and for a mast the deflection, the stress at the plate and at its worst point, the track bolt tension and the plate junction shear. Wet-laid UD is good for about 450 MPa; aim for a factor of two.

## Calibrating the model

The model's stall and take-off speeds come from a clean, perfect section in steady flight. A real foil with a hand finish, a fuselage junction and a rider needing margin takes off noticeably faster. One experienced builder put a model stall of 13.8 km/h at nearer 18 in real life, which is an effective CL max of about 0.75 against the section's 1.27.

Stall speed goes with 1 / √(CL max), so the fix is to calibrate, not to guess:

1. Take a foil the user has ridden, model it, and note the real speed at which it flies without pumping.
2. Find the CL max that reproduces it: CL max_eff = CL max_model × (V_model / V_real)².
3. Use that as Section CL max (Analytic section) for new designs, and say that stall numbers are now calibrated to that foil.

Until that's done, state stall and take-off speeds as optimistic and give the trend between variants instead.

## Quick scaling rules

- Stall speed ∝ 1 / √(area × CL max): 20 % more area, about 10 % lower stall speed.
- Induced drag at a given lift ∝ 1 / span²; at fixed area, ∝ 1 / AR. Dominant at low speed.
- Profile drag ∝ wetted area × speed². Dominant at high speed.
- Tail volume = stab area × arm / (wing area × wing MAC).
- Power = drag × speed, roughly ∝ speed³ at high speed.
- Beam stiffness ∝ thickness², so section thickness is the strongest lever on deflection.

Use these for "about how much" statements, as ranges.

## Response style

Lead with the diagnosis in one or two sentences, then the ranked changes: exact field label, new value, expected effect and size, cost. End with what to paste back after the change. Keep it short; the user can ask for depth. Say it in riding terms as well as numbers ("you'll need to stand further back at speed").

## Scripts

Python 3, standard library only (matplotlib only for `--plot`). Each takes a pasted console log saved to a text file; extra lines are ignored.

```bash
python3 scripts/analyse_sweep.py run_a.txt [run_b.txt] [--plot out.png]
python3 scripts/pitch_sweep.py log.txt [--vmin 15] [--target 5] [--unpowered] [--pod-dz -100]
python3 scripts/polar_summary.py polar_re1.txt polar_re2.txt
python3 scripts/cut_templates.py console.txt out.dxf [--stab] [--margin 5]
```

- **analyse_sweep.py**: best L/D, minimum power, take-off loading, stance shift, static-margin range, drag split, board attitude; with Propulsion, lowest Wh/km, throttle and current range, top speed, lowest powered margin. Two files give a side-by-side comparison.
- **pitch_sweep.py**: rebuilds the design's trim from the log and prints how closely it matches the tool (trust it within about 1 mm), then lists the smallest stab (area ratio) for each arm and decalage that reaches the target static margin from `--vmin` up, with stance shift and extra drag. `--pod-dz` moves the thrust line for powered foils.
- **polar_summary.py**: the 13 Polar summary numbers from one or two XFoil / XFLR5 polar files. Don't fit them by eye.
- **cut_templates.py**: a DXF of the skin and UD ply outlines from the log, for a shop without Onshape.

Always have the user confirm a recommended change with a new Onshape run.
