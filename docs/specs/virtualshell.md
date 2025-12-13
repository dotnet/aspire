# Virtual Shell API Surface Spec

## Goals

* **Portable semantics** across Windows/macOS/Linux
* **No shell execution** (never `cmd.exe`, `bash`, etc.)
* **Direct process exec** with canonical parsing + PATH resolution
* **Modern streaming** (`IAsyncEnumerable`) and efficient defaults
* **DI + mocking** friendly

---

## Core Types

### OutputLine

```csharp
public readonly record struct OutputLine(
    bool IsStdErr,
    string Text
);
```

### CliResult

```csharp
public sealed record CliResult(
    int ExitCode,
    string? Stdout,   // null if not captured
    string? Stderr,   // null if not captured
    CliExitReason Reason = CliExitReason.Exited
)
{
    public bool Success => Reason == CliExitReason.Exited && ExitCode == 0;
}
```

```csharp
public enum CliExitReason
{
    Exited,
    Canceled,
    TimedOut,
    Killed,
    Signaled
}
```

### CancellationMode

```csharp
public enum CancellationMode
{
    /// <summary>Kill the process and all child processes (default).</summary>
    KillTree,
    /// <summary>Kill only the process, children become orphaned.</summary>
    KillProcess,
    /// <summary>Stop waiting, process continues running in background.</summary>
    Detach
}
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

### Output targets (Future)

> **Note**: Output targets are not yet implemented. Currently, output is captured via `CliResult.Stdout/Stderr` or streamed via `IRunningProcess.Lines()`.

```csharp
public abstract record StdoutTarget
{
    public static StdoutTarget Capture();                        // capture into result.Stdout
    public static StdoutTarget LineWriter(Action<string> write); // stream lines
    public static StdoutTarget Tee(StdoutTarget a, StdoutTarget b);
    public static StdoutTarget Stream(Stream stream, bool leaveOpen = false);
}

public abstract record StderrTarget
{
    public static StderrTarget Capture();
    public static StderrTarget LineWriter(Action<string> write);
    public static StderrTarget Tee(StderrTarget a, StderrTarget b);
    public static StderrTarget Stream(Stream stream, bool leaveOpen = false);
}
```

---

## The Virtual Shell Interface

### IVirtualShell

```csharp
public interface IVirtualShell
{
    // Fork shell state (immutable)
    IVirtualShell Cd(string workingDirectory);
    IVirtualShell Env(string key, string? value);
    IVirtualShell Env(IReadOnlyDictionary<string, string?> vars);
    IVirtualShell PrependPath(string path);
    IVirtualShell AppendPath(string path);
    IVirtualShell Timeout(TimeSpan timeout);
    IVirtualShell Tag(string category); // optional diagnostic label (deploy/build/etc.)

    // Command builder (fluent API for per-command configuration)
    ICommand Command(string commandLine);
    ICommand Command(string fileName, IReadOnlyList<string> args);

    // Shortcuts for simple cases (delegate to Command().RunAsync())
    Task<CliResult> Run(string commandLine, CancellationToken ct = default);
    Task<CliResult> Run(string fileName, IReadOnlyList<string> args, CancellationToken ct = default);

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
    ICommand WithStdin(Stdin stdin);
    ICommand WithTimeout(TimeSpan timeout);
    ICommand WithCaptureOutput(bool capture);
    ICommand WithMaxCaptureBytes(int maxBytes);
    ICommand WithCancellationMode(CancellationMode mode);

    // Execution methods
    Task<CliResult> RunAsync(CancellationToken ct = default);
    IRunningProcess Start();
}
```

**Design notes**:
- `IVirtualShell.Run()` is a shortcut for `Command().RunAsync()`
- `IVirtualShell.Start()` is a shortcut for `Command().WithCaptureOutput(false).Start()`
- For advanced configuration (stdin, timeout, cancellation mode), use `Command()` explicitly

### IRunningProcess

```csharp
public interface IRunningProcess : IAsyncDisposable
{
    IAsyncEnumerable<OutputLine> Lines(CancellationToken ct = default);

    Task<int> ExitCodeAsync(CancellationToken ct = default);
    Task<CliResult> ResultAsync(CancellationToken ct = default);
    Task EnsureSuccessAsync(CancellationToken ct = default);

    // stdin (interactive)
    Task WriteAsync(ReadOnlyMemory<char> text, CancellationToken ct = default);
    Task WriteLineAsync(string line, CancellationToken ct = default);
    Task CompleteStdinAsync(CancellationToken ct = default);

    // control
    void Signal(CliSignal signal);
    void Kill(bool entireProcessTree = true);
}

public enum CliSignal
{
    Interrupt,   // portable intent; mapped per OS best-effort
    Terminate,
    Kill
}
```

**Defaults**

* `Start(...)`: `CaptureOutput = false` (stream-first, avoid buffering giant output)
* `Run(...)`: `CaptureOutput = true` (so `Stdout/Stderr` are available)

---

## Custom Interpolated String Handler Surface (Future)

> **Note**: This section describes planned features that are not yet implemented.

Goal: eliminate quoting bugs and reduce verbosity, especially for stdin/secrets.

### Handler-built command type

```csharp
public sealed record ShCommand(
    string FileName,
    IReadOnlyList<string> Args,
    Stdin? Stdin = null,
    IReadOnlyDictionary<string, string?>? Env = null,
    string? Display = null // redacted form for logs/diagnostics
);
```

### Secret wrapper

```csharp
public readonly record struct SecretText(string Value);
public static SecretText Secret(string value) => new(value);
```

### IVirtualShell overloads using the handler

```csharp
public interface IVirtualShell
{
    ICommand Command(ShCommand command);

    // Shortcuts
    Task<CliResult> Run(ShCommand command, CancellationToken ct = default);
    IAsyncEnumerable<OutputLine> Lines(ShCommand command, CancellationToken ct = default);
    IStreamRun Stream(ShCommand command);
}
```

### ShCommandHandler (spec-level behavior)

* Tokenizes literals on whitespace.
* `{value}` inserts **one argument** (no quoting).
* Supports **array expansion**: `{Args(list)}` → multiple args.
* Supports **stdin directive**: `< {Secret(password)}` or `< {Text("...")}` → sets `ShCommand.Stdin`.
* Produces a **redacted Display** string when secrets are used.

(Exact syntax/glyphs for stdin directives are part of the handler grammar; keep it intentionally small.)

---

## Parsing and Executable Resolution (internal seams)

These are part of the virtual shell definition and are mockable, but not required in app-facing API.

### ICommandLineParser (canonical, portable)

```csharp
public interface ICommandLineParser
{
    (string FileName, IReadOnlyList<string> Args) Parse(string commandLine);
}
```

**Spec**: One canonical grammar across OSes (quotes/escapes consistent). Reject shell operators in strings (`|`, `>`, `<`, `&&`, `||`, `;`, `$()`, globbing, etc.) unless explicitly supported as *typed* APIs.

### IExecutableResolver (PATH/PATHEXT)

```csharp
public interface IExecutableResolver
{
    string ResolveOrThrow(string fileName, ShellState state);
}
```

```csharp
public sealed record ShellState(
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string?> Environment
);
```

### IProcessRunner (direct exec, internal)

```csharp
internal interface IProcessRunner
{
    Task<CliResult> RunAsync(string exePath, IReadOnlyList<string> args, ExecSpec spec, ShellState state, CancellationToken ct);
    IRunningProcess Start(string exePath, IReadOnlyList<string> args, ExecSpec spec, ShellState state);
}
```

Note: `ExecSpec` is an internal class used by infrastructure; the public API is the fluent `ICommand` interface.

---

## Quick Usage Examples (as spec guidance)

### One-shot (simple)

```csharp
await sh.Run("docker build -t myimg:dev .");
```

### One-shot with timeout

```csharp
await sh.Command("docker build -t myimg:dev .")
    .WithTimeout(TimeSpan.FromMinutes(10))
    .RunAsync(ct);
```

### Capture stdout

```csharp
var result = await sh.Run("dotnet --version");
var version = result.Stdout?.Trim();
```

### Stream docker build

```csharp
await using var run = sh.Start("docker build -t myimg:dev .");
await foreach (var line in run.Lines(ct))
    Console.WriteLine(line.Text);
await run.EnsureSuccessAsync(ct);
```

### Docker login with stdin (fluent)

```csharp
await sh.Command("docker", ["login", "ghcr.io", "--username", user, "--password-stdin"])
    .WithStdin(Stdin.FromText(password + "\n"))
    .RunAsync();
```

### Same with handler sugar (target form)

```csharp
await sh.Run($"docker login ghcr.io --username {user} --password-stdin < {Secret(password)}");
```

### Advanced: detach on cancel

```csharp
await sh.Command("long-running-server")
    .WithCancellationMode(CancellationMode.Detach)
    .RunAsync(ct);
```
