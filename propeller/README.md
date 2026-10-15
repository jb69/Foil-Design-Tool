# B-Series Propeller for Onshape

Parametric marine propellers and the molds to make them in carbon, as five custom features in one Onshape Feature Studio. Give it a diameter, a pitch, a blade area and a blade count and it builds a fixed or folding propeller on Wageningen B-series blades with its hub and mount, checks it against your motor and battery, and makes the printable mold, the hub core, the ply templates and a press for forging the core.

By JB69. Build 115.

> **Read this first.** Every number the operating check prints is an open-water estimate from the B-series polynomials, about ±20 %, from diameter, pitch, blade area, blade count and the hub alone. Use it to compare designs and to catch a mis-sized drive before a print; trust your own logs over it.

## What's here

| Folder / file | What it is |
|---|---|
| `bseries_propeller.fs` | The Feature Studio source. Only needed for your own copy; the published features are added in Onshape by searching custom features for **B-Series Propeller**. |
| `docs/BSeries_Propeller_Guide.pdf` | The user guide: every feature and field, the operating check, the folder, the molds, warnings and fixes |
| `docs/guide.md` | The guide's source, readable here on GitHub, with its images |
| `docs/parameters.md` | Every dialog field with its limits, default and tooltip |
| `assistant/` | An AI assistant skill that reads the features' logs and suggests what to change next, for Claude, ChatGPT and others |
| `LICENSE` | CC BY-NC-SA 4.0 |

## The five features

1. **B-Series Propeller**: a complete fixed or folding propeller with its hub, mount (bore, keyway or bolt circle), nose or tail cap, dish and pocket, and an optional printed hub core for a carbon layup. The folder has pins or captive bolts, bosses, tongues, stops, tail relief and a fold check.
2. **Universal B-Series Blade Engine**: one blade for a hub of your own.
3. **Propeller mold**: a printable matched mold for laying a fixed propeller up as one carbon part: two halves parted on the blades, hub sectors for the undercuts beside the roots, keys, a rod for the bore, pinch-off, clamps or a vacuum bag, release checks, the layup and cut templates.
4. **Propeller model from part**: measures a propeller drawn in any other CAD, imported as a STEP, so the mold can take it.
5. **Core forging mold**: a printed piston press for forging the hub core in chopped carbon, with the charge, the closing torque and a strength check.

Both propeller features carry an operating check: static thrust at the current limit, full throttle and steady cruise against a drag, the cavitation number, and for a folder the pin moment and stop bearing.

## Getting started

1. In a Part Studio, open *Add custom features*, search for **B-Series Propeller** and add the one by Jason Bird: the features appear in the toolbar. Right-click any of them and choose *Open linked document* for the public Onshape document with the guide. Or create your own Feature Studio and paste in `bseries_propeller.fs`.
2. Add **B-Series Propeller** and follow the guide's quick start. Fill in the Operating check with your motor and battery and turn on *Print to notices*.
3. Read the guide's operating check and folder sections before tuning anything.

## The AI assistant

`assistant/` holds a skill for AI assistants. Paste a log and it explains the results, checks them against rules of thumb, and recommends a few changes in the dialog's own labels, with the expected effect and the cost.

- **Claude**: add `assistant/propeller-design-assistant.skill`.
- **ChatGPT**: use `assistant/propeller-design-assistant-portable.md` as a custom GPT's instructions, with `docs/parameters.md` and `assistant/scripts/` as knowledge.
- **Others**: attach the portable file and `parameters.md` at the start of a chat.

## Licence

CC BY-NC-SA 4.0: use it, copy it, change it, not commercially, and share changes alike.

## Feedback

This repository: https://github.com/jb69/Foil-Design-Tool.
