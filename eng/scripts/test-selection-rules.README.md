# Test Selection Rules Configuration

The `test-selection-rules.json` file configures which tests run based on changed files in a pull request. This enables faster CI by only running relevant tests.

## Configuration Reference

### Categories

Categories define groups of tests that run together. Each category has a name (used as `run_{name}` in workflow outputs) and configuration options.

#### Always Run a Category

Use `runByDefault: true` to run a category whenever any tests run:

```json
{
  "categories": {
    "smoke": {
      "description": "Smoke tests that always run",
      "runByDefault": true
    }
  }
}
```

#### Trigger a Category Based on Changed Files

Use `triggerPaths` to specify glob patterns that trigger the category:

```json
{
  "categories": {
    "cli_e2e": {
      "description": "CLI end-to-end tests",
      "triggerPaths": [
        "src/Aspire.Cli/**",
        "tests/Aspire.Cli.EndToEnd.Tests/**"
      ]
    }
  }
}
```

#### Trigger All Tests (Critical Paths)

Use `triggerAll: true` for critical files where any change should run all tests:

```json
{
  "categories": {
    "core": {
      "description": "Critical paths - triggers ALL tests",
      "triggerAll": true,
      "triggerPaths": [
        "global.json",
        "Directory.Build.props",
        "Directory.Packages.props"
      ]
    }
  }
}
```

#### Exclude Paths from Triggering

Use `excludePaths` to prevent certain files from triggering a category even if they match `triggerPaths`:

```json
{
  "categories": {
    "integrations": {
      "description": "Integration tests",
      "triggerPaths": [
        "src/Aspire.*/**"
      ],
      "excludePaths": [
        "src/Aspire.Cli/**",
        "src/Aspire.ProjectTemplates/**"
      ]
    }
  }
}
```

#### Chain Categories Together

Use `alsoTriggers` to enable additional categories when one triggers:

```json
{
  "categories": {
    "hosting": {
      "description": "Hosting tests",
      "triggerPaths": ["src/Aspire.Hosting/**"],
      "alsoTriggers": ["integrations"]
    }
  }
}
```

#### Skip Category When Only Certain Files Change

Use `excludeWhenOnly` to skip a category if changes are limited to specific categories:

```json
{
  "categories": {
    "e2e": {
      "description": "End-to-end tests",
      "triggerPaths": ["src/**"],
      "excludeWhenOnly": ["docs", "extension"]
    }
  }
}
```

### Ignore Paths

Files matching `ignorePaths` patterns are completely ignored—they don't trigger any tests and don't cause fallback to running all tests:

```json
{
  "ignorePaths": [
    "*.md",
    "docs/**",
    ".github/workflows/**"
  ]
}
```

### Project Mappings

Project mappings automatically discover which test projects to run based on source file changes:

```json
{
  "projectMappings": [
    {
      "sourcePattern": "src/Components/{name}/**",
      "testPattern": "tests/{name}.Tests/"
    },
    {
      "sourcePattern": "src/Aspire.Hosting.{name}/**",
      "testPattern": "tests/Aspire.Hosting.{name}.Tests/"
    }
  ]
}
```

The `{name}` placeholder captures part of the path and substitutes it into the test pattern.

## Workflow Integration

The test selector outputs are used in `.github/workflows/tests.yml`:

- `run_all` - Set to `true` when critical paths change or on non-PR events
- `run_{category}` - Set to `true` when the category should run
- `integrations_projects` - JSON array of specific test projects to run

Example workflow usage:

```yaml
jobs:
  detect_scope:
    outputs:
      run_all: ${{ steps.detect.outputs.run_all }}
      run_cli_e2e: ${{ steps.detect.outputs.run_cli_e2e }}
      run_polyglot: ${{ steps.detect.outputs.run_polyglot }}

  my_tests:
    needs: detect_scope
    if: ${{ needs.detect_scope.outputs.run_all == 'true' || needs.detect_scope.outputs.run_my_category == 'true' }}
```

## Adding a New Category

1. Add the category to `test-selection-rules.json`:

   ```json
   {
     "categories": {
       "my_category": {
         "description": "Description of tests",
         "triggerPaths": ["path/to/trigger/**"]
       }
     }
   }
   ```

2. Add the output to `detect_scope` job in `tests.yml`:

   ```yaml
   outputs:
     run_my_category: ${{ steps.detect.outputs.run_my_category }}
   ```

3. Update `all_skipped` expression to include the new category

4. Add `run_my_category=true` to the non-PR event case

5. Add the category to the "Show test selection results" step

6. Create or update jobs to use the new condition:

   ```yaml
   if: ${{ needs.detect_scope.outputs.run_all == 'true' || needs.detect_scope.outputs.run_my_category == 'true' }}
   ```

## Testing Changes

Run the test selector locally to verify your configuration:

```bash
dotnet run --project tools/Aspire.TestSelector/Aspire.TestSelector.csproj -- \
  --config eng/scripts/test-selection-rules.json \
  --from <base-commit> \
  --to <head-commit> \
  --verbose
```
