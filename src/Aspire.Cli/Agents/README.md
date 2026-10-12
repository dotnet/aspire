# Agent setup

`aspire agent init` selects agents, independent assets, and a Project or Global
scope. Detected supported agent groups are preselected. If none are detected,
GitHub Copilot is the fallback. Selection is independent of asset choices and
shared paths; it can also include undetected agents.

The `--agents` option accepts the picker names, case-insensitively:
`"GitHub Copilot"`, `"Claude Code"`, and `"OpenCode"`, or `all` / `none`.
Quote names containing spaces, including a comma-separated list.

```shell
aspire agent init --agents "GitHub Copilot,Claude Code" --scope project --aspire-skills y --mcp n --playwright n --dotnet-inspect n
aspire agent init --agents OpenCode --scope global --aspire-skills y --non-interactive
aspire agent init --agents none
```

| Asset | Default | Behavior |
| --- | --- | --- |
| `--aspire-skills` | Yes | Register Aspire's native plugin or catalog configuration offline. |
| `--mcp` | No | Configure the standalone Aspire MCP server for the selected agents and scope. |
| `--playwright` | No | Verify/acquire Playwright CLI, generate its skill in an isolated workspace, then publish only to selected destinations. |
| `--dotnet-inspect` | No | Install the bootstrap skill for dotnet-inspect. |

Each asset option accepts a bare affirmative, `y` / `n`, or `true` / `false`.
An empty agent selection, `--agents none`, or disabling every asset leaves agent
configuration and skills unchanged.

## Native configuration and shared files

`AgentClient` is the static, localized client catalog. Native scanners handle
read-only discovery, version evidence, and each client's configuration schema.
`AgentSetupService` shares selection and the atomic configuration pipeline between
explicit initialization and installation onboarding.

Native registration does not download or activate Aspire skills or canvases.
Agents retain responsibility for trust, acquisition, activation, caching, and
updates. Existing pins, disabled settings, managed policy, and telemetry opt-out
are preserved; blocked and failed operations are reported rather than described
as successful installation.

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

## Installation onboarding

The installer-run `aspire setup` and guarded first interactive use offer optional
Global native plugin registration. This flow is workspace-free and does not
configure MCP, acquire tool skills, install standalone hooks, or write project
settings. Declining or completing the offer persists its completion state;
cancellation and headless execution without consent leave it pending.

`aspire new` and `aspire init` retain suppression and successful native-registration
follow-up behavior. Their chained initialization cannot configure MCP. Legacy
`aspire mcp init` delegates to the standalone agent initialization command.
