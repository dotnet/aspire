---
name: rebalance-tests
description: Analyzes CI build timings from the main branch, identifies test jobs exceeding 5 minutes, downloads TRX files to get per-test durations, and computes optimal test partitions. Use when asked to rebalance tests, optimize test splitting, or reduce CI test job duration.
---

# Rebalance Test Partitions

This skill analyzes recent CI builds on the `main` branch to find slow test jobs (>5 min), then uses per-test timing data from TRX files to compute and apply optimal partition assignments so all jobs finish under 5 minutes.

## Parameters

The user may provide:
- **N days** (default: 7) — how far back to look for CI builds
- **Max builds** (default: 5) — cap on builds to analyze (the `All-TestResults` artifact is large)
- **Target duration** (default: 5 minutes) — the maximum desired job duration

## Overview

1. Query recent CI builds on `main` to find slow test jobs
2. Download `All-TestResults` artifacts and parse TRX files for per-test durations
3. Aggregate timings across builds and platforms using P90 (worst OS wins)
4. Compute optimal partition assignments using greedy bin-packing
5. Apply changes: add `[Trait("Partition", "N")]` attributes and update `.csproj` files

## Step 1: Find Slow Test Jobs

### 1.1: List Recent CI Runs on Main

```bash
gh run list --repo dotnet/aspire --branch main --workflow ci.yml --limit 10 \
  --json databaseId,conclusion,createdAt \
  --jq '[.[] | select(.conclusion == "success" or .conclusion == "failure")] | .[:5]'
```

Only use **completed** runs (success or failure). Skip cancelled/in-progress runs since they won't have the `All-TestResults` artifact.

Filter to runs within the last N days:

```bash
# Get runs from last N days
SINCE=$(date -v-${N}d +%Y-%m-%dT00:00:00Z 2>/dev/null || date -d "${N} days ago" +%Y-%m-%dT00:00:00Z)
gh run list --repo dotnet/aspire --branch main --workflow ci.yml --limit 20 \
  --json databaseId,conclusion,createdAt \
  --jq "[.[] | select(.createdAt >= \"$SINCE\") | select(.conclusion == \"success\" or .conclusion == \"failure\")] | .[:${MAX_BUILDS}]"
```

### 1.2: Get Job Durations for Each Run

For each run, extract test job names and durations:

```bash
gh run view <run-id> --repo dotnet/aspire --json jobs \
  --jq '[.jobs[]
    | select(.name | test("Tests"))
    | select(.conclusion == "success" or .conclusion == "failure")
    | {
        name: .name,
        start: .startedAt,
        end: .completedAt,
        duration_min: ((((.completedAt | fromdateiso8601) - (.startedAt | fromdateiso8601)) / 60) | . * 10 | round / 10)
      }
    ] | sort_by(-.duration_min)'
```

### 1.3: Identify Jobs Over the Target Duration

Filter to jobs that took longer than 5 minutes in **any** build. The job name contains the test short name and OS:

```
Tests / Integrations Linux (Hosting) / Hosting (ubuntu-latest)
Tests / Integrations Windows (Hosting-P2) / Hosting-P2 (windows-latest)
```

Extract the test project short name from the job name. The pattern is:
- `Tests / {category} {os} ({shortname}) / {shortname} ({runs-on})`
- The shortname in parentheses is what we need

**Important**: If a project is already partitioned (e.g., `Hosting-P1`, `Hosting-P2`), sum up all partition durations per OS to get the total project time. The goal is to ensure the total is well-distributed, not that individual partitions are under 5 min.

### 1.4: Report Slow Jobs

Before downloading TRX files, report the slow jobs to the user in a table:

```markdown
| Project | OS | Duration (P90) | Currently Partitioned? |
|---------|----|-----------------|-----------------------|
| Hosting | ubuntu-latest | 21m | Yes (6 partitions) |
| Hosting.Testing | ubuntu-latest | 9m | No |
| Pomelo.EntityFrameworkCore.MySql | windows-latest | 8m | No |
```

**Checkpoint**: Confirm with the user which projects to rebalance before proceeding to the expensive TRX download step.

## Step 2: Download and Parse TRX Files

### 2.1: Download All-TestResults Artifacts

For each build identified in Step 1, download the `All-TestResults` artifact:

```bash
# Create temp directory for this analysis
WORK_DIR=$(mktemp -d /tmp/rebalance-tests-XXXXXX)

# Download the artifact for a specific run
gh run download <run-id> --repo dotnet/aspire \
  --name All-TestResults \
  --dir "$WORK_DIR/run-<run-id>"
```

The artifact structure is:
```
run-<run-id>/
  ubuntu-latest/
    testresults/
      Hosting.trx
      Hosting-P2.trx
      Hosting.Testing.trx
      ...
  windows-latest/
    testresults/
      Hosting.trx
      ...
  macos-latest/
    testresults/
      ...
```

**Note**: TRX filenames follow the pattern `{testShortName}.trx` or `{testShortName}_net10.0_{timestamp}.trx`. The short name is the prefix before `.trx` or `_net`.

### 2.2: Parse TRX Files for Per-Test Durations

Use the existing `TrxReader` from `tools/GenerateTestSummary/TrxReader.cs` as reference. The TRX XML structure:

```xml
<TestRun>
  <Times start="..." finish="..." />
  <Results>
    <UnitTestResult testName="Namespace.Class.Method" duration="00:00:05.123" startTime="..." endTime="..." outcome="Passed" />
  </Results>
</TestRun>
```

Write a script or use `dotnet-script` / PowerShell to parse TRX files. For each test result, extract:
- `testName` — fully qualified test name (includes namespace, class, and method)
- `duration` — test execution time as TimeSpan
- The **class name** — extract from testName by removing the method (last `.`-separated segment)

**PowerShell approach** (preferred for cross-platform):

```powershell
function Parse-TrxFile {
    param([string]$Path)

    [xml]$trx = Get-Content $Path
    $ns = @{ t = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" }

    $results = @()
    foreach ($result in $trx.TestRun.Results.UnitTestResult) {
        $testName = $result.testName
        $duration = [TimeSpan]::Parse($result.duration)

        # Extract class name (everything before the last dot, which is the method)
        $parts = $testName -split '\.'
        $className = ($parts[0..($parts.Length - 2)] -join '.')

        $results += [PSCustomObject]@{
            TestName  = $testName
            ClassName = $className
            Duration  = $duration
            DurationSeconds = $duration.TotalSeconds
        }
    }
    return $results
}
```

### 2.3: Aggregate Per-Class Durations

For partitioning purposes, we care about **class-level** totals (since `[Trait("Partition")]` is applied at the class level):

1. Group all test results by class name
2. Sum durations within each class to get per-class total
3. Across multiple builds, take the **P90** (90th percentile) of each class's total duration
4. Across OS platforms, take the **worst case** (maximum) per class

```
For each (project, class):
  For each OS:
    Collect [sum(test_durations_in_class)] across all builds
    class_duration_for_os = P90(collected_sums)
  class_duration = MAX(class_duration_for_os across all OSes)
```

## Step 3: Compute Optimal Partitions

### 3.1: Determine Partition Count

For each project that needs partitioning:

```
total_class_duration = SUM(class_durations)
partition_count = CEIL(total_class_duration / target_duration)
```

Add 1 extra partition as headroom (build/setup overhead, variance). Minimum 2 partitions.

### 3.2: Assign Classes to Partitions (Greedy Bin-Packing)

Use the **Longest Processing Time (LPT)** algorithm:

1. Sort classes by duration descending
2. For each class, assign it to the partition with the smallest current total duration
3. This produces a well-balanced partition assignment

```
partitions = [[] for _ in range(partition_count)]
partition_totals = [0 for _ in range(partition_count)]

for class in sorted(classes, key=lambda c: c.duration, reverse=True):
    # Find partition with smallest total
    min_idx = argmin(partition_totals)
    partitions[min_idx].append(class)
    partition_totals[min_idx] += class.duration
```

### 3.3: Report Proposed Partitions

Present the result to the user before applying:

```markdown
## Proposed Partitions for Aspire.Hosting.Tests

| Partition | Classes | Est. Duration |
|-----------|---------|---------------|
| 1 | DistributedApplicationTests, ... | 4m 30s |
| 2 | PublishingTests, ResourceTests, ... | 4m 15s |
| 3 | ... | 4m 45s |

Total classes: 85, Partitions: 3, Max partition: 4m 45s
```

Also report classes that currently have a `[Trait("Partition")]` assignment that would change.

## Step 4: Apply Changes

### 4.1: Add/Update Partition Traits on Test Classes

For each class that needs a partition assignment:

1. Find the `.cs` file containing the class
2. Add or update `[Trait("Partition", "N")]` at the class level (before the class declaration, after any other attributes)

**Finding the file**:
```bash
# Search for the class declaration
grep -rn "class ClassName" tests/ProjectName.Tests/ --include="*.cs"
```

**Adding the trait**:
- If the class already has `[Trait("Partition", "X")]`, update the value
- If the class has no partition trait, add `[Trait("Partition", "N")]` on a new line before the class declaration
- Ensure `using Xunit;` is present (or the appropriate using for Trait)

**Pattern for adding/updating**:

```csharp
// BEFORE (no partition):
[Collection("someCollection")]
public class MyTestClass

// AFTER:
[Trait("Partition", "2")]
[Collection("someCollection")]
public class MyTestClass
```

```csharp
// BEFORE (existing partition):
[Trait("Partition", "3")]
public class MyTestClass

// AFTER (updated):
[Trait("Partition", "2")]
public class MyTestClass
```

### 4.2: Update .csproj for Newly Split Projects

For projects that are being partitioned for the first time, add to the `.csproj`:

```xml
<PropertyGroup>
  <!-- Split tests into partitions for CI parallelization -->
  <SplitTestsOnCI>true</SplitTestsOnCI>
  <TestClassNamePrefixForCI>Aspire.ProjectName.Tests</TestClassNamePrefixForCI>
</PropertyGroup>
```

The `TestClassNamePrefixForCI` should match the assembly/namespace prefix (e.g., `Aspire.Hosting.Tests`).

### 4.3: Handle Unpartitioned Classes

Any test class in a split project that does NOT have a `[Trait("Partition")]` will fall into the `uncollected:*` safety-net job. This is by design in `split-test-projects-for-ci.ps1`. However, for optimal balance, **assign all classes** to a partition.

If a class is very small (< 5 seconds), it can be grouped with other small classes into a single partition rather than getting its own.

### 4.4: Build and Verify

After applying all changes, build each affected project to verify:

```bash
dotnet build tests/ProjectName.Tests/ProjectName.Tests.csproj
```

Then verify the partition extraction works:

```bash
# Build the ExtractTestPartitions tool
dotnet build tools/ExtractTestPartitions/ExtractTestPartitions.csproj

# Run it against the test assembly
dotnet run --project tools/ExtractTestPartitions -- \
  artifacts/bin/Aspire.ProjectName.Tests/Debug/net10.0/Aspire.ProjectName.Tests.dll \
  /tmp/partitions.txt

cat /tmp/partitions.txt
```

Confirm the partition names match what you assigned.

## Step 5: Generate Summary Report

After applying changes, produce a final summary:

```markdown
## Test Partition Rebalancing Summary

### Data Sources
- Analyzed N builds from YYYY-MM-DD to YYYY-MM-DD
- Aggregation: P90 per class, worst-case across OS platforms

### Changes Applied

#### Aspire.Hosting.Tests (rebalanced: 6 -> 4 partitions)
| Partition | Classes | Est. Duration | Change |
|-----------|---------|---------------|--------|
| 1 | ClassA, ClassB, ... | 4m 30s | Rebalanced |
| 2 | ClassC, ClassD, ... | 4m 15s | Rebalanced |

#### Aspire.Hosting.Testing.Tests (new: 2 partitions)
| Partition | Classes | Est. Duration |
|-----------|---------|---------------|
| 1 | ... | 4m 00s |
| 2 | ... | 4m 30s |

### Files Modified
- tests/Aspire.Hosting.Tests/*.cs — updated Partition traits
- tests/Aspire.Hosting.Testing.Tests/*.cs — added Partition traits
- tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj — added SplitTestsOnCI

### Verification
- [ ] All affected projects build successfully
- [ ] ExtractTestPartitions correctly extracts partition names
- [ ] No test classes left unpartitioned (check uncollected bucket)
```

## Important Constraints

- **Never modify `api/*.cs` files** — these are auto-generated
- **Never modify `global.json`, `package.json`, or `NuGet.config`**
- **Partition traits go on the class, not on methods** — the CI filtering works at the class level via `--filter-trait "Partition=N"`
- **Partition names must be simple** — use numeric names (`"1"`, `"2"`, `"3"`) to match the existing convention in `Aspire.Hosting.Tests`
- **The `uncollected:*` job** catches any classes without partition traits — this is a safety net, not a dumping ground. Aim for zero uncollected classes.
- **Build after changes** — always verify the project compiles and the partition extraction tool works
- **Don't change test logic** — only add/update `[Trait("Partition", "N")]` attributes
- **Respect existing partitioned projects** — when rebalancing Aspire.Hosting.Tests (already has 6 partitions), update the existing traits rather than adding new ones
- **Watch for nested classes** — some test classes are nested; the partition trait goes on the outer class
- **Consider test parallelism** — TRX durations reflect wall-clock time including parallel execution within the job. The sum of individual test durations may exceed the job wall-clock time.

## Troubleshooting

### All-TestResults artifact not found
The `All-TestResults` artifact is only created by the `results` job in `tests.yml`. If the build was cancelled before that job ran, the artifact won't exist. Skip that build and try another.

### TRX file has no results
Some TRX files may be empty if tests were skipped or the job failed during build. Skip these files.

### Class not found in search
Some test classes use partial classes or are in nested directories. Use broader search:
```bash
grep -rn "class ClassName" tests/ --include="*.cs"
```

### Duration seems too low
The `duration` attribute in TRX is per-test execution time. If tests run in parallel within a class, the class wall-clock time will be less than the sum. Use the TRX `Times` element's start/finish for overall job duration validation.

## References

- `docs/ci/TestingOnCI.md` — Full documentation on test splitting infrastructure
- `eng/scripts/split-test-projects-for-ci.ps1` — How partitions are discovered at CI time
- `eng/scripts/build-test-matrix.ps1` — How the test matrix is built from partition data
- `tools/ExtractTestPartitions/Program.cs` — Extracts partition traits from test assemblies
- `tools/GenerateTestSummary/TrxReader.cs` — TRX file parser (C# reference)
- `.github/workflows/tests.yml` — Test orchestration workflow
- `.github/workflows/run-tests.yml` — Individual test runner (artifact upload pattern: `logs-{shortname}-{os}`)
