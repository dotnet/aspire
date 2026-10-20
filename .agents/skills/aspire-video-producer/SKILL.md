---
name: aspire-video-producer
description: Produces narrated Aspire videos (feature walkthroughs, dashboard tours, integration overviews, pull request demos) with one unified look and specification - 1080p, the Aspire logo, purple palette and Poppins font from aspire.dev, a neural voice picked from a shortlist, captions, slight dynamic zooms on demos, and renditions for GitHub, aspire.dev, YouTube or chat apps. Use when asked to record, narrate, or produce a video of Aspire, its dashboard or its CLI: narrated and branded by default, or silent, unbranded, or a side-by-side comparison (such as before and after a performance fix) when the user asks for a simpler video. Not for a GIF or a raw screen capture.
---

# Aspire Video Producer

Every Aspire video looks the same: the same branding, layout, captions, zooms, and encodings. The user picks the
narrator, the renditions, and whether sections get chapter cards (step 0).
This skill holds that specification and the scripts that apply it. It's adapted from the Orchard Core video producer
skill (BSD 3-Clause License, see "Orchard Core" in the repository's `THIRD-PARTY-NOTICES.TXT`). **Don't change the specification for a single
video.** If it must change, change it here, in `scripts/brand.py`, `scripts/narrate.py` and `scripts/assemble.py`,
and in `references/specs.md`, so every later video follows.

## When to use it

By default, a video is narrated and branded: walkthroughs, overviews, tours and demos with a voice, captions, cards
and slides. A simpler video turns those off, in the storyboard:

- **No narration** (`"narration": false`): a silent video, without captions; each step lasts its `"seconds"`.
- **No branding** (`"branding": false`): recordings fill the whole frame, and the cards are plain, without the logo
  or the purple.
- **A side-by-side comparison** (a `"compare"` clip): two runs started together, each with its label and, with
  `"timer": true`, the time it takes; from recordings or from existing video files. See `examples/side-by-side/`.

For a simpler video, ask only what it needs (usually the renditions), skip the voice and chapter-card questions, and
don't add cards, slides or narration the user didn't ask for. A GIF, or a raw screen capture with nothing added,
doesn't need this skill: make it the way the user asks. When it isn't clear which one the user wants, ask.

The worked example in `examples/dashboard-tour/` is the storyboard and slides of a short tour of the Aspire
dashboard, matching the example clip in `scripts/recorder/Scenes.cs`. Read it before writing a new one.

## The specification, in short

| | |
|---|---|
| Output | 1920x1080, 30 fps, H.264 (`-tune stillimage`), `+faststart`, in the renditions the user picks: `web` (1080p, **under 10 MB**, the default), `hd` (1080p, best quality), `light` (720p, under 5 MB) |
| Recording | Chromium at 1536x864 CSS pixels, device scale 1.25, dark color scheme, en-US, shown at 1728x972 in a purple frame |
| Branding | The aspire.dev dark theme (`#1F1E33`) with purple and magenta glows, the Aspire logo (symbol and white "Aspire" name), Poppins and Fira Code from `assets/fonts` |
| Colors | Purple `#512BD4` (pills), primary `#7455DD` (accents, frame, highlights, cursor clicks), secondary `#B9AAEE`, muted `#DCD5F6`, black `#1F1E33`, white |
| Voice | The narrator the user picks from a shortlist (default `en-US-AndrewMultilingualNeural`), rate `+2%`, through the `msedge-tts` npm package (`scripts/tts.mjs`) |
| Structure | Opening card, numbered chapters (an optional chapter card + slides or recordings), closing card |
| Zooms | Slight (1.25x), eased zooms on what each recorded step points at, rendered by `assemble.py` |
| Captions | WebVTT and SRT, from the narration, at most about 13 words each |

The full specification, with every value and the reasons behind them, is in `references/specs.md`.

## Prerequisites

- Python 3.11+ with `pillow pygments imageio-ffmpeg` (ffmpeg comes with `imageio-ffmpeg`), in a dedicated virtual
  environment: `python3 -m venv ~/.venvs/aspire-video && ~/.venvs/aspire-video/bin/pip install pillow pygments
  imageio-ffmpeg`. Run every script with `~/.venvs/aspire-video/bin/python`, wherever the steps below say `python`.
- Node.js 18+ with the `msedge-tts` npm package, for the voice, installed from the public npm registry next to the
  virtual environment: `mkdir -p ~/.venvs/aspire-video/node && npm install --prefix ~/.venvs/aspire-video/node
  --registry https://registry.npmjs.org msedge-tts@^2.1.0` (or set `ASPIRE_VIDEO_NODE` to another folder).
  Microsoft changes the speech service's handshake from time to time: when every request fails with a 403, update
  `msedge-tts` (2.1.0 has the October 2026 fix). It gets these fixes sooner than the Python `edge-tts` package.
- .NET 10 SDK, for the recorder (Playwright for .NET; run `pwsh <bin>/playwright.ps1 install chromium` once).
- The Aspire CLI and a throwaway sample app to record: see `references/recording.md`.
- Internet access for the voice (`msedge-tts` calls the Microsoft Edge speech service).
- Docker (or Podman) running, when the app to record has container resources.

Check these first. If one is missing, tell the user what to install rather than working around it.

`<skill>` below is the folder of this skill.

## Workflow

### 0. Understand the request

The user describes what the video is about; everything else comes from this skill. Work out:

- **The subject**: a feature, an integration, the dashboard, the CLI, or the change of a pull request.
- **The kind**: an overview (4 to 9 minutes, the outline of `references/storyboard.md`) or a pull request demo
  (shorter: an opening card, the recordings, a closing card).
- **What to show**: the app or the pull request branch to record, and the code for the slides.
- **Where it goes**: aspire.dev, a pull request, or only the file.

Ask only about what you can't infer. Then ask the user these three, together, as multiple-choice questions (with the
question tool when the session has one), unless they already said:

1. **Renditions** (several can be picked): `web`, 1080p under 10 MB, for GitHub pull requests and aspire.dev (the
   default); `hd`, 1080p at the best quality, for YouTube, conferences or editing; `light`, 720p under 5 MB, for
   Teams, Slack, email or phones. Recommend from where the video goes. See "Renditions" in `references/specs.md`;
   there is no vertical or 4K rendition.
2. **Voice**: the shortlist of "Voice" in `references/specs.md`, Andrew (the default) first. Offer a sample before
   they choose: `python <skill>/scripts/voices.py <workspace>/voices.mp4` reads one line in each voice of the
   shortlist (or in the voices given), on cards that name them. Send it, and use the voice they pick. For another
   voice or language, pick from `python <skill>/scripts/voices.py --list <prefix>` (such as `es-`): only the voices
   it lists work.
3. **Chapter cards**: a card between sections ("Chapter 2, The resources"), or none, for a quicker video. Recommend
   cards for overviews of 3 minutes or more, none for short videos and pull request demos.

Write the answers into the storyboard: `renditions`, `voice`, `chapterCards`. To test the skill itself, produce
`examples/dashboard-tour` as it is.

### 1. Create a workspace outside any repository

Use the session's scratchpad or a temp folder, never a repository: frames and audio are large, and the recorder must
not pick up a repository's build settings.

```text
<workspace>/
  storyboard.json   the chapters, clips and narration (see references/storyboard.md)
  slides.py         the slides, if the storyboard has "slide" clips
  recorder/         a copy of scripts/recorder, with your scenes in Scenes.cs
  app/              the throwaway Aspire app to record, if the video shows one
  audio/            generated: one mp3 per step
  durations.json    generated: the length of each step's narration
  clips/<clip>/     generated: the frames and timeline.json of each recording
  build/            generated: intermediate segments, and master.mp4
  <output>.mp4/.vtt/.srt   the result
```

Start from `examples/dashboard-tour/storyboard.json` and `slides.py`, and copy `scripts/recorder` into the workspace.

### 2. Write the storyboard

Read `references/storyboard.md` first. In short:

- Write for the long term: say "the Aspire dashboard", never "the new dashboard", "now", "in this release", or version
  numbers, so the video stays accurate for years.
- One idea per step, one to three sentences, spoken in the present tense. A step is what the viewer sees while it plays.
- Chapters: why it exists, each feature, how to configure it in the AppHost, how to adopt or extend it.

Show the user the outline and the narration before generating anything, unless they asked you to go straight
through: changing the text after recording means recording again.

### 3. Generate the narration

```bash
python <skill>/scripts/narrate.py <workspace>
```

It writes `audio/` and `durations.json`. Run it again after any change to the text: only the changed steps are
generated again. **Record after narrating**, since the recorder holds each step for the length of its narration.

If the voice mispronounces a word, add it to `SPOKEN` in `scripts/narrate.py` (for every video) or to `"spoken"` in
the storyboard (for this one). The captions keep the written form. **Have a person confirm a new spelling before
using it**: make a pronunciation sample, a few seconds per candidate spelling, and ask which option sounds right.
Rebuilding a whole video to try a spelling wastes minutes per attempt. Pass the storyboard, so the sample uses the
video's voice and rate (or `--voice` and `--rate`): a spelling that works for one voice can fail with another.

```bash
python <skill>/scripts/pronounce.py --storyboard <workspace>/storyboard.json otlp.mp4 "The dashboard receives {} telemetry from every resource." "OTLP" "O.T.L.P." "O-T-L-P" "O T L P"
```

Letters said one by one are written as an abbreviation with periods, such as "A.S.P.": the voice spells it at an
even pace. With spaces ("A S P"), the letters run together; with commas ("A, S, P"), the pauses are uneven.
"ASP.NET" is spoken as "A.S.P. .NET".

### 4. Record the clips

Start the sample app (`aspire run`), then write one method per "clip" in `recorder/Scenes.cs`, with one
`scene.StepAsync("<step id>", ...)` per step, in the storyboard's order. See `references/recording.md` for the app
to record, the scene API, and the pitfalls. Wait until every resource is Running and healthy, and generate some
traffic so logs, traces and metrics have data. Then:

`dotnet build` prints where the recorder is: `Recorder -> <bin>/Recorder.dll`. Each workspace's copy builds into its
own folder, so several videos can be recorded side by side.

```bash
cd <workspace>/recorder && dotnet build
export VIDEO_ROOT=<workspace> VIDEO_BASE_URL=https://localhost:17010
export VIDEO_LOGIN_URL='<the dashboard login URL that aspire run prints>'
<bin>/Recorder setup          # logs in to the dashboard, once per run of the app
<bin>/Recorder probe / "..."  # checks each selector of Scenes.cs before recording
<bin>/Recorder record all     # or the names of some clips
python <skill>/scripts/review.py <workspace> <clip>
```

Look at every contact sheet (`shots/<clip>.jpg`) before assembling: the frame at the end of each step must show what
its narration describes, and no caption may cover what the narration names.

To make several versions of one video (30 seconds, 1 minute, 3 minutes), use one workspace per version, with its own
storyboard, slides and recorder, and record them all from the same running app.

### 5. Assemble

```bash
python <skill>/scripts/assemble.py <workspace>
```

It writes one file per rendition (`<output>.mp4` for `web`, `<output>-hd.mp4`, `<output>-light.mp4`), and prints
the length and the size of each. If one is over its limit, shorten the video, or raise its CRF one step at a time
(`CRF=34` for `web`, `CRF_LIGHT=31` for `light`) and check that text stays readable. Extract a few frames with ffmpeg
and look at them: a recording, a slide with code, a card, and a recording while it's zoomed in.

### 6. Publish

- **aspire.dev** (`microsoft/aspire.dev`, an Astro Starlight site): put `<name>.mp4`, `<name>.vtt` and a poster
  (`build/<opening card>.png` saved as JPEG) in `src/frontend/public/videos/`, and embed it under the introduction of
  the `.mdx` page:

  ```html
  <video controls preload="metadata" width="100%" poster="/videos/<name>.jpg">
      <source src="/videos/<name>.mp4" type="video/mp4" />
      <track kind="captions" src="/videos/<name>.vtt" srclang="en" label="English" />
      Your browser does not support embedded videos. <a href="/videos/<name>.mp4">Download the video</a>.
  </video>
  ```

  Don't use the `LoopingVideo` component for narrated videos: it autoplays muted, without captions. Introduce the
  video with one sentence that says what it covers, without "new". Build the site locally, following the
  repository's README, and check the page.
- **Pull request**: GitHub only accepts videos through its web uploader (drag and drop, or the attachment button
  of the description editor), up to 10 MB on free plans. The upload gives a `https://github.com/user-attachments/assets/...`
  URL: put it alone on a line near the top of the description, so GitHub shows a player.

### 7. Clean up

Stop the app you recorded (`aspire stop` from `<workspace>/app`, or Ctrl+C in the terminal running `aspire run`).
Its containers can outlive it: check `docker ps -a` and remove them (`docker rm -f <name>`, such as `cache-<suffix>`).
Keep the throwaway project and the workspace until step 8 has copied their sources and the user has accepted the
video, in case a step must be re-recorded; remove the project after that.

### 8. Deliver

Give the user `<output>.mp4` (send the file when the session can), and end with a summary that lists, for each video:

- **A link to each rendition**, as a clickable link to its absolute path (`[<output>.mp4](/abs/path/<output>.mp4)`),
  with its length, its resolution, its size, its voice, and its language. Copy the videos and their captions out of a temporary workspace, to a
  folder the user can open (such as `<working directory>/<name>-videos/`), and link to the copies: a temporary folder is
  temporary.
- **Links to its captions**: `<output>.vtt` and `<output>.srt`.
- **The path to its workspace**, with links to `storyboard.json` (the narration), `slides.py` if any, and the
  recorder's `Scenes.cs`, so the user can change the text or a clip and produce the video again. When the workspace
  is in a temporary folder, also copy its sources next to the videos (`<name>-videos/workspaces/`: the storyboards,
  `slides.py`, `recorder/` without `bin` and `obj`, and the app without `node_modules`, `bin`, `obj` and
  `aspire-output`), and link to both: the copy outlives the session, the workspace keeps the frames and audio.
- **Shared files**, when several videos use them: the sample app, the recorder, and any settings or AppHost versions
  the clips copy in.

End with the commands that produce a video again from its workspace (`narrate.py`, `Recorder record`, `assemble.py`).

## Improving the skill

When a script fails, a selector breaks with a newer dashboard, a slide overflows, or a reference is wrong or unclear,
fix it here, in the skill (scripts, references or example), not only in the workspace, so the next video benefits.
When an external tool misbehaves (VS Code, the Aspire CLI, a package, the voice service), add the symptom, the cause and
the fix to `references/troubleshooting.md`.
Tell the user what you changed. Never change the specification (colors, fonts, layout, voice, encoding) without the
user's agreement.

## References

- `references/specs.md`: the full specification.
- `references/storyboard.md`: the storyboard format, and how to write the narration and the slides.
- `references/recording.md`: the app to record, the recorder, the scene API, and pitfalls.
- `references/troubleshooting.md`: problems with external tools (VS Code in the browser, the Aspire CLI, NuGet
  packages, the dashboard, the voice), with their fixes. Check it before debugging one, and add what you find.
- `scripts/brand.py`: the branding, the layout, and the slide building blocks.
- `scripts/pronounce.py`: a pronunciation sample, to confirm the spelling of a word the voice misreads.
- `scripts/voices.py`: a voice sample, to choose the narrator.
- `assets/`: the Aspire logo (from aspire.dev) and the Poppins and Fira Code fonts (SIL Open Font License).
- `examples/dashboard-tour/`: a complete storyboard and slides.
- `examples/side-by-side/`: a silent, unbranded before-and-after comparison, with timers.
- `examples/aspire-introduction/`: a video with no slides: the AppHost typed in VS Code with IntelliSense, `aspire run`
  in its terminal, the dashboard, and `aspire deploy`, in English and Spanish. **Start from it for any video
  that shows VS Code or a terminal.**
