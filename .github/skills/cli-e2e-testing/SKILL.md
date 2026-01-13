---
name: cli-e2e-testing
description: Guide for writing Aspire CLI end-to-end tests using Hex1b terminal automation. Use this when asked to create, modify, or debug CLI E2E tests.
---

# Aspire CLI End-to-End Testing with Hex1b

This skill provides patterns and practices for writing end-to-end tests for the Aspire CLI using the Hex1b terminal automation library.

## Overview

CLI E2E tests use the Hex1b library to automate terminal sessions, simulating real user interactions with the Aspire CLI. Tests run in CI with asciinema recordings for debugging.

**Location**: `tests/Aspire.Cli.EndToEndTests/`

**Supported Platforms**: Linux only. Hex1b requires a Linux terminal environment. Tests are configured to skip on Windows and macOS in CI.

## Key Components

### Core Classes

- **`Hex1bTerminal`**: The main terminal class from the Hex1b library for terminal automation
- **`Hex1bTerminalInputSequenceBuilder`**: Fluent API for building sequences of terminal input/output operations
- **`CellPatternSearcher`**: Pattern matching for terminal cell content
- **`SequenceCounter`** (`Helpers/SequenceCounter.cs`): Tracks command execution count for deterministic prompt detection
- **`CliE2ETestHelpers`** (`Helpers/CliE2ETestHelpers.cs`): Extension methods and environment variable helpers
- **`TemporaryWorkspace`**: Creates isolated temporary directories for test execution

### Test Architecture

Each test:
1. Creates a `TemporaryWorkspace` for isolation
2. Builds a `Hex1bTerminal` with headless mode and asciinema recording
3. Creates a `Hex1bTerminalInputSequenceBuilder` with operations
4. Applies the sequence to the terminal and awaits completion

## Test Structure

```csharp
public sealed class SmokeTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _workDirectory;

    public SmokeTests(ITestOutputHelper output)
    {
        _output = output;
        _workDirectory = Path.Combine(Path.GetTempPath(), "aspire-cli-e2e", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_workDirectory);
    }

    [Fact]
    public async Task MyCliTest()
    {
        var workspace = TemporaryWorkspace.Create(_output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(MyCliTest));
        
        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Define pattern searchers for expected output
        var waitingForExpectedOutput = new CellPatternSearcher()
            .Find("Expected output text");

        // Create a sequence counter for tracking command prompts
        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        // Build the input sequence
        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
        }

        sequenceBuilder
            .Type("aspire --version")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
        await pendingRun;
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up work directory
        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
        await ValueTask.CompletedTask;
    }
}
```

## SequenceCounter and Prompt Detection

The `SequenceCounter` class tracks the number of shell commands executed. This enables deterministic waiting for command completion via a custom shell prompt.

### How It Works

1. `PrepareEnvironment()` configures the shell with a custom prompt: `[N OK] $ ` or `[N ERR:code] $ `
2. Each command increments the counter
3. `WaitForSuccessPrompt(counter)` waits for a prompt showing the current count with `OK`

```csharp
var counter = new SequenceCounter();

sequenceBuilder.PrepareEnvironment(workspace, counter)  // Sets up prompt, counter starts at 1
    .Type("echo hello")
    .Enter()
    .WaitForSuccessPrompt(counter)  // Waits for "[1 OK] $ ", then increments to 2
    .Type("ls -la")
    .Enter()
    .WaitForSuccessPrompt(counter)  // Waits for "[2 OK] $ ", then increments to 3
    .Type("exit")
    .Enter();
```

This approach is more reliable than arbitrary timeouts because it deterministically waits for each command to complete.

## Pattern Searching with CellPatternSearcher

Use `CellPatternSearcher` to find text patterns in terminal output:

```csharp
// Simple text search
var waitingForPrompt = new CellPatternSearcher()
    .Find("Enter the project name");

// Pattern with wildcards
var waitingForTemplate = new CellPatternSearcher()
    .FindPattern("> Starter App");

// Chained patterns (find "b", then scan right until "$", then right of " ")
var waitingForShell = new CellPatternSearcher()
    .Find("b").RightUntil("$").Right(' ').Right(' ');

// Use in WaitUntil
sequenceBuilder.WaitUntil(
    snapshot => waitingForPrompt.Search(snapshot).Count > 0,
    TimeSpan.FromSeconds(30));
```

## Extension Methods

### CliE2ETestHelpers Extensions on Hex1bTerminalInputSequenceBuilder

| Method | Description |
|--------|-------------|
| `PrepareEnvironment(workspace, counter)` | Sets up custom prompt with command tracking, changes to workspace directory |
| `InstallAspireCliFromPullRequest(prNumber, counter)` | Downloads and installs CLI from PR artifacts |
| `SourceAspireCliEnvironment(counter)` | Adds `~/.aspire/bin` to PATH (Linux only) |

### SequenceCounterExtensions

| Method | Description |
|--------|-------------|
| `WaitForSuccessPrompt(counter, timeout?)` | Waits for `[N OK] $ ` prompt and increments counter |
| `IncrementSequence(counter)` | Manually increments the counter |

## DO: Use CellPatternSearcher for Output Detection

Wait for specific output patterns rather than arbitrary delays:

```csharp
var waitingForMessage = new CellPatternSearcher()
    .Find("Project created successfully.");

sequenceBuilder
    .Type("aspire new")
    .Enter()
    .WaitUntil(s => waitingForMessage.Search(s).Count > 0, TimeSpan.FromMinutes(2));
```

## DO: Use WaitForSuccessPrompt After Commands

After running shell commands, use `WaitForSuccessPrompt()` to wait for the command to complete:

```csharp
sequenceBuilder
    .Type("dotnet build")
    .Enter()
    .WaitForSuccessPrompt(counter)  // Waits for prompt, verifies success
    .Type("dotnet run")
    .Enter()
    .WaitForSuccessPrompt(counter);
```

## DO: Handle Interactive Prompts

For CLI commands with interactive prompts, wait for each prompt before responding:

```csharp
var waitingForTemplatePrompt = new CellPatternSearcher()
    .FindPattern("> Starter App");

var waitingForProjectNamePrompt = new CellPatternSearcher()
    .Find("Enter the project name");

sequenceBuilder
    .Type("aspire new")
    .Enter()
    .WaitUntil(s => waitingForTemplatePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
    .Enter()  // Select first template
    .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
    .Type("MyProject")
    .Enter();
```

## DO: Use Ctrl+C to Stop Long-Running Processes

For processes like `aspire run` that don't exit on their own:

```csharp
sequenceBuilder
    .Type("aspire run")
    .Enter()
    .WaitUntil(s => waitForCtrlCMessage.Search(s).Count > 0, TimeSpan.FromSeconds(30))
    .Ctrl().Key(Hex1bKey.C)  // Send Ctrl+C
    .WaitForSuccessPrompt(counter);
```

## DO: Check IsRunningInCI for CI-Only Operations

Some operations only apply in CI (like installing CLI from PR artifacts):

```csharp
var isCI = CliE2ETestHelpers.IsRunningInCI;

sequenceBuilder.PrepareEnvironment(workspace, counter);

if (isCI)
{
    sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
    sequenceBuilder.SourceAspireCliEnvironment(counter);
}

// Continue with test commands...
```

## DO: Get Environment Variables Using Helpers

Use `CliE2ETestHelpers` for CI environment variables:

```csharp
var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();   // GITHUB_PR_NUMBER (0 when local)
var commitSha = CliE2ETestHelpers.GetRequiredCommitSha(); // GITHUB_PR_HEAD_SHA ("local0000" when local)
var isCI = CliE2ETestHelpers.IsRunningInCI;               // true when both env vars set
var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath("test-name"); // Appropriate path for CI vs local
```

## DON'T: Use Hard-coded Delays

Use `WaitUntil()` with specific output patterns instead of arbitrary delays:

```csharp
// DON'T: Arbitrary delays
.Wait(TimeSpan.FromSeconds(30))

// DO: Wait for specific output
.WaitUntil(
    snapshot => pattern.Search(snapshot).Count > 0,
    TimeSpan.FromSeconds(30))
```

## DON'T: Hard-code Prompt Sequence Numbers

Don't hard-code the sequence numbers in `WaitForSuccessPrompt` calls. Use the counter:

```csharp
// DON'T: Hard-coded sequence numbers
.WaitUntil(s => s.GetScreenText().Contains("[3 OK] $ "), timeout)

// DO: Use the counter
.WaitForSuccessPrompt(counter)
```

The counter automatically tracks which command you're waiting for, even if command sequences change.

## Adding New Extension Methods

When adding new CLI operations as extension methods:

```csharp
internal static Hex1bTerminalInputSequenceBuilder MyNewOperation(
    this Hex1bTerminalInputSequenceBuilder builder,
    string arg,
    SequenceCounter counter,
    TimeSpan? timeout = null)
{
    var expectedOutput = new CellPatternSearcher()
        .Find("Expected output");

    return builder
        .Type($"aspire my-command {arg}")
        .Enter()
        .WaitUntil(
            snapshot => expectedOutput.Search(snapshot).Count > 0,
            timeout ?? TimeSpan.FromSeconds(30))
        .WaitForSuccessPrompt(counter);
}
```

Key points:
1. Define as extension method on `Hex1bTerminalInputSequenceBuilder`
2. Accept `SequenceCounter` parameter for prompt tracking
3. Use `CellPatternSearcher` for output detection
4. Call `WaitForSuccessPrompt(counter)` after command completion
5. Return the builder for fluent chaining

## CI Configuration

Environment variables set in CI:
- `GITHUB_PR_NUMBER`: PR number for downloading CLI artifacts
- `GITHUB_PR_HEAD_SHA`: PR head commit SHA for version verification (not the merge commit)
- `GH_TOKEN`: GitHub token for API access
- `GITHUB_WORKSPACE`: Workspace root for artifact paths

Each test class runs as a separate CI job via `CliEndToEndTestRunsheetBuilder` for parallel execution.

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
- `Tests / Cli E2E Linux (SmokeTests) / SmokeTests (ubuntu-latest)`

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

# Download a specific test artifact (e.g., SmokeTests)
mkdir -p /tmp/cli-e2e-debug && cd /tmp/cli-e2e-debug
gh run download <run-id> -n logs-Cli.EndToEnd.SmokeTests-ubuntu-latest -R dotnet/aspire
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
    ├── CreateAndRunAspireStarterProject.cast   # Asciinema recording for each test
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
   asciinema play testresults/recordings/CreateAndRunAspireStarterProject.cast
   
   # Or view as text for AI analysis
   head -100 testresults/recordings/CreateAndRunAspireStarterProject.cast
   ```

3. **Test results XML** - Parse for specific failures:
   ```bash
   # Find failed tests in TRX file
   grep -A 5 'outcome="Failed"' testresults/*.trx
   ```

### Step 5: Common Issues and Solutions

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Timeout waiting for prompt | Command failed or hung | Check recording to see terminal output at timeout |
| `[N ERR:code] $ ` in prompt | Previous command exited with non-zero | Check recording to see which command failed |
| Pattern not found | Output format changed | Update `CellPatternSearcher` patterns |
| Test hangs indefinitely | Waiting for wrong prompt number | Verify `SequenceCounter` usage matches commands |
| Timeout waiting for dashboard URL | Project failed to build/run | Check recording for build errors |

### One-Liner: Download and Examine Latest Failed Run

```bash
# Get the latest failed run ID and download CLI E2E logs
RUN_ID=$(gh run list --branch $(git branch --show-current) --status failure --limit 1 --json databaseId --jq '.[0].databaseId') && \
  mkdir -p /tmp/cli-e2e-debug && cd /tmp/cli-e2e-debug && \
  gh run download $RUN_ID -n logs-Cli.EndToEnd.SmokeTests-ubuntu-latest -R dotnet/aspire 2>/dev/null && \
  echo "=== Downloaded to /tmp/cli-e2e-debug ===" && ls -la testresults/
```
