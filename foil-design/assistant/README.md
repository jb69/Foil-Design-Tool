# Foil design assistant

An AI assistant skill for the Onshape Foil design Feature Studio. Paste what the features print, the notices, the console log or the CSV, and the assistant explains what it means and which fields to change next, in the tool's own labels.

It's written as a Claude skill and works best there, but everything in it is plain Markdown and standard-library Python, so it works with other assistants too.

## Claude

- **Claude desktop, claude.ai or Claude Code:** add `foil-design-assistant.skill` as a skill. Claude loads it whenever you paste foil output and can run the scripts itself.
- **A Claude Project instead:** add `SKILL.md` as the project instructions and the `references/` files as project knowledge.

## ChatGPT

- **Custom GPT:** paste `foil-design-assistant-portable.md` into the instructions, or upload it as knowledge with a one-line instruction ("Follow foil-design-assistant-portable.md"). Upload `references/parameters.md` and the `scripts/` folder as knowledge too. With Code Interpreter on, it can run the scripts.
- **Plain chat:** attach `foil-design-assistant-portable.md` and `parameters.md` at the start of the conversation, then paste your log.

## Other assistants

Gemini, Copilot, a local model or anything that takes a file or a long prompt: give it `foil-design-assistant-portable.md` as the system prompt or first message, and `parameters.md` if it has room. Without code execution the assistant follows the manual method in the file instead of the scripts.

## What to paste

Turn on *Log inputs* on every feature, regenerate, and copy the whole FeatureScript console. That one paste carries every input and output, so every recommendation can be checked. Add the notice text of any feature that shows a warning.

## Files

| File | What |
|---|---|
| `SKILL.md` | The instructions: workflow, how to read every output, calibration, response style |
| `references/parameters.md` | Every dialog field with limits, default and tooltip, generated from the Feature Studio |
| `references/outputs.md` | Every console line and warning, with its fix |
| `references/targets.md` | Target ranges by discipline, structure and molds |
| `references/levers.md` | What each change does, and the trade-offs |
| `references/propulsion.md` | eFoil and foil assist: prop, motor, battery, powered trim |
| `references/model-limits.md` | What the model can't know, and how to calibrate it |
| `references/examples.md` | Worked diagnosis sessions |
| `scripts/*.py` | Run summary and comparison, pitch sweep, polar fit, DXF cut templates |
| `foil-design-assistant-portable.md` | Everything except parameters.md in one file, for assistants without skills |

## Keeping it current

`references/parameters.md` and `foil-design-assistant-portable.md` are generated from the Feature Studio by the maintainer tools in the repository's `tools/` folder. Use the files from the same release as your Feature Studio; the build number is in both.

Licence: CC BY-NC-SA 4.0, JB69.
