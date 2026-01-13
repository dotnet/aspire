# Conditional Test Execution Design

> **SUPERSEDED**: This design document has been superseded by [test-selection-by-changed-paths.md](../specs/test-selection-by-changed-paths.md).
> The implementation now uses PowerShell (`eng/scripts/Evaluate-TestSelection.ps1`) and JSON configuration (`eng/scripts/test-selection-rules.json`).

## Overview

Skip test categories in CI when their source files haven't changed, reducing CI time and cost while maintaining a conservative stance (if in doubt, run tests).

**Scope:** PR validation only. Push to main and scheduled runs always execute all tests.

## Goals

1. **Reduce CI time** - Skip expensive test categories when unrelated code changes
2. **Faster PR feedback** - Developers get results sooner
3. **Zero false negatives** - Never skip tests that should run (conservative)
4. **Easy maintenance** - Simple config file, moderate logging
5. **Full coverage on main** - Scheduled/push runs always run everything

## Non-Goals

- Project-level granularity (category-level only for now)
- Dependency graph analysis (simple path matching)
- Local development tooling (CI only)
- Aggressive optimization (we accept some false positives)
- Skipping tests on main branch or scheduled runs

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    .github/test-filters.yml                  │
│  - fallback patterns (trigger all tests)                     │
│  - category definitions with include/exclude patterns        │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│              eng/pipelines/evaluate-paths.sh                 │
│  1. Get changed files via git diff                           │
│  2. Check fallback patterns                                  │
│  3. Match files to categories                                │
│  4. Check for unmatched files (conservative fallback)        │
│  5. Output: run_<category>=true/false                        │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│              .github/workflows/tests.yml                     │
│  - detect_scope job runs evaluate-paths.sh                   │
│  - test jobs have `if:` conditions based on outputs          │
│  - skipped jobs appear as "skipped" in GitHub UI             │
└──────────────────────────────────────────────────────────────┘
```

---

## Configuration File

**Location:** `.github/test-filters.yml`

```yaml
fallback:
  # Changes to these paths trigger ALL tests
  - eng/**
  - Directory.Build.props
  - Directory.Build.targets
  - Directory.Packages.props
  - global.json
  - NuGet.config
  - .github/workflows/**
  - .github/actions/**
  - tests/Shared/**
  - "*.sln"
  - "*.slnx"

categories:
  # Listed in order of skip priority (most expensive first)

  templates:
    description: "Template tests - runs on 3 platforms"
    include:
      - src/Aspire.ProjectTemplates/**
      - tests/Aspire.Templates.Tests/**

  cli_e2e:
    description: "CLI end-to-end tests"
    include:
      - src/Aspire.Cli/**
      - tests/Aspire.Cli.EndToEndTests/**

  endtoend:
    description: "General E2E tests"
    include:
      - tests/Aspire.EndToEnd.Tests/**
      - playground/**

  integrations:
    description: "Integration tests - 3 platforms × ~20 projects"
    include:
      - src/Aspire.*/**
      - tests/Aspire.*.Tests/**
    exclude:
      - src/Aspire.ProjectTemplates/**
      - src/Aspire.Cli/**
      - tests/Aspire.Templates.Tests/**
      - tests/Aspire.EndToEnd.Tests/**
      - tests/Aspire.Cli.EndToEndTests/**

  extension:
    description: "VS Code extension tests"
    include:
      - extension/**
```

---

## Evaluation Logic

### Algorithm

```
INPUT: list of changed files, config file
OUTPUT: run_<category>=true/false for each category

1. Parse config file
2. Get changed files from git diff

3. FOR each changed file:
     IF matches ANY fallback pattern:
       RETURN all categories = true (run everything)

4. FOR each changed file:
     matched = false
     FOR each category:
       IF matches category include patterns AND NOT exclude patterns:
         mark category for running
         matched = true
     IF NOT matched:
       RETURN all categories = true (conservative fallback)

5. RETURN categories marked for running = true, others = false
```

### Key Behaviors

| Scenario | Result |
|----------|--------|
| Push to main branch | Run ALL tests (no skipping) |
| Scheduled run | Run ALL tests (no skipping) |
| PR: File matches fallback pattern | Run ALL tests |
| PR: File matches no category | Run ALL tests (conservative) |
| PR: File matches one category | Run that category only |
| PR: File matches multiple categories | Run all matched categories |
| PR: No changed files | Run nothing (shouldn't happen) |

---

## Test Cases

### Fallback Tests

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| F1 | `eng/Version.Details.xml` | ALL | Matches fallback `eng/**` |
| F2 | `Directory.Build.props` | ALL | Matches fallback exactly |
| F3 | `.github/workflows/ci.yml` | ALL | Matches fallback `.github/workflows/**` |
| F4 | `tests/Shared/TestHelper.cs` | ALL | Matches fallback `tests/Shared/**` |
| F5 | `global.json` | ALL | Matches fallback exactly |
| F6 | `Aspire.slnx` | ALL | Matches fallback `*.slnx` |

### Category: templates

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| T1 | `src/Aspire.ProjectTemplates/templates/aspire-starter/Program.cs` | templates only | Matches templates include |
| T2 | `tests/Aspire.Templates.Tests/TemplateTests.cs` | templates only | Matches templates include |

### Category: cli_e2e

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| C1 | `src/Aspire.Cli/Commands/NewCommand.cs` | cli_e2e only | Matches cli_e2e include |
| C2 | `tests/Aspire.Cli.EndToEndTests/NewCommandTests.cs` | cli_e2e only | Matches cli_e2e include |

### Category: endtoend

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| E1 | `tests/Aspire.EndToEnd.Tests/SomeTest.cs` | endtoend only | Matches endtoend include |
| E2 | `playground/TestShop/TestShop.AppHost/Program.cs` | endtoend only | Matches endtoend include |

### Category: integrations

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| I1 | `src/Aspire.Dashboard/Components/Layout.razor` | integrations only | Matches integrations, not excluded |
| I2 | `src/Aspire.Hosting/ApplicationModel/Resource.cs` | integrations only | Matches integrations, not excluded |
| I3 | `tests/Aspire.Dashboard.Tests/DashboardTests.cs` | integrations only | Matches integrations, not excluded |
| I4 | `src/Aspire.Hosting.Azure/AzureExtensions.cs` | integrations only | Matches integrations, not excluded |

### Category: extension

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| X1 | `extension/package.json` | extension only | Matches extension include |
| X2 | `extension/src/extension.ts` | extension only | Matches extension include |

### Multi-Category Tests

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| M1 | `src/Aspire.Dashboard/Foo.cs`, `extension/bar.ts` | integrations + extension | Each matches its category |
| M2 | `src/Aspire.Cli/Cmd.cs`, `src/Aspire.Dashboard/Foo.cs` | cli_e2e + integrations | Each matches its category |
| M3 | `src/Aspire.ProjectTemplates/X.cs`, `playground/Y.cs` | templates + endtoend | Each matches its category |

### Conservative Fallback Tests

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| U1 | `README.md` | ALL | Doesn't match any category |
| U2 | `some-random-file.txt` | ALL | Doesn't match any category |
| U3 | `docs/getting-started.md` | ALL | Doesn't match any category |
| U4 | `.gitignore` | ALL | Doesn't match any category |

### Edge Cases

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| EC1 | `src/Aspire.ProjectTemplates/README.md` | templates only | In templates dir, even though .md |
| EC2 | `tests/Aspire.Cli.EndToEndTests/README.md` | cli_e2e only | In cli_e2e dir |
| EC3 | `src/Aspire.Cli.SomeNew/Foo.cs` | integrations only | Matches Aspire.* but not Aspire.Cli/ |
| EC4 | (no changes) | NONE | Empty diff |

### Exclude Pattern Tests

| # | Changed Files | Expected | Reason |
|---|---------------|----------|--------|
| EX1 | `src/Aspire.ProjectTemplates/Foo.cs` | templates only | Excluded from integrations |
| EX2 | `src/Aspire.Cli/Bar.cs` | cli_e2e only | Excluded from integrations |
| EX3 | `tests/Aspire.Templates.Tests/X.cs` | templates only | Excluded from integrations |

---

## Workflow Integration

### detect_scope Job

The detect_scope job determines whether to run each test category. For PRs, it evaluates
changed files. For push/scheduled events, it outputs `true` for all categories.

```yaml
detect_scope:
  runs-on: ubuntu-latest
  outputs:
    run_templates: ${{ steps.eval.outputs.run_templates }}
    run_cli_e2e: ${{ steps.eval.outputs.run_cli_e2e }}
    run_endtoend: ${{ steps.eval.outputs.run_endtoend }}
    run_integrations: ${{ steps.eval.outputs.run_integrations }}
    run_extension: ${{ steps.eval.outputs.run_extension }}
  steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Need full history for git diff

    - name: Evaluate changed paths
      id: eval
      run: |
        chmod +x eng/pipelines/evaluate-paths.sh

        # For PRs: evaluate changed files
        # For push/scheduled: run everything
        if [ "${{ github.event_name }}" == "pull_request" ]; then
          eng/pipelines/evaluate-paths.sh \
            --config .github/test-filters.yml \
            --base ${{ github.event.pull_request.base.sha }} \
            --head ${{ github.event.pull_request.head.sha }}
        else
          echo "::notice::Non-PR event (${{ github.event_name }}) - running all tests"
          echo "run_templates=true" >> "$GITHUB_OUTPUT"
          echo "run_cli_e2e=true" >> "$GITHUB_OUTPUT"
          echo "run_endtoend=true" >> "$GITHUB_OUTPUT"
          echo "run_integrations=true" >> "$GITHUB_OUTPUT"
          echo "run_extension=true" >> "$GITHUB_OUTPUT"
        fi
```

### Conditional Jobs

```yaml
templates_test_lin:
  needs: [detect_scope, setup_for_tests_lin, build_packages]
  if: ${{ needs.detect_scope.outputs.run_templates == 'true' }}
  # ...

integrations_test_lin:
  needs: [detect_scope, setup_for_tests_lin]
  if: ${{ needs.detect_scope.outputs.run_integrations == 'true' }}
  # ...
```

### Event Type Behavior

| Event | Behavior |
|-------|----------|
| `pull_request` | Evaluate changed files, skip unaffected categories |
| `push` (to main) | Run all tests |
| `schedule` | Run all tests |
| `workflow_dispatch` | Run all tests |

---

## Sample CI Output

### Scenario: Dashboard-only change

```
=== Conditional Test Execution ===
Config: .github/test-filters.yml
Base: abc123
Head: def456

=== Changed Files (2) ===
  src/Aspire.Dashboard/Components/Layout.razor
  tests/Aspire.Dashboard.Tests/LayoutTests.cs

=== Checking Fallback Patterns ===
  eng/** - no match
  Directory.Build.props - no match
  ... (all fallback patterns)
  Result: No fallback triggered

=== Evaluating Categories ===

[templates]
  Checking: src/Aspire.ProjectTemplates/**
    No matches
  Checking: tests/Aspire.Templates.Tests/**
    No matches
  Result: SKIP

[cli_e2e]
  Checking: src/Aspire.Cli/**
    No matches
  Checking: tests/Aspire.Cli.EndToEndTests/**
    No matches
  Result: SKIP

[endtoend]
  Checking: tests/Aspire.EndToEnd.Tests/**
    No matches
  Checking: playground/**
    No matches
  Result: SKIP

[integrations]
  Checking: src/Aspire.*/**
    Matched: src/Aspire.Dashboard/Components/Layout.razor
  Checking excludes: src/Aspire.ProjectTemplates/**
    Not excluded
  Checking: tests/Aspire.*.Tests/**
    Matched: tests/Aspire.Dashboard.Tests/LayoutTests.cs
  Result: RUN

[extension]
  Checking: extension/**
    No matches
  Result: SKIP

=== Unmatched Files Check ===
  All files matched at least one category

=== Summary ===
  run_templates=false
  run_cli_e2e=false
  run_endtoend=false
  run_integrations=true
  run_extension=false
```

---

## Testing Strategy

### Level 1: Local Script Testing

A test harness that simulates changed files and verifies correct outputs.

```bash
# eng/pipelines/test-evaluate-paths.sh

run_test "F1: eng fallback" \
  "eng/Version.Details.xml" \
  "templates=true,cli_e2e=true,endtoend=true,integrations=true,extension=true"

run_test "I1: Dashboard change" \
  "src/Aspire.Dashboard/Components/Layout.razor" \
  "templates=false,cli_e2e=false,endtoend=false,integrations=true,extension=false"
```

**Implementation:**
- Script accepts `--test-files` flag to override git diff
- Runs all 35 test cases from this document
- Exit 0 if all pass, exit 1 with failures listed

### Level 2: Git-Based Local Testing

Test against actual git history:

```bash
# Test against a known commit range
eng/pipelines/evaluate-paths.sh \
  --base abc123 \
  --head def456 \
  --dry-run

# Test against uncommitted changes
eng/pipelines/evaluate-paths.sh --dry-run
```

**Dry-run mode:**
- Shows what would be output
- Doesn't write to `$GITHUB_OUTPUT`
- Useful for manual verification

### Level 3: CI Validation (Audit Mode)

For initial rollout, run in audit mode - log what would be skipped but run everything:

```yaml
- name: Audit mode
  run: |
    echo "=== AUDIT MODE ==="
    echo "Would skip templates: ${{ steps.eval.outputs.run_templates == 'false' }}"
    echo "Would skip cli_e2e: ${{ steps.eval.outputs.run_cli_e2e == 'false' }}"
    echo "=== Running all tests anyway (audit mode) ==="
```

Compare "what would have been skipped" vs "what actually failed" to validate patterns.

### Level 4: Gradual Rollout

1. **Week 1:** Audit mode - log what would skip, run everything
2. **Week 2:** Enable for `extension` (lowest risk, isolated)
3. **Week 3:** Enable for `templates` (high skip value, well-isolated)
4. **Week 4:** Enable for remaining categories

```yaml
# Gradual rollout via environment flags
env:
  SKIP_ENABLED_EXTENSION: true
  SKIP_ENABLED_TEMPLATES: true
  SKIP_ENABLED_CLI_E2E: false      # not yet
  SKIP_ENABLED_ENDTOEND: false     # not yet
  SKIP_ENABLED_INTEGRATIONS: false # not yet
```

### Level 5: Ongoing Validation

**Push/scheduled runs provide continuous validation:**
- Every push to main runs all tests (no skipping)
- Scheduled runs (if any) run all tests
- This catches any regressions that PRs might have missed

**If a test fails on main that would have been skipped in the PR:**
- Indicates a gap in the path patterns
- Review and update `.github/test-filters.yml`

### Test Artifacts Summary

| Artifact | Purpose |
|----------|---------|
| `eng/pipelines/test-evaluate-paths.sh` | Local test harness (35 test cases) |
| `--dry-run` flag | Manual verification |
| CI logs (moderate verbosity) | Debug production issues |
| Audit mode | Validate before enabling skips |
| Push-to-main runs | Ongoing full validation |

---

## Future Enhancements

1. **Project-level granularity** - Skip individual test projects within integrations
2. **Dependency graph analysis** - Use `dotnet-affected` for transitive dependencies
3. **Caching** - Cache evaluation results for identical file sets
4. **Dry-run mode** - Test config changes without running CI
5. **Config validation** - Warn on overlapping patterns or unreachable categories

---

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `.github/test-filters.yml` | Create | Category and fallback pattern definitions |
| `eng/pipelines/evaluate-paths.sh` | Create | Path evaluation script |
| `.github/workflows/tests.yml` | Modify | Add detect_scope job, conditionals |
| `.github/test-filters.json` | Delete | Remove old implementation |
| `.github/actions/detect-test-scope/` | Delete | Remove old implementation |
