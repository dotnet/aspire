# Test Selection by Changed Paths

## Overview

This document describes a system for determining which test projects/collections to run based on git changes on a branch. The goal is to optimize CI time by running only relevant tests while ensuring coverage is not compromised.

## Problem Statement

Currently, all tests run on every change regardless of what files were modified. This leads to:
- Longer CI times
- Unnecessary resource consumption
- Slower feedback loops for developers

We need a system that can:
- Detect which files changed
- Map changes to relevant test categories/projects
- Handle complex rules (triggers, exclusions, dependencies)
- Fall back to running all tests when uncertain

## Design Goals

1. **Portable**: Works for both GitHub Actions and Azure DevOps
2. **Configurable**: Rules defined in a separate config file, not hardcoded
3. **Convention-based**: Automatic mapping where possible (e.g., `src/Components/X` → `tests/X.Tests/`)
4. **Safe**: When in doubt, run all tests
5. **Debuggable**: Clear output explaining why each decision was made

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  eng/scripts/test-selection-rules.json   (Configuration)        │
│  - Define categories, trigger paths, exclusions, dependencies   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  eng/scripts/Evaluate-TestSelection.ps1  (Evaluator Script)     │
│  - Get changed files from git                                   │
│  - Apply rules from config                                      │
│  - Output JSON result                                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Output JSON                                                    │
│  { "run_all": false, "categories": { ... }, "projects": [...] } │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
     GitHub Actions                    Azure DevOps
     (parse JSON, set outputs)         (parse JSON, set variables)
```

## Configuration File

### Location

`eng/scripts/test-selection-rules.json`

### Schema

```json
{
  "$schema": "./test-selection-rules.schema.json",

  "conventions": {
    "componentMapping": {
      "sourcePattern": "src/Components/{name}/**",
      "testPattern": "tests/{name}.Tests/"
    }
  },

  "categories": {
    "<category-name>": {
      "description": "Human-readable description",
      "triggerAll": false,
      "runByDefault": false,
      "triggerPaths": ["glob/pattern/**"],
      "excludeWhenOnly": ["other-category"],
      "alsoTriggers": ["other-category"],
      "useConvention": false,
      "projects": ["tests/Project.Tests/"]
    }
  }
}
```

### Category Properties

| Property | Type | Description |
|----------|------|-------------|
| `description` | string | Human-readable description of the category |
| `triggerAll` | boolean | If any path matches, set `run_all: true` and run everything |
| `runByDefault` | boolean | Category runs whenever tests run (unless excluded) |
| `triggerPaths` | string[] | Glob patterns - if any match, category is triggered |
| `excludeWhenOnly` | string[] | Skip this category if changes are ONLY in listed categories |
| `alsoTriggers` | string[] | If this category triggers, also enable these categories |
| `useConvention` | boolean | Auto-map source paths to test projects via conventions |
| `projects` | string[] | Explicit list of test projects for this category |

### Conventions

The `conventions` section defines automatic mappings:

```json
"conventions": {
  "componentMapping": {
    "sourcePattern": "src/Components/{name}/**",
    "testPattern": "tests/{name}.Tests/"
  }
}
```

When `useConvention: true` is set on a category, changed files matching `sourcePattern` will automatically map to the corresponding test project using `testPattern`.

Example: A change to `src/Components/Aspire.Npgsql/Client.cs` maps to `tests/Aspire.Npgsql.Tests/`

## Example Configuration

```json
{
  "$schema": "./test-selection-rules.schema.json",

  "conventions": {
    "componentMapping": {
      "sourcePattern": "src/Components/{name}/**",
      "testPattern": "tests/{name}.Tests/"
    }
  },

  "categories": {
    "core": {
      "description": "Central/shared files - triggers ALL tests",
      "triggerAll": true,
      "triggerPaths": [
        "global.json",
        "Directory.Build.props",
        "Directory.Packages.props",
        "eng/Versions.props",
        "src/Shared/**",
        "eng/pipelines/**"
      ]
    },

    "default": {
      "description": "Standard unit/integration tests - always run unless only isolated changes",
      "runByDefault": true,
      "excludeWhenOnly": ["templates", "e2e", "dashboard"],
      "projects": [
        "tests/Aspire.Hosting.Tests/",
        "tests/Aspire.Components.Common.Tests/"
      ]
    },

    "templates": {
      "description": "Template tests - isolated, only when template paths change",
      "triggerPaths": [
        "src/Aspire.ProjectTemplates/**",
        "tests/Aspire.ProjectTemplates.Tests/**",
        "tests/Shared/WorkloadTesting/**"
      ],
      "projects": [
        "tests/Aspire.ProjectTemplates.Tests/"
      ]
    },

    "e2e": {
      "description": "End-to-end CLI tests - isolated",
      "triggerPaths": [
        "src/Aspire.Cli/**",
        "tests/Aspire.Cli.EndToEndTests/**"
      ],
      "projects": [
        "tests/Aspire.Cli.EndToEndTests/"
      ]
    },

    "dashboard": {
      "description": "Dashboard tests - isolated",
      "triggerPaths": [
        "src/Aspire.Dashboard/**",
        "tests/Aspire.Dashboard.Tests/**"
      ],
      "projects": [
        "tests/Aspire.Dashboard.Tests/"
      ]
    },

    "hosting": {
      "description": "Hosting infrastructure tests",
      "triggerPaths": [
        "src/Aspire.Hosting/**",
        "src/Aspire.Hosting.*/**"
      ],
      "alsoTriggers": ["default"],
      "useConvention": true
    },

    "components": {
      "description": "Component tests - uses convention mapping",
      "triggerPaths": [
        "src/Components/**"
      ],
      "useConvention": true
    }
  }
}
```

## PowerShell Script

### Location

`eng/scripts/Evaluate-TestSelection.ps1`

### Interface

```powershell
# Basic usage (defaults to HEAD~1)
./eng/scripts/Evaluate-TestSelection.ps1

# Custom diff target
./eng/scripts/Evaluate-TestSelection.ps1 -DiffTarget "origin/main...HEAD"

# Output to file
./eng/scripts/Evaluate-TestSelection.ps1 -OutputFile "./test-selection.json"

# Custom config file
./eng/scripts/Evaluate-TestSelection.ps1 -ConfigFile "./custom-rules.json"

# Verbose for debugging
./eng/scripts/Evaluate-TestSelection.ps1 -Verbose
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-DiffTarget` | string | `HEAD~1` | Git ref to compare against (merge commit scenario) |
| `-OutputFile` | string | (stdout) | Path to write JSON output |
| `-ConfigFile` | string | `./test-selection-rules.json` | Path to config file |
| `-Verbose` | switch | false | Enable detailed logging |

### Algorithm

```
1. Load configuration from JSON file
2. Get list of changed files: git diff --name-only $DiffTarget
3. If git diff fails → output run_all: true, exit

4. Initialize all categories as disabled
5. Track which categories were triggered by path matches

6. For each changed file:
   a. Check if matches any "triggerAll" category → set run_all: true, exit early
   b. For each category with triggerPaths:
      - If file matches any pattern → mark category as triggered
   c. For categories with useConvention:
      - Extract component name from path
      - Add corresponding test project to output

7. Process alsoTriggers:
   - For each triggered category, also trigger its alsoTriggers

8. Process excludeWhenOnly:
   - For each category with excludeWhenOnly:
     - Get set of triggered categories
     - If triggered set is subset of excludeWhenOnly → disable this category

9. Process runByDefault:
   - For categories with runByDefault: true
     - Enable if not excluded by step 8

10. Collect projects:
    - For each enabled category, add its projects to output
    - Deduplicate project list

11. Output JSON result
```

### Output JSON Structure

```json
{
  "run_all": false,
  "trigger_reason": "normal | fallback | critical_path",
  "changed_files": [
    "src/Components/Aspire.Npgsql/NpgsqlExtensions.cs",
    "tests/Aspire.Npgsql.Tests/NpgsqlTests.cs"
  ],
  "categories": {
    "core": {
      "enabled": false,
      "reason": "no matching changes"
    },
    "default": {
      "enabled": false,
      "reason": "excluded: only [components] changed"
    },
    "templates": {
      "enabled": false,
      "reason": "no matching changes"
    },
    "e2e": {
      "enabled": false,
      "reason": "no matching changes"
    },
    "dashboard": {
      "enabled": false,
      "reason": "no matching changes"
    },
    "hosting": {
      "enabled": false,
      "reason": "no matching changes"
    },
    "components": {
      "enabled": true,
      "reason": "matched: src/Components/Aspire.Npgsql/**"
    }
  },
  "projects": [
    "tests/Aspire.Npgsql.Tests/"
  ]
}
```

## Rule Evaluation Examples

### Example 1: Only template files changed

**Changed files:**
- `src/Aspire.ProjectTemplates/templates/aspire-starter/Program.cs`

**Result:**
```json
{
  "run_all": false,
  "categories": {
    "core": { "enabled": false },
    "default": { "enabled": false, "reason": "excluded: only [templates] changed" },
    "templates": { "enabled": true },
    "e2e": { "enabled": false },
    "dashboard": { "enabled": false },
    "hosting": { "enabled": false },
    "components": { "enabled": false }
  },
  "projects": ["tests/Aspire.ProjectTemplates.Tests/"]
}
```

### Example 2: Hosting + template files changed

**Changed files:**
- `src/Aspire.Hosting/DistributedApplication.cs`
- `src/Aspire.ProjectTemplates/templates/aspire-starter/Program.cs`

**Result:**
```json
{
  "run_all": false,
  "categories": {
    "core": { "enabled": false },
    "default": { "enabled": true, "reason": "triggered by: hosting" },
    "templates": { "enabled": true },
    "e2e": { "enabled": false },
    "dashboard": { "enabled": false },
    "hosting": { "enabled": true },
    "components": { "enabled": false }
  },
  "projects": [
    "tests/Aspire.Hosting.Tests/",
    "tests/Aspire.ProjectTemplates.Tests/",
    "tests/Aspire.Components.Common.Tests/"
  ]
}
```

### Example 3: Critical path changed

**Changed files:**
- `Directory.Build.props`

**Result:**
```json
{
  "run_all": true,
  "trigger_reason": "critical_path",
  "categories": {
    "core": { "enabled": true, "reason": "matched: Directory.Build.props" }
  },
  "projects": []
}
```

### Example 4: Component files changed

**Changed files:**
- `src/Components/Aspire.Npgsql/NpgsqlExtensions.cs`
- `src/Components/Aspire.Redis/RedisExtensions.cs`

**Result:**
```json
{
  "run_all": false,
  "categories": {
    "core": { "enabled": false },
    "default": { "enabled": false, "reason": "excluded: only [components] changed" },
    "templates": { "enabled": false },
    "e2e": { "enabled": false },
    "dashboard": { "enabled": false },
    "hosting": { "enabled": false },
    "components": { "enabled": true }
  },
  "projects": [
    "tests/Aspire.Npgsql.Tests/",
    "tests/Aspire.Redis.Tests/"
  ]
}
```

### Example 5: Git diff fails (fallback)

**Result:**
```json
{
  "run_all": true,
  "trigger_reason": "fallback",
  "categories": {},
  "projects": []
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
      projects: ${{ steps.eval.outputs.projects }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 2  # Need parent commit for diff

      - name: Evaluate test selection
        id: eval
        shell: pwsh
        run: |
          $result = ./eng/scripts/Evaluate-TestSelection.ps1 -DiffTarget "HEAD~1"
          $json = $result | ConvertFrom-Json
          echo "run_all=$($json.run_all)" >> $env:GITHUB_OUTPUT
          echo "categories=$($result | ConvertTo-Json -Compress)" >> $env:GITHUB_OUTPUT
          echo "projects=$($json.projects | ConvertTo-Json -Compress)" >> $env:GITHUB_OUTPUT

  test-templates:
    needs: evaluate
    if: needs.evaluate.outputs.run_all == 'true' || fromJson(needs.evaluate.outputs.categories).templates.enabled == true
    runs-on: ubuntu-latest
    steps:
      - run: echo "Running template tests..."

  test-default:
    needs: evaluate
    if: needs.evaluate.outputs.run_all == 'true' || fromJson(needs.evaluate.outputs.categories).default.enabled == true
    runs-on: ubuntu-latest
    steps:
      - run: echo "Running default tests..."
```

### Azure DevOps

```yaml
stages:
- stage: Evaluate
  jobs:
  - job: EvaluateChanges
    steps:
    - pwsh: |
        $result = ./eng/scripts/Evaluate-TestSelection.ps1 -DiffTarget "HEAD~1"
        $json = $result | ConvertFrom-Json
        Write-Host "##vso[task.setvariable variable=runAll;isOutput=true]$($json.run_all)"
        Write-Host "##vso[task.setvariable variable=runTemplates;isOutput=true]$($json.categories.templates.enabled)"
        Write-Host "##vso[task.setvariable variable=runDefault;isOutput=true]$($json.categories.default.enabled)"
      name: eval

- stage: TestTemplates
  dependsOn: Evaluate
  condition: or(eq(dependencies.Evaluate.outputs['EvaluateChanges.eval.runAll'], 'true'), eq(dependencies.Evaluate.outputs['EvaluateChanges.eval.runTemplates'], 'true'))
  jobs:
  - job: RunTemplateTests
    steps:
    - script: echo "Running template tests..."
```

## Implementation Tasks

### Phase 1: Core Implementation
1. [ ] Create `eng/scripts/test-selection-rules.json` with initial categories
2. [ ] Create `eng/scripts/Evaluate-TestSelection.ps1` with core logic
3. [ ] Implement glob pattern matching for triggerPaths
4. [ ] Implement convention-based project mapping
5. [ ] Implement `triggerAll` logic
6. [ ] Implement `excludeWhenOnly` logic
7. [ ] Implement `alsoTriggers` logic
8. [ ] Implement `runByDefault` logic
9. [ ] Add fallback behavior (run all on error)

### Phase 2: Testing
10. [ ] Write unit tests for the PowerShell script
11. [ ] Test with various change scenarios manually
12. [ ] Validate glob patterns match expected files

### Phase 3: CI Integration
13. [ ] Integrate into GitHub Actions workflow
14. [ ] Update test jobs to use conditional execution
15. [ ] Add verbose logging for debugging CI issues

### Phase 4: Refinement
16. [ ] Fine-tune category definitions based on actual usage
17. [ ] Add JSON schema for config validation
18. [ ] Document common scenarios and troubleshooting

## References

- [dotnet/runtime evaluate-changed-paths.sh](https://github.com/dotnet/runtime/blob/main/eng/pipelines/evaluate-changed-paths.sh)
- [dorny/paths-filter GitHub Action](https://github.com/dorny/paths-filter)
- [Buildkite Monorepo Plugin](https://buildkite.com/resources/blog/solving-ci-cd-in-monorepos-with-buildkite-s-official-plugin/)

## Open Questions / Future Considerations

1. **Caching**: Should we cache the evaluation result for the same commit SHA?
2. **Manual override**: Should there be a way to force `run_all` via commit message (e.g., `[ci-full]`)?
3. **Metrics**: Should we track which categories run most often to optimize further?
4. **Validation**: Should we validate that mapped test projects actually exist?
