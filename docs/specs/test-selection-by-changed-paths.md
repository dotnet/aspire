# Test Selection by Changed Paths

## Overview

This document describes the MSBuild-based test selection system for determining which test projects to run based on git changes. The system uses `dotnet-affected` for dependency analysis combined with custom rules for edge cases.

## Problem Statement

Running all tests on every change leads to:
- Longer CI times
- Unnecessary resource consumption
- Slower feedback loops for developers

The solution uses MSBuild dependency analysis to accurately determine which tests are affected by code changes.

## Design Goals

1. **Accurate**: Uses MSBuild project graph for precise dependency tracking
2. **Portable**: Works for both GitHub Actions and Azure DevOps
3. **Configurable**: Non-.NET rules defined in a separate config file
4. **Safe**: Falls back to running all tests when uncertain
5. **Debuggable**: Clear JSON output explaining decisions

## Architecture

```
Changed Files
     │
     ▼
┌────────────────────────────┐
│ Layer 1: Critical Files    │  global.json, *.sln, NuGet.config
│ (triggers ALL tests)       │  → Run everything, stop here
└────────────────────────────┘
     │ (not critical)
     ▼
┌────────────────────────────┐
│ Layer 2: dotnet-affected   │  Get all affected projects via MSBuild
└────────────────────────────┘
     │
     ├──────────────────────────────────┐
     ▼                                  ▼
┌────────────────────────────┐   ┌────────────────────────────┐
│ Filter: IsTestProject=true │   │ Check: Any IsPackable=true │
│ → Affected test projects   │   │ → Trigger NuGet-dependent  │
└────────────────────────────┘   │   tests (Templates, E2E)   │
     │                           └────────────────────────────┘
     ▼
┌────────────────────────────┐
│ Layer 3: Non-.NET Rules    │  extension/** → extension tests
│                            │  playground/** → endtoend tests
└────────────────────────────┘
     │
     ▼
  OUTPUT: JSON with categories + project paths
```

## What dotnet-affected Handles

| Scenario | Coverage |
|----------|----------|
| `src/Aspire.Hosting/Foo.cs` changed | ✅ Finds all test projects that depend on Aspire.Hosting |
| `tests/Aspire.TestUtilities/` changed | ✅ Finds all test projects that reference TestUtilities |
| `Directory.Build.props` changed | ✅ Detected via MSBuild.Prediction |
| `Directory.Packages.props` changed | ✅ Explicit CPM support |
| Shared source files (`<Compile Include>`) | ✅ Detected via ProjectGraph |
| Transitive dependencies | ✅ Core feature |

## What Needs Custom Logic

| Scenario | Handling |
|----------|----------|
| `global.json` changed | Trigger all tests |
| `NuGet.config` changed | Trigger all tests |
| `*.sln/*.slnx` changed | Trigger all tests |
| `extension/**` changed | Trigger extension tests (non-.NET) |
| `playground/**` changed | Trigger endtoend tests |
| Any `IsPackable=true` project affected | Also run NuGet-dependent tests |

## Tool: Aspire.TestSelector

### Location

`tools/Aspire.TestSelector/`

### CLI Usage

```bash
# Basic usage (compares to origin/main)
dotnet run --project tools/Aspire.TestSelector -- --from origin/main

# With explicit changed files (for testing)
dotnet run --project tools/Aspire.TestSelector -- \
  --changed-files "src/Aspire.Hosting/Foo.cs,tests/Aspire.TestUtilities/Bar.cs"

# Output to file
dotnet run --project tools/Aspire.TestSelector -- \
  --from origin/main \
  --output test-selection.json

# Verbose mode
dotnet run --project tools/Aspire.TestSelector -- --from origin/main --verbose
```

### Parameters

| Parameter | Short | Description | Default |
|-----------|-------|-------------|---------|
| `--solution` | `-s` | Path to the solution file | `Aspire.slnx` |
| `--config` | `-c` | Path to the config file | `eng/scripts/test-selector-config.json` |
| `--from` | `-f` | Git ref to compare from | `origin/main` |
| `--to` | `-t` | Git ref to compare to | `HEAD` |
| `--changed-files` | | Comma-separated changed files (for testing) | |
| `--output` | `-o` | Output file path for JSON | (stdout) |
| `--verbose` | `-v` | Enable verbose output | `false` |

## Configuration File

### Location

`eng/scripts/test-selector-config.json`

### Schema

```json
{
  "$schema": "./test-selector-config.schema.json",

  "ignorePaths": [
    "docs/**",
    ".github/**",
    "*.md"
  ],

  "triggerAllPaths": [
    "global.json",
    "NuGet.config",
    "Directory.Build.props",
    "*.sln",
    "*.slnx"
  ],

  "triggerAllExclude": [
    "eng/pipelines/**"
  ],

  "nonDotNetRules": [
    {
      "pattern": "extension/**",
      "category": "extension"
    },
    {
      "pattern": "playground/**",
      "category": "endtoend"
    }
  ],

  "categories": {
    "templates": {
      "description": "Template tests - requires NuGet packages",
      "testProjects": ["tests/Aspire.Templates.Tests/"]
    },
    "integrations": {
      "description": "Integration tests - derived from MSBuild",
      "testProjects": "auto"
    }
  }
}
```

### Configuration Sections

| Section | Description |
|---------|-------------|
| `ignorePaths` | Glob patterns for files that don't trigger any tests |
| `triggerAllPaths` | Glob patterns for files that trigger ALL tests |
| `triggerAllExclude` | Exceptions to triggerAllPaths |
| `nonDotNetRules` | Pattern → category mapping for non-.NET files |
| `categories` | Test category configurations; `"auto"` means derive from MSBuild |

## Output JSON Structure

```json
{
  "runAllTests": false,
  "reason": "msbuild_analysis",

  "categories": {
    "core": false,
    "templates": true,
    "cli_e2e": false,
    "endtoend": true,
    "integrations": true,
    "extension": false,
    "infrastructure": false
  },

  "affectedTestProjects": [
    "tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj",
    "tests/Aspire.Azure.Storage.Blobs.Tests/Aspire.Azure.Storage.Blobs.Tests.csproj"
  ],

  "integrationsProjects": [
    "tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj"
  ],

  "nugetDependentTests": {
    "triggered": true,
    "reason": "IsPackable projects affected: Aspire.Hosting",
    "affectedPackableProjects": ["src/Aspire.Hosting/Aspire.Hosting.csproj"],
    "projects": [
      "tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj",
      "tests/Aspire.EndToEnd.Tests/Aspire.EndToEnd.Tests.csproj"
    ]
  },

  "changedFiles": ["src/Aspire.Hosting/Foo.cs"],
  "ignoredFiles": ["README.md"],
  "dotnetAffectedProjects": [
    "src/Aspire.Hosting/Aspire.Hosting.csproj",
    "tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj"
  ]
}
```

## Test Categories

| Category | Description | Trigger Condition |
|----------|-------------|-------------------|
| `core` | Core hosting tests | Aspire.Hosting changes |
| `templates` | Template tests (requires NuGets) | Template paths OR packable project affected |
| `cli_e2e` | CLI end-to-end tests (requires NuGets) | CLI paths OR packable project affected |
| `endtoend` | General E2E tests (requires NuGets) | E2E paths OR playground/** OR packable project affected |
| `integrations` | Integration tests | MSBuild dependency analysis |
| `extension` | VS Code extension tests | extension/** changes |
| `infrastructure` | Infrastructure tests | Infrastructure.Tests changes |

## NuGet-Dependent Tests

Tests in these projects require built NuGet packages:
- `tests/Aspire.Templates.Tests/`
- `tests/Aspire.EndToEnd.Tests/`
- `tests/Aspire.Cli.EndToEnd.Tests/`

**Logic**: If any affected source project has `IsPackable=true`, all NuGet-dependent tests are triggered.

## Rule Evaluation Examples

### Example 1: Component file changed

**Changed files:**
- `src/Components/Aspire.Npgsql/NpgsqlExtensions.cs`

**Result:**
```json
{
  "runAllTests": false,
  "reason": "msbuild_analysis",
  "categories": {
    "integrations": true,
    "templates": true,
    "endtoend": true
  },
  "affectedTestProjects": ["tests/Aspire.Npgsql.Tests/Aspire.Npgsql.Tests.csproj"],
  "nugetDependentTests": {
    "triggered": true,
    "reason": "IsPackable projects affected: Aspire.Npgsql"
  }
}
```

### Example 2: Critical file changed

**Changed files:**
- `global.json`

**Result:**
```json
{
  "runAllTests": true,
  "reason": "critical_path",
  "triggerFile": "global.json",
  "triggerPattern": "global.json",
  "categories": {
    "core": true,
    "templates": true,
    "cli_e2e": true,
    "endtoend": true,
    "integrations": true,
    "extension": true,
    "infrastructure": true
  }
}
```

### Example 3: Only docs changed

**Changed files:**
- `docs/getting-started.md`
- `README.md`

**Result:**
```json
{
  "runAllTests": false,
  "reason": "all_ignored",
  "categories": {
    "core": false,
    "templates": false,
    "integrations": false
  },
  "ignoredFiles": ["docs/getting-started.md", "README.md"]
}
```

### Example 4: Extension files changed

**Changed files:**
- `extension/package.json`
- `extension/src/index.ts`

**Result:**
```json
{
  "runAllTests": false,
  "reason": "msbuild_analysis",
  "categories": {
    "extension": true,
    "integrations": false
  }
}
```

## CI Integration

### GitHub Actions

```yaml
jobs:
  evaluate:
    runs-on: ubuntu-latest
    outputs:
      run_all: ${{ steps.eval.outputs.run_all }}
      categories: ${{ steps.eval.outputs.categories }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for git diff

      - name: Install dotnet-affected
        run: dotnet tool install dotnet-affected -g

      - name: Evaluate test selection
        id: eval
        run: |
          result=$(dotnet run --project tools/Aspire.TestSelector -- --from origin/main)
          echo "run_all=$(echo $result | jq -r '.runAllTests')" >> $GITHUB_OUTPUT
          echo "categories=$(echo $result | jq -c '.categories')" >> $GITHUB_OUTPUT

  test-templates:
    needs: evaluate
    if: needs.evaluate.outputs.run_all == 'true' || fromJson(needs.evaluate.outputs.categories).templates == true
    runs-on: ubuntu-latest
    steps:
      - run: echo "Running template tests..."

  test-integrations:
    needs: evaluate
    if: needs.evaluate.outputs.run_all == 'true' || fromJson(needs.evaluate.outputs.categories).integrations == true
    runs-on: ubuntu-latest
    steps:
      - run: echo "Running integration tests..."
```

### Azure DevOps

```yaml
stages:
- stage: Evaluate
  jobs:
  - job: EvaluateChanges
    steps:
    - task: UseDotNet@2
      inputs:
        packageType: 'sdk'
        version: '9.x'

    - script: dotnet tool install dotnet-affected -g
      displayName: 'Install dotnet-affected'

    - script: |
        result=$(dotnet run --project tools/Aspire.TestSelector -- --from origin/main)
        echo "##vso[task.setvariable variable=runAll;isOutput=true]$(echo $result | jq -r '.runAllTests')"
        echo "##vso[task.setvariable variable=runTemplates;isOutput=true]$(echo $result | jq -r '.categories.templates')"
      name: eval
      displayName: 'Evaluate test selection'

- stage: TestTemplates
  dependsOn: Evaluate
  condition: or(eq(dependencies.Evaluate.outputs['EvaluateChanges.eval.runAll'], 'true'), eq(dependencies.Evaluate.outputs['EvaluateChanges.eval.runTemplates'], 'true'))
  jobs:
  - job: RunTemplateTests
    steps:
    - script: echo "Running template tests..."
```

## Dependencies

The tool requires:
- `dotnet-affected` CLI tool (installed via `dotnet tool install dotnet-affected -g`)
- .NET 10.0 SDK

## File Structure

```
tools/Aspire.TestSelector/
├── Aspire.TestSelector.csproj
├── Program.cs                       # CLI entry point
├── CategoryMapper.cs                # Map projects → categories
├── Models/
│   ├── TestSelectionResult.cs       # Output model
│   └── TestSelectorConfig.cs        # Config model for JSON
└── Analyzers/
    ├── CriticalFileDetector.cs      # Check triggerAllPaths
    ├── IgnorePathFilter.cs          # Filter ignorePaths
    ├── DotNetAffectedRunner.cs      # Wraps dotnet-affected CLI
    ├── TestProjectFilter.cs         # Filter IsTestProject=true
    ├── NuGetDependencyChecker.cs    # Check IsPackable → NuGet tests
    └── NonDotNetRulesHandler.cs     # Pattern → category for non-.NET

eng/scripts/
├── test-selector-config.json        # Configuration rules
└── test-selector-config.schema.json # JSON schema for validation
```

## Migration from Pattern-Based Approach

The previous pattern-based approach used `Evaluate-TestSelection.ps1` with `test-selection-rules.json`. The new MSBuild-based approach provides:

1. **More accurate dependency tracking** via MSBuild project graph
2. **Automatic detection** of transitive dependencies
3. **Built-in support** for shared source files and Directory.Build.props
4. **Simpler configuration** - only non-.NET rules need explicit mapping

To migrate:
1. Install `dotnet-affected` tool in CI
2. Run `Aspire.TestSelector` instead of the PowerShell script
3. Update CI workflows to parse the new JSON output format

## References

- [dotnet-affected](https://github.com/leonardochaia/dotnet-affected) - MSBuild dependency analysis tool
- [MSBuild.Prediction](https://github.com/microsoft/MSBuildPrediction) - Input/output prediction for MSBuild projects
