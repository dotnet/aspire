# Test Selection by Changed Paths

## Overview

This document describes the MSBuild-based test selection system for determining which test projects to run based on git changes. The system uses `dotnet-affected` for dependency analysis combined with category-based rules and project mappings for edge cases.

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

```text
Changed Files
     │
     ▼
┌────────────────────────────────┐
│ Step 1: Filter Ignored Files   │  docs/**, *.md, .github/**
│ (never trigger tests)          │  → Remove from consideration
└────────────────────────────────┘
     │ (active files remain)
     ▼
┌────────────────────────────────┐
│ Step 2: Check Critical Files   │  global.json, Directory.Build.props,
│ (categories with triggerAll)   │  src/Aspire.Hosting/**, etc.
│                                │  → Run everything, stop here
└────────────────────────────────┘
     │ (not critical)
     ▼
┌────────────────────────────────┐
│ Step 3a: Match Files to        │  Category triggerPaths matching
│ Categories                     │  → Sets run_<category>=true/false
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 3b: Run dotnet-affected   │  MSBuild dependency graph analysis
│                                │  → Finds affected projects
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 3c: Apply Project         │  tests/{name}.Tests/** → test project
│ Mappings                       │  → Discovers test projects by pattern
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 4: Check Unmatched Files  │  Any file not matched by categories,
│ (conservative fallback)        │  solution scope, or mappings
│                                │  → Run everything
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 5: Classify Affected      │  Filter dotnet-affected results
│ Projects                       │  → Separate test vs source projects
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 6: Check NuGet-Dependent  │  Any IsPackable=true project affected?
│ Tests                          │  → Trigger templates, E2E, CLI E2E
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 7: Combine Test Projects  │  Merge: dotnet-affected test projects
│                                │       + project mapping results
│                                │       + NuGet-dependent tests
└────────────────────────────────┘
     │
     ▼
┌────────────────────────────────┐
│ Step 8: Build Final Result     │  Categories from step 3a
│                                │  IntegrationsProjects = all test projects
│                                │  run_integrations = category OR projects
└────────────────────────────────┘
     │
     ▼
  OUTPUT: GitHub Actions outputs + JSON
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
| `global.json` changed | `core` category with `triggerAll: true` → run all tests |
| `NuGet.config` changed | `core` category with `triggerAll: true` → run all tests |
| `*.sln/*.slnx` changed | `core` category with `triggerAll: true` → run all tests |
| `extension/**` changed | `extension` category triggered |
| Test file changes not caught by dotnet-affected | `projectMappings` resolves to test project |
| Any `IsPackable=true` project affected | NuGet-dependent tests triggered |

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

# GitHub Actions format (writes to $GITHUB_OUTPUT)
dotnet run --project tools/Aspire.TestSelector -- \
  --from origin/main --github-output

# Verbose mode
dotnet run --project tools/Aspire.TestSelector -- --from origin/main --verbose
```

### Parameters

| Parameter | Short | Description | Default |
|-----------|-------|-------------|---------|
| `--solution` | `-s` | Path to the solution file | `Aspire.slnx` |
| `--config` | `-c` | Path to the config file | `eng/scripts/test-selection-rules.json` |
| `--from` | `-f` | Git ref to compare from | `origin/main` |
| `--to` | `-t` | Git ref to compare to | `HEAD` |
| `--changed-files` | | Comma-separated changed files (for testing) | |
| `--output` | `-o` | Output file path for JSON | (stdout) |
| `--github-output` | | Write outputs in GitHub Actions format | `false` |
| `--verbose` | `-v` | Enable verbose output | `false` |

## Configuration File

### Location

`eng/scripts/test-selection-rules.json`

### Schema

```json
{
  "$schema": "./test-selection-rules.schema.json",

  "ignorePaths": [
    "*.md",
    "docs/**",
    ".github/workflows/**",
    ".github/actions/**",
    "tools/Aspire.TestSelector/**",
    "tests/Aspire.TestSelector.Tests/**"
  ],

  "projectMappings": [
    {
      "sourcePattern": "src/Components/{name}/**",
      "testPattern": "tests/{name}.Tests/"
    },
    {
      "sourcePattern": "src/Aspire.Hosting.{name}/**",
      "testPattern": "tests/Aspire.Hosting.{name}.Tests/"
    },
    {
      "sourcePattern": "tests/{name}.Tests/**",
      "testPattern": "tests/{name}.Tests/"
    }
  ],

  "categories": {
    "core": {
      "description": "Critical paths - triggers ALL tests",
      "triggerAll": true,
      "triggerPaths": [
        "global.json",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "NuGet.config",
        "tests/Shared/**",
        "tests/Aspire.TestUtilities/**",
        "tests/Aspire.Hosting.Tests/**",
        "*.sln",
        "*.slnx",
        "src/Aspire.Hosting/**"
      ]
    },

    "templates": {
      "description": "Template tests - runs on 3 platforms",
      "triggerPaths": [
        "src/Aspire.ProjectTemplates/**",
        "tests/Aspire.Templates.Tests/**"
      ]
    },

    "extension": {
      "description": "VS Code extension tests",
      "triggerPaths": [
        "extension/**"
      ]
    },

    "integrations": {
      "description": "Integration tests for components, dashboard, and hosting extensions",
      "triggerPaths": [
        "src/Aspire.Dashboard/**",
        "src/Aspire.Hosting.*/**",
        "src/Aspire.*/**",
        "src/Components/**",
        "src/Shared/**",
        "tests/Aspire.*.Tests/**"
      ],
      "excludePaths": [
        "src/Aspire.ProjectTemplates/**",
        "src/Aspire.Cli/**",
        "src/Aspire.Hosting/**",
        "tests/Aspire.Templates.Tests/**",
        "tests/Aspire.Cli.EndToEnd.Tests/**",
        "tests/Aspire.EndToEnd.Tests/**"
      ]
    }
  }
}
```

### Configuration Sections

| Section | Description |
|---------|-------------|
| `ignorePaths` | Glob patterns for files that don't trigger any tests |
| `projectMappings` | Pattern-based mappings from source/test files to test project directories. Uses `{name}` capture groups. |
| `categories` | Test category definitions with `triggerPaths`, optional `excludePaths`, and optional `triggerAll` flag |

### Category Properties

| Property | Type | Description |
|----------|------|-------------|
| `description` | string | Human-readable description |
| `triggerAll` | bool | If true, matching files trigger ALL tests (not just this category) |
| `triggerPaths` | string[] | Glob patterns that trigger this category |
| `excludePaths` | string[] | Glob patterns to exclude from matching (optional) |

## GitHub Actions Output

When run with `--github-output`, the tool writes these outputs:

| Output | Description |
|--------|-------------|
| `run_all` | `true` if all tests should run (critical path, error fallback) |
| `run_<category>` | `true`/`false` for each category defined in config |
| `run_integrations` | `true` if the integrations category is triggered by paths **OR** if any test projects were discovered via dotnet-affected/project mappings |
| `integrations_projects` | JSON array of test project paths for matrix builds |

**Important**: `run_integrations` merges two signals:
1. The `integrations` category being triggered by its `triggerPaths`
2. Any test projects being discovered via `dotnet-affected` or `projectMappings`

This means even if no files match `integrations` trigger paths, `run_integrations` will be `true` if test projects were found through other mechanisms (e.g., a test file changed and was resolved via project mappings).

## Output JSON Structure

```json
{
  "runAllTests": false,
  "reason": "msbuild_analysis",

  "categories": {
    "core": false,
    "templates": true,
    "cli_e2e": false,
    "endtoend": false,
    "integrations": false,
    "extension": false,
    "infrastructure": false
  },

  "affectedTestProjects": [
    "tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj"
  ],

  "integrationsProjects": [
    "tests/Aspire.Hosting.Tests/"
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
| `core` | Critical infrastructure | `triggerAll: true` — any match runs ALL tests |
| `templates` | Template tests (requires NuGets) | Template paths OR packable project affected |
| `cli_e2e` | CLI end-to-end tests (requires NuGets) | CLI paths OR packable project affected |
| `endtoend` | General E2E tests (requires NuGets) | E2E paths OR packable project affected |
| `integrations` | Integration tests | Path matching OR test projects found via dotnet-affected/project mappings |
| `extension` | VS Code extension tests | `extension/**` changes |
| `infrastructure` | Infrastructure tests | `tests/Infrastructure.Tests/**` changes |

## NuGet-Dependent Tests

Tests in these projects require built NuGet packages:
- `tests/Aspire.Templates.Tests/`
- `tests/Aspire.EndToEnd.Tests/`
- `tests/Aspire.Cli.EndToEnd.Tests/`

**Logic**: If any affected source project has `IsPackable=true`, all NuGet-dependent tests are triggered.

## Rule Evaluation Examples

### Example 1: Test file changed (project mappings)

**Changed files:**
- `tests/Infrastructure.Tests/Filtering/ProjectFilterTests.cs`
- `extension/Extension.proj`

**Result:**
- `extension` category triggered by `extension/Extension.proj`
- `Infrastructure.Tests` resolved via project mapping `tests/{name}.Tests/**`
- `run_extension=true`, `run_integrations=true` (due to discovered test project)
- `integrations_projects=["tests/Infrastructure.Tests/"]`

### Example 2: Critical file changed

**Changed files:**
- `global.json`

**Result:**
```json
{
  "runAllTests": true,
  "reason": "critical_path",
  "triggerFile": "global.json",
  "triggerPattern": "global.json"
}
```
All `run_<category>` outputs set to `true`.

### Example 3: Only docs changed

**Changed files:**
- `docs/getting-started.md`
- `README.md`

**Result:**
```json
{
  "runAllTests": false,
  "reason": "all_ignored"
}
```
All `run_<category>` outputs set to `false`.

### Example 4: Extension files only

**Changed files:**
- `extension/package.json`
- `extension/src/index.ts`

**Result:**
- `run_extension=true`
- All other categories `false`
- `integrations_projects=[]`

### Example 5: Source project changed (dotnet-affected)

**Changed files:**
- `src/Aspire.Dashboard/Components/Layout.razor`

**Result:**
- `dotnet-affected` finds `Aspire.Dashboard.Tests` as affected
- `integrations` category triggered by path matching `src/Aspire.Dashboard/**`
- `run_integrations=true`
- `integrations_projects=["tests/Aspire.Dashboard.Tests/"]`

## CI Integration

### GitHub Actions

The workflow runs the tool in the `detect_scope` job:

```yaml
jobs:
  detect_scope:
    runs-on: ubuntu-latest
    outputs:
      run_all: ${{ steps.detect.outputs.run_all }}
      run_extension: ${{ steps.detect.outputs.run_extension }}
      run_integrations: ${{ steps.detect.outputs.run_integrations }}
      # ... other categories
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Install dotnet-affected
        if: github.event_name == 'pull_request'
        run: dotnet tool install --global dotnet-affected --prerelease

      - name: Evaluate changed paths
        id: detect
        run: |
          if [ "${{ github.event_name }}" == "pull_request" ]; then
            dotnet run --project tools/Aspire.TestSelector/Aspire.TestSelector.csproj -- \
              --config eng/scripts/test-selection-rules.json \
              --from "${{ github.event.pull_request.base.sha }}" \
              --to "${{ github.event.pull_request.head.sha }}" \
              --github-output --verbose
          else
            # Non-PR: run everything
            echo "run_all=true" >> $GITHUB_OUTPUT
            echo "run_extension=true" >> $GITHUB_OUTPUT
            # ... all categories true
          fi
```

### Conditional Jobs

```yaml
extension_tests_win:
  needs: detect_scope
  if: ${{ needs.detect_scope.outputs.run_all == 'true' || needs.detect_scope.outputs.run_extension == 'true' }}

integrations_test_lin:
  needs: [detect_scope, setup_for_tests_lin]
  if: ${{ needs.detect_scope.outputs.run_all == 'true' || needs.detect_scope.outputs.run_integrations == 'true' }}
```

### Event Type Behavior

| Event | Behavior |
|-------|----------|
| `pull_request` | Evaluate changed files, skip unaffected categories |
| `push` (to main) | Run all tests |
| `schedule` | Run all tests |
| `workflow_dispatch` | Run all tests |

## Dependencies

The tool requires:
- `dotnet-affected` CLI tool (installed via `dotnet tool install --global dotnet-affected --prerelease`)
- .NET 10.0 SDK

## File Structure

```text
tools/Aspire.TestSelector/
├── Aspire.TestSelector.csproj
├── Program.cs                       # CLI entry point + evaluation logic
├── CategoryMapper.cs                # Map files → categories via triggerPaths
├── DiagnosticLogger.cs              # Verbose logging helper
├── Models/
│   ├── TestSelectionResult.cs       # Output model + GitHub output writer
│   └── TestSelectorConfig.cs        # Config model for JSON deserialization
└── Analyzers/
    ├── CriticalFileDetector.cs      # Check triggerAll categories
    ├── IgnorePathFilter.cs          # Filter ignorePaths
    ├── DotNetAffectedRunner.cs      # Wraps dotnet-affected CLI
    ├── TestProjectFilter.cs         # Filter IsTestProject=true
    ├── NuGetDependencyChecker.cs    # Check IsPackable → NuGet tests
    └── ProjectMappingResolver.cs    # Resolve projectMappings patterns

eng/scripts/
├── test-selection-rules.json        # Configuration rules
└── test-selection-rules.schema.json # JSON schema for validation
```

## References

- [dotnet-affected](https://github.com/leonardochaia/dotnet-affected) - MSBuild dependency analysis tool
- [MSBuild.Prediction](https://github.com/microsoft/MSBuildPrediction) - Input/output prediction for MSBuild projects
