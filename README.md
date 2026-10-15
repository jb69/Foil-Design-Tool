# Foil Design Tool and B-Series Propeller for Onshape

Two free, parametric toolsets for Onshape, for people who design and build their own hydrofoils and propellers. Both run in a free Onshape account, both make the 3D-printable molds to build the parts in carbon, and both come with a user guide and an AI assistant that reads their output and suggests what to change next.

By JB69. Licence CC BY-NC-SA 4.0: use them, copy them, change them, not commercially, and share changes alike.

| | Foil design | B-Series Propeller |
|---|---|---|
| Search custom features in Onshape for | **Foil designer** | **B-Series Propeller** |
| Features | Foil wing, Foil stabiliser, Foil performance, Foil mast, Foil fuselage, Foil mold | B-Series Propeller, Universal B-Series Blade Engine, Propeller mold, Propeller model from part, Core forging mold |
| Guide | [Foil_Design_Tool_Guide.pdf](foil-design/docs/Foil_Design_Tool_Guide.pdf) | [BSeries_Propeller_Guide.pdf](propeller/docs/BSeries_Propeller_Guide.pdf) |
| Everything in one download | [foil-design.zip](foil-design.zip) | [propeller.zip](propeller.zip) |
| Browse the files | [foil-design/](foil-design/) | [propeller/](propeller/) |
| AI assistant | `foil-design-assistant`, in the zip | `propeller-design-assistant`, in the zip |

## Foil design

Give it an area, an aspect ratio and a handful of shape choices, and it builds the front wing, stabiliser, mast and fuselage, runs a speed sweep of lift, drag, trim, stability, structure and power (with a propulsion group for eFoil and foil assist), and generates printable mold shells, a layup schedule and fabric cutting templates.

1. **Foil wing**: area-first planform, NACA or tabulated sections with a tip section blended along the span, sweep, tip drop, washout, and the joint to the fuselage.
2. **Foil stabiliser**: the same geometry, placed by arm, with a mount pad.
3. **Foil performance**: no geometry; the speed sweep and the propulsion group.
4. **Foil mast**: tapered, with fillet, optional motor pod and board plate.
5. **Foil fuselage**: a round tube or a hub body, seated on the stab's mount.
6. **Foil mold**: two-part printable molds with keys, pinch-off, plugs, tiling, clamps or bag, the layup, cut templates and core molds.

> Every number it produces is a steady-state estimate. Trust the trend from one variant to the next; treat absolute numbers, stall speed especially, as optimistic until you've checked them against a foil you've ridden. The guide explains how.

## B-Series Propeller

Give it a diameter, a pitch, a blade area and a blade count and it builds a fixed or folding propeller on Wageningen B-series blades with its hub and mount, checks it against your motor and battery, and makes the mold to lay it up as one carbon part.

1. **B-Series Propeller**: a complete fixed or folding propeller with its hub and mount (bore, keyway or bolt circle), and an optional printed hub core for a carbon layup. The folder has pins or captive bolts, bosses, stops, tail relief and a fold check.
2. **Universal B-Series Blade Engine**: one blade for a hub of your own.
3. **Propeller mold**: a printable matched mold for a fixed propeller, with hub sectors for the undercuts beside the roots, pinch-off, clamps or a vacuum bag, release checks, the layup and cut templates.
4. **Propeller model from part**: measures a propeller drawn in any other CAD, imported as a STEP, so the mold can take it.
5. **Core forging mold**: a printed piston press for forging the hub core in chopped carbon.

> The operating check is an open-water estimate from the B-series polynomials, about ±20 %. Use it to compare designs and to catch a mis-sized drive before a print; trust your own logs over it.

## Getting started

1. In any Part Studio, open *Add custom features*, search for **Foil designer** or **B-Series Propeller**, and add the one by Jason Bird. Copies of older builds exist; the one by Jason Bird is current. Right-click any feature and choose *Open linked document* for the public Onshape document with its guide.
2. Read the guide's quick start, then change one or two things at a time.
3. Turn on *Log inputs* (foil) or *Print to notices* (propeller). That text is what you read, compare and paste when asking for help.

## The AI assistants

Each zip has an `assistant/` folder with a skill for AI assistants. Paste a log and it explains the results, checks them against target ranges, and recommends a few changes in the dialog's own labels, with the expected effect and the cost.

- **Claude**: add the `.skill` file.
- **ChatGPT**: use the `-portable.md` file as a custom GPT's instructions, with `parameters.md` as knowledge.
- **Others**: attach the portable file and `parameters.md` at the start of a chat.

## What's in each zip

| `foil-design.zip` | `propeller.zip` |
|---|---|
| `foil_design.fs`, the Feature Studio source | `bseries_propeller.fs`, the Feature Studio source |
| `docs/`: the guide PDF and its source, the charts, every parameter | `docs/`: the guide PDF and its source, its images, every parameter |
| `assistant/`: the skill, its scripts, the portable version | `assistant/`: the skill, its log reader, the portable version |
| `examples/`: a full console log and speed sweep | |
| `LICENSE` | `LICENSE` |

The Feature Studio source is only needed for your own copy; the published features are added in Onshape by search.

## Feedback

Issues and suggestions here, or on foil.zone in "Going down a rabbit hole - Parametric foil design and printable molds in Onshape" (the foil tool) and "Three-Blade Folding Prop Experiment" (the propeller).
