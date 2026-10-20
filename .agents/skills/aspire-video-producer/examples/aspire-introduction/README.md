# An introduction to Aspire (3 minutes, VS Code and live demo)

A video with no slides: the AppHost in VS Code (typed with IntelliSense), `aspire run` in its terminal, the dashboard
it opens, and `aspire deploy` to Docker Compose. Made in two versions from the same recordings' code: English
(`en-US-AvaMultilingualNeural`) and Spanish
(`es-ES-AlvaroNeural`, `storyboard.es.json`, rate `+6%`).

**Start here for any video that shows VS Code or a terminal**, and read the VS Code section of
`references/troubleshooting.md`.

This folder is the shared folder of the recorder (`VIDEO_SHARED`): the app, the two versions of its AppHost, and the
VS Code user settings. Each version of the video has its own workspace (`VIDEO_ROOT`) with its storyboard and a copy of
the recorder.

| File | What it is |
|---|---|
| `storyboard.json`, `storyboard.es.json` | Chapters 00 to 99, with `voice`, `rate`, `chapter` and `spoken` |
| `recorder/Scenes.cs` | The clips. Reusable helpers: `OpenVsCodeAsync` (seeds the browser's user settings, closes editors), `TypeWithSuggestionAsync`, `HighlightLinesAsync`, `ClickLineEndAsync`, `OpenTerminalAsync`, `TypeInTerminalAsync`, `ExpectAppHostAsync`, `WaitForHealthyAsync`, `DismissUnsecuredBannerAsync`, `Text(en, es)` for localized highlight captions |
| `vscode-user-settings.json` | The user settings written into IndexedDB before each VS Code scene (trust off, TypeScript on the server) |
| `apphost.start.mts`, `apphost.final.mts` | The AppHost before and after the typing of chapter 1 (the clip copies the start version into `app/apphost.mts`, and checks the result) |
| `app/` | The sample app, "Contoso": a TypeScript AppHost (`aspire.config.json`), the Node.js `catalog` (Express, Redis from `CACHE_URI`), the .NET `Api` (Npgsql, OpenTelemetry, creates its database outside `aspire run`), and the Vite `frontend` |
| `app/.vscode/settings.json` | The look of the editor, the terminal profile (with anonymous dashboard access), `files.exclude` |

## Set up the app

The app started from `aspire new aspire-ts-starter`, with `aspire add Aspire.Hosting.Redis`,
`Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.Docker`; `app/` holds its sources as recorded, without build outputs
or lock files. It needs Node.js 22.18 or later: the AppHost starts the catalog with `node src/index.ts`, which relies
on Node's built-in TypeScript support. Set it up outside any repository, so no repository's build settings apply:

```bash
cp -R <skill>/examples/aspire-introduction <shared>
cd <shared>/app
cp ../apphost.final.mts apphost.mts      # the clips copy the right version in before recording
npm install && (cd catalog && npm install) && (cd frontend && npm install)
aspire restore                           # generates .aspire/modules, the typed AppHost API
aspire run                               # once, to check that the six resources are Running and Healthy
```

The recorder expects fixed addresses, set in the app:

| Address | Set in |
|---|---|
| Dashboard `https://localhost:17010` | `aspire.config.json`, `profiles.https.applicationUrl` |
| API `https://localhost:7180` (traffic before recording) | `Api/Properties/launchSettings.json` |
| VS Code `http://localhost:8123` | `code serve-web --port 8123 --without-connection-token`, started from `<shared>/app` |

The terminal profile in `app/.vscode/settings.json` is for macOS (`terminal.integrated.profiles.osx`, zsh); on Windows
or Linux, add the same profile under `profiles.windows` or `profiles.linux`, with its `env`. Run `aspire deploy` once
before recording chapter 3: the first deploy builds the images, and takes much longer than the others.

## Record

Environment of the recorder: `VIDEO_ROOT` (the workspace of one version), `VIDEO_SHARED` (`<shared>`), `VIDEO_LANG`
(`en` or `es`), and `VIDEO_VSCODE_URL` (default `http://localhost:8123`). `VIDEO_DEBUG_SHOT=<png>` saves a screenshot
of VS Code just before the capture starts.
