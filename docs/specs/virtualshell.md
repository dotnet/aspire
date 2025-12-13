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

### ExecSpec

Used to override behavior per call (and as defaults stored on the shell).

```csharp
public sealed class ExecSpec
{
    // State
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string?> Environment { get; } = new(StringComparer.Ordinal);

    // Control
    public TimeSpan? Timeout { get; set; }
    public bool KillProcessTree { get; set; } = true;

    // Output policy
    public bool CaptureOutput { get; set; } = true;   // default for Run/Cap
    public int? MaxCaptureBytes { get; set; }         // optional guardrail

    // I/O
    public Stdin? Stdin { get; set; }
    public StdoutTarget? Stdout { get; set; }         // optional override
    public StderrTarget? Stderr { get; set; }         // optional override
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

### Output targets (minimal)

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
    IVirtualShell Timeout(TimeSpan timeout);
    IVirtualShell Tag(string category); // optional diagnostic label (deploy/build/etc.)

    // One-shot execution (portable parse + PATH resolve + direct exec)
    Task<CliResult> Run(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    Task<CliResult> Run(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    // Capture stdout (shell-style $(...))
    Task<string> Cap(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    Task<string> Cap(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    // Efficient streaming (no capture by default)
    IAsyncEnumerable<OutputLine> Lines(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    IAsyncEnumerable<OutputLine> Lines(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    // Advanced handle (stdin writing, signals, ensure success after streaming)
    StreamRun Stream(
        string commandLine,
        Action<ExecSpec>? perCall = null);

    StreamRun Stream(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null);
}
```

### StreamRun

```csharp
public sealed class StreamRun : IAsyncDisposable
{
    public IAsyncEnumerable<OutputLine> Lines(CancellationToken ct = default);

    public Task<int> ExitCodeAsync(CancellationToken ct = default);
    public Task<CliResult> ResultAsync(CancellationToken ct = default); // optional
    public Task EnsureSuccessAsync(CancellationToken ct = default);

    // stdin (interactive)
    public Task WriteAsync(ReadOnlyMemory<char> text, CancellationToken ct = default);
    public Task WriteLineAsync(string line, CancellationToken ct = default);
    public Task CompleteStdinAsync(CancellationToken ct = default);

    // control
    public void Signal(CliSignal signal);
    public void Kill(bool entireProcessTree = true);

    public ValueTask DisposeAsync();
}

public enum CliSignal
{
    Interrupt,   // portable intent; mapped per OS best-effort
    Terminate,
    Kill
}
```

**Streaming defaults**

* `Lines(...)`: `CaptureOutput = false` unless overridden (avoid buffering giant output)
* `Stream(...)`: same default as `Lines(...)` (stream-first)

**Run/Cap defaults**

* `Run(...)` defaults to `CaptureOutput = true` (so `Stdout/Stderr` are available)
* `Cap(...)` implies capture of stdout and returns `Stdout.TrimEnd()` by default

---

## Custom Interpolated String Handler Surface

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
    Task<CliResult> Run(
        ShCommand command,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    Task<string> Cap(
        ShCommand command,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    IAsyncEnumerable<OutputLine> Lines(
        ShCommand command,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    StreamRun Stream(
        ShCommand command,
        Action<ExecSpec>? perCall = null);
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

### IProcessRunner (direct exec)

```csharp
public interface IProcessRunner
{
    Task<CliResult> RunAsync(string exePath, IReadOnlyList<string> args, ExecSpec spec, CancellationToken ct);
    StreamRun Start(string exePath, IReadOnlyList<string> args, ExecSpec spec);
}
```

---

## Quick Usage Examples (as spec guidance)

### One-shot

```csharp
await sh.Run("docker build -t myimg:dev .");
```

### Capture stdout

```csharp
var version = await sh.Cap("dotnet --version");
```

### Stream docker build

```csharp
await foreach (var line in sh.Lines("docker build -t myimg:dev .", ct: ct))
    Console.WriteLine(line.Text);
```

### Docker login with stdin

```csharp
await sh.Run("docker", ["login", "ghcr.io", "--username", user, "--password-stdin"],
    spec => spec.Stdin = Stdin.FromText(password + "\n"));
```

### Same with handler sugar (target form)

```csharp
await sh.Run($"docker login ghcr.io --username {user} --password-stdin < {Secret(password)}");
```
