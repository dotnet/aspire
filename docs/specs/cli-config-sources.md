# Aspire CLI config sources

**Status:** Proposed.

Config sources let organizations provide shared Aspire CLI configuration through a Git-hosted `aspire.config.json`. There are two separate phases:

1. **Acquire configuration:** select a remote source, ask the user whether to use it, acquire the file using Git, cache it in the Aspire home directory, and record the source in the project's `aspire.config.json`.
2. **Acquire content:** use the effective project configuration to acquire skills, templates, and integrations.

This specification focuses on the first phase. Content acquisition is summarized at the end; its detailed schema and behavior are deferred.

## Phase 1: Acquire remote configuration

### Select a source

During `aspire new` or `aspire init`, resolve a candidate source:

| Priority | Source |
|---|---|
| 1 | `ASPIRE_INIT_SOURCE`, when explicitly set |
| 2 | `https://msft.ghe.com/coreai/aspire-1p-init`, when the existing Microsoft-employee detector reports an internal Microsoft user |
| 3 | No remote source; continue with standard Aspire behavior |

| Environment variable | Purpose | Default |
|---|---|---|
| `ASPIRE_INIT_SOURCE` | Git repository address, not a local file path or raw JSON URL | Microsoft repository for detected employees; otherwise none |
| `ASPIRE_INIT_CONFIG` | Repository-relative configuration file path, allowing multiple configurations in one repository | `aspire.config.json` at the repository root |

For example:

```powershell
$env:ASPIRE_INIT_SOURCE = 'https://git.acme.example/aspire-init'
$env:ASPIRE_INIT_CONFIG = 'teams/platform.json'
aspire new
```

The file-path override applies to either an explicit repository or the Microsoft default. Repository-relative paths use `/` separators on every platform. Reject empty or absolute paths and paths that escape the repository.

Reuse `IInternalMicrosoftDetector` directly, independently of telemetry opt-in. An explicit source bypasses employee detection. Detection selects a default, not authorization or consent: Git still authenticates repository access. An inconclusive or failed detection does not select the Microsoft source.

Keep this mechanism separate from Aspire SDK/channel identity and existing package-source flags. Invalid explicit source values produce an error rather than falling back to another source.

### Prompt during project creation

Before acquiring or using the remote configuration, `aspire new` and `aspire init` display the candidate repository, selected file path, and whether the repository came from the environment or employee detection:

```text
Configuration source: https://msft.ghe.com/coreai/aspire-1p-init
Configuration file: aspire.config.json
Source origin: Microsoft employee default

Use configuration from this source?
  Yes
  No, use standard Aspire configuration
```

- **Accept:** acquire and validate the remote file before using its configuration in the creation flow, then record `configSource` in the project's `aspire.config.json`.
- **Decline:** continue normally, without Git acquisition or a `configSource` entry.
- **Cancel:** stop without creating the application.

The choice applies to the project being created or initialized, not every project beneath the invoking directory. Existing projects use their saved `configSource` without repeating the prompt. An environment variable or employee detection alone never enables a source.

Other commands, including standalone `aspire agent init` and `aspire add`, do not prompt to enable a source. Without a saved project `configSource`, they ignore the initialization-source environment variables and employee default.

Non-interactive creation requires an explicit consent mechanism; its option spelling remains to be decided. It must not hang waiting for input or silently treat employee detection as consent.

### Acquire using Git

Use the user's Git installation and normal credential helpers or SSH authentication. Do not introduce a separate login flow or reuse credentials from employee detection.

Resolve the repository's default branch to a commit and read the selected configuration file from that snapshot. Validate the configuration before publishing it to the cache or applying it to the project. Retain the repository, file path, and commit so relative content paths can later be resolved against the configuration file's directory in the same snapshot, without escaping the repository.

Git operations must support cancellation and bounded execution. Report missing Git, authentication failures, inaccessible repositories, missing files, and invalid configuration distinctly. Do not silently substitute standard configuration after the user accepts a source.

### Store and refresh in Aspire home

Store acquired configuration centrally using the resolved Aspire home, including `ASPIRE_HOME` overrides:

```text
<AspireHome>\configsources\<source-key>\
  aspire.config.json
  metadata.json
```

The usual home is `<user-profile>\.aspire`. The source key distinguishes repository and upstream file path, so configurations in the same repository have separate cache entries. The cached filename remains `aspire.config.json`; metadata records the upstream identity, resolved commit, and last successful refresh time. The cache is shared storage, not a directory-scoped opt-in registry; project configuration determines whether a source applies.

Every command scoped to a project with `configSource` checks cache freshness before executing, even when it does not acquire skills, templates, or integrations.

| Command scope | Config-source check/update trigger |
|---|---|
| `aspire new` / `aspire init` creating or initializing a project | After the user accepts the source, check the cache and acquire or refresh as needed |
| Any command scoped to an existing project with `configSource`, including lifecycle, inspection, and content commands | Resolve the project's saved source, check the cache, and acquire or refresh as needed before executing |
| Commands scoped to a project without `configSource` | No remote-config check or acquisition |
| Global commands not scoped to a project, help, and completion | No remote-config check or acquisition |

| Cache state at the check | Behavior |
|---|---|
| Missing | Acquire and validate before use |
| Refreshed less than 24 hours ago | Use the cached file without fetching |
| Refreshed at least 24 hours ago | Refresh on this invocation before use |
| Acquisition or refresh fails | Report the error and fail the affected operation; preserve the last valid cache |

Refresh is triggered by command invocation, not a background schedule. Publish validated configuration and matching metadata atomically under a cache lock, and advance freshness only after success. A creation flow and its chained commands use one consistent snapshot.

Protect private cached content with user-scoped permissions. Never store credentials in configuration or metadata, log them, or send private source details in ordinary telemetry.

### Persist the project binding

The created or initialized project's `aspire.config.json` records the remote identity, not the machine-specific cache path. Proposed shape:

```json
{
  "configSource": {
    "repository": "https://git.acme.example/aspire-init",
    "path": "teams/platform.json"
  }
}
```

Persist the selected file path even when it is the default. This makes the selection portable: another developer can acquire the same source using their own Git credentials and Aspire home. Later commands resolve the repository and file path from project configuration rather than re-running employee detection or replacing them with the current `ASPIRE_INIT_SOURCE` or `ASPIRE_INIT_CONFIG`.

The acquired file provides shared defaults; project-owned settings remain local overrides. Refresh must not overwrite the project's AppHost path, package selections, or launch settings. Detailed merge rules remain to be defined alongside the content schema.

Report actual remote-config use once per invocation, identifying the source and whether it was cached or refreshed. Preserve machine-readable stdout by routing human-readable notices to stderr where necessary.

### Validation when implemented

Cover source precedence and telemetry-independent detection; default/custom file paths, path validation, and separate cache entries for files in one repository; accept/decline/cancel and non-interactive consent; project `configSource` persistence and portability; Git failures and configuration validation; home overrides, concurrent cache publication, and the exact 24-hour refresh boundary. Verify freshness checks for lifecycle, inspection, and content commands, and no remote-config checks for global commands, help, or completion. No-source and declined-source flows must preserve existing behavior and perform no remote-config acquisition.

## Phase 2: Acquire content from configuration

After configuration is resolved, skills, templates, and integrations are acquired according to the effective `aspire.config.json`. These are separate operations from downloading the configuration file.

| Content | Acquisition today | Possible configuration customizations |
|---|---|---|
| Skills | `aspire agent init` installs the embedded Aspire bundle by default, with existing optional remote-bundle support | Additional validated bundles, selected/default skills, and workspace installation destinations |
| Templates | .NET templates are installed from NuGet; CLI-native starters use embedded files or scaffolding | Template catalogs, package identities, versions, and feeds; external starter content could follow later |
| Integrations | Templates declare dependencies; `aspire add` and integration list/search discover NuGet packages | Additional catalog entries, package versions, feeds, and package-source mappings |

Keep standard content available initially and preserve explicit user selections and suppression options. Configuration-source refresh changes future acquisition choices; it does not automatically regenerate applications or reinstall skills. Git access does not grant access to private package feeds, and all acquired content must retain its existing validation and authentication requirements.
