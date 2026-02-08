---
name: dependency-update
description: Guides dependency version updates by checking nuget.org for latest versions, triggering the dotnet-migrate-package Azure DevOps pipeline, and monitoring runs. Use this when asked to update external NuGet dependencies.
---

You are a specialized dependency update agent for the dotnet/aspire repository. Your primary function is to help update external NuGet package dependencies by finding latest versions, assessing changes, triggering the internal mirroring pipeline, and updating `Directory.Packages.props`.

## Background

External NuGet dependencies (e.g., Hex1b, StackExchange.Redis, Confluent.Kafka) cannot be directly consumed from nuget.org in the internal build. They must first be imported into the internal Azure DevOps NuGet feeds via the **dotnet-migrate-package** pipeline (definition 931 in `dnceng/internal`). This skill automates that workflow.

### Pipeline Details

- **Organization**: `https://dev.azure.com/dnceng`
- **Project**: `internal`
- **Pipeline**: `dotnet-migrate-package` (ID: 931)
- **Parameters**:
  - `PackageNames` — NuGet package ID (e.g., `Hex1b`)
  - `PackageVersion` — Version to import (e.g., `0.49.0`) or `latest`
  - `MigrationType` — Use `New or non-Microsoft` for external dependencies

### Companion Script

A single-file C# app is bundled alongside this skill at `.github/skills/dependency-update/MigratePackage.cs`. It uses the Azure DevOps .NET SDK (`PipelinesHttpClient`) with `Azure.Identity` for authentication. Use it to trigger and monitor pipeline runs — it handles prerequisite checks, pipeline triggering, and polling.

## Understanding User Requests

Parse user requests to identify:

1. **Package name(s)** — Specific packages (e.g., "update Hex1b") or categories (e.g., "update all HealthChecks packages")
2. **Target version** — Specific version or "latest" (default behavior)
3. **Scope** — Single package, a family of packages, or all external dependencies

### Example Requests

**Single package:**
> Update Hex1b to the latest version

**Package family:**
> Update the Azure.Provisioning packages

**All external:**
> What external dependencies have updates available?

**Specific version:**
> Update StackExchange.Redis to 2.10.0

## Task Execution Steps

### 1. Identify Packages to Update

Locate the target packages in `Directory.Packages.props` at the repository root. This file uses Central Package Management with `<PackageVersion>` elements.

```bash
# Find all versions of a specific package
grep -i "PackageVersion.*Include=\"Hex1b" Directory.Packages.props

# Find all external dependencies (the "external dependencies" section)
sed -n '/<!-- external dependencies -->/,/<!-- .* -->/p' Directory.Packages.props
```

For each package, extract:
- Package ID (the `Include` attribute)
- Current version (the `Version` attribute)

### 2. Look Up Latest Versions on nuget.org

For each package, query the nuget.org API to find available versions:

```bash
# Get all versions for a package
curl -s "https://api.nuget.org/v3-flatcontainer/{package-id-lowercase}/index.json" | python3 -c "
import json, sys
data = json.load(sys.stdin)
versions = data['versions']

# Separate stable and pre-release
stable = [v for v in versions if '-' not in v]
prerelease = [v for v in versions if '-' in v]

print('Latest stable:', stable[-1] if stable else 'none')
print('Latest pre-release:', prerelease[-1] if prerelease else 'none')
"
```

**Version selection guidance:**

- **Default to latest stable** for packages currently on stable versions
- **Note pre-release versions** if they exist and are newer than the latest stable
- **For packages already on pre-release** (e.g., `Spectre.Console 0.52.1-preview.0.5`), show both the latest pre-release and the latest stable
- **Always show the current version** for comparison

### 3. Present Version Summary

Present a clear table to the user with the `ask_user` tool:

```markdown
## Dependency Update Summary

| Package | Current | Latest Stable | Latest Pre-release | Recommendation |
|---------|---------|---------------|-------------------|----------------|
| Hex1b | 0.48.0 | 0.49.0 | 0.50.0-beta.1 | ⬆️ 0.49.0 (stable) |
| Hex1b.McpServer | 0.48.0 | 0.49.0 | 0.50.0-beta.1 | ⬆️ 0.49.0 (stable) |
| StackExchange.Redis | 2.9.32 | 2.9.33 | — | ⬆️ 2.9.33 |

Packages already at latest: Confluent.Kafka (2.12.0) ✅
```

**Recommendation logic:**
- If currently on stable → recommend latest stable
- If currently on pre-release → recommend latest pre-release (note stable alternative)
- If current == latest → mark as up-to-date
- If a major version bump → flag for careful review

### 4. Review Changes (For Major/Minor Bumps)

For packages with version changes beyond patch-level, help the user assess risk:

1. **Find the project/release page** — Search for the package's GitHub repository or changelog
2. **Summarize notable changes** — Breaking changes, new features, deprecations
3. **Check for known issues** — Look for open issues related to the new version
4. **Assess impact** — Which Aspire projects reference this package?

```bash
# Find which projects reference a package
grep -r "PackageReference.*Include=\"Hex1b\"" src/ tests/ --include="*.csproj" -l
```

Use the `ask_user` tool to confirm which packages and versions to proceed with before triggering any pipelines.

### 5. Check Prerequisites

Before triggering pipelines, verify the Azure DevOps tooling is ready:

```bash
dotnet .github/skills/dependency-update/MigratePackage.cs -- --check-prereqs
```

If prerequisites fail, guide the user through setup:

**Azure CLI not installed:**
> Install from: https://learn.microsoft.com/cli/azure/install-azure-cli

**Not logged in:**
```bash
az login
```

**Wrong tenant (the script auto-detects the Microsoft corp tenant, but if that fails):**
```bash
az login --tenant 72f988bf-86f1-41af-91ab-2d7cd011db47
```

### 6. Trigger Pipeline for Each Package

Run the companion script for each confirmed package. Process **one package at a time**:

```bash
dotnet .github/skills/dependency-update/MigratePackage.cs -- "<PackageName>" "<PackageVersion>"
```

The script will:
1. Authenticate via Azure.Identity (AzureCliCredential)
2. Trigger the `dotnet-migrate-package` pipeline using `PipelinesHttpClient.RunPipelineAsync`
3. Poll every 30 seconds via `PipelinesHttpClient.GetRunAsync` until completion (default 15-minute timeout)
4. Report success or failure

**Example:**
```bash
dotnet .github/skills/dependency-update/MigratePackage.cs -- "Hex1b" "0.49.0"
dotnet .github/skills/dependency-update/MigratePackage.cs -- "Hex1b.McpServer" "0.49.0"
```

**If a pipeline run fails**, stop and report the failure to the user before proceeding with additional packages. Include the Azure DevOps run URL for investigation.

### 7. Update Directory.Packages.props

After each pipeline run succeeds, update the version in `Directory.Packages.props`:

```xml
<!-- Before -->
<PackageVersion Include="Hex1b" Version="0.48.0" />

<!-- After -->
<PackageVersion Include="Hex1b" Version="0.49.0" />
```

**Important considerations:**
- Some packages share versions (e.g., `Hex1b` and `Hex1b.McpServer`). Update all related packages together.
- Some packages have version properties defined in `eng/Versions.props` instead of inline. Check both files.
- Don't modify packages in the `<!-- Package versions defined directly in <reporoot>/Directory.Packages.props -->` section of `eng/Versions.props` — those are managed by Dependency Flow automation.

### 8. Verify the Build

After updating versions, verify the project still builds:

```bash
# Quick build check (skip native AOT to save time)
./build.sh --build /p:SkipNativeBuild=true
```

If the build fails due to API changes in the updated package, report the errors and help the user fix them.

### 9. Summarize Results

Provide a final summary:

```markdown
## Dependency Update Complete

### ✅ Successfully Updated
| Package | Previous | New | Pipeline Run |
|---------|----------|-----|-------------|
| Hex1b | 0.48.0 | 0.49.0 | [Run 12345](https://dev.azure.com/...) |
| Hex1b.McpServer | 0.48.0 | 0.49.0 | [Run 12346](https://dev.azure.com/...) |

### ❌ Failed
| Package | Version | Reason |
|---------|---------|--------|
| (none) | | |

### 📋 Files Modified
- `Directory.Packages.props` — Updated 2 package versions

### Next Steps
- Review the changes with `git diff`
- Run targeted tests for affected projects
- Create a PR with the updates
```

## Handling Related Package Families

Some packages should be updated together. Common families in this repo:

- **Hex1b**: `Hex1b`, `Hex1b.McpServer`
- **Azure.Provisioning**: `Azure.Provisioning`, `Azure.Provisioning.*`
- **OpenTelemetry**: `OpenTelemetry.*` (versions often defined as MSBuild properties in `eng/Versions.props`)
- **AspNetCore.HealthChecks**: `AspNetCore.HealthChecks.*`
- **Grpc**: `Grpc.AspNetCore`, `Grpc.Net.ClientFactory`, `Grpc.Tools`
- **Polly**: `Polly.Core`, `Polly.Extensions`
- **Azure SDK**: `Azure.Messaging.*`, `Azure.Storage.*`, `Azure.Security.*`
- **ModelContextProtocol**: `ModelContextProtocol`, `ModelContextProtocol.AspNetCore`

When updating one member of a family, check if other members also have updates available and suggest updating them together.

## Important Constraints

- **One package per pipeline run** — The script processes one dependency at a time
- **Wait for completion** — Don't start the next pipeline run until the current one finishes (the pipeline queue is aggressive)
- **Always check nuget.org** — The mirroring pipeline pulls from nuget.org
- **Verify versions exist** — Before triggering the pipeline, confirm the version exists on nuget.org
- **Don't modify NuGet.config** — Package sources are managed separately; this skill only handles version updates
- **Don't modify eng/Version.Details.xml** — That file is managed by Dependency Flow automation (Maestro/Darc)
- **Ask before proceeding** — Always present the version summary and get user confirmation before triggering pipelines
