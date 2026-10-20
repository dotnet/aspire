# A side-by-side comparison (silent, unbranded)

A before-and-after comparison, such as the startup time of a resource before and after a performance fix: a plain
title card, then both runs side by side, started together, each with its label and a timer that stops when its run
ends. No narration, no captions, no Aspire branding.

| Setting | Why |
|---|---|
| `"narration": false` | A silent video: each step lasts its `"seconds"` (the title card, 2 s) |
| `"branding": false` | Plain card and panels, without the logo, the header or the purple |
| `"kind": "compare"`, `"timer": true` | The runs side by side, with the time each one takes |
| `"sides"` with `"file"` | Existing recordings (such as terminal captures), in `runs/`, with the second their run starts and ends at |

To record the sides in a browser instead, write one clip per side in `recorder/Scenes.cs` with `scene.Mark("start")`
and `scene.Mark("end")` around the run (see "Recording one side of a comparison" in `references/recording.md`), and
use `{ "clip": "<clip>", "label": "Before" }` as the side. Leave out the title card for an even shorter video, or
turn branding back on for one that goes on aspire.dev.

Produce it with the usual commands: `narrate.py` (it only writes `durations.json`), then `assemble.py`.
