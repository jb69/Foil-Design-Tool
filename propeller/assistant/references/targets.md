# Targets and rules of thumb

These come from the tool's own warnings and the user guide. They are starting points, not limits: say which one a result misses and why it matters for the user's goal.

## Operating check

| Quantity | Target | Why |
|---|---|---|
| Static point, % of no-load speed | 35 to 95 %, a workable band | Under 35 % the prop is over-pitched or too large: the motor lugs at the current limit, hot and slow. Above no-load speed it is under-pitched or too small. |
| Cruise point, % of no-load | well under 95 % | Hard against no-load means the drive has no headroom: raise P/D or cells. Lowering pitch makes it worse. |
| Tip speed | under 25 m/s | Above it, expect ventilation and noise near the surface. |
| Cavitation number at 0.7R | above the blade loading's need: the check gives a verdict | At full throttle through the hump every eFoil prop of this size cavitates for seconds; at cruise it should be clear. |
| Mean CL at 0.7R at cruise | under about 0.3 | Clear for a NACA 66-style or YS-920 section; marginal for a flat-faced (ogival or B-series outboard) section. |
| Static thrust for an assist or eFoil take-off | about a third of the all-up weight or more | Rough line for an easy start; below it the rider has to work the hump. |

## Geometry

| Quantity | Target | Why |
|---|---|---|
| Blade overlap at the hub | blades x chord x cos(pitch angle) under the hub circumference | The feature warns at 95 % and refuses at 130 %. Lower EAR, a bigger hub, or fewer blades. |
| Root thickness, printed | 6 to 9 mm on a 150 mm prop, about 0.045 D | Bending stress at the root goes with the inverse square of the thickness. Print solid. |
| Tip thickness | 1.2 to 2 mm, at least 2 to 3 extrusion widths | Thinner chips on grounding and will not print as modelled. |
| Edge thickness | 0.8 to 1.2 mm with a 0.4 nozzle | Under 0.8 the root gets hard to cap and the edge won't print. |
| Tip chord | under half the chord at 0.9R | Else a fin or ripple near the tip. |
| Sections | 12 iterating, 25 to 30 for the print | |

## Folder

| Quantity | Target | Why |
|---|---|---|
| Cheek between gaps | 3 to 4 mm plus any head or nut pocket | The cheeks carry the pins. |
| Core behind the pins (tail relief) | 3 mm of wall over the bore or more | Under it the root swings into the core when it folds. Pin aft (Pin axial), a Tube wall, pin out, lower EAR. |
| Pin | 4 mm on 140 to 160 mm; 5 mm run hard or on 200 mm | The pin is in bending between the cheeks; the stop gap is the lever. |
| Clearance | 0.3 mm printed | |

## Mold

| Quantity | Target | Why |
|---|---|---|
| Release check outside the well | nothing hooked | Anything there is an undercut the halves cannot form: usually a blade overlapping its neighbour in the axial view. |
| Sectors | every one clear along its own pull | A root sector usually releases a few degrees off radial; the pull hole follows that. |
| Cavity fill (layup line) | close to 100 % | Far over: too many plies for the cavity. Under: too few. |
| Tabs | under half the hub skin | Each tab ply takes a ply's thickness out of Skin thick where it lies. |
| Groove against flash | groove volume above the expected flash | Else the excess has nowhere to go and the mold will not close. |
| Fibre volume | 45 to 50 % clamped, 55 to 60 % bagged | |

## Forging mold

| Quantity | Target | Why |
|---|---|---|
| Sleeve hoop stress | well under printed PETG along its layers (about 30 MPa) | The charge presses outward like a fluid. More Wall, a lower Target MPa, or a steel band. |
| Target pressure | about 5 MPa | Enough to consolidate a hand-loaded charge. |
| Stroke | about 2 x the core's height | The loose charge takes about twice its finished volume. |
