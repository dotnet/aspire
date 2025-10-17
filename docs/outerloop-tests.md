# Outerloop Tests

## Overview

Outerloop tests are tests that are excluded from regular CI runs but run in a separate outerloop CI workflow. These are typically tests that are:
- Long-running
- Resource-intensive
- Need specific conditions to run like Playwright

They are marked with the `[OuterloopTest]` attribute and are excluded from regular CI runs but run in the outerloop CI workflow.

## How Outerloop Tests Work

The `OuterloopTestAttribute` applies the xUnit trait `outerloop=true` to tests. This trait can then be used with test filters to include or exclude these tests.

## Running Tests Without Outerloop Tests

To run tests excluding outerloop tests (this is what regular CI does):

```bash
dotnet test --filter-not-trait "outerloop=true"
```

## Running Only Outerloop Tests

To run only outerloop tests (useful for debugging):

```bash
dotnet test --filter-trait "outerloop=true"
```

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
- **Outerloop CI on Schedule/Push**: Runs all outerloop tests separately on a schedule (daily at 02:00 UTC)
- **Outerloop CI on Pull Requests**: Runs only the first outerloop test project across all OSes (Windows, Linux, macOS) as a sanity check when PRs modify workflow/infrastructure files
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
