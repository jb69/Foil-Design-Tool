# Propeller design assistant

An AI assistant skill for the Onshape B-Series Propeller Feature Studio. Paste what the features print (the propeller's log and operating check, the mold's notice, the from-part or forging log) and the assistant explains what it means and which fields to change next, in the tool's own labels.

It's written as a Claude skill and works best there, but everything in it is plain Markdown and standard-library Python, so it works with other assistants too.

## Claude

- **Claude desktop, claude.ai or Claude Code:** add `propeller-design-assistant.skill` as a skill. Claude loads it whenever you paste propeller output and can run the script itself.
- **A Claude Project instead:** add `SKILL.md` as the project instructions and the `references/` files as project knowledge.

## ChatGPT and others

- **Custom GPT:** paste `propeller-design-assistant-portable.md` into the instructions, and upload `references/parameters.md` and `scripts/read_prop_log.py` as knowledge. With Code Interpreter on, it can run the script.
- **Plain chat, Gemini, a local model:** attach `propeller-design-assistant-portable.md` and `parameters.md` at the start of the conversation, then paste your log.

## What to paste

Turn on *Print to notices* on the B-Series Propeller (or *Log inputs* on the mold features), regenerate, and copy the notices panel, or the `propLog`, `moldLog`, `partModelLog` or `forgeLog` variable from the Part Studio's variable table. Fill in the Operating check group first: without Motor kV there is no check to read.

## Files

| File | What |
|---|---|
| `SKILL.md` | The instructions: workflow, what to check, response style |
| `references/parameters.md` | Every dialog field with limits, default and tooltip, generated from the Feature Studio |
| `references/outputs.md` | What each log and notice line means |
| `references/targets.md` | Rules of thumb for the operating check, the geometry, the folder and the molds |
| `references/levers.md` | What each field changes, and the cost |
| `references/model-limits.md` | What the operating check and the molds' figures can't know |
| `scripts/read_prop_log.py` | Summarises a log, or compares two |
| `propeller-design-assistant-portable.md` | Everything except parameters.md in one file, for assistants without skills |
