# Foil design for Onshape

Parametric hydrofoil design, analysis and printable molds, as six custom features in one Onshape Feature Studio. Give it an area, an aspect ratio and a handful of shape choices, and it builds the front wing, stabiliser, mast and fuselage, runs a speed sweep of lift, drag, trim, stability, structure and power, and then generates 3D-printable mold shells, a layup schedule and fabric cutting templates to build the parts in carbon.

By JB69. Build 197.

> **Read this first.** This is a modelling exercise by someone who designs props, not a foil builder. It started as a side quest to the B-series propeller blade generator: sizing a prop needs a drag curve, a drag curve needs a foil, and a nine-hour flight turned that into this. Every number it produces is a steady-state estimate: one speed, one weight, flat water, a rider who doesn't move. Trust the trend from one variant to the next. Treat absolute numbers, stall speed especially, as optimistic until you've calibrated them against a foil you've ridden. The guide explains how.

## What's here

| Folder / file | What it is |
|---|---|
| `foil_design.fs` | The Feature Studio source. Only needed for your own copy; the published features are added in Onshape by searching custom features for **Foil designer**. |
| `docs/Foil_Design_Tool_Guide.pdf` | The user guide: every feature, every parameter, how to read the results, design and building notes, a worked example |
| `docs/manual.md` | The guide's source, readable here on GitHub |
| `foil-design.zip` | All of the above as one download: [foil-design.zip](foil-design.zip) |
| `docs/parameters.md` | Every dialog field with its limits, default and tooltip |
| `assistant/` | An AI assistant skill that reads the tool's output and suggests what to change next, for Claude, ChatGPT and others |
| `examples/` | A full console log and speed sweep from the worked example, to try the assistant on |
| `tools/` | Maintainer scripts that rebuild the parameter reference, the portable assistant file and the PDF guide |
| `LICENSE` | CC BY-NC-SA 4.0 |

## Why there's a PDF

Onshape only publishes custom features, so that they appear in its custom feature search, if the document is public, each feature has a description, and the document includes a PDF explaining how to use them. `docs/Foil_Design_Tool_Guide.pdf` is that PDF: it lives in the public Onshape document beside the Feature Studio, and right-clicking a feature in Onshape and choosing *Open linked document* takes you to it.

## The six features

Used in this order in a Part Studio:

1. **Foil wing**: area-first planform with superellipse, linear or double-taper chord, NACA 4-digit or tabulated sections (E817, E818, NACA 63-412, Clark Y) with a separate tip section blended along the span, sweep, tip drop, winglets, leading-edge bumps, washout, optional tube socket for a round fuselage.
2. **Foil stabiliser**: the same geometry, placed by arm, optionally linked to the wing, with a production-style mount pad, tail fairing and bolt pattern pinned to its pivot. Double taper gives the full-base outline of current production stabs.
3. **Foil performance**: no geometry. A speed sweep with lifting line, trim, stall, static margin, drag including mast, pod and fuselage, root bending, and an optional propulsion group for eFoil and foil assist.
4. **Foil mast**: tapered, thickened toward the plate, root fillet, optional motor pod, board plate.
5. **Foil fuselage**: a round tube that spigots into the wing and seats on the stab's mount.
6. **Foil mold**: two-part printable molds (three-part for the mast, its plate tray sized from the plate as modelled) with keys, pinch-off groove, plugs and bolt cones, tiled to your printer, closed with clamps or in a vacuum bag (vented groove, rounded edges, closing force reported), plus the layup with a tow, foam, monolithic or printed core (the printed core generated as a part), material and resin estimate, a stiffness check, cut templates, and a mold for the foam core itself.

## Getting started

1. In a Part Studio, open *Add custom features*, search for **Foil designer** and add it: the six features appear in the toolbar. Right-click any of them and choose *Open linked document* for the public Onshape document, which has the Feature Studio, an example Part Studio and the guide. Or download [foil-design.zip](foil-design.zip), create your own Feature Studio and paste in `foil_design.fs`.
2. In a Part Studio, add the features in the order above. Start from defaults and change one or two things at a time.
3. Turn on *Log inputs* on every feature and open the FeatureScript console. That text is what you read, compare and paste when asking for help.
4. Read the guide, chapters 2 to 4, before tuning anything.

## The AI assistant

`assistant/` holds a skill for AI assistants. Paste your console log and it explains the results, checks them against target ranges, and recommends a few changes in the dialog's own labels, with the expected effect and the cost.

- **Claude**: add `assistant/foil-design-assistant.skill`.
- **ChatGPT**: use `assistant/foil-design-assistant-portable.md` as a custom GPT's instructions, with `docs/parameters.md` and `assistant/scripts/` as knowledge and Code Interpreter on.
- **Others**: attach the portable file and `parameters.md` at the start of a chat.

Details are in `assistant/README.md` and section 10.3 of the guide.

## Printing the molds

Print each mold tile standing on its spanwise end, so every layer carries the section profile and the cavity is drawn smoothly. PLA works for wing and stab molds with a slow hardener and a room-temperature cure; PETG for the mast mold and anything post-cured. Settings and finishing are in section 6.4 of the guide.

## Feedback

This repository: https://github.com/jb69/Foil-Design-Tool.

Discussion is on foil.zone: [Going down a rabbit hole: parametric foil design and printable molds in Onshape](https://foil.zone/t/going-down-a-rabbit-hole-parametric-foil-design-and-printable-molds-in-onshape/27227). Measured drag, deflection or take-off speeds on a known foil are the most useful thing anyone can send: they're how the model gets calibrated.

## Licence

Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0). You may share and adapt this work for non-commercial purposes, with credit to JB69, under the same licence. See `LICENSE`.
