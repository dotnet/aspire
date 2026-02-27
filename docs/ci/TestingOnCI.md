# Testing on CI

This document describes the test infrastructure for CI pipelines in the Aspire repository. The infrastructure is designed to be platform-agnostic, with a canonical matrix format that can be consumed by GitHub Actions, Azure DevOps, or other CI systems.

## Overview

The CI test infrastructure uses a unified matrix generation system that:

1. Enumerates all test projects and their metadata
2. Optionally splits large test projects into parallel jobs
3. Generates a canonical test matrix (platform-agnostic)
4. Expands the matrix for specific CI platforms (GitHub Actions, Azure DevOps)
5. Runs tests in parallel across multiple operating systems

## Architecture

```text
┌─────────────────────────────────────────────────────────────────────┐
│                         MSBuild Phase                                │
│  (TestEnumerationRunsheetBuilder.targets + build-test-matrix.ps1)   │
│                                                                      │
│  ┌──────────────┐    ┌──────────────┐    ┌─────────────────────┐   │
│  │ .tests-      │    │ .tests-      │    │ canonical-test-     │   │
│  │ metadata.json│ ─► │ partitions.  │ ─► │ matrix.json         │   │
│  │ (per project)│    │ json (split) │    │ (canonical format)  │   │
│  └──────────────┘    └──────────────┘    └─────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Platform-Specific Expansion                       │
│                                                                      │
│  ┌─────────────────────────┐    ┌─────────────────────────┐        │
│  │ expand-test-matrix-     │    │ (future)                │        │
│  │ github.ps1              │    │ expand-test-matrix-     │        │
│  │ • OS → runner mapping   │    │ azdo.ps1                │        │
│  │ • { "include": [...] }  │    │ • OS → vmImage mapping  │        │
│  └─────────────────────────┘    └─────────────────────────┘        │
└─────────────────────────────────────────────────────────────────────┘
```

## Test Matrix Generation Flow

### Phase 1: Test Enumeration

The `enumerate-tests` GitHub Action (`.github/actions/enumerate-tests/action.yml`) triggers a special build:

```bash
./build.sh -test /p:TestRunnerName=TestEnumerationRunsheetBuilder
```

This invokes `eng/TestEnumerationRunsheetBuilder/TestEnumerationRunsheetBuilder.targets` for each test project, which:

- Checks OS compatibility via `RunOnGithubActions{Linux/Windows/MacOS}` properties
- Determines if the project uses test splitting (`SplitTestsOnCI` property)
- Writes a `.tests-metadata.json` file to `artifacts/helix/` containing:
  - `projectName`, `shortName`, `testProjectPath`
  - `supportedOSes` array (e.g., `["windows", "linux", "macos"]`)
  - `requiresNugets`, `requiresTestSdk`, `requiresCliArchive` flags
  - `enablePlaywrightInstall` flag
  - `testSessionTimeout`, `testHangTimeout` values
  - `uncollectedTestsSessionTimeout`, `uncollectedTestsHangTimeout` values
  - `splitTests` flag

### Phase 2: Test Partition Discovery

For projects with `SplitTestsOnCI=true`, the `GenerateTestPartitionsForCI` target runs `eng/scripts/split-test-projects-for-ci.ps1`, which:

1. **Attempts partition extraction**: Uses `tools/ExtractTestPartitions` to scan the test assembly for `[Trait("Partition", "name")]` attributes on test classes
2. **If partitions found**: Writes entries like `collection:PartitionName` plus `uncollected:*` (safety net for tests without partition traits). Note: the term "collection" here refers to partition groups, not xUnit `[Collection]` attributes which serve a different purpose (shared test fixtures).
3. **If no partitions found**: Falls back to class-based splitting using `--list-tests` output, writing entries like `class:Namespace.ClassName`

Output: `.tests-partitions.json` file alongside the metadata file.

### Phase 3: Canonical Matrix Generation

After all projects build, `eng/AfterSolutionBuild.targets` runs `eng/scripts/build-test-matrix.ps1`, which:

1. Collects all `.tests-metadata.json` files
2. For split test projects, reads the corresponding `.tests-partitions.json`
3. Applies default values for missing properties
4. Normalizes boolean values
5. Creates matrix entries:
   - **Regular tests**: One entry per project
   - **Partition-based splits**: One entry per partition + one for `uncollected:*`
   - **Class-based splits**: One entry per test class
6. Outputs `artifacts/canonical-test-matrix.json` in canonical format (flat array with `requiresNugets`, `requiresCliArchive` booleans per entry)

**Canonical format:**
```json
{
  "tests": [
    {
      "name": "Templates-StarterTests",
      "shortname": "Templates-StarterTests",
      "testProjectPath": "tests/Aspire.Templates.Tests/...",
      "supportedOSes": ["windows", "linux", "macos"],
      "requiresNugets": true,
      "requiresTestSdk": true,
      "testSessionTimeout": "20m",
      "testHangTimeout": "10m",
      "extraTestArgs": "--filter-class \"...\""
    },
    {
      "name": "Hosting-Docker",
      "shortname": "Hosting-Docker",
      "testProjectPath": "tests/Aspire.Hosting.Tests/...",
      "supportedOSes": ["linux"],
      "requiresNugets": false,
      "testSessionTimeout": "30m",
      "extraTestArgs": "--filter-trait \"Partition=Docker\""
    }
  ]
}
```

### Phase 4: Platform-Specific Expansion

Each CI platform has a thin script that transforms the canonical matrix:

**GitHub Actions** (`eng/scripts/expand-test-matrix-github.ps1`):
- Expands each entry for every OS in its `supportedOSes` array
- Maps OS names to GitHub runners (`linux` → `ubuntu-latest`, etc.)
- Splits entries into categories by dependency requirements:
  - `no_nugets` — tests with no package dependencies
  - `requires_nugets` — tests needing built NuGet packages
  - `requires_cli_archive` — tests needing native CLI archives
- Applies overflow splitting for the `no_nugets` category (threshold: 250 entries) to stay under the GitHub Actions 256-job-per-matrix limit
- Outputs 4 GitHub Actions matrices: `no_nugets` (primary), `no_nugets_overflow`, `requires_nugets`, `requires_cli_archive`

**Azure DevOps** (future):
- Would map OS names to vmImage or pool names
- Would output Azure DevOps matrix format: `{ ConfigName: { vars } }`

This separation keeps 90% of the logic platform-agnostic while allowing each CI system to use its native matrix format.

### Phase 5: Test Execution

In `.github/workflows/tests.yml`, the workflow:

1. Receives 4 pre-split matrices from the `enumerate-tests` action (split by `expand-test-matrix-github.ps1`)
2. Runs 4 job groups using the split matrices:
   - `tests_no_nugets`: Runs immediately after enumeration
   - `tests_no_nugets_overflow`: Runs immediately (handles entries beyond the 250-entry threshold)
   - `tests_requires_nugets`: Waits for `build_packages` job
   - `tests_requires_cli_archive`: Waits for both `build_packages` and `build_cli_archives` jobs

Each job invokes `.github/workflows/run-tests.yml` with matrix parameters including `extraTestArgs` for filtering (e.g., `--filter-trait "Partition=X"`).

> **Note:** The workflow automatically prepends `--filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"` before any `extraTestArgs`, ensuring quarantined and outerloop tests are always excluded from the main test run.

#### GitHub Actions 256-Job Limit

GitHub Actions enforces a maximum of 256 jobs per `strategy.matrix`. To stay within this limit, the `no_nugets` category (typically the largest) is split into primary and overflow buckets at a threshold of 250 entries. If the total entry count is below 250, the overflow matrix is empty and the overflow job is skipped.

## Enabling Test Splitting for a Project

To split a test project into parallel CI jobs, add these properties to the `.csproj`:

```xml
<PropertyGroup>
  <!-- Enable test splitting -->
  <SplitTestsOnCI>true</SplitTestsOnCI>
  <TestClassNamePrefixForCI>Aspire.YourProject.Tests</TestClassNamePrefixForCI>

  <!-- Optional: Custom timeouts -->
  <TestSessionTimeout>30m</TestSessionTimeout>
  <TestHangTimeout>15m</TestHangTimeout>
  <UncollectedTestsSessionTimeout>15m</UncollectedTestsSessionTimeout>  <!-- default: 15m -->
  <UncollectedTestsHangTimeout>10m</UncollectedTestsHangTimeout>        <!-- default: 10m -->
</PropertyGroup>
```

### Using Partition Traits (Recommended)

For explicit control over test grouping, add `[Trait("Partition", "name")]` to test classes:

```csharp
[Trait("Partition", "Docker")]
public class DockerResourceTests
{
    // Tests that require Docker
}

[Trait("Partition", "Publishing")]
public class PublishingTests
{
    // Publishing-related tests
}
```

Tests without a `Partition` trait run in a separate `uncollected` job, ensuring nothing is missed.

### Class-Based Splitting (Fallback)

If no `Partition` traits are found, the infrastructure automatically falls back to class-based splitting, creating one CI job per test class. This is less efficient but requires no code changes.

## Controlling OS Compatibility

By default, tests run on all three platforms. To restrict a project to specific OSes:

```xml
<PropertyGroup>
  <!-- Only run on Linux (e.g., Docker-dependent tests) -->
  <RunOnGithubActionsWindows>false</RunOnGithubActionsWindows>
  <RunOnGithubActionsLinux>true</RunOnGithubActionsLinux>
  <RunOnGithubActionsMacOS>false</RunOnGithubActionsMacOS>
</PropertyGroup>
```

## Requiring NuGet Packages

For tests that need the built Aspire packages (e.g., template tests, end-to-end tests):

```xml
<PropertyGroup>
  <RequiresNugets>true</RequiresNugets>
  <!-- Also common for template tests -->
  <RequiresTestSdk>true</RequiresTestSdk>
</PropertyGroup>
```

These tests wait for the `build_packages` job before running.

## Requiring CLI Native Archives

For tests that need native CLI archives (e.g., CLI end-to-end tests):

```xml
<PropertyGroup>
  <RequiresNugets>true</RequiresNugets>
  <RequiresCliArchive>true</RequiresCliArchive>
</PropertyGroup>
```

These tests wait for both the `build_packages` and `build_cli_archives` jobs before running. The workflow also sets `GH_TOKEN`, `GITHUB_PR_NUMBER`, and `GITHUB_PR_HEAD_SHA` environment variables for CLI E2E test scenarios.

## Enabling Playwright

For tests that require Playwright browser automation:

```xml
<PropertyGroup>
  <EnablePlaywrightInstall>true</EnablePlaywrightInstall>
</PropertyGroup>
```

This flag is tracked in the test metadata and controls whether Playwright browsers are installed during the test build step.

## Deployment Tests

Deployment end-to-end tests have a separate flow from the standard test matrix:

1. The `enumerate-tests` action builds the deployment test project with `GenerateTestPartitionsForCI`
2. A separate `generate_deployment_matrix` step reads the generated partitions
3. The deployment matrix is output independently from the main test matrices
4. Deployment tests run in a dedicated workflow (`tests-deployment.yml`) with Azure credentials

## File Artifacts

During enumeration, these files are generated in `artifacts/`:

| File | Description |
|------|-------------|
| `helix/<ProjectName>.tests-metadata.json` | Project metadata (OS support, timeouts, flags) |
| `helix/<ProjectName>.tests-partitions.json` | Partition/class list for split projects |
| `canonical-test-matrix.json` | Canonical matrix (platform-agnostic) |

## Scripts Reference

| Script | Purpose |
|--------|---------|
| `eng/scripts/build-test-matrix.ps1` | Generates canonical matrix from metadata files |
| `eng/scripts/expand-test-matrix-github.ps1` | Expands canonical matrix for GitHub Actions |
| `eng/scripts/split-test-projects-for-ci.ps1` | Discovers test partitions/classes for splitting |

## Debugging Test Enumeration

To run enumeration locally and inspect the generated matrix:

```bash
./build.sh -test \
  /p:TestRunnerName=TestEnumerationRunsheetBuilder \
  /p:TestMatrixOutputPath=artifacts/canonical-test-matrix.json \
  /p:IncludeTemplateTests=true \
  /p:GenerateCIPartitions=true
```

Then inspect:
- `artifacts/helix/*.tests-metadata.json` for per-project metadata
- `artifacts/helix/*.tests-partitions.json` for split test entries
- `artifacts/canonical-test-matrix.json` for the canonical matrix

To test GitHub-specific expansion locally:

```powershell
pwsh eng/scripts/expand-test-matrix-github.ps1 `
  -CanonicalMatrixFile artifacts/canonical-test-matrix.json `
  -OutputMatrixFile artifacts/github-matrix.json
```
