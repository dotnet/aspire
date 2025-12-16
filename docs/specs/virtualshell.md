# Virtual Shell API Surface Spec

> **Note**: This API is experimental and marked with `[Experimental("ASPIREHOSTINGVIRTUALSHELL001")]`.

## Overview

VirtualShell is a cross-platform process execution abstraction for .NET that provides a consistent, testable, and secure way to run external commands. It was built to address several pain points with traditional process execution:

**The Problem**

Running external processes in .NET typically involves `System.Diagnostics.Process`, which has several challenges:
- Platform-specific behavior (Windows vs Unix shell semantics)
- Inconsistent argument quoting and escaping rules
- Shell injection vulnerabilities when building command strings
- Difficult to mock and test
- No built-in support for secrets redaction in logs
- Complex streaming patterns with stdout/stderr

**The Solution**

VirtualShell provides:
- **No shell execution**: Commands are parsed and executed directly, never through `cmd.exe` or `bash`. This eliminates shell injection attacks and ensures consistent behavior across platforms.
- **Immutable shell state**: Environment variables, working directory, and PATH modifications return new shell instances, making it safe to share and fork configurations.
- **Modern I/O**: Built on `System.IO.Pipelines` for efficient streaming of process output without allocating strings for every line.
- **First-class testing**: `FakeVirtualShell` captures all commands for assertion without executing anything, enabling fast unit tests.
- **Secrets management**: Values can be marked as secrets and are automatically redacted in logs, traces, and error messages.
- **Observability**: Built-in OpenTelemetry-compatible logging, tracing, and metrics.

**Usage in Aspire**

VirtualShell is the standard way to execute external processes throughout Aspire's codebase, replacing direct `Process` usage with a consistent, testable abstraction.

**Mental Model**

Think of `IVirtualShell` like a terminal session. When you open a terminal, it has a working directory, environment variables, and a PATH. Commands you run inherit these settings. VirtualShell works the same way:

```csharp
// Configure the "shell session"
var buildShell = sh
    .Cd("/projects/myapp")
    .Env("NODE_ENV", "production")
    .PrependPath("/opt/tools/bin");

// All commands inherit these settings
await buildShell.RunAsync("npm install");
await buildShell.RunAsync("npm run build");
await buildShell.RunAsync("npm test");
```

The key difference from a real shell: **state is immutable**. Each `Cd()`, `Env()`, or `PrependPath()` call returns a *new* shell instance, leaving the original unchanged. This makes it safe to fork configurations:

```csharp
var baseShell = sh.Env("CI", "true");

// Fork into specialized configurations
var debugShell = baseShell.Env("DEBUG", "1");
var releaseShell = baseShell.Env("CONFIGURATION", "Release");

// Each shell has its own isolated state
await debugShell.RunAsync("dotnet build");
await releaseShell.RunAsync("dotnet build");
```

This immutable design eliminates shared mutable state bugs and makes shell configurations composable and reusable.

## Goals

* **Portable semantics** across Windows/macOS/Linux
* **No shell execution** (never `cmd.exe`, `bash`, etc.)
* **Direct process exec** with canonical parsing + PATH resolution
* **Modern streaming** using `System.IO.Pipelines` primitives
* **DI + mocking** friendly

---

## Types

### IVirtualShell

The primary entry point for creating and executing commands.

```csharp
public interface IVirtualShell
{
    // Fork shell state (immutable)
    IVirtualShell Cd(string workingDirectory);
    IVirtualShell Env(string key, string? value);
    IVirtualShell Env(IReadOnlyDictionary<string, string?> vars);
    IVirtualShell PrependPath(string path);
    IVirtualShell AppendPath(string path);

    // Secrets (values redacted in logs/traces)
    IVirtualShell DefineSecret(string name, string value);
    string Secret(string name);
    IVirtualShell SecretEnv(string key, string value);

    // Diagnostics
    IVirtualShell Tag(string category); // optional diagnostic label (deploy/build/etc.)
    IVirtualShell WithLogging(); // enable structured logging for commands

    // Command builder (fluent API for per-command configuration)
    ICommand Command(string commandLine);
    ICommand Command(string fileName, IReadOnlyList<string> args);

    // Shortcuts for simple cases (delegate to Command().RunAsync())
    Task<ProcessResult> RunAsync(string commandLine, CancellationToken ct = default);
    Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> args, CancellationToken ct = default);

    // Advanced handle shortcuts (stdin writing, signals, ensure success after streaming)
    IRunningProcess Start(string commandLine);
    IRunningProcess Start(string fileName, IReadOnlyList<string> args);
}
```

### ICommand

Fluent builder for per-command configuration. Created via `IVirtualShell.Command()`.

```csharp
public interface ICommand
{
    // Fluent configuration
    ICommand WithStdin(Stdin stdin);  // Write content from source, then close stdin
    ICommand WithStdin();             // Enable interactive stdin via Input pipe
    ICommand WithCaptureOutput(bool capture);

    // Execution methods
    Task<ProcessResult> RunAsync(CancellationToken ct = default);
    IRunningProcess Start();
}
```

### IRunningProcess

Provides advanced control over a running process using `System.IO.Pipelines` primitives.

```csharp
public interface IRunningProcess : IAsyncDisposable
{
    // I/O Primitives
    PipeWriter Input { get; }   // Write to stdin
    PipeReader Output { get; }  // Read from stdout
    PipeReader Error { get; }   // Read from stderr

    // Lifecycle
    Task<ProcessResult> WaitAsync(CancellationToken ct = default);

    // Control
    void Signal(ProcessSignal signal);
    void Kill(bool entireProcessTree = true);
}
```

### ProcessResult

```csharp
public sealed record ProcessResult(
    int ExitCode,
    string? Stdout,   // null if not captured
    string? Stderr,   // null if not captured
    ProcessExitReason Reason = ProcessExitReason.Exited
)
{
    public bool Success => Reason == ProcessExitReason.Exited && ExitCode == 0;
}
```

### ProcessExitReason

```csharp
public enum ProcessExitReason
{
    Exited,
    Killed,
    Signaled
}
```

### ProcessSignal

```csharp
public enum ProcessSignal
{
    Interrupt,   // portable intent; mapped per OS best-effort
    Terminate,
    Kill
}
```

### OutputLine

```csharp
public readonly record struct OutputLine(
    bool IsStdErr,
    string Text
);
```

### Stdin

```csharp
public abstract record Stdin
{
    public static Stdin FromText(string text, Encoding? encoding = null);
    public static Stdin FromBytes(ReadOnlyMemory<byte> bytes);
    public static Stdin FromStream(Stream stream, bool leaveOpen = false);
    public static Stdin FromFile(string path);
    public static Stdin FromWriter(Func<Stream, CancellationToken, Task> writeAsync);
}
```

### RunningProcessExtensions

Convenience extension methods built on the primitives:

```csharp
public static class RunningProcessExtensions
{
    // Output - merges stdout and stderr lines as they arrive
    IAsyncEnumerable<OutputLine> ReadLinesAsync(this IRunningProcess process, CancellationToken ct = default);

    // Input
    Task WriteAsync(this IRunningProcess process, ReadOnlyMemory<byte> data, CancellationToken ct = default);
    Task WriteAsync(this IRunningProcess process, ReadOnlyMemory<char> text, CancellationToken ct = default);
    Task WriteLineAsync(this IRunningProcess process, string line, CancellationToken ct = default);

    // Convenience
    Task EnsureSuccessAsync(this IRunningProcess process, CancellationToken ct = default);
}
```

---

**Defaults**

* `Start(...)`: `CaptureOutput = false` (stream-first, avoid buffering giant output)
* `RunAsync(...)`: `CaptureOutput = true` (so `Stdout/Stderr` are available)

**Design notes**:
- `IVirtualShell.RunAsync()` is a shortcut for `Command().RunAsync()`
- `IVirtualShell.Start()` is a shortcut for `Command().WithCaptureOutput(false).Start()`
- `WithStdin(Stdin source)` writes the content and closes stdin when done
- `WithStdin()` (parameterless) enables interactive stdin via `IRunningProcess.Input`
- For timeouts, use `CancellationTokenSource` with timeout

---

## Execution Models

### RunAsync - Fire and wait

```csharp
var result = await sh.RunAsync("dotnet build");
```

- Starts the process, waits for exit, returns `ProcessResult`
- Process is automatically cleaned up when done
- If cancellation token fires, process is killed and `OperationCanceledException` is thrown

### Start - Manual control

```csharp
await using var process = sh.Start("dotnet build");
// ... use process (stream output, send signals, etc.) ...
var result = await process.WaitAsync();
// process is cleaned up automatically when scope exits
```

- Returns `IRunningProcess` immediately (process is running)
- Caller controls the process lifetime
- Use `await using` to ensure cleanup on scope exit

---

## Process Lifecycle

| Method | Behavior |
|--------|----------|
| `WaitAsync(ct)` | Waits for process to exit. Does **not** kill the process. If cancelled, throws `OperationCanceledException` but process continues running. |
| `DisposeAsync()` | Sends interrupt signal, waits up to 5 seconds for graceful shutdown, then force-kills if still running. Always cleans up. |
| `Kill()` | Immediately kills the process (and optionally its tree). |
| `Signal(signal)` | Sends a signal to the process (interrupt, terminate, kill). |

### Key differences: WaitAsync vs DisposeAsync

| Aspect | `WaitAsync(ct)` | `DisposeAsync()` |
|--------|-----------------|------------------|
| Purpose | Wait for process to exit | Clean up resources |
| Kills process? | **No** | **Yes** (if still running) |
| On cancellation | Throws, process continues | N/A |
| Returns | `ProcessResult` | void |
| Required? | Optional | Yes (for cleanup) |

### The golden rule

> **Always dispose `IRunningProcess`** unless you intentionally want the process to outlive your code.

---

## Signal Handling

The `Signal()` method provides portable intent, but actual behavior varies by platform and .NET version:

| Platform | .NET Version | `Interrupt` | `Terminate` | `Kill` |
|----------|--------------|-------------|-------------|--------|
| Windows  | .NET 10+     | CTRL+C (`GenerateConsoleCtrlEvent`) | CTRL+BREAK | `Process.Kill()` |
| Windows  | .NET 8/9     | `CloseMainWindow()` -> Kill | Kill | `Process.Kill()` |
| Unix     | Any          | SIGINT (2) | SIGTERM (15) | SIGKILL (9) |

**Implementation notes**:

* On Windows with .NET 10+, processes are started with `CreateNewProcessGroup = true`, enabling proper CTRL+C/CTRL+BREAK signals via `GenerateConsoleCtrlEvent`.
* On older .NET versions on Windows, graceful signal support is limited.
* On Unix, signals are sent via the `kill` syscall and work for all process types.

---

## Thread Safety

**I/O Pipes**:
* `Input`, `Output`, and `Error` are standard `System.IO.Pipelines` types
* Multiple consumers can read from `Output`/`Error` concurrently (but coordination is caller's responsibility)
* `Input` writes should be serialized by the caller or use the extension methods

**Input.CompleteAsync()**:
* Signals end of stdin to the process
* After calling, further writes will fail
* Required for processes that wait for EOF on stdin

**Disposed state**:
* All methods throw `ObjectDisposedException` after `DisposeAsync()` is called

---

## Secrets

VirtualShell supports marking values as secrets so they are automatically redacted in logs and traces.

```csharp
// Define named secrets at shell level
var shell = sh
    .DefineSecret("db-password", config["Database:Password"])
    .DefineSecret("api-key", config["Api:Key"]);

// Use Secret() to get the value for use in arguments
await shell.RunAsync("tool", ["--password", shell.Secret("db-password")]);

// SecretEnv sets an env var and marks its value for redaction
var buildShell = sh
    .SecretEnv("NPM_TOKEN", config["Npm:Token"])
    .SecretEnv("DOCKER_PASSWORD", config["Docker:Password"]);
```

**Redaction behavior**:
- Secret values are automatically replaced with `[REDACTED]` in logs, traces, and error messages
- The shell tracks secret values and redacts any argument or env value that matches

---

## Diagnostics

### Logging

Logging is **opt-in** via `WithLogging()`:

```csharp
var sh = shell.WithLogging();
await sh.RunAsync("docker build .");
```

| Event | Level |
|-------|-------|
| Command start | Debug |
| Command success | Information |
| Command failure | Warning |
| Exception | Error |

### Tracing (ActivitySource)

Each command creates an Activity with:

- **Name**: The executable name (e.g., "docker", "dotnet")
- **Tags**: `virtualshell.command`, `virtualshell.working_dir`, `virtualshell.tag`, `virtualshell.exit_code`
- **Status**: `Ok` for success, `Error` for failures

ActivitySource name: `Aspire.VirtualShell`

### Metrics (Meter)

| Metric | Type | Tags |
|--------|------|------|
| `virtualshell.command.duration` | Histogram (ms) | command, tag, exit_code, success |
| `virtualshell.command.count` | Counter | command, tag, exit_code, success |

Meter name: `Aspire.VirtualShell`

### Dependency injection

```csharp
services.AddVirtualShell();
```

---

## Internal Seams

These are part of the virtual shell definition and are mockable, but not required in app-facing API.

### ICommandLineParser

```csharp
public interface ICommandLineParser
{
    (string FileName, IReadOnlyList<string> Args) Parse(string commandLine);
}
```

**Spec**: One canonical grammar across OSes. Reject shell operators (`|`, `>`, `<`, `&&`, `||`, `;`, `$()`, globbing, etc.).

### IExecutableResolver

```csharp
public interface IExecutableResolver
{
    string ResolveOrThrow(string fileName, ShellState state);
}
```

### IProcessRunner

```csharp
internal interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string exePath, IReadOnlyList<string> args, ExecSpec spec, ShellState state, CancellationToken ct);
    IRunningProcess Start(string exePath, IReadOnlyList<string> args, ExecSpec spec, ShellState state);
}
```

---

## Examples

### One-shot execution

```csharp
var result = await sh.RunAsync("dotnet build");
if (!result.Success)
    Console.WriteLine($"Failed: {result.Stderr}");
```

### Capture stdout

```csharp
var result = await sh.RunAsync("dotnet --version");
var version = result.Stdout?.Trim();
```

### Stream output

```csharp
await using var run = sh.Start("docker build -t myimg:dev .");
await foreach (var line in run.ReadLinesAsync(ct))
    Console.WriteLine(line.Text);
await run.EnsureSuccessAsync(ct);
```

### Stdin from text

```csharp
await sh.Command("docker", ["login", "ghcr.io", "--username", user, "--password-stdin"])
    .WithStdin(Stdin.FromText(password + "\n"))
    .RunAsync();
```

### Interactive stdin

```csharp
await using var proc = sh
    .Command("node")
    .WithStdin()
    .Start();

await proc.WriteLineAsync("console.log('Hello');");
await proc.Input.CompleteAsync();

await foreach (var line in proc.ReadLinesAsync())
    Console.WriteLine(line.Text);
```

### Low-level pipe access

```csharp
await using var proc = sh
    .Command("binary-tool")
    .WithStdin()
    .Start();

await proc.Input.WriteAsync(binaryData.AsMemory());
await proc.Input.CompleteAsync();

while (true)
{
    var result = await proc.Output.ReadAsync();
    if (result.IsCompleted) break;
    ProcessBytes(result.Buffer);
    proc.Output.AdvanceTo(result.Buffer.End);
}
```

### Timeout

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
try
{
    await sh.RunAsync("long-task", cts.Token);
}
catch (OperationCanceledException)
{
    // Process was killed due to timeout
}
```

### Graceful shutdown

```csharp
await using var server = sh.Start("web-server");

server.Signal(ProcessSignal.Interrupt);
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await server.WaitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Didn't exit gracefully, will be force-killed by DisposeAsync
}
```

### Environment and PATH

```csharp
var buildShell = sh
    .Cd("/path/to/project")
    .Env("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
    .PrependPath("/opt/mytool/bin");

await buildShell.RunAsync("dotnet build");
```

### Reusable shell configurations

```csharp
IVirtualShell CreateDockerShell(IVirtualShell baseShell)
{
    return baseShell
        .Env("DOCKER_BUILDKIT", "1")
        .Tag("docker")
        .WithLogging();
}

var dockerShell = CreateDockerShell(sh);
await dockerShell.RunAsync("docker build -t myapp .");
```

### Exit reason handling

```csharp
var result = await sh.RunAsync("task", ct);

switch (result.Reason)
{
    case ProcessExitReason.Exited:
        Console.WriteLine($"Completed with code {result.ExitCode}");
        break;
    case ProcessExitReason.Killed:
        Console.WriteLine("Process was killed");
        break;
    case ProcessExitReason.Signaled:
        Console.WriteLine("Process was signaled");
        break;
}
```
