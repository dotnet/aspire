# RunTestsInLoop

A utility to run tests repeatedly in a loop to help reproduce intermittent failures or hangs that occur in CI.

## Purpose

This tool is designed to help developers:

- **Reproduce CI hangs** by running tests multiple times until a hang occurs
- **Identify flaky tests** by tracking pass/fail statistics across many runs
- **Stress test specific tests** to ensure stability before unquarantining

## Prerequisites

- .NET SDK 10+ (installed via `./restore.sh` or `./restore.cmd`)
- The repository should be built first (`./build.sh` or `./build.cmd`)

## Usage

```bash
# Show help
dotnet run --project tools/RunTestsInLoop -- --help

# Run DistributedApplicationTests 10 times with 5 minute timeout per run
dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --iterations 10 --timeout 5

# Run a specific test class 20 times
dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --class "Aspire.Hosting.Tests.DistributedApplicationTests" --iterations 20

# Run a specific test method 50 times
dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --method "RegisteredLifecycleHookIsExecutedWhenRunAsynchronously" --iterations 50

# Run with verbose output and continue on failure
dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --iterations 5 --verbose --stop-on-failure false

# Skip building (if already built)
dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --iterations 10 --no-build
```

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--project` | `-p` | Path to test project (required) | - |
| `--iterations` | `-i` | Number of test runs | 10 |
| `--timeout` | `-t` | Timeout per run in minutes (0 = no timeout) | 10 |
| `--method` | `-m` | Filter by test method name | - |
| `--class` | `-c` | Filter by test class name | - |
| `--namespace` | `-n` | Filter by namespace | - |
| `--verbose` | `-v` | Show detailed test output | false |
| `--stop-on-failure` | `-s` | Stop after first failure/timeout | true |
| `--extra-args` | `-e` | Additional dotnet test arguments | - |
| `--no-build` | - | Skip building the project | false |

## Example Output

```
╔══════════════════════════════════════════════════════════════╗
║              Test Loop Runner for Aspire                     ║
╚══════════════════════════════════════════════════════════════╝

Project:    tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj
Iterations: 10
Timeout:    5 minutes
Class:      Aspire.Hosting.Tests.DistributedApplicationTests
Stop on failure: true

Building test project...
Build succeeded.

╔══════════════════════════════════════════════════════════════╗
║                    Starting Test Loop                        ║
╚══════════════════════════════════════════════════════════════╝

┌──────────────────────────────────────────────────────────────┐
│  Iteration 1/10                                              │
└──────────────────────────────────────────────────────────────┘
  ✅ PASSED in 45.2s

┌──────────────────────────────────────────────────────────────┐
│  Iteration 2/10                                              │
└──────────────────────────────────────────────────────────────┘
  ⏱️  TIMEOUT after 5.0 minutes!
  Last 50 lines of output:
    ...

  Stopping due to timeout (--stop-on-failure is enabled)

╔══════════════════════════════════════════════════════════════╗
║                      Final Results                           ║
╚══════════════════════════════════════════════════════════════╝
┌────────────────────────────────────────┐
│  Statistics                            │
├────────────────────────────────────────┤
│  Passed:         1                     │
│  Failed:         0                     │
│  Timed out:      1                     │
│  Total:          2                     │
├────────────────────────────────────────┤
│  Avg time:   152.6s                    │
│  Min time:    45.2s                    │
│  Max time:   300.0s                    │
├────────────────────────────────────────┤
│  Success rate:  50.0%                  │
└────────────────────────────────────────┘

💡 Tip: If you found a flaky test, consider quarantining it:
   dotnet run --project tools/QuarantineTools -- -q -i <issue-url> <Namespace.Class.Method>
```

## Use Cases

### Reproducing CI hangs

```bash
# Run the problematic tests many times to trigger the hang
dotnet run --project tools/RunTestsInLoop -- \
    --project tests/Aspire.Hosting.Tests \
    --class "Aspire.Hosting.Tests.DistributedApplicationTests" \
    --iterations 100 \
    --timeout 5
```

### Stress testing a specific test

```bash
# Run a specific test 1000 times to ensure stability
dotnet run --project tools/RunTestsInLoop -- \
    --project tests/Aspire.Hosting.Tests \
    --method "RegisteredLifecycleHookIsExecutedWhenRunAsynchronously" \
    --iterations 1000 \
    --stop-on-failure false
```

### Finding timeout issues

```bash
# Run with a short timeout to catch slow tests
dotnet run --project tools/RunTestsInLoop -- \
    --project tests/Aspire.Hosting.Tests \
    --iterations 20 \
    --timeout 2
```

## How it works

1. **Resolves the project path** relative to the repository root
2. **Builds the test project** (unless `--no-build` is specified)
3. **Runs tests in a loop** for the specified number of iterations
4. **Applies timeout** to each run and kills hung processes
5. **Tracks statistics** including pass/fail/timeout counts and timing
6. **Reports results** with a summary and success rate

## Notes

- Tests are run with `--filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"` to exclude quarantined and outerloop tests
- The tool uses the repository's `dotnet.sh`/`dotnet.cmd` wrapper to ensure the correct SDK is used
- When a timeout occurs, the entire process tree is killed to clean up orphaned processes
- Statistics are printed every 5 iterations and at the end

## See Also

- [QuarantineTools](../QuarantineTools/README.md) - For quarantining flaky tests
- [Test README](../../tests/README.md) - For general test running information
