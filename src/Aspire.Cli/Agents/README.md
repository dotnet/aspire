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

`AgentClient` is the static, localized client catalog, including native settings
destinations and inherited policy locations. The existing
`AgentInitCommand` retains client discovery, bundled skill publishing, tool
acquisition, MCP configuration, and telemetry hooks. There is no shared setup or
onboarding service.

`NativeAgentPluginConfiguration` performs only offline plugin/catalog registration
and source-conflict checks. It uses `AgentFileWriter` for atomic publication,
preserving semantic no-op bytes, permissions, physical path aliases, and inputs
that change concurrently. It does not replace the existing MCP, skill, or hook
writers.

Explicit Project selections do not probe clients for copied skills or native
registration. OpenCode catalog registration uses existing configuration to select
its schema, defaulting to stable V1 when there is no evidence, without running
OpenCode. Global setup retains existing discovery for eligible hooks.

Native registration does not download or activate Aspire skills or canvases.
Agents retain responsibility for trust, acquisition, activation, caching, and
updates. Existing pins, disabled settings, managed policy, and telemetry opt-out
are preserved; blocked and failed operations are reported rather than described
as successful installation.

OpenCode MCP updates reuse the shared effective-document reader: exact legacy
commands are repaired in their owning JSON file without adding a shadowing
default to JSONC. Catalog registration still follows JSONC precedence, and
existing pins and explicit configuration-file overrides remain authoritative.
Global catalog registration uses the same single destination declared for the
client's MCP configuration: explicit `OPENCODE_CONFIG`, then `OPENCODE_CONFIG_DIR`,
then the normal user configuration. Other configuration layers are read-only.
Schema selection does not validate or rewrite unrelated MCP server definitions.

New Copilot and OpenCode tool skills use `.agents/skills` within the selected
Project or Global root. Claude Code uses `.claude/skills`, with its Global
configuration-directory override honored. Shared physical destinations are
written once. Other compatible agents can discover shared files, but that does
not authorize private configuration, native registration, or hooks for unselected
agents. Existing compatibility skill directories are inspected for collisions,
not rewritten or deleted.

Global telemetry hooks retain their existing installer and configurator and are
eligible only for selected, actually detected environments. Copilot CLI and App
share a hook target; VS Code hints do not invent CLI/App detection.

## Chained initialization

`aspire new` and `aspire init` retain suppression and an `aspireify` follow-up after
successful bundled installation or native registration. Native registration still
requires client-owned acquisition before the skill is available. Their chained
initialization cannot configure MCP. Legacy `aspire mcp init` delegates to the
standalone agent initialization command.

Shared installation onboarding is deferred; `aspire setup` and first use do not
offer native plugin registration.
