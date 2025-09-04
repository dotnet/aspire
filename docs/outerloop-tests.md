# Outerloop Tests

## Overview

Outerloop tests are tests that are excluded from regular CI runs but run in a separate outerloop CI workflow. These are typically tests that are:
- Long-running
- Resource-intensive 
- Require special infrastructure
- Need specific conditions to run

They are marked with the `[OuterloopTest]` attribute and are excluded from regular CI runs but run in the outerloop CI workflow.

## How Outerloop Tests Work

The `OuterloopTestAttribute` applies the xUnit trait `outerloop=true` to tests. This trait can then be used with test filters to include or exclude these tests.

## Running Tests Without Outerloop Tests

To run tests excluding outerloop tests (this is what regular CI does):

```bash
dotnet test --filter-not-trait "outerloop=true"
```

Or using the direct test runner:

```bash
dotnet exec YourTestAssembly.dll --filter-not-trait "outerloop=true"
```

## Running Only Outerloop Tests

To run only outerloop tests (useful for debugging):

```bash
dotnet test --filter-trait "outerloop=true"
```

Or using the direct test runner:

```bash
dotnet exec YourTestAssembly.dll --filter-trait "outerloop=true"
```

## Outerloop Test Lifecycle

1. **Mark as Outerloop**: When a test is long-running or resource-intensive, mark it with `[OuterloopTest("reason")]`
2. **Outerloop Execution**: Outerloop tests run automatically in the outerloop CI (every 6 hours)
3. **Regular Exclusion**: These tests are excluded from regular CI to keep it fast
4. **Monitoring**: Outerloop test results are monitored separately

## Example

```csharp
[Fact]
[OuterloopTest("Long running integration test")]
public async Task LongRunningIntegrationTest()
{
    // Long running test implementation
}
```

## CI Integration

- **Regular CI**: Uses `--filter-not-trait "outerloop=true"` to exclude outerloop tests
- **Outerloop CI**: Runs outerloop tests separately on a schedule
- **Results**: Outerloop test failures are tracked separately from regular CI

## For Copilot Agent and Test Runners

When running tests in automated environments like Copilot agent, exclude outerloop tests for faster execution:

```bash
# Good - excludes outerloop tests
dotnet test --filter-not-trait "outerloop=true"

# Bad - runs all tests including long-running outerloop ones
dotnet test
```

## Difference from Quarantined Tests

- **Quarantined Tests**: Flaky tests that are temporarily disabled but monitored
- **Outerloop Tests**: Stable tests that are moved to a separate workflow due to execution time/resource requirements