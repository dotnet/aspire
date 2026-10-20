# The storyboard

`storyboard.json` describes the whole video: its chapters, the clips of each chapter, and the narrated steps of each
clip. `narrate.py`, the recorder and `assemble.py` all read it.

```json
{
  "title": "The Aspire dashboard",
  "output": "dashboard-tour",
  "voice": "en-US-AndrewMultilingualNeural",
  "renditions": ["web", "light"],
  "chapterCards": true,
  "spoken": { "OTLP": "O.T.L.P." },
  "chapters": [
    {
      "id": "00", "title": "The Aspire dashboard", "subtitle": "See every resource of your app, as it runs",
      "clips": [ { "id": "00-intro", "kind": "card", "steps": [ { "id": "intro", "text": "..." } ] } ]
    },
    {
      "id": "01", "title": "Why the dashboard", "subtitle": "The problem it solves",
      "clips": [ { "id": "01-why", "kind": "slide", "steps": [ { "id": "why-1", "text": "..." } ] } ]
    },
    {
      "id": "02", "title": "The resources", "subtitle": "Everything the AppHost runs",
      "clips": [ { "id": "02-resources", "kind": "clip", "steps": [ { "id": "resources-1", "text": "..." } ] } ]
    }
  ]
}
```

- `output`: the file name of the result, without extension: `<output>.mp4`, `.vtt`, `.srt`.
- `spoken`: pronunciation fixes for this video only (see `SPOKEN` in `scripts/narrate.py` for those of every video).
- `voice`, `rate` (optional): the narrator the user picked (see "Voice" in `references/specs.md`), such as
  `en-US-AvaMultilingualNeural`, or a voice for a video in another language (`es-ES-AlvaroNeural`). For English with a French accent, use a French
  single-language voice, such as `fr-FR-HenriNeural`: a multilingual voice (`fr-FR-RemyMultilingualNeural`) switches
  to a native accent in English. Without it, the
  default voice.
- `renditions` (optional): the renditions to encode, from the table of `references/specs.md`: `"web"`, `"hd"`,
  `"light"`. The default is `["web"]`.
- `chapterCards` (optional): `false` for no chapter card between sections. The default is `true`.
- `zoom` (optional): `false` to render the recordings without their dynamic zooms. The default is `true`.
- `chapter` (optional): the word for "Chapter" on chapter cards and pills, in the language of the video ("Capítulo").
- `chapterCardSeconds` (optional): how long each chapter card stays on screen, when the user wants quicker (or
  slower) transitions than the default 2.6 s of `references/specs.md`, such as `1.2`.
- Chapter `id`: two digits. Chapters made only of `card` clips (the opening `00` and the closing `99`) get no
  chapter card; every other chapter is numbered "Chapter N" from its id.
- Clip `kind`:
  - `card`: the opening or closing card, with the chapter's title and subtitle, and one narrated step.
  - `slide`: one image per step, drawn by `slide(step_id, chapter_title, label)` in the workspace's `slides.py`.
  - `clip`: a recording, made by the method of the same name in `recorder/Scenes.cs`.
- Step `id`: unique in the video; the recorder marks the time each one starts.

## Structure

A feature or integration overview usually follows this outline. Keep it between 4 and 9 minutes.

1. **Opening card** (`00`): what the subject is, in one or two sentences, and what the video covers.
2. **Why** (slides): the problem it solves, then how it solves it.
3. **The features** (recording): where they are in the dashboard, what each one does.
4. **Using it** (recordings): one chapter per task, in the order a developer would do them.
5. **Configuring it** (slides with code): the AppHost code (`AppHost.cs`, or `apphost.ts` for a TypeScript AppHost),
   the client integration in the service, `appsettings.json`, CLI commands.
6. **Deploying or extending it** (slides with code): the few steps a developer follows, with real API names.
7. **Closing card** (`99`): one sentence that sums it up, then "Thanks for watching."; the subtitle is `aspire.dev`.

A pull request demo can be shorter: an opening card, the recordings, and a closing card.

### Short versions

For a video of a given length, add up the timing of `references/specs.md` before recording: each card takes its
narration plus 2.4 s, each chapter card 2.6 s, each recording its steps (narration plus 0.45 s each) plus 1.7 s, and
each slide clip its narration plus about 1.6 s. Run `narrate.py`, compute the length, and adjust the text until it
fits, then record.

| Length | Outline |
|---|---|
| 30 seconds | Opening card (one sentence), one recorded chapter of two steps, closing card ("Get started at aspire.dev.") |
| 1 minute | Opening card, the AppHost (one code slide), the dashboard (one recording of two or three steps), closing card |
| 3 minutes | The full outline above, one or two steps per chapter |

## Cards

The opening and closing cards show the Aspire logo above the title: don't use "Aspire" alone as the title, or the
card reads "Aspire Aspire". Name what the video is, such as "An introduction to Aspire" or "The Aspire dashboard".

## Writing the narration

- **Timeless.** Say "the Redis integration", never "the new Redis integration", "now", "this release", "we added",
  or a version number. Describe what is, not what changed, so the video stays accurate for years.
- **One idea per step**, one to three sentences, 3 to 16 seconds. The viewer sees the step's actions while it plays,
  so describe what's on screen, as it happens.
- **Plain, direct, present tense.** Address the viewer as "you", or use "let's" for actions in a recording.
- **Name UI elements as they're labeled** on screen, so the viewer can find them: "Resources", "Console",
  "Structured", "Traces", "Metrics".
- **Use Aspire's terms**: "the AppHost", "a resource", "an integration" (hosting or client), "the dashboard", "the
  Aspire CLI". Say "Aspire", not ".NET Aspire".
- **Acronyms**: the voice reads most of them correctly (API, HTTP, SQL). For one that must be spelled out letter by
  letter, add a `spoken` entry that writes it as an abbreviation with periods, such as "A.S.P. .NET" for "ASP.NET",
  and have a person confirm it with a sample from `scripts/pronounce.py`.
- **Spell out** what a voice could misread: say "Add Redis" for `AddRedis()`, "With Reference" for
  `WithReference()`, "Wait For" for `WaitFor()`. Check every name of the narration for pronunciation, and add fixes
  to `spoken`.
- **No marketing words**: no "powerful", "seamless", "simply", "easily", "magic".
- Avoid numbers that change, such as counts of integrations or settings, unless the video shows them.

## Writing the slides

`slides.py` exposes `slide(step_id, chapter_title, label)`, which returns a 1920x1080 PIL image built with the
helpers of `scripts/brand.py` (`assemble.py` puts it on the import path):

```python
from brand import W, bullets, draw_code, note_cards, slide_base

def slide(step_id, chapter_title, label):
    if step_id == "why-1":
        image = slide_base(chapter_title, label, "Many processes, many consoles")
        bullets(image, PROBLEMS, active={0, 1})   # the cards of this step are bright, the others dimmed
        return image
    if step_id == "apphost-1":
        image = slide_base(chapter_title, label, "The AppHost describes the app")
        draw_code(image, (96, 240, 1180, 1030), "AppHost.cs", "csharp", APPHOST, types=TYPES, size=26)
        return image
    raise SystemExit(f"No slide for {step_id}")
```

- Use only the helpers of `brand.py` and its colors and fonts: don't introduce new colors, fonts or layouts.
- `bullets`: up to four cards of (icon, title, text); icon is a short text such as "1", or `"check"`. Spread the
  cards over several steps with `active` to follow the narration.
- `draw_code`: real, compilable code from aspire.dev or the templates, trimmed to what the narration talks about;
  mark what's omitted with `// ...`. Pass the type names to `types`, so they are colored as types. Languages:
  `csharp`, `typescript`, `javascript`, `json`, `yaml`, `bash`, `powershell`, `xml`, `html`.
- `note_cards`: up to four (title, text) cards next to a code panel.
- `bullets`, `note_cards` and `draw_code` stop with a "Slide layout" error when a text runs off its card, its panel or
  the slide: shorten the text, or give the panel more room. Size code panels to their code, so they don't end in a
  large empty area.
- Keep every text at 21 px or more, and render each slide once (`python -c "import slides; slides.slide('why-1', 'T', 'Chapter 1').save('x.png')"`
  from the workspace, with the skill's `scripts` folder on `PYTHONPATH`) to check nothing overflows.
