---
name: cli-e2e-testing
description: Guide for writing Aspire CLI end-to-end tests using Hex1b terminal automation. Use this when asked to create, modify, or debug CLI E2E tests.
---

# Aspire CLI End-to-End Testing with Hex1b

This skill provides patterns and practices for writing end-to-end tests for the Aspire CLI using the Hex1b terminal automation library.

## Overview

CLI E2E tests use the Hex1b library to automate terminal sessions, simulating real user interactions with the Aspire CLI. Tests run in CI with asciinema recordings for debugging.

**Location**: `tests/Aspire.Cli.EndToEndTests/`

**Supported Platforms**: Linux, macOS, and Windows. The automation builder automatically uses the appropriate shell and commands for each platform.

## Key Components

### Helper Classes

- **`AspireCliAutomationBuilder`** (`Helpers/AspireCliAutomationBuilder.cs`): Main builder class that encapsulates terminal session lifecycle and automation methods
- **`CliE2ETestHelpers`** (`Helpers/CliE2ETestHelpers.cs`): Factory methods for terminal sessions and environment variable helpers

### AspireCliAutomationBuilder

The builder owns the terminal session and provides a fluent API for CLI automation:

```csharp
await using var builder = await AspireCliAutomationBuilder.CreateAsync(
    workingDirectory: _workDirectory,
    recordingName: "my-test",
    output: _output,
    prNumber: prNumber);

await builder
    .PrepareEnvironment()
    .InstallAspireCliFromPullRequest(prNumber)
    .SourceAspireCliEnvironment()
    .VerifyAspireCliVersion(commitSha)
    .ExitTerminal()
    .ExecuteAsync();
```

### AspireCliAutomationContext

For custom operations, use `AddSequence()` with a callback that receives the context:

```csharp
public sealed record AspireCliAutomationContext(
    Hex1bTerminalInputSequenceBuilder SequenceBuilder,
    AspireTerminalSession Session);
```

## DO: Use AspireCliAutomationBuilder for Tests

Create tests using the builder pattern which handles:
- Recording path configuration (CI vs local)
- Session lifecycle management
- Error handling with terminal snapshots

```csharp
[Fact]
public async Task MyCliTest()
{
    var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
    var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();

    await using var builder = await AspireCliAutomationBuilder.CreateAsync(
        workingDirectory: _workDirectory,
        recordingName: "my-test",
        output: _output,
        prNumber: prNumber);

    await builder
        .PrepareEnvironment()
        .InstallAspireCliFromPullRequest(prNumber)
        .SourceAspireCliEnvironment()
        .VerifyAspireCliVersion(commitSha)
        .ExitTerminal()
        .ExecuteAsync();
}
```

## DO: Use AddSequence() for Custom Operations

When you need to run custom commands not covered by built-in methods, use `AddSequence()`:

```csharp
await builder
    .PrepareEnvironment()
    .AddSequence(ctx =>
    {
        ctx.SequenceBuilder
            .Type("aspire new starter --name MyApp")
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains("Created project"),
                TimeSpan.FromMinutes(2));
    })
    .ExitTerminal()
    .ExecuteAsync();
```

The context provides access to:
- `SequenceBuilder`: The underlying `Hex1bTerminalInputSequenceBuilder`
- `Session`: The `AspireTerminalSession` for direct terminal access if needed

Use `WaitUntil()` to wait for specific output patterns in the terminal.

## DO: Always Call ExecuteAsync() at the End

`ExecuteAsync()` runs the built sequence and handles:
- Applying the input sequence to the terminal
- Catching `TimeoutException`
- Logging terminal content on failure
- Failing the test with descriptive messages
- Cleaning up resources

```csharp
await builder
    .PrepareEnvironment()
    // ... operations ...
    .ExitTerminal()
    .ExecuteAsync();  // Always end with this
```

## DO: Always Call PrepareEnvironment() First

`PrepareEnvironment()` sets up a custom prompt that tracks command count and exit status:
- Success: `[N ✔] $ `
- Failure: `[N ✘:code] $ `

This prompt is useful for debugging recordings - you can see which command failed by looking at the prompt.

## DO: Use Built-in Methods When Available

The builder provides high-level methods that handle command sequencing automatically.
All methods are cross-platform and use appropriate shell commands for each OS.

| Method | Description |
|--------|-------------|
| `PrepareEnvironment()` | Sets up debug prompt (bash on Linux/macOS, PowerShell on Windows) |
| `InstallAspireCliFromPullRequest(prNumber, timeout?)` | Installs CLI from PR artifacts (uses appropriate script per OS) |
| `SourceAspireCliEnvironment()` | Sources ~/.bashrc on Linux/macOS (no-op on Windows) |
| `VerifyAspireCliVersion(commitSha, timeout?)` | Runs `aspire --version` and verifies SHA |
| `ExitTerminal()` | Types `exit` to close the shell |
| `AddSequence(ctx => ...)` | Custom operations using the underlying Hex1b builder |

## DO: Get Environment Variables Using Helpers

Use `CliE2ETestHelpers` for CI environment variables with built-in assertions:

```csharp
var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();   // GITHUB_PR_NUMBER
var commitSha = CliE2ETestHelpers.GetRequiredCommitSha(); // GITHUB_SHA
```

## DON'T: Manually Manage Terminal Sessions

Let the builder handle session lifecycle:

```csharp
// DON'T: Manual session management
await using var session = await CliE2ETestHelpers.CreateTerminalSessionAsync(...);
var sequence = new Hex1bTerminalInputSequenceBuilder()...

// DO: Use the builder
await using var builder = await AspireCliAutomationBuilder.CreateAsync(...);
await builder.PrepareEnvironment()...ExecuteAsync();
```

## DON'T: Use Hard-coded Delays

Use `WaitUntil()` with specific output patterns instead of arbitrary delays:

```csharp
// DON'T: Arbitrary delays
.Wait(TimeSpan.FromSeconds(30))

// DO: Wait for specific output
.WaitUntil(
    snapshot => snapshot.GetScreenText().Contains("Successfully installed"),
    TimeSpan.FromMinutes(5))
```

## DON'T: Catch Exceptions from ExecuteAsync()

Let `ExecuteAsync()` handle errors - it logs terminal state and fails the test appropriately:

```csharp
// DON'T: Wrap in try-catch
try { await builder...ExecuteAsync(); }
catch (Exception ex) { /* custom handling */ }

// DO: Let ExecuteAsync handle it
await builder...ExecuteAsync();
```

## DON'T: Skip Tests When Environment Variables Are Missing

Tests should fail explicitly, not skip silently:

```csharp
// DON'T: Skip.If(string.IsNullOrEmpty(...))
// DO: Use GetRequiredPrNumber() which asserts
```

## Adding New Builder Methods

When adding new CLI operations to the builder:

```csharp
public AspireCliAutomationBuilder MyNewOperation(
    string arg,
    TimeSpan? timeout = null)
{
    return AddSequence(ctx =>
    {
        ctx.SequenceBuilder
            .Type($"aspire my-command {arg}")
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains("Expected output"),
                timeout ?? TimeSpan.FromSeconds(30));
    });
}
```

Key points:
1. Use `AddSequence()` to add the operation
2. Type the command and press Enter
3. Use `WaitUntil()` with a specific output pattern
4. Return the builder for fluent chaining

## CI Configuration

Environment variables set in `run-tests.yml`:
- `GITHUB_PR_NUMBER`: PR number for downloading CLI artifacts
- `GITHUB_SHA`: Commit SHA for version verification
- `GH_TOKEN`: GitHub token for API access
- `GITHUB_WORKSPACE`: Workspace root for artifact paths

The `cli_e2e_tests` job depends on `build_packages` to ensure CLI packages are built before tests run.
