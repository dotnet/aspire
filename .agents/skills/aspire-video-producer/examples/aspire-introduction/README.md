# An introduction to Aspire (3 minutes, VS Code and live demo)

A video with no slides: the AppHost in VS Code (typed with IntelliSense), `aspire run` in its terminal, the dashboard
it opens, and `aspire deploy` to Docker Compose. Made in three versions from the same recordings' code: English
(`en-US-AvaMultilingualNeural`), English with a French accent (`fr-FR-HenriNeural` reading English), and Spanish
(`es-ES-AlvaroNeural`, `storyboard.es.json`, rate `+6%`).

**Start here for any video that shows VS Code or a terminal**, and read the VS Code section of
`references/troubleshooting.md`.

| File | What it is |
|---|---|
| `storyboard.json`, `storyboard.es.json` | Chapters 00 to 99, with `voice`, `rate`, `chapter` and `spoken` |
| `recorder/Scenes.cs` | The clips. Reusable helpers: `OpenVsCodeAsync` (seeds the browser's user settings, closes editors), `TypeWithSuggestionAsync`, `HighlightLinesAsync`, `ClickLineEndAsync`, `OpenTerminalAsync`, `TypeInTerminalAsync`, `ExpectAppHostAsync`, `WaitForHealthyAsync`, `DismissUnsecuredBannerAsync`, `Text(en, es)` for localized highlight captions |
| `vscode-user-settings.json` | The user settings written into IndexedDB before each VS Code scene (trust off, TypeScript on the server) |
| `app/vscode-workspace-settings.json` | The app's `.vscode/settings.json`: look of the editor, terminal profile, `files.exclude` |
| `app/apphost.start.mts`, `app/apphost.final.mts` | The AppHost before and after the typing of chapter 1 (the clip copies the start version in, and checks the result) |
| `app/catalog-index.ts`, `app/Api-Program.cs` | The Node.js catalog (Redis from `CACHE_URI`) and the .NET API (Npgsql, OpenTelemetry, creates its database outside `aspire run`) |

The app itself comes from `aspire new aspire-ts-starter`, with `aspire add Aspire.Hosting.Redis`,
`Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.Docker`. Environment of the recorder: `VIDEO_ROOT` (the workspace of
one version), `VIDEO_SHARED` (the folder with `app/`, the two AppHost versions and `vscode-user-settings.json`),
`VIDEO_LANG` (`en` or `es`), and `VIDEO_VSCODE_URL` (default `http://localhost:8123`). `VIDEO_DEBUG_SHOT=<png>` saves
a screenshot of VS Code just before the capture starts.
