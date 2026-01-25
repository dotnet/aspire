# Conditional Test Runs

## Overview

Conditional test runs is a CI optimization feature that selectively runs tests based on which files have changed in a pull request. Instead of running all tests for every change, the system analyzes the changed files and determines which test categories are affected, reducing CI time and resource usage.

## Motivation

Running the full test suite for every pull request is expensive:

- **CI Efficiency**: Integration tests run on 3 platforms with ~20 projects each. Running all tests can take significant time and compute resources.
- **Faster Feedback**: Developers get quicker feedback when only relevant tests run.
- **Resource Optimization**: Reduces GitHub Actions minutes and runner usage.

The conditional test runs feature addresses these concerns by intelligently selecting which tests to run based on the actual changes in a PR.

## Architecture

The system consists of three main components:

### 1. Configuration File (`eng/scripts/test-selection-rules.json`)

A JSON file that defines:
- Which paths to ignore (documentation, workflows, etc.)
- How to map source files to test projects
- Test categories with their trigger patterns

### 2. PowerShell Evaluation Engine (`eng/scripts/Evaluate-TestSelection.ps1`)

A PowerShell script that:
- Reads the configuration file
- Gets the list of changed files from git
- Evaluates which categories should run
- Outputs the results for GitHub Actions

### 3. GitHub Actions Integration (`.github/workflows/tests.yml`)

The workflow that:
- Runs the evaluation script
- Uses the outputs to conditionally run test jobs
- Passes project filters to test enumeration

## Configuration Reference

### `ignorePaths`

Files matching these glob patterns are completely ignored - they don't trigger any tests and don't cause a conservative fallback.

```json
"ignorePaths": [
  ".editorconfig",
  ".gitignore",
  "*.md",
  "docs/**",
  "eng/**",
  ".github/workflows/**"
]
```

### `projectMappings`

Convention-based mappings that automatically discover which test project corresponds to a source file. Uses `{name}` as a placeholder for pattern matching.

```json
"projectMappings": [
  {
    "sourcePattern": "src/Components/{name}/**",
    "testPattern": "tests/{name}.Tests/"
  },
  {
    "sourcePattern": "src/Aspire.Hosting.{name}/**",
    "testPattern": "tests/Aspire.Hosting.{name}.Tests/",
    "exclude": ["src/Aspire.Hosting.Testing/**"]
  }
]
```

| Property | Description |
|----------|-------------|
| `sourcePattern` | Glob pattern with `{name}` placeholder for source files |
| `testPattern` | Pattern with `{name}` placeholder for the test project path |
| `exclude` | Optional glob patterns for paths to exclude from this mapping |

### `categories`

Test categories with their trigger rules.

```json
"categories": {
  "core": {
    "description": "Critical paths - triggers ALL tests",
    "triggerAll": true,
    "triggerPaths": [
      "global.json",
      "Directory.Build.props",
      "src/Aspire.Hosting/**"
    ]
  },
  "templates": {
    "description": "Template tests",
    "triggerPaths": [
      "src/Aspire.ProjectTemplates/**",
      "tests/Aspire.Templates.Tests/**"
    ],
    "projects": [
      "tests/Aspire.Templates.Tests/"
    ]
  },
  "integrations": {
    "description": "Integration tests",
    "triggerPaths": [
      "src/Aspire.*/**",
      "src/Components/**"
    ],
    "excludePaths": [
      "src/Aspire.ProjectTemplates/**",
      "src/Aspire.Cli/**"
    ],
    "projects": []
  }
}
```

| Property | Description |
|----------|-------------|
| `description` | Human-readable description |
| `triggerAll` | If true and any path matches, ALL tests run |
| `triggerPaths` | Glob patterns that trigger this category |
| `excludePaths` | Glob patterns excluded from triggering this category |
| `projects` | Explicit list of test projects for this category |

## Categories

The following test categories are defined:

| Category | Description | Trigger Behavior |
|----------|-------------|------------------|
| `core` | Critical infrastructure | Triggers ALL tests (`triggerAll: true`) |
| `templates` | Template tests | Runs template tests on 3 platforms |
| `cli_e2e` | CLI end-to-end tests | Runs CLI E2E tests |
| `endtoend` | General E2E tests | Runs E2E tests |
| `integrations` | Integration tests | Runs integration tests with project filtering |
| `extension` | VS Code extension tests | Runs extension tests |

## Evaluation Algorithm

1. **Get Changed Files**: Retrieve the list of changed files from git diff or test input.

2. **Filter Ignored Files**: Remove files matching `ignorePaths` patterns.

3. **Check for No Changes**: If no active files remain, no tests run.

4. **Check TriggerAll Patterns**: For each category with `triggerAll: true`, if any file matches a trigger pattern, ALL tests run immediately.

5. **Evaluate Categories**: For each non-triggerAll category:
   - Check if file matches any `triggerPaths`
   - If matched, check if file also matches any `excludePaths`
   - If not excluded, mark category as triggered

6. **Conservative Fallback**: If any active file doesn't match ANY category's trigger patterns, run ALL tests (fail-safe behavior).

7. **Apply Project Mappings**: For triggered categories, use `projectMappings` to determine which specific test projects should run.

8. **Output Results**: Return the list of enabled categories and specific projects to run.

## Usage Guide

### Adding a New Category

1. Edit `eng/scripts/test-selection-rules.json`
2. Add a new entry under `categories`:

```json
"my_new_category": {
  "description": "My new test category",
  "triggerPaths": [
    "src/MyFeature/**",
    "tests/MyFeature.Tests/**"
  ],
  "projects": [
    "tests/MyFeature.Tests/"
  ]
}
```

3. Update the GitHub Actions workflow to handle the new category output.

### Modifying Existing Rules

1. Identify the category in `test-selection-rules.json`
2. Modify `triggerPaths` or `excludePaths` as needed
3. Test locally using the `-TestFiles` flag

### Testing Locally

Use the `-TestFiles` flag to simulate which tests would run for specific files:

```bash
# Test with a single file
./eng/scripts/Evaluate-TestSelection.ps1 -TestFiles "src/Aspire.Dashboard/Components/Layout.razor" -DryRun

# Test with multiple files
./eng/scripts/Evaluate-TestSelection.ps1 -TestFiles "src/Aspire.Cli/Commands/NewCommand.cs src/Aspire.Dashboard/Foo.cs" -DryRun

# Test with no changes
./eng/scripts/Evaluate-TestSelection.ps1 -TestFiles "" -DryRun
```

### Running C# Tests

```bash
# Build and run all Infrastructure tests
dotnet test tests/Infrastructure.Tests

# Run specific test categories
dotnet test tests/Infrastructure.Tests --filter "FullyQualifiedName~TestSelection"
dotnet test tests/Infrastructure.Tests --filter "FullyQualifiedName~Filtering"
```

## Troubleshooting

### All Tests Running When They Shouldn't

**Cause**: A changed file doesn't match any category's trigger patterns, causing conservative fallback.

**Solution**:
1. Run the evaluation script with `-DryRun` to see which file is unmatched
2. Either add the file to `ignorePaths` or add appropriate trigger patterns

### Tests Not Running When They Should

**Cause**: File matches an `excludePaths` pattern or doesn't match any `triggerPaths`.

**Solution**:
1. Check if the file path matches the expected `triggerPaths` glob patterns
2. Verify the file isn't being excluded by `excludePaths`
3. Add the path pattern to the appropriate category

### Wrong Test Projects Running

**Cause**: `projectMappings` patterns don't match the file path correctly.

**Solution**:
1. Verify the `sourcePattern` matches the source file path
2. Check that `{name}` captures the correct part of the path
3. Ensure the `testPattern` produces the correct test project path

### Debugging Glob Patterns

Use the C# test project to verify glob pattern behavior:

```csharp
// In a test or debug session
var matches = GlobPatternMatcher.IsMatch("src/Aspire.Dashboard/Foo.cs", "src/Aspire.*/**");
// Returns: true

var captured = GlobPatternMatcher.TryMatchSourcePattern(
    "src/Components/Aspire.Npgsql/Extension.cs",
    "src/Components/{name}/**",
    out var name);
// captured: true, name: "Aspire.Npgsql"
```
