# Conditional Test Runs

## Overview

Conditional test runs is a CI optimization feature that selectively runs tests based on which files have changed in a pull request. Instead of running all tests for every change, the system uses MSBuild dependency analysis (`dotnet-affected`) to determine exactly which test projects are affected, reducing CI time and resource usage.

## Motivation

Running the full test suite for every pull request is expensive:

- **CI Efficiency**: Integration tests run on 3 platforms with ~20 projects each. Running all tests can take significant time and compute resources.
- **Faster Feedback**: Developers get quicker feedback when only relevant tests run.
- **Resource Optimization**: Reduces GitHub Actions minutes and runner usage.

## How It Works

The system is intentionally simple - it relies on MSBuild's project reference graph rather than manual pattern-based mappings:

1. **Get changed files** from git
2. **Filter out ignored files** (docs, workflows, etc.)
3. **Check for critical infrastructure changes** - if build files change, run everything
4. **Run `dotnet-affected`** to find all projects affected by the changes
5. **Filter to test projects** and output the list

The key insight is that MSBuild already knows the dependency graph. If you change `src/Aspire.Dashboard/`, MSBuild knows that `tests/Aspire.Dashboard.Tests/` depends on it. We don't need to manually maintain those mappings.

## Architecture

### 1. Test Selector Tool (`tools/Aspire.TestSelector`)

A C# CLI tool that:
- Reads the configuration file
- Gets changed files from git
- Runs `dotnet-affected` for MSBuild dependency analysis
- Outputs JSON results for GitHub Actions

### 2. Configuration File (`eng/scripts/test-selector-config.json`)

A minimal JSON file that defines:
- **ignorePaths**: Files that never trigger tests (docs, workflows)
- **triggerAllPaths**: Critical infrastructure that triggers all tests
- **nonDotNetRules**: Rules for files that `dotnet-affected` can't analyze

### 3. GitHub Actions Integration

The workflow runs the tool and uses the output to conditionally run test jobs.

## Configuration Reference

### `ignorePaths`

Files matching these glob patterns are completely ignored - they don't trigger any tests.

```json
"ignorePaths": [
  "*.md",
  "docs/**",
  ".github/workflows/**",
  "eng/pipelines/**"
]
```

### `triggerAllPaths` / `triggerAllExclude`

Critical infrastructure files that trigger ALL tests when changed. These are files that affect the entire build system.

```json
"triggerAllPaths": [
  "global.json",
  "Directory.Build.props",
  "Directory.Packages.props",
  "eng/Versions.props",
  "tests/Shared/**",
  "src/Aspire.Hosting/**"
],
"triggerAllExclude": [
  "eng/pipelines/**"
]
```

### `nonDotNetRules`

Rules for files that `dotnet-affected` cannot analyze. These map file patterns to categories for CI workflow routing.

```json
"nonDotNetRules": [
  {
    "pattern": "extension/**",
    "category": "extension"
  },
  {
    "pattern": "playground/**",
    "category": "endtoend"
  }
]
```

### `categories`

Minimal category definitions. Most test discovery is automatic via `dotnet-affected`.

```json
"categories": {
  "extension": {
    "description": "VS Code extension tests (non-.NET)",
    "testProjects": []
  },
  "integrations": {
    "description": "All .NET tests - discovered via dotnet-affected",
    "testProjects": "auto"
  }
}
```

## Evaluation Algorithm

1. **Get Changed Files**: Retrieve files from `git diff` between base and head refs.

2. **Filter Ignored Files**: Remove files matching `ignorePaths`. If all files are ignored, no tests run.

3. **Check Critical Paths**: If any file matches `triggerAllPaths` (excluding `triggerAllExclude`), ALL tests run.

4. **Check Non-.NET Rules**: Apply `nonDotNetRules` for files like `extension/**` that can't be analyzed by MSBuild.

5. **Run dotnet-affected**: Find all MSBuild projects affected by the changed files.

6. **Filter Test Projects**: Identify which affected projects are test projects (`IsTestProject=true` or has test SDK references).

7. **Check NuGet Dependencies**: If any affected source projects are packable (`IsPackable=true`), trigger NuGet-dependent tests (templates, E2E tests).

8. **Output Results**: Generate JSON with affected test projects for CI consumption.

## Tool Usage

### CLI Reference

```bash
# Basic usage - compare against origin/main
dotnet run --project tools/Aspire.TestSelector -- --from origin/main

# With verbose output
dotnet run --project tools/Aspire.TestSelector -- --from origin/main --verbose

# Test with explicit changed files
dotnet run --project tools/Aspire.TestSelector -- \
  --changed-files "src/Aspire.Dashboard/App.razor" --verbose

# Output to file
dotnet run --project tools/Aspire.TestSelector -- --from origin/main --output result.json
```

### Options

| Option | Short | Description |
|--------|-------|-------------|
| `--solution` | `-s` | Path to solution file (default: `Aspire.slnx`) |
| `--config` | `-c` | Path to config file (default: `eng/scripts/test-selector-config.json`) |
| `--from` | `-f` | Git ref to compare from (default: `origin/main`) |
| `--to` | `-t` | Git ref to compare to (default: `HEAD`) |
| `--changed-files` | | Comma-separated list of files (bypasses git) |
| `--output` | `-o` | Output file path for JSON result |
| `--verbose` | `-v` | Enable verbose output |

### Output Format

```json
{
  "runAllTests": false,
  "reason": "msbuild_analysis",
  "affectedTestProjects": [
    "tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj",
    "tests/Aspire.Dashboard.Components.Tests/Aspire.Dashboard.Components.Tests.csproj"
  ],
  "changedFiles": [
    "src/Aspire.Dashboard/Components/Layout.razor"
  ],
  "dotnetAffectedProjects": [
    "src/Aspire.Dashboard/Aspire.Dashboard.csproj",
    "tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj"
  ],
  "nugetDependentTests": null
}
```

When packable projects are affected:

```json
{
  "nugetDependentTests": {
    "triggered": true,
    "reason": "IsPackable projects affected: Aspire.Hosting",
    "affectedPackableProjects": ["src/Aspire.Hosting/Aspire.Hosting.csproj"],
    "projects": [
      "tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj",
      "tests/Aspire.EndToEnd.Tests/Aspire.EndToEnd.Tests.csproj",
      "tests/Aspire.Cli.EndToEnd.Tests/Aspire.Cli.EndToEnd.Tests.csproj"
    ]
  }
}
```

## NuGet-Dependent Tests

Some tests require built NuGet packages (templates, E2E tests). These are automatically triggered when any packable project (`IsPackable=true`) is affected:

- `tests/Aspire.Templates.Tests/`
- `tests/Aspire.EndToEnd.Tests/`
- `tests/Aspire.Cli.EndToEnd.Tests/`

This is handled by `NuGetDependencyChecker` without needing config entries.

## Testing

### Running Tool Tests

```bash
# Run all test selector tests
dotnet test tests/Aspire.TestSelector.Tests

# Run specific test categories
dotnet test tests/Aspire.TestSelector.Tests --filter "FullyQualifiedName~IgnorePathFilter"
dotnet test tests/Aspire.TestSelector.Tests --filter "FullyQualifiedName~CriticalFileDetector"
```

### Test Coverage

The test project provides coverage for:

- **Analyzers**: IgnorePathFilter, CriticalFileDetector, NonDotNetRulesHandler, TestProjectFilter, DotNetAffectedRunner, NuGetDependencyChecker
- **Models**: TestSelectorConfig, TestSelectionResult, TestProjectsConverter
- **Path normalization**: Forward/back slash handling across platforms

## Troubleshooting

### All Tests Running Unexpectedly

**Cause**: A changed file matches `triggerAllPaths`.

**Solution**: Run with `--verbose` to see which file triggered it. Add to `triggerAllExclude` if it shouldn't trigger all tests.

### Tests Not Running When Expected

**Cause**: Missing MSBuild project reference, or file is being ignored.

**Solution**:
1. Check if file matches `ignorePaths`
2. Verify project references exist in the `.csproj` files
3. Run `dotnet affected` manually to debug

### Non-.NET Files Not Triggering Tests

**Cause**: Files like extension code can't be analyzed by `dotnet-affected`.

**Solution**: Add a `nonDotNetRules` entry mapping the pattern to a category.

## Dependencies

- .NET 10.0 SDK
- `dotnet-affected` global tool: `dotnet tool install -g dotnet-affected`
- Git
