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
            .WriteTestLog(_output, "Creating new Aspire project...")
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

## DO: Use WriteTestLog() for Deferred Logging

The `WriteTestLog()` extension method writes log messages during sequence execution (not build time),
including the current terminal snapshot:

```csharp
ctx.SequenceBuilder
    .WriteTestLog(_output, "About to run command...")
    .Type("my-command")
    .Enter()
    .WriteTestLog(_output, "Command completed");
```

This outputs:
```
[LOG] About to run command...
[TERMINAL]
<current terminal screen content>
--------------------------------------------------------------------------------
```

Use `Callback()` for arbitrary deferred actions:

```csharp
ctx.SequenceBuilder.Callback(() => _output?.WriteLine("Simple message"));
```

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
| `SourceAspireCliEnvironment()` | Sources ~/.bashrc on Linux/macOS (no-op on Windows), sets DOTNET_CLI env vars |
| `RunDiagnostics(timeout?)` | Runs `dotnet nuget list source` and `dotnet --list-sdks` for debugging |
| `VerifyAspireCliVersion(commitSha, timeout?)` | Runs `aspire --version` and verifies SHA |
| `ExitTerminal()` | Types `exit` to close the shell |
| `AddSequence(ctx => ...)` | Custom operations using the underlying Hex1b builder |

### Extension Methods for Hex1bTerminalInputSequenceBuilder

| Method | Description |
|--------|-------------|
| `WriteTestLog(output, message)` | Logs message with terminal snapshot during execution |
| `Callback(action)` | Executes arbitrary action during sequence execution |
| `WaitUntil(predicate, timeout)` | Waits for terminal content to match predicate |

## DO: Get Environment Variables Using Helpers

Use `CliE2ETestHelpers` for CI environment variables with built-in assertions:

```csharp
var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();   // GITHUB_PR_NUMBER
var commitSha = CliE2ETestHelpers.GetRequiredCommitSha(); // GITHUB_PR_HEAD_SHA
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
            .WriteTestLog(_output, $"Running my-command with {arg}...")
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
2. Use `WriteTestLog()` for deferred logging with terminal snapshots
3. Type the command and press Enter
4. Use `WaitUntil()` with a specific output pattern
5. Return the builder for fluent chaining

## CI Configuration

Environment variables set in `run-tests.yml`:
- `GITHUB_PR_NUMBER`: PR number for downloading CLI artifacts
- `GITHUB_PR_HEAD_SHA`: PR head commit SHA for version verification (not the merge commit)
- `GH_TOKEN`: GitHub token for API access
- `GITHUB_WORKSPACE`: Workspace root for artifact paths

The `cli_e2e_tests` job depends on `build_packages` to ensure CLI packages are built before tests run.

## CI Troubleshooting

When CLI E2E tests fail in CI, follow these steps to diagnose the issue:

### Step 1: Find the Failed CI Run

First, identify the run ID for your PR's CI run:

```bash
# List recent CI runs for your branch
gh run list --branch <your-branch-name> --limit 5 --json databaseId,status,conclusion,url

# Or find the latest failed run
gh run list --branch <your-branch-name> --status failure --limit 1 --json databaseId,url
```

### Step 2: Identify Failed CLI E2E Jobs

Check which specific CLI E2E test jobs failed. Job names follow this pattern:
`Tests / Cli E2E <Platform> (<TestClass>) / <TestClass> (<os>-latest)`

For example:
- `Tests / Cli E2E Linux (RunTests) / RunTests (ubuntu-latest)`
- `Tests / Cli E2E Windows (RunTests) / RunTests (windows-latest)`

```bash
# Replace <run-id> with the actual run ID
gh run view <run-id> --json jobs --jq '.jobs[] | select(.name | test("Cli E2E")) | {name, conclusion}'
```

### Step 3: Download Test Artifacts

CLI E2E tests upload artifacts with names like `logs-Cli.EndToEnd.<TestClass>-ubuntu-latest`. Download them:

```bash
# List available CLI E2E artifacts
gh api --paginate "repos/dotnet/aspire/actions/runs/<run-id>/artifacts" \
  --jq '.artifacts[].name' | grep -E "Cli\.EndToEnd|cli-e2e"

# Download a specific test artifact (e.g., RunTests)
mkdir -p /tmp/cli-e2e-debug && cd /tmp/cli-e2e-debug
gh run download <run-id> -n logs-Cli.EndToEnd.RunTests-ubuntu-latest -R dotnet/aspire
```

### Step 4: Examine Downloaded Artifacts

The downloaded artifact contains:

```
testresults/
├── <TestClass>_net10.0_*.trx          # Test results XML
├── Aspire.Cli.EndToEndTests_*.log     # Console output log
├── *.crash.dmp                        # Crash dump (if test crashed)
├── test.binlog                        # MSBuild binary log
└── recordings/
    ├── run-aspire-starter.cast        # Asciinema recording for each test
    ├── run-aspire-py-starter.cast
    └── ...
```

**Key files to examine:**

1. **Console log** - Search for errors and final terminal state:
   ```bash
   # Look at the end of the log (shows final terminal state and error summary)
   tail -100 testresults/*.log
   
   # Search for timeout errors
   grep -i "timeout\|timed out" testresults/*.log
   ```

2. **Asciinema recordings** - Replay terminal sessions to see exactly what happened:
   ```bash
   # List recordings
   ls -la testresults/recordings/
   
   # Play a recording (requires asciinema installed)
   asciinema play testresults/recordings/run-aspire-starter.cast
   
   # Or view as text for AI analysis
   head -100 testresults/recordings/run-aspire-starter.cast
   ```

3. **Test results XML** - Parse for specific failures:
   ```bash
   # Find failed tests in TRX file
   grep -A 5 'outcome="Failed"' testresults/*.trx
   ```

### Step 5: Common Issues and Solutions

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| "Interactive input is not supported" | `ASPIRE_PLAYGROUND=true` not set | Ensure `SourceAspireCliEnvironment()` is called |
| Test hangs at "Creating new Aspire project..." | npm hanging on interactive prompt | Set `CI=true` environment variable |
| "No command prompts found" | `PrepareEnvironment()` not called or prompt not rendered | Ensure `PrepareEnvironment()` is first in chain |
| Command verification fails | Previous command exited with non-zero | Check terminal recording to see which command failed |
| Timeout waiting for dashboard URL | Project failed to build/run | Check recording for build errors |

### One-Liner: Download and Examine Latest Failed Run

```bash
# Get the latest failed run ID and download CLI E2E logs
RUN_ID=$(gh run list --branch $(git branch --show-current) --status failure --limit 1 --json databaseId --jq '.[0].databaseId') && \
  mkdir -p /tmp/cli-e2e-debug && cd /tmp/cli-e2e-debug && \
  gh run download $RUN_ID -n logs-Cli.EndToEnd.RunTests-ubuntu-latest -R dotnet/aspire 2>/dev/null || \
  gh run download $RUN_ID -n logs-Cli.EndToEnd.AcquisitionTests-ubuntu-latest -R dotnet/aspire && \
  echo "=== Downloaded to /tmp/cli-e2e-debug ===" && ls -la testresults/
```
