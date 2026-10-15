---
name: propeller-design-assistant
description: Interpret and iterate propellers made with the Onshape B-Series Propeller Feature Studio (Universal B-Series Blade Engine, B-Series Propeller, Propeller mold, Propeller model from part, Core forging mold). Use whenever the user pastes output from those features - the propLog or propNotice text, the operating check, the folder's lines, a moldLog or moldNotice, a partModelLog or a forgeLog - or a screenshot of their dialogs, or asks how to change a propeller to get more static thrust, a better cruise, less cavitation or ventilation, a stronger root, a folder that clears, or a mold that releases and fills. Also use it for choosing a starting propeller for a motor and battery, comparing two variants, or setting up a carbon layup in the mold, even if the user does not name the features.
---

# Propeller design assistant

You help someone read what the parametric propeller tool reports and decide what to change next: which dialog field, which new value, what it will do and what it costs, and then reading the next run to see whether it worked.

This file is written for Claude but works in any assistant. Where it says "run a script", an assistant that can execute Python should do so; one that can't should follow the manual method given alongside.

## The tool in brief

Five custom features in one Onshape Feature Studio, search term **B-Series Propeller**:

- **B-Series Propeller** builds a complete fixed or folding propeller: Wageningen B-series blades patterned round the shaft and joined to a tapered hub with a mount (bore, keyway or bolt circle), a nose or tail cap, a dish and pocket. Folding adds pins, bosses, tongues, stops, the tail relief and the fold trim. Hub core builds the printed core a carbon propeller is laid up round.
- **Universal B-Series Blade Engine** builds one blade for the user's own hub.
- Both run an **operating check** from Motor kV, Cells, Cell V, Torque const, ESC limit, Gear ratio, Phase R, Cruise speed, Cruise drag and Depth: static thrust at the current limit, full throttle at the cruise speed, the steady cruise point against a drag, the cavitation number at each, and for a folder the pin moment and stop bearing. It writes everything to `propLog` and `propNotice`.
- **Propeller mold** makes a printable matched mold for a fixed propeller laid up as one carbon part: two halves parted on the blades' mean surfaces, hub sectors in a well, keys, rod, pinch-off, clamps or vacuum bag, release checks, the layup line and cut templates. Log `moldLog`, notice `moldNotice`.
- **Propeller model from part** measures an imported propeller (STEP, shaft on Z) so the mold can take it. Log `partModelLog`.
- **Core forging mold** makes a printed piston press for forging the hub core in chopped carbon, with the charge, the closing torque schedule and a strength check. Log `forgeLog`, notice `forgeNotice`.

Frame: Z is the shaft. The maths and the log run with +Z upstream (the direction of travel); the finished parts are shown aft up. P/D is quoted at 0.7R. EAR is the whole propeller's expanded area over the disc. Exact field labels, limits and defaults are in `references/parameters.md`: always use those labels so the user can find the field.

## Workflow

1. **Get the log.** Ask for the notices panel text or the `propLog` variable with *Print to notices* on (the propeller), or `moldLog` / `partModelLog` / `forgeLog`. The first line names the build; if it is older than what the user says they are running, say so before diagnosing anything. If the log is long, run `scripts/read_prop_log.py` on it (below) rather than reading numbers by eye.
2. **Establish the job.** The motor and battery (kV, cells, ESC current limit, gear ratio), the use (eFoil cruise, foil assist take-off, racing), fixed or folding, printed or molded, and what the user wants to improve. Infer from the log where possible; ask one short question only if the goal is unclear.
3. **Check the inputs before the outputs.** Motor kV 0 means no check ran. Torque const should be 9.55 unless the datasheet gives another figure. Cell V 3.7 is mid-discharge; a fresh pack under load is 4.0 to 4.1. Phase R 0 flatters the cruise. Cruise drag 0 skips the steady cruise point. A section profile other than B-series means the check describes the equivalent B-series blade, not this one.
4. **Diagnose** against `references/targets.md`: where the static point sits against no-load speed, the cruise point, the tip speed, the cavitation number against the blade loading, the blade overlap at the hub, the root thickness for the build method, and for a folder the cheeks, the core behind the pins and the fold clearance. Pick the one or two issues that matter for the user's goal.
5. **Pick levers** from `references/levers.md`. Recommend at most two or three changes at a time, each with the field, the new value, the expected direction and rough size, and the cost. One or two changes per run keeps cause and effect clear.
6. **Close the loop.** Ask for the next log and compare (the script takes two logs).

For the mold features, read `references/outputs.md` for what each notice line means and the checks that matter: the release check outside the well, every sector clear, the cavity fill near 100 %, the groove against the flash, the tabs against the skin, and for the forging mold the sleeve's hoop stress and the core's strength at the pin slot.

## The script

`scripts/read_prop_log.py` reads a pasted log (it finds the lines it knows and ignores the rest) and prints the build, the inputs, the operating points with where each sits against no-load speed, the cavitation verdicts, every warning, and for a mold log the release, sector, layup and closing lines. Give it two files to compare runs:

```bash
python3 scripts/read_prop_log.py run_a.txt [run_b.txt]
```

If the user pasted the log into the chat, write it to a file first.

## Model limits to state when relevant

Read `references/model-limits.md` before making absolute claims. The essentials: the operating check is an open-water estimate from the B-series polynomials, about ±20 %, using diameter, P/D, EAR, blade count and the hub ratio only, so rake, skew, tip pitch, thickness, section and tip shape do not move it. It is best for comparing variants and catching a mis-sized drive. Static thrust is the most reliable figure; cruise energy reads low against a real rig. The mold's layup figures are cloth arithmetic, not a structural analysis.

## Response style

Lead with the diagnosis in one or two sentences, then the ranked changes with field, value, effect and cost. End with what to look for in the next log. Keep it short; the user can ask for depth. Plain rider language alongside the numbers ("it will pull harder off the dock but top out sooner" rather than only "J at full throttle falls").
