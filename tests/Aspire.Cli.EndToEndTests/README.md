# Aspire CLI End-to-End Tests

This test project contains end-to-end tests for the Aspire CLI using the [Hex1b](https://github.com/hex1b/hex1b) terminal automation library.

## Overview

These tests simulate real user interactions with the Aspire CLI by automating terminal input/output. Each test class runs as a **separate CI job** to enable parallel execution across GitHub Actions runners.

## Architecture

### Test Infrastructure

- **`CliEndToEndTestBase`**: Base class providing Hex1b terminal setup, work directory management, and helper methods
- **`HeadlessPresentationAdapter`**: A headless rendering adapter for running tests without a display
- **`CliEndToEndTestRunsheetBuilder`**: MSBuild targets that extract test classes and generate per-class runsheets

### CI Pipeline

The CI infrastructure uses a matrix strategy to fan out test execution:

1. **Discovery Phase**: The `CliEndToEndTestRunsheetBuilder` builds the test project and runs `--list-tests` to discover all test classes
2. **Runsheet Generation**: A JSON runsheet entry is created for each unique test class
3. **Parallel Execution**: GitHub Actions creates a separate job for each test class, running them in parallel on different agents

```plaintext
┌─────────────────────────────────────────────────────────────────┐
│                    generate_cli_e2e_matrix                       │
│    Discovers test classes and generates runsheet                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      build_packages                              │
│    Builds NuGet packages needed for tests                        │
└─────────────────────────────────────────────────────────────────┘
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
            ┌───────────┐ ┌───────────┐ ┌───────────┐
            │ Job 1     │ │ Job 2     │ │ Job N     │
            │ NewCmd    │ │ RunCmd    │ │ ...       │
            │ Tests     │ │ Tests     │ │           │
            └───────────┘ └───────────┘ └───────────┘
```

## Writing Tests

### Test Class Structure

Each test class should:
1. Inherit from `CliEndToEndTestBase`
2. Implement `IAsyncLifetime` for proper setup/teardown
3. Call `await base.InitializeAsync()` in `InitializeAsync`
4. Call `await base.DisposeAsync()` in `DisposeAsync`

```csharp
public sealed class MyCommandTests : CliEndToEndTestBase, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public async Task MyCommand_DoesExpectedThing()
    {
        // Use the helper methods to interact with the CLI
        await RunAspireAsync("my-command --option value");
        await WaitAsync(2000);
        
        // Assert on file system changes, process output, etc.
    }
}
```

### Available Helper Methods

- `RunAspireAsync(string arguments)`: Runs `aspire <arguments>` in the terminal
- `TypeAndEnterAsync(string text)`: Types text and presses Enter
- `WaitAsync(int milliseconds)`: Waits for the specified duration
- `CreateSequence()`: Creates a new `Hex1bTerminalInputSequenceBuilder` for complex interactions
- `WorkDirectory`: The unique temporary directory for this test

### Complex Interactions

For interactive prompts and complex terminal interactions:

```csharp
var sequence = CreateSequence()
    .SlowType("aspire new")
    .Enter()
    .Wait(2000)
    .Key(Hex1bKey.DownArrow)  // Navigate menu
    .Enter()                   // Select option
    .SlowType("myproject")     // Enter project name
    .Enter()
    .Build();

await sequence.ApplyAsync(Terminal);
```

## Running Tests Locally

```bash
# Build the test project
./build.sh -restore -build -projects tests/Aspire.Cli.EndToEndTests/Aspire.Cli.EndToEndTests.csproj

# Run all tests
dotnet test tests/Aspire.Cli.EndToEndTests/Aspire.Cli.EndToEndTests.csproj

# Run a specific test class
dotnet test tests/Aspire.Cli.EndToEndTests/Aspire.Cli.EndToEndTests.csproj -- --filter-class "Aspire.Cli.EndToEndTests.AspireNewCommandTests"
```

## Requirements

- **Linux only**: Hex1b requires a Linux terminal environment. Tests are skipped on Windows and macOS.
- **Aspire CLI installed**: The `aspire` command must be available in the PATH.
- **Built NuGet packages**: These tests require the Aspire packages to be built first.

## Adding New Test Classes

When adding a new test class:

1. Create a new file with the pattern `Aspire*Tests.cs` or `*CommandTests.cs`
2. Follow the test class structure above
3. The CI will automatically discover and run your tests as a separate job

No changes to the CI configuration are required - the runsheet builder automatically discovers all test classes.
