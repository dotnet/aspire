# Conditional Test Runs

## Overview

Conditional test runs is a CI optimization feature that selectively runs tests based on which files have changed in a pull request. Instead of running all tests for every change, the system uses MSBuild dependency analysis (`dotnet-affected`) combined with category-based rules and project mappings to determine exactly which test projects are affected, reducing CI time and resource usage.

## Motivation

Running the full test suite for every pull request is expensive:

- **CI Efficiency**: Integration tests run on 3 platforms with ~20 projects each. Running all tests can take significant time and compute resources.
- **Faster Feedback**: Developers get quicker feedback when only relevant tests run.
- **Resource Optimization**: Reduces GitHub Actions minutes and runner usage.

## How It Works

The system combines MSBuild's project reference graph with configurable rules:

1. **Get changed files** from git
2. **Filter out ignored files** (docs, workflows, etc.)
3. **Check for critical infrastructure changes** — categories with `triggerAll: true` run everything
4. **Match files to categories** via `triggerPaths` patterns — sets `run_<category>` flags
5. **Run `dotnet-affected`** to find all projects affected by the changes
6. **Apply project mappings** — resolve test files to test project directories via `{name}` capture patterns
7. **Check for unmatched files** — conservative fallback runs everything if any file is unaccounted for
8. **Filter to test projects** from dotnet-affected results
9. **Check NuGet dependencies** — if packable projects are affected, trigger template/E2E tests
10. **Combine test projects** from dotnet-affected + project mappings + NuGet dependencies
11. **Output results** — `run_integrations` is `true` if the category is triggered by paths OR if any test projects were discovered

The key insight is that MSBuild already knows the dependency graph. If you change `src/Aspire.Dashboard/`, MSBuild knows that `tests/Aspire.Dashboard.Tests/` depends on it. We don't need to manually maintain those mappings.

## Architecture

### 1. Test Selector Tool (`tools/Aspire.TestSelector`)

A C# CLI tool that:
- Reads the configuration file
- Gets changed files from git
- Matches files to categories via `triggerPaths`
- Runs `dotnet-affected` for MSBuild dependency analysis
- Resolves test projects via `projectMappings`
- Outputs JSON results and GitHub Actions outputs

### 2. Configuration File (`eng/scripts/test-selection-rules.json`)

A JSON file that defines:
- **ignorePaths**: Files that never trigger tests (docs, workflows)
- **categories**: Category definitions with `triggerPaths`, optional `excludePaths`, and optional `triggerAll` flag
- **projectMappings**: Pattern-based mappings from source/test files to test project directories

### 3. GitHub Actions Integration

The workflow runs the tool and uses the output to conditionally run test jobs.

## Configuration Reference

### `ignorePaths`

Files matching these glob patterns are completely ignored — they don't trigger any tests.

```json
"ignorePaths": [
  "*.md",
  "docs/**",
  ".github/workflows/**",
  ".github/actions/**",
  "tools/Aspire.TestSelector/**",
  "tests/Aspire.TestSelector.Tests/**"
]
```

### `categories`

Category definitions with trigger paths. Each category maps to a `run_<category>` output in CI.

```json
"categories": {
  "core": {
    "description": "Critical paths - triggers ALL tests",
    "triggerAll": true,
    "triggerPaths": [
      "global.json",
      "Directory.Build.props",
      "Directory.Packages.props",
      "tests/Shared/**",
      "src/Aspire.Hosting/**"
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
      "src/Aspire.*/**",
      "src/Components/**",
      "tests/Aspire.*.Tests/**"
    ],
    "excludePaths": [
      "src/Aspire.ProjectTemplates/**",
      "src/Aspire.Cli/**",
      "src/Aspire.Hosting/**"
    ]
  }
}
```

Category properties:

| Property | Type | Description |
|----------|------|-------------|
| `description` | string | Human-readable description |
| `triggerAll` | bool | If true, matching files trigger ALL tests (not just this category) |
| `triggerPaths` | string[] | Glob patterns that trigger this category |
| `excludePaths` | string[] | Glob patterns to exclude from matching (optional) |

### `projectMappings`

Pattern-based mappings that resolve changed files to test project directories. Uses `{name}` as a capture group.

```json
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
]
```

When a changed file matches a `sourcePattern`, the captured `{name}` is substituted into `testPattern` to identify the test project directory. Test projects discovered this way are added to `integrations_projects` and trigger `run_integrations=true`.

## Evaluation Algorithm

1. **Filter Ignored Files**: Remove files matching `ignorePaths`. If all files are ignored, no tests run.

2. **Check Critical Paths**: If any file matches a category with `triggerAll: true`, ALL tests run.

3. **Match Files to Categories**: Apply each category's `triggerPaths` (minus `excludePaths`) to set `run_<category>` flags.

4. **Run dotnet-affected**: Find all MSBuild projects affected by the changed files.

5. **Apply Project Mappings**: Resolve changed files to test project directories via `{name}` capture patterns.

6. **Check Unmatched Files**: If any active file isn't matched by categories, solution scope, or project mappings, conservatively run all tests.

7. **Filter Test Projects**: Identify which affected projects are test projects (`IsTestProject=true`).

8. **Check NuGet Dependencies**: If any affected source projects are packable (`IsPackable=true`), trigger NuGet-dependent tests (templates, E2E tests).

9. **Combine and Output**: Merge test projects from all sources. Set `run_integrations=true` if the integrations category is triggered OR if any test projects were discovered.

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

# GitHub Actions format
dotnet run --project tools/Aspire.TestSelector -- --from origin/main --github-output
```

### Options

| Option | Short | Description |
|--------|-------|-------------|
| `--solution` | `-s` | Path to solution file (default: `Aspire.slnx`) |
| `--config` | `-c` | Path to config file (default: `eng/scripts/test-selection-rules.json`) |
| `--from` | `-f` | Git ref to compare from (default: `origin/main`) |
| `--to` | `-t` | Git ref to compare to (default: `HEAD`) |
| `--changed-files` | | Comma-separated list of files (bypasses git) |
| `--output` | `-o` | Output file path for JSON result |
| `--github-output` | | Write outputs in GitHub Actions format |
| `--verbose` | `-v` | Enable verbose output |

### Output Format

```json
{
  "runAllTests": false,
  "reason": "msbuild_analysis",
  "categories": {
    "extension": true,
    "integrations": false,
    "infrastructure": true
  },
  "affectedTestProjects": [
    "tests/Infrastructure.Tests/"
  ],
  "integrationsProjects": [
    "tests/Infrastructure.Tests/"
  ],
  "changedFiles": [
    "tests/Infrastructure.Tests/Filtering/ProjectFilterTests.cs",
    "extension/Extension.proj"
  ],
  "dotnetAffectedProjects": [],
  "nugetDependentTests": null
}
```

### GitHub Actions Outputs

| Output | Description |
|--------|-------------|
| `run_all` | `true` if all tests should run |
| `run_<category>` | `true`/`false` per category |
| `run_integrations` | `true` if category triggered by paths OR test projects discovered |
| `integrations_projects` | JSON array of test project paths |

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

- **Analyzers**: IgnorePathFilter, CriticalFileDetector, TestProjectFilter, DotNetAffectedRunner, NuGetDependencyChecker, ProjectMappingResolver
- **Models**: TestSelectorConfig, TestSelectionResult (including GitHub output generation)
- **CategoryMapper**: File-to-category matching via triggerPaths/excludePaths
- **Path normalization**: Forward/back slash handling across platforms

## Troubleshooting

### All Tests Running Unexpectedly

**Cause**: A changed file matches a `triggerAll` category, or a file is unmatched by any rule (conservative fallback).

**Solution**: Run with `--verbose` to see which file triggered it. If it's an unmatched file, add it to `ignorePaths` or ensure it's covered by a category or project mapping.

### Tests Not Running When Expected

**Cause**: Missing MSBuild project reference, file is being ignored, or missing project mapping.

**Solution**:
1. Check if file matches `ignorePaths`
2. Verify project references exist in the `.csproj` files
3. Check if a `projectMapping` entry is needed
4. Run `dotnet affected` manually to debug

### Non-.NET Files Not Triggering Tests

**Cause**: Files like extension code can't be analyzed by `dotnet-affected`.

**Solution**: Add a category with `triggerPaths` matching the file pattern.

## Dependencies

- .NET 10.0 SDK
- `dotnet-affected` global tool: `dotnet tool install --global dotnet-affected --prerelease`
- Git
