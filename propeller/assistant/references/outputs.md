# Reading the outputs

## The propeller's log (propLog / propNotice)

- **First line**: the build. A log from an older build than the user's Feature Studio is the usual cause of a number that does not match the dialog.
- **Inputs**: D, hub, P/D and pitch, tip pitch change, EAR, Z, direction, rake, skew, thicknesses, tip chord, taper. On a tapered hub, "hub" is the hub's diameter at the blade root plane.
- **Station table**: index, r/R, r, chord, t max, pitch, pitch angle, wrap (how far round its radius each section reaches) and the leading edge to generator line.
- **Stages**: the loft (stage A direct, else B1 to B4 surface loft and caps), then hub, trim, pattern and union, fillet, tail, mount. A stage that fails says so.
- **Folder** (`folder:`, `root section`, `tail relief:`, `trimmed against the hub`): pin position and length, boss, gap, stop, cheek width, the root's reach about the pin, the core behind the pins, and how much the fold trim took.
- **Operating check**: shaft torque at the limit and no-load prop speed; the hub's share; static at the limit (rpm, thrust, shaft kW, tip speed, % of no-load, cavitation number, loading, mean CL, verdict); full throttle at the cruise speed (rpm, J, thrust, efficiency, current, % of no-load); steady cruise against the drag (rpm, J, efficiency, shaft W, motor A and V, throttle, W in, Wh/km); for a folder the pin moment and stop bearing.

## Propeller mold (moldLog / moldNotice)

- **MOLD_INPUTS**: every field, for pasting back.
- **Part and model**: which propeller, its stations, its axis.
- **Block and halves**: block size, the halves' z ranges.
- **Sectors**: how many, keys, and each sector's release scan, with the direction it releases on and "clear". A sector that is not clear is the first thing to fix.
- **Release along the axis outside the well**: rays that see the part more than twice; should be none.
- **Layup**: laminate volume, cured mass and carbon, plies per radius, tabs, hub skin, cloth to buy, resin, and the **cavity fill %**.
- **Pinch-off**: groove volume against the flash.
- **Closing**: clamps or bag, with the bag's force on the block.
- **Cut templates**: the key, one line per shape with its cut count, and the cloth through the templates.
- **ERROR in Propeller mold**: with Keep log on error the failure is a warning and the log stands; the last stage line before it shows where it failed.

## Propeller model from part (partModelLog)

The axis, the bore, the hub frustum, the blade count and the measured stations (chord, pitch angle, thickness, crest), plus the Hub core if asked for. Check them against the propeller's drawing.

## Core forging mold (forgeLog / forgeNotice)

The charge by weight (chopped and long tow, resin), the closing force and the nut torque in stages, the sleeve's hoop stress and the piston flange's load against printed PETG, the core's strength at the pin slot, and the technique step by step: printing, release, loading, closing, demolding, post-cure.
