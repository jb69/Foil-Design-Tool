> Portable version: the reference files are included below this point, except `parameters.md` (every dialog field), which comes as a separate file. Where the instructions say `references/x.md`, read the section of that name below. If you cannot run Python, use the manual method given next to each script.

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

---

# references/outputs.md

## Reading the output: console lines, notices and warnings

Everything the features print, what it means, and what to change. Field names in *italics* are dialog labels (see parameters.md).

### Console lines

Turn on *Log inputs* on a feature to get its `*_INPUTS` line. Every line starts with `toolVersion=`; the Feature Studio header carries the build number.

| Line | From | What it says |
|---|---|---|
| `WING_INPUTS`, `STAB_INPUTS`, `MAST_INPUTS`, `FUSELAGE_INPUTS`, `PERFORMANCE_INPUTS`, `MOLD_INPUTS` | each feature | Every input as `key=value`. `STAB_INPUTS` has `height_mm` (typed: seat offset from the fuselage centreline) and `pivotZ_mm` (where the stab pivot ended up). |
| `===== FOIL PERFORMANCE =====` ... `===== END =====` | Foil performance | The full report, then the CSV. |
| `FUSELAGE_SEAT` | Foil fuselage | The seat on the stab and the tapped holes made, or the conflict and its fix. |
| `MAST_FILLET` | Foil mast | Seam edges found where the mast meets the plate, and the fillet attempt that worked. |
| `UD_CAP_PLIES` | Foil mold | Cut list per cap: ply, length, width at root, width at the end. |
| `UD_TABS` | Foil mold (mast) | Tab plies per face, their size, fold position and interleave. |
| `LAYUP` | Foil mold | Laminate volume and cured mass, skins, UD caps, tow, resin, what to buy. |
| `STIFFNESS` | Foil mold | Deflection and cap stress; mast: stress at the plate and at the worst point, bolt tension, junction, plate bending. |
| `STAB_CONES` | Foil mold (stab) | Bolt cones made and their positions. |
| `MAST_PLUG` | Foil mold (mast) | The pod plug's cores and depth. |
| (warning) countersink deeper than the section | Foil stabiliser, and the mold's cone note | A bolt's countersink leaves under 1 mm of laminate (an M5 is 2.9 mm deep; a bolt in a 3 mm root tail cannot take it): move it forward with *Bolt 1 at* or *Bolt pitch*, a smaller *Bolt*, or *Countersink* off. The mold then shows a flat disc instead of a cone there. |
| (warning) socket as half cores | Foil mold (wing) | *Socket plug* is off with the wing's Round or Square socket on: the halves mold the bore with a seam and meeting pins; turn Socket plug on in the Socket group. |
| `MAST_PLATE` | Foil mold (mast) | The plate measured off the part (length x width x thickness) against the Foil mast's numbers; says when direct edits were found and the tray followed them, or when it could not measure and used the nominal plate. |
| `TEMPLATES` | Foil mold | Order of shapes on the template plate and each one's cut count. |

### Foil stabiliser notice

- **Seat line.** It names what *Height* is measured from. With a Round or Square socket on the wing: "Height is measured from the square socket's axis, the fuselage centreline (Z …): seat plane 4 mm below it, pivot at Z … mm (the Height that keeps the stab here with no socket on the wing)". With the pad on top the seat must be at or below the centreline; with the pad underneath, at or above. A WARNING in this line names the sign to use. With no socket (none, Flat hub, Flat mount): "Height is the stab pivot's offset from the wing pivot …". Turning a socket on or off therefore moves the stab unless *Height* changes with it: the pivot Z in the socket line is the no-socket Height (build 233).
- **Fixed interface.** "flat from … mm, full width to …, tail to …": positions from the stab pivot. They stay the same for every stab with the same Arm, so any stab fits the same fuselage.
- **Interface warnings.** "Flat start is ahead of the leading edge" or "is within 6 mm of the leading edge (LE at X mm) - held at Y mm" (6 mm with *Front ramp* on, room for the ramp; 2 mm with it off, a square front; build 234), "the trailing edge is behind Tail start", "Tail end is inside the root": the geometry is held so it still builds, but the interface is no longer fixed. Move the named field, or accept it for a stab that only ever fits its own fuselage. With a *Root tail* the LE stays where the base chord puts it (the tail is all aft); up to build 233 the notice put it a quarter of the tail further forward (LE -18 on a stab whose LE is at -13), and the pad, the bolt check and the fuselage seat followed that wrong root.
- **Bolts.** "2 M5 bolt holes 5.4 mm at 0 mm from the pivot, pitch 35 mm, 2 countersunk 11.2 mm on the underside". A skipped bolt is off the root; change *Bolt 1 at* or *Bolt pitch*. The countersink warning counts the pad, and a *Block to body* fill on the bolt line, as material under the head from build 234 ("..., pad included"), as the wing's flat mount does.
- **Tail volume and decalage** appear when a wing is above. See targets.md.

### FUSELAGE_SEAT

- `seats on the stab's mount contour, X a-b, pad plane Z … (d mm under the axis)` with `n of n tapped holes d mm for Mx`: working.
- `stab seat on top, X a-b at Z …`: the stab sits on top (*Mount side* Bottom). A flat into the top of the body is normal.
- The block that fills between the tube and the seat is the stab's own since build 198 (*Block to body* in the stab's Mount pad group, off by default from build 228: its height is minus *Height*, so it grows as the stab moves off the centreline, and with it off the pad stays exactly *Pad height*); the stab's notice reports it: `block to the body … (cheeks beside it)`, `(fills … mm under it)`, or `no block to the body needed (the fuselage's flat at the pad face is … mm wide)`.

| Conflict | Fix |
|---|---|
| pad plane above the fuselage centreline, nothing to seat on | Foil stabiliser *Height* 0 or below (−4 typical) |
| stab's underside below the centreline (pad underneath) | *Height* 0 or above (+4 typical) |
| pad plane below the tube, the stab's Block to body carries it | valid; set *Height* nearer 0 for a flush seat |
| flat seat on the tube only x mm wide | turn the stab's *Block to body* on (cheeks beside the tube), or *Height* closer to 0 |
| stab's clearance hole not bigger than the tapping drill | only with *Bolt* = Custom: fix the two hole sizes, or pick a named bolt |
| stab bolt at X skipped, off the seat | *Bolt 1 at* or *Bolt pitch* on the stab |
| parallel section runs into the tail taper | move the mast forward, the stab aft, or reduce *Parallel aft* |
| mast base not on the tube / mast leading edge outside the body | move the mast (*Mast X*) or change *Base height* |
| Mast pocket on but no Foil mast | add Foil mast above, or turn *Mast pocket* off |

Errors that stop the fuselage: no Round tube socket on the wing (*Fuselage joint* on, *Joint* = Round tube socket), a Square socket on the wing (it takes a flat bar, which Foil fuselage does not model: use your own bar, build 233), no stab above it, stab feature older than the fuselage (paste the whole Feature Studio again), no room between socket and tail (longer *Arm* or more *Overhang*).

### Foil mast

- Notice: height, chord and taper, thickness law, the root fillet result, pod summary, plate. From build 233, `WARNING: the mast's leading edge (X …) is ahead of the rear face of the wing's round/square socket (X …), so the mast foot stands on the nacelle …: set Mast X … or more, or the wing's Rear extension … mm or less`: the tube or bar starts at the socket's rear face, so the mast belongs behind it (the second fix is offered only where the wing's bore and fairing still fit).
- `MAST_FILLET: ... OK: long edges only, no propagation at 12 mm` is normal: Onshape fillets the section but not its 0.8 mm trailing edge line, which is left as a small notch. "ROOT FILLET FAILED" lists every attempt; lower *Root fillet* or add a manual fillet above Foil mold.
- Errors: thickness outside 4-25 % of chord (*Thickness* (Foil mast), *Top thick*); countersink deeper than the plate (*Plate thick* or a smaller *Bolt*); plate too narrow or too short for the bolts; bolt heads would hit the mast (*Track spacing* or smaller *Bolt*); pod doesn't fit the mast height (*Pod height*); fairing doesn't fit (*Fairing length*, *Nose start %*, *Aft extension*).

### Foil mold

Mold notice: block size, number of tiles and bed size, keys, plugs, cones, pinch-off, then the LAYUP and STIFFNESS summaries. Mast: "plate tray ... from the plate as modelled" means the tray pocket was sized from the part; fillets, chamfers and outline changes made to the plate above the mold are in it, and the bolt pins stay on the Foil mast's pattern (move bolts in the mast's dialog, not by editing holes).

**LAYUP** example: `layup 1036 cm3, 1.59 kg cured at Vf 50% | skin 2 x 200 gsm each face: 0.43 m2, 92 g | UD caps 300 gsm, 40% chord: 15 plies per cap at the root to 4 at the tip (cut 2 of each), 819 mm long | tow 800 tex: 600 g | resin 596 g in the part, mix 834 g | buy 0.53 m2 woven, 1.27 m2 UD`.

- Ply counts are **per cap**: cut every UD strip twice, one per face.
- "skins + UD caps exceed the part": lower *Cap thick %*, *Cap width %* or *Skin plies*.
- Mast: the plate's woven fill, cap run-out and tab plies are included. "tabs take more than the tow core at the plate": fewer *Tab plies* or a larger *Top thick*.

**STIFFNESS**, wing or stab: `4 g: tip deflection 6.5 mm (1.6 mm at 1 g), root EI 2459 N m2, root cap stress 137 MPa`. The load is the spanwise moment Foil performance published at its *Load factor g*; without Foil performance above, there's no check.

**STIFFNESS**, mast: `mast side 1 g at the fuselage, plate fixed: F=932 N, deflection at the fuselage 53 mm, cap stress at the plate 198 MPa, worst 243 MPa at 494 mm, track bolt tension 3.9 kN each, junction: cap force 38.5 kN, shear 2.1 MPa over 47 mm x (1 + 2 x 4 tabs), allow 15, needs 6 mm, plate bending 96 MPa, fillet 12 mm`.

- The worst point is often where the thickening starts, not the plate. To flatten it, start *Thicken from %* lower or raise *Thickness* (Foil mast).
- Junction shear over its allowable means the caps would peel off the plate: more *Tab plies*, longer *Cap run-out*, thicker *Top thick*, or a wider plate.
- 1 g of the whole mass sideways is a severe case; a hard carve is roughly 0.3-0.5 g. The rider picks *Mast side g*.

| Mold warning or error | Fix |
|---|---|
| tip section only x mm thick where the cap starts | Foil wing / stab: turn *Min tip* on, or raise *End ratio* or *Tip thick %* |
| expected n upper and m lower tiles, found … | usually harmless after a skipped key; inspect the bodies if pieces look wrong |
| block taller than the usable print height | lower *Base thickness* or the tip drop, or a taller printer |
| pinch-off groove skipped | the mold is complete; add a groove by hand or turn *Pinch-off groove* off |
| n keys skipped where a seam crosses the plug channel | normal; the neighbouring keys locate the tiles |
| the parting leaves the section by x mm near the trailing (leading) edge at y = … mm: the lower half laps over / the upper half laps under the TE (LE) … | build 235's self-check found a half wrapping round the edge, an undercut: report it with the mold's MOLD_INPUTS and the wing's WING_INPUTS. More *Split stations* and *Tip bias* are the first things to try; the console's `PARTING_CHECK` line gives the worst gap at each edge on every run |
| n slivers under 0.05 cm3 removed (a knife-edge tip …) | not a warning: a knife-edge tip left bodies of almost no volume at the cut, now deleted (build 235); turn *Min tip* on or raise *End ratio* in the Foil wing / stabiliser for a tip that can be laid up |
| Tile key … skipped: its flange leaves x mm beyond the groove and the pocket needs y | *Tile joint* = Puzzle inlay: raise *Flange width* to the value given, or a narrower *Inlay width* |
| Tile key … skipped: it would reach a half key / the socket plug's channel / a groove vent / another tile seam … | the pocket found no clear place in its flange; usually the seam beside the nacelle's plug channel; that seam keeps its other key |
| n inlay keys tilt more than 12 deg with the flange … | the floors and keys follow the flange; past 12 deg the end wedges get large: a shorter *Inlay length*, or a seam elsewhere (another *Printer Y*) |
| the flange curves off the plane its key follows (Tile key …): that key stands x mm proud … | the flange under that key is not flat (a parting station's kink, a tip's camber): sand it flush after gluing, or a shorter *Inlay length* |
| Tile key … skipped: tilted x deg with the flange, its longer pocket would reach … | the tilt allowance pushed the pocket into a key or a seam beside it; the seam keeps its other key |
| joint k at y … has no inlay: the core is x mm thick … under 6 mm | milled core with Puzzle inlay: that joint is glued without a key; a thicker section there, or a dowel by hand |
| Could not cut the cavity into the UPPER / LOWER half | first check the tip thickness as above; then more *Split stations* and a higher *Tip bias*; then confirm the Source model matches the selected part |
| Source model not found / feature older than this Foil mold | add the source feature above, or paste the whole Feature Studio again |
| key size errors | keep *Pin dia*, *Key dia* and *Key height* inside *Base thickness* − 4 mm and *Flange width* − 4 mm |
| pad underneath in the design: molded upside down | expected for a stab that sits on top of the fuselage; turn the finished part over |

### Foil performance warnings

| Warning | Meaning and first fix |
|---|---|
| wing stalls at take-off speed | raise *Take-off km/h*, or more area / CL max; see levers.md |
| take-off within 15 % of stall | the same, less urgent |
| stab CL above 0.8 in range | stab near its own stall: larger stab or change its incidence |
| cavitation number below 1.5 | thinner, less cambered sections or more depth; check with XFoil |
| depth under 2 MAC | free surface not modelled; results optimistic |
| no Foil mast / fuselage above this feature | move Foil performance below them in the feature list |
| wing / stab polar used outside its Re range | the polar's CL max is extrapolated; add a polar at nearer Reynolds numbers |
| motor pod or prop can never be submerged | lower *Pod height*, smaller *Prop dia* |
| full power cannot hold take-off speed | more *Current lim A*, *Gear ratio* or *Prop dia* |
| cruise needs more than the current limit / full throttle | more cells, lower *Prop P/D*, or a smaller foil |
| pitch-unstable under power / static margin under power only x % | lower *Pod height*, longer *Arm*, more stab area or decalage; run pitch_sweep.py |

### LE tubercles line (build 232)

With *LE tubercles* on the wing and *Tubercles* on in Foil performance, the notice ends with `LE tubercles, reported apart (not in the figures above): height h % of chord, pitch p c, flank sweep s deg; trough-first stall, local cl +r %: take-off a % of CL max (smooth b), stall v km/h (w); published CL max -5 to +6 %: take-off … %, stall … km/h; the flipper's +4 deg stall angle: … km/h if the lift curve held …; drag before stall of unknown sign within 8 % of the wing's profile drag: glide x to y at cruise (z)`, and the report carries a `Tubercles - reported apart` block with the same numbers and their sources. Rules for reading it:

- Never fold it into the sweep. The sweep, the CSV, the stall and the take-off loading in the notice are the smooth wing's; the line is what the tubercles would change, as a separate statement.
- The trough-first numbers are the tool's own: the local cl rises in the troughs by the chord's share of half the height, so the first stall begins there, a little earlier. That is the only part computed from the geometry.
- The CL max range and the stall-angle bound are published test results (Johari 2007, Hansen 2011 for 2D sections at low Re; Miklosovic 2004 for the humpback flipper at Re 0.5 million). The point of tubercles, lift kept past stall, has no number here because the polars have no post-stall branch: say so, and say it is what softens a pump stroke or a carve that has stalled the most loaded station.
- `outside the tested range` means the height or pitch, as fractions of the chord, lie beyond what was tested (height 2.5-12 % c, pitch 0.11-0.5 c): the credits are extrapolated.
- The glide range is ±8 % of the wing's profile drag, the spread between finite-wing tests (less drag) and 2D tests (more). Recommend the smooth-against-tubercled test from the same molds when a builder wants a real number.

### Joints and mounts (builds 185-228)

From build 230 the wing and stab notices carry `rolls like x° of anhedral (Cl_beta ±y/rad, this surface alone)` after the planform: the surface's own drop, gull, winglets and sweep, from a vortex lattice, as the straight anhedral from the root that rolls the same. + anhedral rolls into a sideslip, − dihedral. It compares shapes; it does not include the mast or rider.

Wing notice fragments by *Joint*:

- `tube socket: … mm tube, nacelle …` / `square socket: w x h mm bore (corners R …), nacelle …`: the two sockets, held by countersunk bolts through the nacelle's top (*Bolts*, *Bolt*, *Bolt pitch*); *Bore corner* rounds the square bore like extruded bar. From build 233 the line ends `tapers to … mm (square: w x h) by the rear face at X …, mouth wall … mm, bore … mm deep from X …`; *Mouth wall %* sets the wall left at the rear face (50 = every earlier build; 75-100 for a metal bar, `full size to the rear face` at 100). `the bore starts … mm behind the root trailing edge …: Socket depth … mm or Rear extension … mm puts 10 mm of it under the wing` is a notice, not a fault: with *Rear extension* longer than *Socket depth* (the defaults) the joint is a pod behind the wing on any root.
- `flat hub w x l mm from X … (square back), … mm fillet round the join to X …`: the hub, a D in plan with its pocket open through the square back; `(no join fillet: …)` names why a fillet could not be built (hub too close to the trailing edge, off the wing); `JOIN FILLET FAILED` should not happen and wants the WING_INPUTS line.
- `flat mount: n x M6 countersunk through holes … least laminate under a head x mm`: bolts straight down through the wing onto a bar; a skipped bolt is outside the root chord. From build 227 it continues `mount pad under the root: w mm wide, seat face h mm below the underside's lowest point at Z …, flat from X … to the TE; sets the modelled Incidence i deg against the bar (on its bare underside the wing would seat at a deg: seat line s deg off the bar)`: the pad is the shim that makes the typed *Incidence* the built one; `(no pad body: the underside already lies on the pad plane …)` on a Clark Y drawn at 2.1° with *Pad height* 0. With *Mount pad* off the same numbers come as `WARNING: no mount pad - bolted flat on its bare underside the wing seats at a deg, not the modelled i`: either turn the pad on, or set *Incidence* to a to analyse the wing as it will be built (the seat line then reads 0).

Stab notice fragments: `mount pad on top / underneath, face h mm off the skin`, `(no pad body: the section's own flat is the seat …)` when *Pad height* 0 sits on a flat side, `(square front at Flat start, no ramp)` with *Front ramp* off, `aft end on the trailing edge, which the root tail curves forward across the pad` with a *Root tail* and no *Tail* fairing (build 234: the pad stops on the trailing edge instead of its corners hanging behind it), and the block lines above.

Wing mold: `socket plug = the bore as modelled (2 cores) on a … handle`, `n bolt shanks cleared through the nacelle's walls, the countersinks molded by the upper half`, `hub molded as modelled (pocket, fillet, holes): n tapered cones …`, `flat mount: n through holes mold as pins from both halves` (`; the mount pad under the root molds in the half that forms the underside` from build 227). A plug built from 0 cores means the bore is not open at the rear face or a hole off the wing's pattern still ties it to a half. From build 235: `hub molded as modelled (pocket, fillet, nose fairing, holes): its n through holes from the wing's underside are filled, and the lower half forms each countersink and a tapered cone … to … mm under the pocket floor` for a hub bolted from below (`… mold as pins from both halves …` with *Bolt cones* off).

**Puzzle inlays** (build 235, *Tile joint* = Puzzle inlay, for a mold milled on a 3-axis router). The tile-keys segment of the mold notice becomes `puzzle inlays: n bow-tie pockets L x W mm (waist w) on a level flange, d mm deep along the flange's normal, straddling the tile seams in the parting-face flanges, inside corners R …, the waist's outside corners R …, walls vertical: mill each from the top in its tile's cavity setup | n keys built as parts (Tile key ...), L x W mm (waist w) x t mm thick, square-edged, corners rounded R … as the cutter leaves them: cut from N mm sheet faced to t, glue each into its pocket across the seam, 0.15 mm under the parting face`, then `; floors and keys follow the flange: tilt up to x deg (Tile key …), end gap up to y mm beyond the Glue gap (glue-filled)` on a sloping flange (0.6 mm at 7 deg for the default 4.85 mm key) and `; k skipped (see the warning)`, and `| n half keys …`. Each pocket's floor is a plane fitted to the flange under it, moved Inlay depth into the half along its normal; the key lies on it, so it never stands proud on a drooped wing. The keys are parts named `Tile key upper seam 1 LE` (spanwise seams: LE and TE flanges) or `Tile key lower chord seam 1 -Y` (seams along the span: the flanges at each end). With defaults the pockets are 30.2 x 12.2 mm, 5 deep, and the keys 30 x 12 x 4.85 mm cut from 5 mm sheet. The pocket is centred in the flange left beyond the land and the groove with at least 2 mm each side: a 25 mm flange with a 3 mm land and a 4 mm groove leaves 18 mm, 2.9 mm each side of the default pocket, and needs about 23.3 mm. Milled core (*Core made* = Milled): `puzzle inlays on the top and bottom faces across n of m joints, just outside the cap band: pockets …, d mm deep (Inlay depth or a quarter of the core) …; 2n keys built as parts (Core key ...) … standing 1 mm proud: glue them across the joints and sand them to the core's profile`; the dowels are dropped.

**Hub bolted from the wing's underside** (build 235, Custom hub, *Bolts from* = Wing underside): the hub line reads `n x M6 clearance holes 6.4 mm up through the wing from its underside to the pocket floor, countersunk 13.4 mm at 90 deg in the lower skin so the heads sit flush, for bolts up into the bar or the fuselage's tongue, which is tapped; least laminate under a head x mm (countersink to pocket floor); keep Clear joint on in Foil mold with a Core clear so the bolts pass through solid laminate, not foam`, with `WARNING: bolt k at X …: only x mm of laminate between its countersink and the pocket floor (…)` under 2 mm and `its countersink breaks … mm into the pocket` below 0. Move the bolts where the root is thick (*Bolt 1 from*, *Bolt pitch*), raise *Hub above apex*, shallow the *Pocket depth* or take a smaller *Hub bolt*. Example: a 138 mm root, 12 % thick with 4 % camber at 50 %, *Hub above apex* 3 and *Pocket depth* 6 leaves about 11 mm under an M6 head at 35 mm behind the leading edge and 9 mm at 70 mm. The Foil fuselage's hub body then taps blind holes up from its tongue's underside (`n x M6 blind tapped holes 5.0 mm up from the tongue's underside, d mm deep or more for the wing's bolts, which come up through it`).

**Nose fairing** (build 235): `nose fairing from X a (L mm ahead of the pocket's front wall) rising to H mm above the hub's top at the wall (Z z), h mm above the root section's apex; its back face is the wall's plane for the bar's nose, the hub's width with a rounded nose`; `(it dips … mm under the hub's top where it crosses the hub's front …)` when a short or low fairing starts just ahead of a hub that stands proud; `WARNING: the nose fairing would start … mm ahead of the root's leading edge …: started 3 mm behind it instead - Fairing length N mm or less keeps it on the wing`. *Fairing height* is normally the bar's height less *Pocket depth*. Foil fuselage's hub body warns when the wing has one: it is for a bar of your own.

Stab mold: `pad underneath in the design: molded upside down` appears only with the pad's tail fairing on; otherwise the mold keeps the stab's orientation.

---

# references/targets.md

## Targets and typical ranges

Rules of thumb for judging tool output. They are starting ranges drawn from common practice and the tool's own tooltips, not certified figures; brands and riders differ. Always weigh them against the user's stated goal.

### Front wing by discipline

| Discipline | Area cm^2 | Aspect ratio | Take-off km/h | Cruise km/h | Notes |
|---|---|---|---|---|---|
| Pump / downwind | 900-1500 | 9-14+ | 8-12 | 12-18 | Glide and low induced drag matter most; high AR, thin-ish sections |
| SUP / surf | 1200-2500 | 4-7 | 10-14 | 14-22 | Turning and low-speed lift; lower AR, more sweep and anhedral |
| Wing (winging) | 900-1800 | 6-10 | 12-16 | 18-28 | All-round compromise |
| Windfoil | 700-1200 | 6-10 | 12-18 | 25-35 | Higher speed; cavitation starts to matter |
| Kite race | 300-700 | 8-12 | 15-25 | 30-45+ | Small, thin, high speed; cavitation governs section choice |
| eFoil | 1000-2000 | 5-9 | 12-16 | 18-25 | Efficiency at cruise sets range; beginners want large area |

Scale area with mass: roughly proportional to total mass for the same take-off speed.

Sixth production range (Gong V3 Atmo, 2026; design notes): span, area, root chord, AR and thickness in mm published for every size (126 sizes, 24 lines); thickness over root chord 10.5-12 % on surf, freeride and slalom wings, 12.7-14.4 % on high-aspect downwind and pump wings, 15-16.8 % on the most slender (Supra/Ultra Trail, Veloce XTR); constant section root to tip, no twist (V3 needs ~200 cm2 less than V2 for the same lift); mean/root chord 0.74-0.79 on surf/freeride (superellipse n 1.6-1.95 at end 0.2), 0.64-0.73 on pump/race (n 1.2-1.55); tail wings on S/M/L rear shafts with +1 to -3 deg shims (+ raises the tail's leading edge, less downforce); Gong's own aptitude scores (lift, speed, carving, glide, pumping, pop) per size.

Fifth production range (Omen, 2026; design notes): Agent front wings 700/900/1100 cm² at AR 10.1-10.4 (spans 843/954/1070 mm), superellipse n 1.75 end 0.21, straight mid-chord line with tips raked 27 mm; twist geometric plus reduced tip camber (Washout with a flatter Tip profile); Fix stabs 153/162/171 cm² at a constant 42.5 mm mean chord, stab/wing 0.22 to 0.155, arm 596 mm quarter chord to quarter chord on the 900; tails at -2 to -3 deg with +-0.33/0.67 shims, tuned at high speed in half-degree steps; Omen's shim tool adds 0.45 kg per cm of rider height over 183 cm and 0.9 kg per cm of mast over 80 cm to the weight and steps the 900's tail about 0.35 deg more downforce per 18 kg (85 kg, 183 cm, 80 cm mast: -0.3); tail downforce designed to rise with the system's nose-down moment so the stance shift stays small; the tool's trim omits the mast/fuselage drag couple, so add it in Pitch Nm when judging top-speed trim; Stiffy V2 mast 110 x 13.2 mm (80 cm) and 112 x 14.2 (85 cm), 1 deg rake.

Fourth production range (Lift, 2026; design notes): area in in² (with cm²), span and AR per front wing, area and rear-fuselage length per back wing, M6 mast 14 mm, nothing else. One outline family (superellipse n 1.5, end 0.22, leading edge swept 50-155 mm aft at the tip); anhedral measured from true rear renders: flat centre 20 % of half span, then straight 5.5-6.5 deg, tip drop 3.4-3.7 % of span (Drop shape Power, exp 1, Drop from 20, Drop to 90); camber in Lift's words: HA-X and LCX low, Havoc/Vario/Florence medium, Surf/Camber Pro/old HA high; Vario Twist and Grubb tips = washout 1-2 deg (the only production washout statement); fuselage split at the mast (front stub 260-315 mm plus a rear piece of 140-250 mm sold with the back wing; arm chosen by the back wing); stab / wing 0.11 to 0.28; Carve tails pitched down more than Glide.

Third production range (AXIS, 2026; design notes): designer-stated camber 2.5 % on ART, ART Pro and Spitfire, 3.5 % on PNG, rears about 1 % inverted, mast symmetric; tip section symmetric (Tip profile with Tip camber 0); wings set at 1 deg to the fuselage, rears at 1.5 deg down; thickness from published volumes 10.5-12 % on every current series (Tempo 7-11); outlines near-elliptic with pointed tips on surf wings, square-tipped tapers on glide and race wings; Skinny stabs tuned by chord at a fixed 360 mm span (AR 8-18); Power Carbon mast 140 x 20 mm at the plate to 125 x 14.5 at the foot, PRO 13.5 mm.

Second production range (Code Foils, 2026; design notes): span, area and AR only. Front wings hold one AR per series: S all-round 9.5, X surf 8.2, R downwind 13, Kanga pump 9 to 13 at a fixed 1300 mm span; tails AR 8.1 to 10.8 (the AR series keeps a 42 mm chord and changes span only), R tails 10.8, Race tails 15 to 16; fuselages 420 to 540 mm, shorter than AFS's 650 to 680; Black Series mast 105 x 13.6 to 14.2 mm, tapered and thickened to the plate. Stab / wing 0.14 to 0.18 on their pairings, tail volume 0.8 to 1.0.

Measured production range (AFS, 2026; the project's design notes have every number): thickness 11-13 % of root chord on every front wing whatever the AR (9-10.6 on the Evo and Pure), so millimetres follow chord; AR 6.3 learning (Flyer), 7.5-8 all-round (Evo), 9 surf/carving (Silk V2), 11 all-terrain and dockstart (Enduro), 12-13 HA freeride (Evo HA), 14-16 downwind (Ultra). Outlines: superellipse n 1.7-2.1 at low AR, 1.4 with raked tips (sweep 7-8 % of span at exponent 3-4) for surf and all-terrain, double taper (break 35 %, mid 0.83) on the HA freeride wing. Straight trailing edge (Align line % 70-90) on surf, race and downwind wings; straight-ish leading edge (17 %) on HA freeride. LE tubercles only on pumping/dockstart/surf wings: pitch 53-63 mm, height 5-6.6, 15-85 % of half span. Winglets only on the HA downwind wing and the carving stab. Weight about 0.75 kg per 1000 cm². Use these as the sanity check when a user's design sits outside the table above.

### Section

| Item | Typical |
|---|---|
| Camber % | 3-4 pump/SUP/surf, 2-3 wing/eFoil/wind, 1.5-2.5 kite; stab 0 (or -1 to -2 inverted) |
| Root thick % | 11-14 front wing (AFS: 11-13 at every AR; 9-10.6 on their all-round and race wings), 8-10 high speed, 8-11 stab (AFS stabs are 4.5-7.4 mm thick, 6-11 % of chord); a full-base stab (Double taper) runs thinner at the root than outboard, 7-8 % root / 9 % tip, so the wide root carrying the bolts is not a tubercle (AFS U Carve: 5 mm on a 68 mm root, 7.4 %) |
| Profile | NACA 4-digit for anything parametric; E817/E818 the DIY hydrofoil standards; NACA 63-412 laminar; Clark Y flat-bottomed, the easy one to lay up and check flat, and on a stab its flat face mates to the fuselage (Invert on). All four have Built-in polars |
| Camber position | Forward camber lowers the pitching moment; aft camber (a cupped trailing edge) widens the range and helps low end but costs top-end drag and needs a more powerful stab. A cupped tail is not proof of high camber: its position matters as much as its size. Reflex at the trailing edge cuts the pitching moment (Jonah Lepak, `reference/jonah-lepak`) |
| Section CL max input | 1.0-1.2 for 8-10 % thick low camber, 1.2-1.4 for 12 % with 2-3 % camber, 1.4-1.6 for 14 % with 4 % camber; lower on small stabs (low Reynolds number) |

### Stabiliser and pitch

| Item | Target | Why |
|---|---|---|
| Stab area / wing area | 0.15-0.22; AFS pair at 0.14-0.17 with 650-680 mm fuselages and AR 8-14 stabs (tail volume 0.65-0.8) | Below: weak pitch damping; above: extra drag |
| Stab AR and pairing (AFS) | 8 with learning wings, 9-10 surf and carving, 14 downwind; shim half a degree to a degree between pairings, i.e. about 1 deg of Decalage | Higher-aspect stab with higher-aspect wing |
| Stab planform | Superellipse n 2.5-4, or Double taper Break 20-30 % / Mid 0.55-0.7 / Tip 0.15-0.3 / Blend 8-15 with Tip cap = Rounded + plan for the full-base outline of current production stabs (U Carve 130: 30 / 0.60 / 0.23 / 10, cap 18 mm at 30 %, AR 10, Align 0, Sweep tip 25) | Full base: the mount and both bolts sit in the wide root, and the outer stab can be high aspect |
| Tail volume | about 0.5-0.8 | Estimate for typical foils; higher = more stable and damped, slower to pitch |
| Arm | 550-800 mm (short 450-550 surf, long 700-900+ pump) | Longer = more stable, less pitchy |
| Geometric decalage | 1.5-3 deg | Sets how much stance shifts with speed (see levers) |
| Stab CL across the speed range | within about +/-0.5; warning above 0.8 | Stab near its own stall is unpredictable |
| Static margin at take-off speed | positive; under about 5 % MAC feels twitchy | Lowest point of the range. With Propulsion on and a motor pod, judge static_margin_powered_pct instead - a high thrust line lowers it |
| Stance shift take-off to max speed | tens of mm is normal; over about 100 mm is a lot to move | Depends on decalage and camber |
| Stab trimmed to the wing | each front wing section needs its own stab power: a garage builder makes the stab oversize and trims a few mm off its tips at a time until the balance is right (Jonah Lepak); in the tool, lower *Area* on the stab a few % per step |
| Board attitude (alpha) at cruise | within a few degrees of level | Riders dislike riding nose-up or nose-down |

### Performance sanity

| Item | Guide |
|---|---|
| Take-off % of CL max | under 80-85 %; 100 % = cannot fly at that speed |
| Stall vs take-off speed | stall roughly 15-25 % below intended take-off |
| L/D wing + stab only | high-teens to mid-20s for efficient designs at best-L/D speed. With a real Foil mast + Foil fuselage present, expect roughly 5-10 points lower at cruise once their drag is added - always quote which one a number is; the notice splits out mast and fuselage/pod drag |
| Other cm^2 | 1-3 - bolt heads, fittings and cables only. The mast, fuselage and motor pod come from their features, and since build 240 the joints (wing, stab and mast junctions, the socket nacelle's and hub's faces) from *Joint finish*; do not count them twice |
| Cavitation number at max speed | should exceed the section's -Cp min; below about 1.5 check with XFoil |
| Depth / MAC | above 2 to avoid free-surface effects |

### Structure

| Item | Guide |
|---|---|
| Load factor | 3 general, 4-5 pumping/jumps/hard landings, 6+ kite freestyle |
| Cap stress | 300-500 MPa wet-laid UD carbon with margin, 600-900 prepreg |
| Cap thickness | comfortable under about 20 % of the root section depth; if more, thicken the root section, widen the caps or accept a lower load factor |


### Propulsion and powered trim (eFoil / foil assist)

Full guide and sanity checks: propulsion.md. The headline targets:

| Item | Guide |
|---|---|
| Powered static margin | >= 5 % MAC from the lowest speed the motor is used while foiling (usually take-off) |
| Cruise throttle | 40-70 %; over 85 % at cruise leaves little margin |
| Cruise motor current | a third to a half of the limit or less |
| Static thrust | several times cruise drag (ploughing before lift-off is not modelled) |
| Foil-assist pod-out depth | >= 2 MAC or it is not a real mode (the tool then calls the layout eFoil) |


### Mast, joint and molds

| Item | Guide |
|---|---|
| Mast side load case (*Mast side g*) | 0.3-0.5 g of all-up mass is a hard carve; 1 g is a severe design case |
| Mast deflection at 1 g | tens of mm is typical for a carbon mast; halve it by adding about 40 % to the section thickness |
| Mast cap stress | worst point, not only the plate, under about 225 MPa (factor of 2 on wet-laid UD) |
| Mast plate junction shear | under the allowable (Shear allow / 2); 3-5 tab plies per face usually gets it well under |
| Mast thickening | start it near the waterline at ride depth: it adds stiffness with no wetted thickness |
| Wing tip deflection at the load factor | a few % of the half span at most; stiffer feels more direct |
| Minimum tip thickness | 1 mm or more molds reliably (*Min tip* on) |
| Stab seat | *Height* -3 to -5 mm with the pad on top, +3 to +5 with it underneath |
| Stab bolts | M5 or M6 for small stabs, M6 to M8 for large |
| Mast section (garage-built, Jonah Lepak) | solid carbon with some UHM, about 20 plies per side, chord only 86 mm at a high thickness ratio: slippery and gliding well at medium downwind speeds; the thickest line straight down the mast and the taper nearly symmetric about it so it bends without twisting; constant thickness from the fuselage up past the junction (earlier masts cracked at a thin spot there) |
| Mast thickness for speed | Formula Kite race masts are about 12 mm thick; a home-built kite foil on a 15 mm mast could not pass about 25 knots without wipeouts, and a 12.6 to 15 mm solid carbon mast was the fix (HANDIY, 2025). Thinner needs a solid, compressed laminate to stay as stiff, and leaves less room for inserts |
| Mast section (AFS) | constant top to bottom: chord 105-135 mm, thickness 10-13 % of chord (12.3-16 mm), 65-95 cm, 1.3-2.3 kg; UHM carbon quoted at up to twice standard stiffness |
| Mast plate joint | the Foil mold molds the plate with the mast, cap plies folded over a root fillet with UD tabs, and checks the shear. The home-built alternative is a separate plate, about 6 mm of carbon and glass, bonded on with a filled-epoxy fillet and a few plies each side over the joint (HANDIY). It is not checked, and it sits where the mast's moment is largest, so advise the molded joint for anything ridden hard |
| Mast to fuselage, barrel nuts | barrel nuts set in the mast on its centreline, sanded thinner than the mast and bonded under bolt load, with the tip tapered so the fuselage seats on it like a Tuttle: no box in the fuselage, but holes through the mast's thinnest section (HANDIY, 2025) |
| Mast to fuselage | bolted-through foot: no wobble but the saddle costs fuselage torsion and threaded inserts need a bulge in the foot; mortise and tenon: tight or slightly loose, never both; a permanently bonded joint with glued cross pins avoids both, but the fuselage then stays on the mast (aluminium piece with a hidden zinc anode, or titanium) (Jonah Lepak) |
| Fuselage (AFS) | 650-680 mm, 32.5 mm carbon or 27 mm titanium (23 % less drag, 30 % stiffer, twice the weight), two stab positions |
| Mold tiles | fewer is better; each seam is a line to fair on the part |
| Clark Y incidence | ask what line a flat-bottomed plan's angle is measured to. Flat mounted directly on the fuselage: the quoted angle is the chord to the flat and goes straight in as Incidence (negative for a stab hung flat side up); the tool's table has its chord 2.1° from its flat, so 2.1 / −2.1 puts the table's flat on the bar. Angle quoted for the flat itself: add 2.1° for the chord. From build 227 the wing's Flat mount carries a *Mount pad* (on by default) that seats the typed *Incidence* whatever the section, and the notice reports the bare-underside seat angle; the 2.1 rule applies only with the pad off, or to a stab hung flat on the bar |
| Fibre volume | 45-50 % wet layup clamped in a matched mold, 55-60 % bagged or prepreg |
| Closing | *Closing* = Clamps (default) or Vacuum bag round the whole mold: the bag gives bag pressure x block plan area (5-15 kN), 1.5-4 bar on the laminate, uniform; with the bag set *Overcharge %* 3-5, keep *Groove vents* on and *Edge round* 4-6 mm; the notice's "holds X cm3 against Y cm3 of flash" must have X above Y or deepen *Groove depth* |
| Printed core | *Core* = Printed core: the mold feature generates the core as printable segments with two dowel holes at each joint, either side of the thickest point. Print with 2 walls and 10-15 % gyroid, standing on the joint face (mold settings with 5 walls gave a near-solid 510 g core: 1000 kg/m3). *Core kg/m^3* = slicer filament mass / core volume from the LAYUP line: the Silk V2 core at 2 walls and 15 % gyroid sliced at 297 g on 508 cm3 = 580 (5 walls gave 1000); *Core GPa* 0.5-1 |
| Printed core shear | the core between the caps carries 1.6-2.3 MPa at 3 g. Foamed filaments cannot: about 0.1-0.4 MPa at a 35 % gyroid band. Plain ASA can: about 2 MPa at 45 % gyroid, 2.3-3.3 at 50 %, and it holds its shape to 90 °C (PLA deforms at 54 °C, PETG at 68 °C, too close to a thick laminate's exotherm). Print the cap band at 45-50 % and the rest at 10 %, each segment stood on its leading edge so the layers lie across the chord. On the Ultra 900 that core is about 90-110 g against 78 g for 200 kg/m³ foam. Enter *Core shear MPa* by hand: about 2 for such a band, 0.2-0.3 for a 10 % ASA core (build 226's default treats any printed core as PVC foam). Analysis: `designs/jason/printed-core-properties.md` |
| Wood core (no mold) | a solid wood core bagged one face at a time is the one-off route, e.g. a 1351 cm² half-ellipse on NACA 63-412 in poplar (James Goldney, 2025). Model it as Foam core with *Core kg/m^3* about 450 and *Core shear MPa* about 6. *Core GPa* stops at 5, below poplar's 8-10 along the grain, so the stiffness check reads such a wing a little soft |
| Core clear | with a joint on the wing (Round or Square socket, Flat hub, Flat mount) and *Clear joint* ticked (*Clear socket* before build 233), a foam or printed core starts at the joint's half width (nacelle, hub, or the flat mount's heads + 3 mm or half its pad) + *Core clear* (15 mm) each side, in the core mold too; the root zone is solid laminate, counted as fill in LAYUP ("root zone solid"); do not set 0 unless the socket wall is meant to be the only solid material there |
| Core mold | a second Foil mold on the same part with *Mold of* = Its foam core: molds the foam itself (sections less the skins, cap grooves), open at the tips for a poured expanding foam |
| Core | Tow fill for a solid laminate; Foam core (Corecell M80 85 kg/m3, Core GPa 0.1) to match production weights, about 0.75 kg per 1000 cm2 of wing; Monolithic where stiffness matters more than weight |
| Mold material | PLA for wing and stab molds with a slow hardener and room-temperature cure (softens at 55-60 °C); PETG for the mast mold and anything post-cured (up to about 60 °C). Print each tile standing on its spanwise end so every layer contains the section profile and the steps run along the span, not across the cavity; 0.16-0.20 mm layers, 5 walls, seam at a back corner, 8-10 mm brim, 15-20 % adaptive cubic infill with 40 % only under flanges and keys (a stab mold with the default 25 mm flange and 20 mm base sliced at 1.29 kg / 19 h with 5 walls and light infill, 1.69 kg / 39 h with dense gyroid); for a stab, Flange width 18 and Base thickness 16; enter the printer height as Printer Y; seal with epoxy, sand to 800-1200, wax. Full settings in the user guide, section 6.4 |

---

# references/levers.md

## Levers: what each change does

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

### What the table shows

**Decalage is the stance-shift and speed-stability knob.** More decalage (stab incidence more negative, stab camber negative, or wing incidence up) makes the stab push down harder at speed, so the trim CG moves forward as speed rises: a large stance shift, a foil that strongly resists speeding up, higher static margin, and a little more drag at speed. Less decalage gives a neutral foil that holds any speed with little stance change, but static margin at low speed falls towards zero. There is a decalage at which trim CG barely moves with speed; below it low-speed stability suffers. Most riders sit between.

**Wing incidence does not change stall speed.** It changes board attitude (alpha) almost one-for-one and acts like extra decalage. Use it to level the board at cruise, not to fix take-off.

**Wing camber** lowers the board attitude needed and raises CL max in reality (the tool only reflects CL max through the input), but adds nose-down moment: bigger stance shift and worse high-speed drag. Pair a camber increase with a slightly smaller decalage to keep the stance shift in check.

**Stab area and arm** both raise tail volume and static margin and increase stance shift. Stab area also adds drag at speed (about -1 L/D at 30 km/h for +50 % stab area). This table predates the Foil fuselage feature: with a real fuselage present, a longer arm now also lengthens the fuselage body and its wetted area, adding a little more drag than this table shows - not re-measured with the fuselage in place; run pitch_sweep.py for a specific design.

**Aspect ratio at fixed area** is the big efficiency lever at low and medium speed (+3.7 L/D at 12 km/h, +2.5 at 20 km/h for AR 7 to 10) and nearly neutral at high speed. Costs: a longer, more flexible wing (higher root moment - check the caps), slower roll and less turning.

**Area** trades low-speed performance for high-speed performance. Stall speed scales with 1/sqrt(area).

**Powered designs** add one more pitch lever, the thrust line (Pod height), and change which static margin matters - see propulsion.md "Levers for powered pitch stability". For any stability question on a specific design, run `scripts/pitch_sweep.py` on the console log instead of reading this generic table: it rebuilds that design's trim from the log, checks itself against the CSV, and sweeps stab area, arm and decalage (and, powered, the thrust line height).

### Symptom -> lever quick map

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


### Structure and joint levers

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

---

# references/propulsion.md

## Propulsion, layout and powered trim (eFoil and foil assist)

Covers Foil performance's **Propulsion** group (performance 1.007+), the thrust couple in trim
(1.008+) and the eFoil / foil-assist layout rule (1.009+). Exact field labels and limits are in
parameters.md under Foil performance > Propulsion and Foil mast > Motor pod.

### Layout: Pod height decides what the foil is

| Layout | When | Propulsion | What Foil performance reports |
|---|---|---|---|
| **eFoil** | Low pod: it can only clear the water with the wing shallower than 2 MAC (not rideable) | Always the motor | `eFoil layout` in the notice; console gives the Pod height raise that would make it foil assist |
| **Foil assist** | High pod: the wing can still ride (>= 2 MAC deep) with the pod out | Deep ride: motor mode (pod and prop in the water). Shallow ride: pump/wave mode (pod out; the rider pumps or rides wave energy) | Both modes, each at its own depth: motor mode (drag = thrust needed) and pump/wave mode (drag and the watts to sustain) |
| No pod | Motor pod off in Foil mast | Prop assumed submerged at Wing depth | No immersion check, no thrust couple (its height is unknown) |

Neither layout is a fault. With Propulsion on, "in" and "out" cover the whole prop disc, not just
the pod: pod-in depth = pod axis + max(pod, prop) radius + 10 mm; pod-out = axis - that - 10 mm.
Riding with the pod partly submerged (the transition band) is the worst case - friction on the
wetted part plus spray on its waterline width - so check whether Wing depth sits in that band
before quoting a single L/D.

### Method: thrust = drag at steady speed

At constant speed thrust equals total foil drag (wing, stab, mast, pod, fuselage). The group
solves this at every speed, at the **powered ride depth**: Wing depth if the prop is fully under
water there, otherwise the pod-in depth (so `thrust_N` can exceed `drag_N`, which stays at Wing depth).

    drag -> prop rpm that makes that thrust (Wageningen B-series KT/KQ: diameter, P/D, EAR, blades)
         -> shaft torque and power, open-water prop efficiency
         -> motor amps = torque / (gear x gearbox eff x 8.27/kV); motor volts = rpm/kV + amps x winding R
         -> throttle = motor volts / pack volts (cells x 3.7 V)
         -> battery amps and watts (via ESC eff) -> Wh/km -> range from Pack Ah x volts x Usable %

**Top speed** is where full-power thrust (falling with speed) meets drag (rising roughly with
V^2) - not a separate calculation and not the static thrust. Full power = the torque the current
limit allows, capped near no-load rpm by back-EMF over the winding resistance.

The B-series polynomials are the same as in the companion B-series blade Feature Studio, so a
blade designed there and the foil analysed here agree (set Gearbox eff 100 % to compare exactly -
the blade check ignores gear loss).

### Reading the output

Notice: `prop at 20 km/h: 2915 rpm, 59% throttle, 468 W battery, 23.4 Wh/km, range 34 km · top speed 35.4 km/h`
plus, with a pod, `under power: thrust couple -4.5 Nm, trim CG 31 mm (+6), static margin 37% MAC`.

Console Propulsion block: drivetrain summary, the ride depth used, static full-power thrust,
no-load prop rpm and pitch speed, the cruise breakdown (thrust, rpm, J, prop efficiency, shaft W,
motor A, throttle, battery A/W, overall efficiency), Wh/km, range and minutes, top speed, the
thrust couple line, and a note if tip speed exceeds 25 m/s.

CSV columns added: `thrust_N, prop_rpm, J, eta_prop_pct, motor_A, throttle_pct, batt_A, batt_W,
Wh_per_km, max_thrust_N, in_limits, thrust_couple_Nm, trim_cg_powered_mm, static_margin_powered_pct`.
`scripts/analyse_sweep.py` summarises them (lowest Wh/km, throttle range, top speed, lowest
powered static margin).

Warnings: full power cannot hold take-off speed; cruise needs more than the current limit; cruise
needs more than 100 % throttle; the prop can never be fully submerged; powered static margin under
5 % (or negative) anywhere from take-off to max speed.

### Powered trim: the thrust couple

Thrust equals drag, so the two form a **pure couple** - the same about any point, so the CG height
does not matter. Couple = sum(each drag x its height) - thrust x prop-axis height (nose-up
positive). Heights: wing drag at the wing root pivot, stab at its Height, fuselage at its axis,
mast friction at mid-wetted span, spray at the surface, pod at its axis.

- **Thrust line above the drag centroid** (a high pod, typical of foil assist) = nose-down couple:
  the rider must stand further back under power, and the powered static margin falls.
- **Thrust line below the drag centroid** (a very low pod) = nose-up couple.
- The couple barely changes with speed while the stab's correcting moment grows with speed
  squared, so the **lowest powered static margin is at the lowest powered speed** - usually
  take-off, which is exactly when a foil-assist rider uses the motor.
- Acceleration (thrust above drag) makes the couple bigger than the steady-state value shown.

### Levers for powered pitch stability (strongest first)

Use `scripts/pitch_sweep.py` on the console log to size these for a specific design.

1. **Pod height** - the couple is thrust x (pod height - drag centroid height), so lowering the
   pod is the direct fix. It also sets the layout: below the foil-assist threshold it becomes an eFoil.
2. **Arm** - moves the neutral point aft at no stab drag cost (a longer Foil fuselage adds a
   little). Raises stance shift.
3. **Lowest powered speed** (Take-off km/h, or how the rider launches) - the margin improves quickly
   with speed; stable from 16 km/h is much easier than from 12.
4. **Stab area** - effective but costs drag and a large stance shift, because more stab also
   raises the unpowered static margin at speed.
5. **Decalage** - acts on the stab's lift, which scales with speed squared, so it is weak at
   take-off and mostly adds stance shift at speed.
6. **Tilting the motor** is not worth modelling: its vertical component acts over only the short
   distance between the pod and the CG, far less than the thrust height lever.

Worked example (prone foil assist, 95 kg, 1000 cm^2 AR 7.2 wing, 160 mm prop, pod at 600 mm on a
750 mm mast, 150 cm^2 linked stab at 620 mm, 2 deg decalage): couple -36 Nm at take-off, powered
static margin -16 % at 15 km/h, positive only from 19. Fixing it with the stab alone needed 290 cm^2
on a 920 mm arm. Pod at 550 mm, a 180 cm^2 stab at 660 mm and 3 deg decalage reached -0.6 % at 15
and +5.5 % at 16, at the cost of a 130 mm stance shift. See examples.md, session 1.

### Sanity checks

| Item | Guide |
|---|---|
| Cruise throttle | 40-70 % leaves headroom for take-off, gusts and turns; over 85 % at cruise is little margin |
| Cruise motor current | well under the limit (a third to a half); take-off needs much more |
| Static thrust vs foiling drag | ploughing before lift-off needs roughly 2-3x the foiling drag at that speed (not modelled); aim for static thrust several times cruise drag |
| Lowest Wh/km | usually just above take-off; cruising much faster than best-Wh/km speed costs range roughly with drag x speed |
| Top speed vs pitch speed | top speed can slightly exceed "pitch speed at no-load" because a cambered blade's effective pitch is above nominal - not a bug |
| Tip speed | over about 25 m/s near the surface expect ventilation and noise |
| Powered static margin | >= 5 % MAC from the lowest speed the motor is used while foiling |
| Prop choice | higher P/D = more top speed and cruise efficiency, less static thrust; judge on the full thrust curve crossing drag, never on static thrust alone |

### Not modelled

Ploughing before lift-off; prop cavitation and ventilation (only a tip-speed note); pod and mast
wake on the prop (open water); part-throttle behaviour beyond the B-series curves; acceleration;
the small lift from the prop axis tilting with body angle. B-series is an open-water estimate,
about +/- 20 % absolute - best for comparing props and layouts.

---

# references/model-limits.md

## Model limits

State the relevant limit whenever a recommendation or a comparison with real-world figures depends on it.

- **Relative, not absolute.** Differences between two variants are more trustworthy than any single absolute number. Profile drag is about +/-20-30 %; induced drag is typically within 5-10 % for modest sweep.
- **Stall**: with Section data = Analytic (default), comes entirely from the Foil performance Section CL max input (applied to sections inboard of 95 % span) - real CL max depends on section, Reynolds number and surface finish, and this input is a stand-in. With Section data = Polar summary or Built-in on the wing (see parameters.md), the polar's own CLmax, interpolated by Reynolds number, is used instead and Section CL max is ignored.
- **Lifting line** ignores sweep and anhedral; they only move the aerodynamic centre in the trim calculation. Checked in a vortex lattice, a drop like DIY-6's moves lift and induced drag under 1 %, so glide and stall are not affected in practice. From build 230 the wing and stab notices give a roll number, `rolls like x° of anhedral (Cl_beta, this surface alone)`, from a vortex lattice on the surface's own drop, gull, winglets and sweep: use it to compare shapes (a 35 mm S-curve on a 900 mm wing is about 5°). It is the surface alone: the mast's side force and the rider decide how the whole foil feels in roll, so whether more anhedral reads as steadier or looser on the water is still advice.
- **Downwash at the stab** uses the classic far-field estimate; a stab close behind the wing or at a different height behaves differently.
- **Mast and fuselage now HAVE real drag** (Foil mast + Foil fuselage present): submerged mast friction scaled to the wetted span (Wing depth vs Base height) plus Hoerner spray where it pierces the surface, motor pod friction scaled to its own wetted fraction plus spray on its waterline width, fuselage body friction on its actual wetted area (the notice and CSV split them out as drag_mast_N and drag_body_N). "Other cm^2" is now for parts NOT covered by this: junctions, the wing socket nacelle, fittings, bolt heads. Junction/interference drag between the mast and fuselage, and the pod's own base drag (normally filled by the spinning prop), are not modelled.
- **Joints are handbook estimates** (build 240, *Joint finish*): the junction corners by Hoerner's strut-wall rule and the faces square to the flow by flat-faced-cylinder Cd, for a finish (bare, filleted, lofted) chosen for the whole foil, at zero lift. A ranking of finishes, not a calibration: at take-off the corners separate sooner and a fillet matters more than it shows. The faces cost far more than the corners: a 40 mm nacelle nose about 6 N filleted at 25 km/h, the corners about 1 N. The JOINTS console line lists each joint. Before build 240 the joints were left out unless put in *Other cm^2*.
- **Still not modelled:** free-surface proximity beyond the 2 MAC depth warning, ventilation, cavitation onset (only the cavitation number is reported, not whether/where it actually cavitates), pumping or any unsteady motion, board and rider aerodynamics, ploughing/pre-lift-off drag (Foil performance only models the foiling regime; take-off needs roughly 2-3x the foiling drag), acceleration.
- **Propulsion** (Propulsion group): Wageningen B-series open-water estimate, about +/- 20 % absolute - no pod/mast wake, no free surface, no prop cavitation or ventilation (tip-speed note only). Motor losses are a fixed 8.27/kV torque constant plus the winding resistance; no no-load current or thermal limits. The thrust couple is included in trim only when the mast has a motor pod (the thrust line is the pod axis); with no pod, enter it by hand in Pitch Nm. The couple is steady-state: during acceleration it is larger.
- **Trim CG** assumes steady level flight; it is where the rider must place their mass, not a measured or fixed CG.
- **Profile drag** uses flat-plate skin friction x a thickness form factor x the real wetted area, plus a small lift-dependent term. It does not capture laminar drag buckets or laminar separation bubbles on small stabs.
- **Structure** is a simple spar-cap estimate at the root: no shear, torsion, buckling, fatigue, core or insert design.

- **Stall and take-off speeds are optimistic.** They come from a clean 2D section CL max in dead steady flight. An experienced builder put a model stall of 13.8 km/h at about 18 km/h in real life (effective CL max about 0.75 against 1.27). Calibrate: model a foil the user has ridden, find the CL max that reproduces its real no-pump take-off speed, CL max_eff = CL max_model x (V_model / V_real)^2, and use it for new designs.
- **One section shape per surface.** Thickness ratio varies root to tip and *Min tip* holds an absolute floor, but camber and profile shape are constant along the span. Builders often blend a thicker, more cambered root into a flatter tip; the tool can't model that yet.
- **Mast and wing loads are static**: a wing at n g and a mast with a constant side force. No fatigue, impact, buckling or torsion; the plate junction check ignores peel at the fillet.
- **Mold geometry is checked, the process is not**: fairing, release agent, resin cure and clamp pressure are the builder's.

- *LE tubercles* are geometry only in the sweep: it uses the smooth outline and cannot see them, so its stall, take-off loading and drag are the plain wing's, always. From build 232 Foil performance adds a separate `LE tubercles, reported apart` line (its *Tubercles* switch) with the trough-first charge it can compute and the published credits; read that line on its own and never add it to the sweep's figures. Winglets are credited only as effective span (0.9 × height).

- What the sweep sees: outline, twist, one or two sections (root and tip, blended), root thickness, real wetted area, winglets as span, mast/pod/fuselage drag. What it does not: sweep, anhedral and gull shapes (their roll effect is the notices' roll number from build 230), the tubercles' stall delay, the winglet's side force, Reynolds variation along the span. Say which when a user asks whether a shape change will show in the numbers.

---

# references/examples.md

## Worked diagnosis sessions

Real iterations with the tool, condensed. Copy the approach and the answer style, not the numbers.

### 1. Foil assist that noses in under power

**Paste:** prone wave foil assist, 95 kg. Wing 1000 cm², AR 7.2. Stab linked at 15 % (150 cm²), Arm 620, Link decalage 2. Pod at 600 mm on a 750 mm mast. Take-off typed as 13 km/h.

**Read first:** the take-off row shows `max_local_cl_pct` 113: stall is 13.8, so the rider can't fly at 13. That's an input problem, not a design one: set *Take-off km/h* to 15 (86 % of CL max).

**Diagnosis:** `static_margin_powered_pct` is −16 % at 15 km/h and only turns positive at 19. The pod's thrust line is 610 mm above the wing and about 490 mm above the drag centroid: a 36 Nm nose-down couple. Gliding margin is 30 %, so the foil itself is fine; it's the motor.

**pitch_sweep.py:** with the pod where it is, the smallest fix is a 290 cm² stab on a 920 mm arm, which is absurd for a wave foil. With `--pod-dz -150`, a 180 cm² stab (area ratio 0.18) at 660 mm with 3° decalage reaches +7 %.

**Answer given:**
1. *Pod height* 600 → 550 (or lower if the pod still clears the water at ride depth). Biggest single effect.
2. *Link area ratio* 0.15 → 0.18, *Arm* 620 → 660.
3. *Link decalage* 2 → 3. Cost: the stance shift grows from about 90 to 130 mm; try 2.5 if that's too much on the water.

**Next run:** powered margin −0.7 % at 15, +5.5 % at 16. Neutral at take-off is acceptable for a foil assist, whose motor phase lasts seconds. Lesson: foil assist trades powered stability for a dry pod; pod height is the lever.

### 2. Mast stress peaks in the wrong place

**Paste:** `STIFFNESS: mast side 1 g at the fuselage, plate fixed: F=932 N, deflection at the fuselage 53 mm, cap stress at the plate 198 MPa, worst 243 MPa at 494 mm`. Mast 750 mm, *Thickness* 16, *Top thick* 22, *Thicken from %* 60. Performance says 340 of 750 mm is submerged.

**Diagnosis:** everything is inside the UD allowable with about a factor of two, but the worst stress is not at the plate. The moment grows all the way up, while the section only starts thickening at 60 % height, so the stress peaks just below that. The waterline at ride depth is at 45 % of the height, so everything above it is dry.

**Answer given:**
1. *Thicken from %* 60 → 45 to 50: the build-up starts near the waterline, which flattens the peak and adds stiffness with no wetted thickness.
2. Or *Thickness* 16 → 18, if the mast should also be stiffer lower down. Stiffness goes with thickness squared; the cost is drag along the whole wetted length.
3. Leave *Cap thick %*: thicker caps sit nearer the neutral axis and barely help.

**Also:** say that 1 g of the whole mass sideways is severe; a hard carve is nearer 0.3-0.5 g, so the real flex is a third to a half of the number.

### 3. The stab-to-fuselage seat disappears

**Paste:** `FUSELAGE_SEAT: | no stab seat | Conflicts: the stab's pad plane is above the fuselage centreline, so there is nothing to seat on`.

**Diagnosis:** the stab was relinked to a bigger area; its root got thicker and the pad face moved up. *Height* is the seat's offset from the fuselage centreline, so the fix is one number.

**Answer given:** Foil stabiliser *Height* −4 (pad on top). With the pad underneath the stab it would be +4. Resizing the stab will never move the seat again.

### 4. "The stall speed isn't realistic"

**Context:** a builder says a foil the model stalls at 13.8 km/h would really need about 18.

**Answer given:** agree it's optimistic, thank them for the point, and explain why: clean 2D section, steady flight, no junction or finish losses, no rider margin. Then turn it into a calibration: 18 km/h means an effective CL max of 1.27 × (13.8 / 18)² ≈ 0.75. Ask for one real foil and its no-pump take-off speed, set *Section CL max* (or the polar CL max) to reproduce it, and use that value for every new design. Until then, quote stall as optimistic and compare variants.

### 5. A mold that won't cut

**Paste:** `Could not cut the cavity into the LOWER half. Tip section is 0.7 mm thick at the cap start`.

**Diagnosis:** a boolean through a sliver under a millimetre fails, and couldn't be laid up anyway.

**Answer given:** Foil wing *Min tip* on at 1 mm. If it still fails, *Split stations* up (30 to 50) and *Tip bias* 3 to 4. If both halves fail on a stab that's been moved, check the Foil mold and Foil stabiliser are the same build.

### 6. A production wing modelled from published numbers

**Paste:** AFS Silk V2 950 (superellipse n 1.4, end ratio 0.26, sweep 67 mm, S-curve drop 30 mm, 15 cm² winglets up at 60°, LE tubercles 5 mm at a 59 mm pitch from 15 to 82 %, NACA 4-digit 3 % root with a 1.5 % tip section from 40 %) and a U Carve 130 stab (double taper, Clark Y inverted, root tail 25 mm) at Arm 560 and Incidence −1.5. 85 kg all-up, no mast or fuselage in the Part Studio, take-off 18, cruise 22, max 36. Full console in the release's `examples/silk-v2-u-carve-console.txt`.

**Read first:** `WING_INPUTS` shows `otherDrag_cm2=0` and there is no `MAST_INPUTS` or `FUSELAGE_INPUTS`, so every drag and L/D figure is wing plus stab only; add 10 to 15 N before comparing with a ridden foil. Stall 15 km/h against a take-off of 18: `max_local_cl_pct` 72 at 18, comfortable.

**Diagnosis:** L/D peaks at 20.0 at 19 km/h and is 19.2 at the 22 km/h cruise, but falls to 11.1 at 36. `CL_stab` runs from −0.11 at take-off to −0.43 at 36 km/h and `static_margin_pct` from 41 to 163 %: the stab is carrying a large and growing download, which is stab-induced drag and a trim CG that moves 137 mm forward between take-off and top speed. The stab at −1.5° is doing more than it needs to; the AFS pairing is designed for a smaller angle than that.

**Recommendation:** *Incidence* (Foil stabiliser) from −1.5 to −0.5. Expect the stab CL at 36 km/h to roughly halve, static margin to fall toward 60 to 80 % at cruise, the stance shift to shrink by a third, and drag at 30 km/h and above to fall a few newtons, with the low-speed margin still positive because the stab's own camber (Clark Y, inverted) keeps it downloaded. Second lever if the margin is still excessive: *Area cm²* (Foil stabiliser) 130 to 115. Leave the wing alone; it is a published shape and the check is whether the tool reproduces it, not whether it can be improved.

**Next run:** compare `static_margin_pct` at 18 and 36, `trim_cg_mm` spread, and `L_D` at 30. If the `MAST_PLATE` or mold lines are in the paste, ignore them for this question: they are the build side.
