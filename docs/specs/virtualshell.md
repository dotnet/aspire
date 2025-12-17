# Virtual Shell API Surface Spec

> **Note**: This API is experimental and marked with `[Experimental("ASPIREHOSTINGVIRTUALSHELL001")]`.

## Overview

VirtualShell is a cross-platform process execution abstraction for .NET. It solves three fundamental problems with process I/O:

### Problem 1: Deadlocks with Buffering

When a process writes more output than the OS pipe buffer can hold (~64KB), it blocks waiting for someone to read. If your code is waiting for the process to exit before reading, you have a deadlock:

```csharp
// DEADLOCK: Process blocks writing to full stdout buffer,
// we block waiting for exit that never comes
process.WaitForExit();
var output = process.StandardOutput.ReadToEnd();
```

VirtualShell's high-level modes (`RunAsync`, `StartReading`, `Start`) handle this automatically—output is always drained concurrently with process execution. The low-level `StartProcess` mode gives you raw pipes, where you take responsibility for reading.

### Problem 2: Efficiency with Raw I/O

Sometimes you need raw access to process streams without the overhead of string allocations. Traditional APIs force you to read everything as strings, even for binary protocols or when you're just forwarding output elsewhere.

VirtualShell provides direct `System.IO.Pipelines` access when you need it:

```csharp
await using var pipes = sh.Command("binary-tool").StartProcess();
// Zero-copy access to raw bytes
var result = await pipes.Output.ReadAsync();
ProcessBytes(result.Buffer);
pipes.Output.AdvanceTo(result.Buffer.End);
```

### Problem 3: Controlling Buffered vs Streaming Output

Different scenarios need different output handling:
- **Buffered**: Run a command, get stdout/stderr as strings
- **Streaming**: Process lines as they arrive (for logs, progress, etc.)
- **Discarded**: Run for side effects, ignore output entirely
- **Custom**: Route output to your own handler (logger, file, etc.)

VirtualShell provides distinct execution modes for each scenario instead of one-size-fits-all.

---

## Quick Start

### The Simplest Case

Run a command, get the result:

```csharp
var result = await shell.RunAsync("dotnet", ["--version"]);
Console.WriteLine(result.Stdout);  // "9.0.100"
```

### Check Success

```csharp
var result = await shell.RunAsync("dotnet", ["build"]);
if (!result.Success)
{
    Console.Error.WriteLine($"Build failed: {result.Stderr}");
}
```

### Parse a Command Line

```csharp
var result = await shell.RunAsync("dotnet build --configuration Release");
```

### Set Environment and Working Directory

```csharp
var buildShell = shell
    .Cd("/path/to/project")
    .Env("CONFIGURATION", "Release");

await buildShell.RunAsync("dotnet", ["build"]);
```

---

## Execution Modes

VirtualShell provides four execution modes, from simplest to most control:

### Mode 1: Buffered (`RunAsync`)

The most common case—run to completion, get output as strings:

```csharp
var result = await shell.RunAsync("dotnet", ["build"]);
Console.WriteLine(result.Stdout);
Console.WriteLine(result.Stderr);
```

Output is captured by default. For commands with large output you don't need:

```csharp
// Output is drained but not stored (faster, less memory)
var result = await shell.Command("verbose-tool").RunAsync(capture: false);
// result.Stdout is null
```

With stdin:

```csharp
var result = await shell.Command("grep", ["pattern"])
    .RunAsync(stdin: ProcessInput.FromText("search this text"));
```

### Mode 2: Line Streaming (`StartReading`)

Process output lines as they arrive—useful for logs, progress, or long-running processes:

```csharp
await using var lines = shell.Command("docker", ["build", "."]).StartReading();
await foreach (var line in lines.ReadLinesAsync())
{
    if (line.IsStdErr)
        Console.Error.WriteLine($"[ERR] {line.Text}");
    else
        Console.WriteLine(line.Text);
}
var result = await lines.WaitAsync();
```

Stdout and stderr are merged into a single stream with `IsStdErr` to distinguish them.

### Mode 3: Raw Pipes (`StartProcess`)

Full control over stdin/stdout/stderr as `System.IO.Pipelines`:

```csharp
await using var pipes = shell.Command("python", ["-i"]).StartProcess();

// Write to stdin
await pipes.WriteLineAsync("print('hello')");
await pipes.Input.CompleteAsync();  // Signal EOF

// Read raw bytes from stdout
while (true)
{
    var result = await pipes.Output.ReadAsync();
    if (result.IsCompleted) break;
    ProcessBytes(result.Buffer);
    pipes.Output.AdvanceTo(result.Buffer.End);
}
```

**⚠️ Deadlock warning**: Unlike the other modes, `StartProcess` does **not** drain pipes automatically. You must read from `Output` and `Error` to prevent the process from blocking when its output buffer fills. This mode trades safety for control—use it when you need raw byte access or interactive I/O.

### Mode 4: Custom Output (`Start`)

Plug in your own output handlers:

```csharp
await using var proc = shell.Command("build-tool")
    .Start(stdout: new LoggerProcessOutput(logger, LogLevel.Info),
           stderr: new LoggerProcessOutput(logger, LogLevel.Error));
await proc.WaitAsync();
```

---

## The Command Builder

For modes beyond simple `RunAsync`, use the `Command()` builder:

```csharp
// Create a command (doesn't execute yet)
var cmd = shell.Command("dotnet", ["build"]);

// Execute it different ways:
await cmd.RunAsync();                    // Mode 1: Buffered
await using var lines = cmd.StartReading();  // Mode 2: Line streaming
await using var pipes = cmd.StartProcess();  // Mode 3: Raw pipes
await using var proc = cmd.Start(...);       // Mode 4: Custom output
```

Command line parsing is also available:

```csharp
var cmd = shell.Command("docker build -t myapp:latest .");
```

---

## Shell Configuration

Shell state is **immutable**. Each configuration method returns a new shell:

```csharp
var baseShell = shell.Env("CI", "true");

// Fork into different configurations
var debugShell = baseShell.Env("DEBUG", "1");
var releaseShell = baseShell.Env("CONFIGURATION", "Release");

// Original unchanged, each fork has its own state
await debugShell.RunAsync("dotnet", ["build"]);
await releaseShell.RunAsync("dotnet", ["build"]);
```

### Available Configuration

```csharp
shell
    .Cd("/working/directory")           // Set working directory
    .Env("KEY", "value")                // Set environment variable
    .Env("KEY", null)                   // Remove environment variable
    .PrependPath("/custom/bin")         // Add to front of PATH
    .AppendPath("/custom/bin")          // Add to end of PATH
    .DefineSecret("name", "value")      // Define named secret (redacted in logs)
    .SecretEnv("KEY", "secret")         // Set env var marked as secret
    .Tag("build")                       // Diagnostic label for traces/metrics
    .WithLogging();                     // Enable structured logging
```

---

## Process Lifecycle

### Waiting vs Disposing

| Method | Kills Process? | Purpose |
|--------|----------------|---------|
| `WaitAsync(ct)` | No | Wait for natural exit |
| `DisposeAsync()` | Yes (if running) | Clean up resources |

```csharp
await using var lines = shell.Command("server").StartReading();

// WaitAsync does NOT kill - just waits
await lines.WaitAsync();  // Waits forever if server runs forever

// DisposeAsync DOES kill (graceful then force after 5s)
// Called automatically by await using
```

### Signals

```csharp
await using var server = shell.Command("web-server").StartReading();

// Request graceful shutdown
server.Signal(ProcessSignal.Interrupt);

// Wait with timeout
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await server.WaitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Didn't exit gracefully, DisposeAsync will force-kill
}
```

### Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
var result = await shell.RunAsync("long-task", ct: cts.Token);
// Process is killed if timeout fires
```

---

## Input and Output Types

### ProcessInput

Sources for stdin:

```csharp
ProcessInput.Null                           // No input (default)
ProcessInput.FromText("hello\n")            // String
ProcessInput.FromBytes(data)                // ReadOnlyMemory<byte>
ProcessInput.FromStream(stream)             // Stream
ProcessInput.FromFile("/path/to/file")      // File
ProcessInput.FromWriter(async (s, ct) => {  // Custom async writer
    await s.WriteAsync(data, ct);
})
```

### ProcessOutput

Destinations for stdout/stderr (Mode 4):

```csharp
ProcessOutput.Null      // Drain and discard
ProcessOutput.Capture   // Capture to ProcessResult.Stdout/Stderr
// Or create custom handlers by subclassing ProcessOutput
```

---

## Secrets

Values marked as secrets are automatically redacted in logs and traces:

```csharp
var shell = baseShell
    .DefineSecret("db-password", config["Database:Password"])
    .SecretEnv("API_KEY", config["Api:Key"]);

// Secret values appear as [REDACTED] in logs
await shell.Command("tool", ["--password", shell.Secret("db-password")]).RunAsync();
```

---

## Testing with FakeVirtualShell

`FakeVirtualShell` captures commands without executing them:

```csharp
var shell = new FakeVirtualShell()
    .WithResponse("dotnet", new ProcessResult(0, "Build succeeded", "", ProcessExitReason.Exited));

await shell.RunAsync("dotnet", ["build"]);

var cmd = Assert.Single(shell.ExecutedCommands);
Assert.Equal("dotnet", cmd.FileName);
Assert.Equal(["build"], cmd.Arguments);
```

---

## Types Reference

### IVirtualShell

```csharp
public interface IVirtualShell
{
    // Configuration (returns new shell)
    IVirtualShell Cd(string workingDirectory);
    IVirtualShell Env(string key, string? value);
    IVirtualShell Env(IReadOnlyDictionary<string, string?> vars);
    IVirtualShell PrependPath(string path);
    IVirtualShell AppendPath(string path);
    IVirtualShell DefineSecret(string name, string value);
    IVirtualShell SecretEnv(string key, string value);
    IVirtualShell Tag(string category);
    IVirtualShell WithLogging();
    string Secret(string name);

    // Command creation
    ICommand Command(string commandLine);
    ICommand Command(string fileName, IReadOnlyList<string>? args);
}
```

### VirtualShellExtensions

```csharp
public static class VirtualShellExtensions
{
    // One-shot execution shortcuts
    public static Task<ProcessResult> RunAsync(this IVirtualShell shell,
        string commandLine, CancellationToken ct = default);
    public static Task<ProcessResult> RunAsync(this IVirtualShell shell,
        string fileName, IReadOnlyList<string>? args, CancellationToken ct = default);
}
```

### ICommand

```csharp
public interface ICommand
{
    string FileName { get; }
    IReadOnlyList<string> Arguments { get; }

    Task<ProcessResult> RunAsync(ProcessInput? stdin = null,
        bool capture = true, CancellationToken ct = default);
    IProcessLines StartReading(ProcessInput? stdin = null);
    IProcessPipes StartProcess();
    IProcessHandle Start(ProcessInput? stdin = null,
        ProcessOutput? stdout = null, ProcessOutput? stderr = null);
}
```

### Process Interfaces

```csharp
// Base interface
public interface IProcessHandle : IAsyncDisposable
{
    Task<ProcessResult> WaitAsync(CancellationToken ct = default);
    void Signal(ProcessSignal signal);
    void Kill(bool entireProcessTree = true);
}

// Line streaming (Mode 2)
public interface IProcessLines : IProcessHandle
{
    IAsyncEnumerable<OutputLine> ReadLinesAsync(CancellationToken ct = default);
}

// Raw pipes (Mode 3)
public interface IProcessPipes : IProcessHandle
{
    PipeWriter Input { get; }
    PipeReader Output { get; }
    PipeReader Error { get; }
}
```

### ProcessResult

```csharp
public sealed record ProcessResult(
    int ExitCode,
    string? Stdout,
    string? Stderr,
    ProcessExitReason Reason = ProcessExitReason.Exited)
{
    public bool Success => Reason == ProcessExitReason.Exited && ExitCode == 0;
}
```

### Enums

```csharp
public enum ProcessExitReason { Exited, Killed, Signaled }
public enum ProcessSignal { Interrupt, Terminate, Kill }
```

### OutputLine

```csharp
public readonly record struct OutputLine(bool IsStdErr, string Text);
```

---

## Diagnostics

### Logging (opt-in)

```csharp
var shell = baseShell.WithLogging();
```

| Event | Level |
|-------|-------|
| Command start | Debug |
| Command success | Information |
| Command failure | Warning |

### Tracing

ActivitySource: `Aspire.VirtualShell`

Tags: `virtualshell.command`, `virtualshell.working_dir`, `virtualshell.tag`, `virtualshell.exit_code`

### Metrics

Meter: `Aspire.VirtualShell`

| Metric | Type |
|--------|------|
| `virtualshell.command.duration` | Histogram (ms) |
| `virtualshell.command.count` | Counter |

---

## Dependency Injection

```csharp
services.AddVirtualShell();
```

Registers `IVirtualShell` as a singleton with logging, tracing, and metrics wired up.
