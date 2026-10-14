# Agent setup

`aspire agent init` selects agents, independent assets, and a Project or Global
scope. Detected supported agent groups are preselected. If none are detected,
GitHub Copilot is the fallback. Selection is independent of asset choices and
shared paths; it can also include undetected agents.

The `--agents` option accepts stable IDs, case-insensitively:
`copilot`, `claude`, and `opencode`, or `all` / `none`.
The picker and status messages use localized names such as GitHub Copilot and
Claude Code; these display names are not command-line identifiers.

```shell
aspire agent init --agents copilot,claude --scope project --aspire-skills native --mcp n --playwright n --dotnet-inspect n
aspire agent init --agents opencode --scope global --aspire-skills bundled --non-interactive
aspire agent init --agents none
```

| Asset | Default | Behavior |
| --- | --- | --- |
| `--aspire-skills` | `bundled` | Choose `bundled` (copy all applicable Aspire skills), `native` (register Aspire's plugin/catalog offline), or `none` (leave Aspire skills unchanged). |
| `--mcp` | No | Configure the standalone Aspire MCP server for the selected agents and scope. |
| `--playwright` | No | Verify/acquire Playwright CLI, generate its skill in an isolated workspace, then publish only to selected destinations. |
| `--dotnet-inspect` | No | Install the bootstrap skill for dotnet-inspect. |

`--aspire-skills` requires one named source, case-insensitively. There is no
per-skill picker: bundled mode installs every applicable skill from the existing
bundle. The other asset options accept a bare affirmative, `y` / `n`, or
`true` / `false`.
An empty agent selection, `--agents none`, or disabling every asset leaves agent
configuration and skills unchanged.

Bundled and native Aspire skills are mutually exclusive. Setup checks the
selected clients' visible local skills and native registrations before writing
configuration or acquiring tools. An existing opposite-mode installation, or an
inspection failure, stops setup without writes. It does not remove copied files,
disable plugins, migrate installations, or fall back to a different mode.

## Native configuration and shared files

`AgentClient` is the static, localized client catalog. Native scanners handle
read-only discovery, version evidence, and each client's configuration schema.
`AgentSetupService` shares selection and the atomic configuration pipeline between
standalone initialization and the chained `new` / `init` flows.

Scan inputs contain only the working directory and repository boundary. Scanners
return independent, read-only version evidence; native schema and catalog checks
reuse that evidence without probing clients again. A missing repository boundary
keeps workspace-free Global setup from inspecting project markers.

Explicit Project selections do not probe clients for copied tool skills or
Copilot/Claude registration. Native OpenCode settings require version evidence
after asset consent; Global setup also discovers eligible selected-client hooks.
Detection is reused once when both defaults and native policy or hooks need it.

Native registration does not download or activate Aspire skills or canvases.
Agents retain responsibility for trust, acquisition, activation, caching, and
updates. Existing pins, disabled settings, managed policy, and telemetry opt-out
are preserved; blocked and failed operations are reported rather than described
as successful installation.

OpenCode MCP updates reuse the shared effective-document reader: exact legacy
commands are repaired in their owning JSON file without adding a shadowing
default to JSONC. Catalog registration still follows JSONC precedence, and
existing pins and explicit configuration-file overrides remain authoritative.

New Copilot and OpenCode tool skills use `.agents/skills` within the selected
Project or Global root. Claude Code uses `.claude/skills`, with its Global
configuration-directory override honored. Shared physical destinations are
written once. Other compatible agents can discover shared files, but that does
not authorize private configuration, native registration, or hooks for unselected
agents. Existing compatibility skill directories are inspected for collisions,
not rewritten or deleted.

Global telemetry hooks are eligible only for selected, actually detected
environments. Copilot CLI and App share a hook target; VS Code hints do not invent
CLI/App detection. New registrations execute the CLI directly, while embedded
scripts remain available to refresh existing compatible registrations.

## Chained initialization

`aspire new` and `aspire init` retain suppression and an `aspireify` follow-up after
successful bundled installation or native registration. Native registration still
requires client-owned acquisition before the skill is available. Their chained
initialization cannot configure MCP. Legacy `aspire mcp init` delegates to the
standalone agent initialization command.

Shared installation onboarding is deferred; `aspire setup` and first use do not
offer native plugin registration.
