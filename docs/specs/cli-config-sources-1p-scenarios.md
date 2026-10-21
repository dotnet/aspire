# Config-source scenarios for Microsoft first-party developers

This companion to the [CLI config-source specification](cli-config-sources.md) follows hypothetical Microsoft first-party (1P) developers. It describes the implemented configuration-acquisition flow, not the proposed skills, template, or integration customization schema.

The Microsoft default configuration repository is `https://msft.ghe.com/coreai/aspire-1p-init`. The developers, application names, team-specific configuration paths, and documentation URLs below are illustrative. The `.example` documentation hosts do not identify actual Microsoft services.

## Scenario 1: Maya creates her first Aspire app

Maya is a Microsoft developer creating an Inventory application on her development machine. She has an Aspire CLI version that supports config sources, Git on `PATH`, and the normal prerequisites for her chosen Aspire template.

### Prepare Git authentication

Maya needs permission to read the configuration repository and working Git authentication for `msft.ghe.com`. Being recognized as a Microsoft employee does not grant repository access, authenticate Git, or authorize use of the source.

Aspire uses normal Git authentication, not an Aspire-specific repository login. Maya can prepare authentication upfront or complete it interactively when Git acquires the configuration.

#### Pre-setup authentication

Maya configures her organization's approved Git credential helper or SSH setup before running Aspire. For an environment that uses GitHub CLI as its HTTPS credential helper, an example setup is:

```powershell
gh auth login --hostname msft.ghe.com --git-protocol https --web
gh auth setup-git --hostname msft.ghe.com
git ls-remote https://msft.ghe.com/coreai/aspire-1p-init HEAD
```

She completes the host's sign-in and any required organizational access or SSO steps. Authentication to `github.com` alone is not authentication to `msft.ghe.com`. The final command checks that Git itself can read the repository.

Pre-setup is useful for verifying access before creating the app and is required for runs that cannot obtain credentials interactively. Terminal credential prompts are disabled with `--non-interactive`, `ASPIRE_NON_INTERACTIVE`, or CI detection that disables interactive input.

#### Interactive authentication

Alternatively, Maya runs Aspire in an interactive terminal without preparing credentials first. After she accepts the configuration source during app creation, Git can prompt for missing terminal credentials, and configured credential helpers can handle their own sign-in flows. The available authentication flow depends on her Git setup and the repository host's requirements.

She completes authentication when prompted, and configuration acquisition continues if access succeeds. The two-minute acquisition timeout and cancellation still apply while waiting for credentials. If authentication fails, Aspire reports the error instead of silently creating the app with standard configuration.

With either approach, Maya never puts credentials in the repository URL or configuration JSON.

### Create the app and accept the source

Maya runs:

```powershell
aspire new aspire-starter --name Inventory --output .\Inventory
```

Assume employee detection succeeds and neither initialization environment variable is set. Before generating the app, Aspire proposes:

```text
Configuration source: https://msft.ghe.com/coreai/aspire-1p-init
Configuration file: aspire.config.json
Source origin: Microsoft employee default

Use configuration from this source?
  Yes
  No, use standard Aspire configuration
```

Detection only chooses a candidate. Maya's **Yes** is the project's consent to use it.

After she accepts, Aspire:

1. Checks the local cache for this repository and configuration-file path.
2. If acquisition is needed, uses Git to resolve the repository's default branch and read `aspire.config.json` from that commit, without checking out a working tree.
3. Validates the JSON and publishes a private cached snapshot before applying its settings.
4. Continues app creation with those shared defaults.
5. After successful creation, saves the source binding in `Inventory\aspire.config.json`, alongside the generated project settings.

For this scenario, suppose the repository initially supplies:

```json
{
  "docs": {
    "llmsTxtUrl": "https://aspire-docs.example/llms-full.txt"
  }
}
```

The application is still created from the selected Aspire starter. Acquiring this configuration does not itself download a custom starter, install a private integration, or replace the embedded agent skills.

If Maya declines, creation continues with standard Aspire configuration and no new source binding or Git acquisition. If she cancels the source prompt, the app is not created. If she accepts but repository access or validation fails, creation reports the error rather than silently switching to standard configuration.

## Scenario 2: Maya finds out what was saved

There are three different things to distinguish:

| Item | Location | Purpose |
|---|---|---|
| Organization-owned configuration | `aspire.config.json` in the configuration repository | Versioned shared defaults maintained by the organization |
| Project binding | The application's `Inventory\aspire.config.json` | Records which repository and file the project uses |
| Local acquired snapshot | The resolved Aspire home's `configsources` directory | Private cached configuration, commit metadata, and bare Git repository |

The relevant excerpt from the project's file is:

```json
{
  "configSource": {
    "repository": "https://msft.ghe.com/coreai/aspire-1p-init",
    "path": "aspire.config.json"
  }
}
```

This is an excerpt, not a replacement for the generated file. Maya keeps its AppHost, SDK, package, and other project-owned settings.

The shared JSON is not pasted into that project file. The binding contains neither Git credentials nor an absolute cache path, so it can be committed with the application and used on another machine.

The acquired snapshot is stored separately:

```text
<AspireHome>\configsources\<source-key>\
  cache.lock
  current
  <snapshot-id>\
    aspire.config.json
    metadata.json
    repository.git\
```

Aspire home is normally `<user-profile>\.aspire`; the resolved location honors `ASPIRE_HOME` and the installation's home resolution. The source key distinguishes the repository and upstream file path. The cached filename is always `aspire.config.json`, even when the upstream file has another name.

`metadata.json` records the source identity, resolved Git commit, and successful refresh time. The `current` pointer selects the published snapshot. Configuration and matching metadata are published together, so an interrupted refresh does not expose a half-written snapshot.

Maya does not commit this cache or edit it to customize the app. Git credentials remain managed by her credential helper or SSH setup, not by these files.

## Scenario 3: Leo expands the shared configuration, and Maya overrides one setting

Leo maintains the organization's configuration repository. He expands its configuration to include the API documentation source:

```json
{
  "docs": {
    "llmsTxtUrl": "https://aspire-docs.example/llms-full.txt",
    "api": {
      "sitemapUrl": "https://aspire-docs.example/sitemap.xml"
    }
  }
}
```

He commits the change to the repository's default branch. This makes it available for acquisition; it does not push changes into every developer's application immediately.

Maya refreshes her source as described in scenario 5. She then wants Inventory to use a team-specific general documentation source while keeping the organization's API documentation source:

```powershell
Set-Location .\Inventory
aspire config set docs.llmsTxtUrl https://inventory-docs.example/llms-full.txt
```

Her project file now contains the saved binding and her local override, in addition to its other project settings:

```json
{
  "configSource": {
    "repository": "https://msft.ghe.com/coreai/aspire-1p-init",
    "path": "aspire.config.json"
  },
  "docs": {
    "llmsTxtUrl": "https://inventory-docs.example/llms-full.txt"
  }
}
```

Aspire expands the effective configuration in memory by merging **global settings, shared defaults, then the selected project's settings**. The resulting `docs` section is:

```json
{
  "docs": {
    "llmsTxtUrl": "https://inventory-docs.example/llms-full.txt",
    "api": {
      "sitemapUrl": "https://aspire-docs.example/sitemap.xml"
    }
  }
}
```

The local `llmsTxtUrl` wins, but the unmodified API setting remains inherited. Objects merge by key; local arrays and explicit nulls replace inherited values. The expanded result is not written back into the application's configuration file.

Maya can inspect these effective values and use them:

```powershell
aspire config get docs.llmsTxtUrl
aspire config get docs.api.sitemapUrl
aspire docs search "service discovery"
aspire docs api list csharp
```

Documentation consumers resolve their URLs and cache identities after project configuration is applied. Git authentication for the config repository is not forwarded to documentation hosts; those hosts must be independently accessible.

Shared defaults cannot select the project's AppHost, SDK/channel, package references, or launch profiles. Those remain project-owned. In particular, existing SDK lookups that explicitly inherit parent settings still search those parents before falling back to global settings.

## Scenario 4: Noah clones Inventory and uses its saved source

Noah joins Maya's team and clones the application repository, including its `aspire.config.json`. He prepares his own Git access to the configuration repository, using the same authentication checks as Maya.

From Inventory's project directory, he runs:

```powershell
aspire docs list
```

The saved binding is enough to identify the source. Noah does not need `ASPIRE_INIT_SOURCE`, and Aspire does not repeat employee detection or the creation-time source prompt. On his first use, a missing cache is acquired using his credentials and stored in his Aspire home.

The behavior is project-scoped:

- A bound project resolves its saved source before project-scoped commands, including lifecycle, inspection, and content commands.
- A project without a binding does not acquire a source merely because Noah is a Microsoft employee or another project uses one.
- Changing initialization environment variables does not retarget Inventory's saved binding.
- Having an entry in the shared local cache does not opt an unrelated project into using it.

For example, subsequent `aspire start`, `aspire logs`, and `aspire docs search` invocations check source freshness before doing their normal work. Aspire reports the source, commit, and whether it used cached or refreshed configuration. The human-readable notice goes to stderr so machine-readable stdout remains usable.

The binding follows the repository's current default branch when refreshed; it is not a project-level commit pin.

## Scenario 5: Maya needs Leo's latest change immediately

Maya has a valid cache from an hour ago. A normal project command reuses that snapshot without fetching, so it does not yet see Leo's new commit.

She forces a refresh from Inventory's project directory:

```powershell
aspire update --config-source
```

This fetches and validates the saved source even though its cache is fresh. It does not update the CLI, SDK, or packages, overwrite the project binding, regenerate application files, or reinstall skills. Her local documentation override still wins.

For an explicitly selected AppHost, she can pass `--apphost` with that AppHost's path. For unattended refresh, the command is:

```powershell
aspire update --config-source --yes --non-interactive
```

The ordinary freshness behavior is:

| Situation on a project-scoped invocation | Result |
|---|---|
| No cached snapshot | Acquire the configuration before continuing |
| Successful refresh less than 24 hours ago | Reuse the cached snapshot without a Git fetch |
| Successful refresh at least 24 hours ago | Refresh before continuing |
| Forced refresh | Fetch regardless of cache age |
| Acquisition or refresh fails | Fail the operation and preserve the previous valid snapshot and refresh time |

There is no background refresh timer. If Maya is offline, a fresh snapshot can satisfy the config-source check, although the command may have other network requirements. Once that snapshot requires refresh, the check fails rather than silently falling back to stale or standard configuration.

A successful forced refresh advances freshness even when the upstream commit is unchanged. Other projects on Maya's machine using the same repository and file benefit from the refreshed cache on their next invocation. Already-running commands keep their existing snapshot.

If authentication expires, Maya repairs Git access, verifies it with `git ls-remote`, and retries. If Leo publishes invalid configuration, he fixes the upstream file; a failed refresh does not replace Maya's last valid snapshot.

`aspire update --config-source` is not a way to enable a source on an unbound project. That operation fails without a saved binding, and the option cannot be combined with CLI self-update, migration, or channel/package-source overrides.

## Scenario 6: Priya explicitly selects a team configuration during initialization

Priya adds Aspire to an existing application that has no saved source binding. Her development environment does not produce a positive employee-detection result, or her team wants a different configuration file in the Microsoft repository.

She first prepares Git access. Then, from the application directory, she explicitly identifies the source:

```powershell
$env:ASPIRE_INIT_SOURCE = 'https://msft.ghe.com/coreai/aspire-1p-init'
$env:ASPIRE_INIT_CONFIG = 'teams/inventory.json'
aspire init
```

The team must actually publish `teams/inventory.json`; this path is illustrative. It is repository-relative and uses `/` separators, including on Windows.

The explicit repository bypasses employee detection, but not authentication, validation, or consent. Priya accepts the proposed source. After successful initialization, her project's binding records that repository and `teams/inventory.json`. Its cache entry is separate from the entry for the repository's root configuration.

For scripted creation or initialization, `--use-config-source true` explicitly accepts the candidate and `--use-config-source false` explicitly declines it. `--non-interactive` alone is not consent; when a candidate exists, omitting that choice fails with instructions. Other creation or initialization choices must also be supplied as needed for non-interactive execution.

The variables affect source selection for creation and initialization, not later commands on a bound project. Re-initializing an already-bound project reuses its saved source rather than replacing it with these variables.

## What these scenarios do not imply

Acquiring and refreshing configuration is implemented separately from the proposed content-acquisition customizations. These scenarios do not promise organization-specific template catalogs, additional skill bundles, integration catalogs, or package feeds merely because a config source was accepted. Those details belong to the specification's future content schema.

Repository access is also not authorization to private documentation or package services. Any later content acquisition retains its own authentication and validation requirements.
