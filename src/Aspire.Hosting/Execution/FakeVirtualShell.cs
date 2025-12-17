// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Execution;

/// <summary>
/// A fake implementation of <see cref="IVirtualShell"/> for testing purposes.
/// Captures all commands executed and allows configuring responses.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeVirtualShell : IVirtualShell
{
    internal readonly ConcurrentQueue<CapturedCommand> _executedCommands = new();
    internal readonly ConcurrentDictionary<string, ProcessResult> _responses = new(StringComparer.OrdinalIgnoreCase);
    internal readonly ConcurrentDictionary<string, Func<CapturedCommand, ProcessResult>> _responseHandlers = new(StringComparer.OrdinalIgnoreCase);
    internal ProcessResult _defaultResult = new(0, "", "", ProcessExitReason.Exited);

    internal string? _workingDirectory;
    internal readonly Dictionary<string, string?> _environment = new(StringComparer.OrdinalIgnoreCase);
    internal readonly Dictionary<string, string> _secrets = new(StringComparer.Ordinal);
    internal readonly HashSet<string> _secretEnvKeys = new(StringComparer.OrdinalIgnoreCase);
    internal string? _tag;

    /// <summary>
    /// Gets all commands that have been executed.
    /// </summary>
    public IReadOnlyList<CapturedCommand> ExecutedCommands => _executedCommands.ToArray();

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    public string? WorkingDirectory => _workingDirectory;

    /// <summary>
    /// Gets the current environment variables.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Environment => _environment;

    /// <summary>
    /// Gets the current tag.
    /// </summary>
    public string? CurrentTag => _tag;

    /// <summary>
    /// Gets the current shell state as a JSON object for test assertions.
    /// </summary>
    /// <returns>A JSON object containing the shell state.</returns>
    public JsonObject GetStateAsJson()
    {
        var json = new JsonObject();

        if (_workingDirectory is not null)
        {
            json["workingDirectory"] = _workingDirectory;
        }

        if (_environment.Count > 0)
        {
            var envObj = new JsonObject();
            foreach (var kvp in _environment.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                envObj[kvp.Key] = kvp.Value;
            }
            json["environment"] = envObj;
        }

        if (_tag is not null)
        {
            json["tag"] = _tag;
        }

        return json;
    }

    /// <summary>
    /// Creates a new instance of <see cref="FakeVirtualShell"/>.
    /// </summary>
    public FakeVirtualShell()
    {
    }

    private FakeVirtualShell(FakeVirtualShell parent)
    {
        _executedCommands = parent._executedCommands;
        _responses = parent._responses;
        _responseHandlers = parent._responseHandlers;
        _defaultResult = parent._defaultResult;
        _workingDirectory = parent._workingDirectory;
        foreach (var kvp in parent._environment)
        {
            _environment[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in parent._secrets)
        {
            _secrets[kvp.Key] = kvp.Value;
        }
        foreach (var key in parent._secretEnvKeys)
        {
            _secretEnvKeys.Add(key);
        }
        _tag = parent._tag;
    }

    /// <summary>
    /// Sets the default result for commands that don't have a specific response configured.
    /// </summary>
    /// <param name="result">The default result.</param>
    /// <returns>This instance for chaining.</returns>
    public FakeVirtualShell WithDefaultResult(ProcessResult result)
    {
        _defaultResult = result;
        return this;
    }

    /// <summary>
    /// Configures a response for a specific command.
    /// </summary>
    /// <param name="command">The command (executable name) to match.</param>
    /// <param name="result">The result to return.</param>
    /// <returns>This instance for chaining.</returns>
    public FakeVirtualShell WithResponse(string command, ProcessResult result)
    {
        _responses[command] = result;
        return this;
    }

    /// <summary>
    /// Configures a response handler for a specific command.
    /// </summary>
    /// <param name="command">The command (executable name) to match.</param>
    /// <param name="handler">A function that receives the captured command and returns a result.</param>
    /// <returns>This instance for chaining.</returns>
    public FakeVirtualShell WithResponseHandler(string command, Func<CapturedCommand, ProcessResult> handler)
    {
        _responseHandlers[command] = handler;
        return this;
    }

    /// <summary>
    /// Clears all captured commands.
    /// </summary>
    public void ClearCommands()
    {
        while (_executedCommands.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public IVirtualShell Cd(string workingDirectory)
    {
        var clone = new FakeVirtualShell(this)
        {
            _workingDirectory = workingDirectory
        };
        return clone;
    }

    /// <inheritdoc />
    public IVirtualShell Env(string key, string? value)
    {
        var clone = new FakeVirtualShell(this);
        if (value is null)
        {
            clone._environment.Remove(key);
        }
        else
        {
            clone._environment[key] = value;
        }
        return clone;
    }

    /// <inheritdoc />
    public IVirtualShell Env(IReadOnlyDictionary<string, string?> vars)
    {
        var clone = new FakeVirtualShell(this);
        foreach (var kvp in vars)
        {
            if (kvp.Value is null)
            {
                clone._environment.Remove(kvp.Key);
            }
            else
            {
                clone._environment[kvp.Key] = kvp.Value;
            }
        }
        return clone;
    }

    /// <inheritdoc />
    public IVirtualShell PrependPath(string path)
    {
        var currentPath = GetCurrentPath();
        var newPath = string.IsNullOrEmpty(currentPath)
            ? path
            : $"{path}{Path.PathSeparator}{currentPath}";
        return Env("PATH", newPath);
    }

    /// <inheritdoc />
    public IVirtualShell AppendPath(string path)
    {
        var currentPath = GetCurrentPath();
        var newPath = string.IsNullOrEmpty(currentPath)
            ? path
            : $"{currentPath}{Path.PathSeparator}{path}";
        return Env("PATH", newPath);
    }

    private string? GetCurrentPath()
    {
        return _environment.TryGetValue("PATH", out var statePath)
            ? statePath
            : "{SYSTEMPATH}";
    }

    /// <inheritdoc />
    public IVirtualShell DefineSecret(string name, string value)
    {
        var clone = new FakeVirtualShell(this);
        clone._secrets[name] = value;
        return clone;
    }

    /// <inheritdoc />
    public string Secret(string name)
    {
        if (!_secrets.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"Secret '{name}' is not defined. Use DefineSecret() first.");
        }
        return value;
    }

    /// <inheritdoc />
    public IVirtualShell SecretEnv(string key, string value)
    {
        var clone = new FakeVirtualShell(this);
        clone._environment[key] = value;
        clone._secretEnvKeys.Add(key);
        return clone;
    }

    /// <inheritdoc />
    public IVirtualShell Tag(string category)
    {
        var clone = new FakeVirtualShell(this)
        {
            _tag = category
        };
        return clone;
    }

    /// <inheritdoc />
    public IVirtualShell WithLogging()
    {
        // Fake shell doesn't log, but maintain immutable pattern
        return new FakeVirtualShell(this);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Command creation
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public ICommand Command(string commandLine)
    {
        // Simple parsing: split on first space
        var parts = commandLine.Split(' ', 2);
        var fileName = parts[0];
        var args = parts.Length > 1 ? ParseArgs(parts[1]) : Array.Empty<string>();
        return new FakeCommand(this, fileName, args);
    }

    /// <inheritdoc />
    public ICommand Command(string fileName, IReadOnlyList<string>? args)
    {
        return new FakeCommand(this, fileName, args ?? Array.Empty<string>());
    }

    private static string[] ParseArgs(string argString)
    {
        // Simple space-splitting for fake - real implementation uses proper parser
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in argString)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result.ToArray();
    }

    internal CapturedCommand CreateCapturedCommand(string fileName, IReadOnlyList<string> args, ProcessInput? stdin, ProcessOutput stdout, ProcessOutput stderr)
    {
        return new CapturedCommand(
            fileName,
            args.ToArray(),
            _workingDirectory,
            new Dictionary<string, string?>(_environment),
            stdin ?? ProcessInput.Null,
            stdout,
            stderr);
    }

    internal ProcessResult GetResult(CapturedCommand command)
    {
        // Check for response handler first
        if (_responseHandlers.TryGetValue(command.FileName, out var handler))
        {
            return handler(command);
        }

        // Check for static response
        if (_responses.TryGetValue(command.FileName, out var result))
        {
            return result;
        }

        return _defaultResult;
    }
}

/// <summary>
/// A fake implementation of <see cref="ICommand"/> for testing purposes.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeCommand : ICommand
{
    private readonly FakeVirtualShell _shell;

    internal FakeCommand(FakeVirtualShell shell, string fileName, IReadOnlyList<string> args)
    {
        _shell = shell;
        FileName = fileName;
        Arguments = args;
    }

    /// <inheritdoc />
    public string FileName { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Arguments { get; }

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(ProcessInput? stdin = null, bool capture = true, CancellationToken ct = default)
    {
        var captured = _shell.CreateCapturedCommand(FileName, Arguments, stdin, capture ? ProcessOutput.Capture : ProcessOutput.Null, capture ? ProcessOutput.Capture : ProcessOutput.Null);
        _shell._executedCommands.Enqueue(captured);
        var result = _shell.GetResult(captured);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public IProcessLines StartReading(ProcessInput? stdin = null)
    {
        var captured = _shell.CreateCapturedCommand(FileName, Arguments, stdin, ProcessOutput.Null, ProcessOutput.Null);
        _shell._executedCommands.Enqueue(captured);
        var result = _shell.GetResult(captured);
        return new FakeProcessLines(result);
    }

    /// <inheritdoc />
    public IProcessPipes StartProcess()
    {
        var captured = _shell.CreateCapturedCommand(FileName, Arguments, null, ProcessOutput.Null, ProcessOutput.Null);
        _shell._executedCommands.Enqueue(captured);
        var result = _shell.GetResult(captured);
        return new FakeProcessPipes(result);
    }

    /// <inheritdoc />
    public IProcessHandle Start(ProcessInput? stdin = null, ProcessOutput? stdout = null, ProcessOutput? stderr = null)
    {
        var captured = _shell.CreateCapturedCommand(FileName, Arguments, stdin, stdout ?? ProcessOutput.Null, stderr ?? ProcessOutput.Null);
        _shell._executedCommands.Enqueue(captured);
        var result = _shell.GetResult(captured);
        return new FakeProcess(result);
    }
}

/// <summary>
/// Represents a command that was captured by <see cref="FakeVirtualShell"/>.
/// </summary>
/// <param name="FileName">The executable name or path.</param>
/// <param name="Arguments">The arguments passed to the command.</param>
/// <param name="WorkingDirectory">The working directory at time of execution.</param>
/// <param name="Environment">The environment variables at time of execution.</param>
/// <param name="Stdin">The stdin source.</param>
/// <param name="Stdout">The stdout destination.</param>
/// <param name="Stderr">The stderr destination.</param>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed record CapturedCommand(
    string FileName,
    string[] Arguments,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string?> Environment,
    ProcessInput Stdin,
    ProcessOutput Stdout,
    ProcessOutput Stderr)
{
    /// <summary>
    /// Gets the command line as a single string.
    /// </summary>
    public string CommandLine => Arguments.Length == 0 ? FileName : $"{FileName} {string.Join(" ", Arguments)}";
}

/// <summary>
/// A fake implementation of <see cref="IProcessHandle"/> for testing purposes.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeProcess : IProcessHandle
{
    private readonly ProcessResult _result;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="FakeProcess"/> with the specified result.
    /// </summary>
    /// <param name="result">The result to return when the process completes.</param>
    public FakeProcess(ProcessResult result)
    {
        _result = result;
    }

    /// <inheritdoc />
    public Task<ProcessResult> WaitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(_result);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Signal(ProcessSignal signal)
    {
        ThrowIfDisposed();
        // No-op for fake
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        ThrowIfDisposed();
        // No-op for fake
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A fake implementation of <see cref="IProcessLines"/> for testing purposes.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeProcessLines : IProcessLines
{
    private readonly ProcessResult _result;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="FakeProcessLines"/> with the specified result.
    /// </summary>
    /// <param name="result">The result to return when the process completes.</param>
    public FakeProcessLines(ProcessResult result)
    {
        _result = result;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OutputLine> ReadLinesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();

        // Parse stdout lines
        if (!string.IsNullOrEmpty(_result.Stdout))
        {
            foreach (var line in _result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return new OutputLine(false, line.TrimEnd('\r'));
            }
        }

        // Parse stderr lines
        if (!string.IsNullOrEmpty(_result.Stderr))
        {
            foreach (var line in _result.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return new OutputLine(true, line.TrimEnd('\r'));
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ProcessResult> WaitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(_result);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Signal(ProcessSignal signal)
    {
        ThrowIfDisposed();
        // No-op for fake
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        ThrowIfDisposed();
        // No-op for fake
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A fake implementation of <see cref="IProcessPipes"/> for testing purposes.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeProcessPipes : IProcessPipes
{
    private readonly ProcessResult _result;
    private readonly Pipe _inputPipe;
    private readonly Pipe _outputPipe;
    private readonly Pipe _errorPipe;
    private readonly TaskCompletionSource<ProcessResult> _resultTcs;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="FakeProcessPipes"/> with the specified result.
    /// </summary>
    /// <param name="result">The result to return when the process completes.</param>
    public FakeProcessPipes(ProcessResult result)
    {
        _result = result;
        _inputPipe = new Pipe();
        _outputPipe = new Pipe();
        _errorPipe = new Pipe();
        _resultTcs = new TaskCompletionSource<ProcessResult>();

        // Write the output to pipes asynchronously
        _ = Task.Run(async () =>
        {
            if (!string.IsNullOrEmpty(result.Stdout))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(result.Stdout);
                await _outputPipe.Writer.WriteAsync(bytes).ConfigureAwait(false);
            }
            await _outputPipe.Writer.CompleteAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(result.Stderr))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(result.Stderr);
                await _errorPipe.Writer.WriteAsync(bytes).ConfigureAwait(false);
            }
            await _errorPipe.Writer.CompleteAsync().ConfigureAwait(false);

            _resultTcs.SetResult(result);
        });
    }

    /// <inheritdoc />
    public PipeWriter Input => _inputPipe.Writer;

    /// <inheritdoc />
    public PipeReader Output => _outputPipe.Reader;

    /// <inheritdoc />
    public PipeReader Error => _errorPipe.Reader;

    /// <inheritdoc />
    public Task<ProcessResult> WaitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return _resultTcs.Task.WaitAsync(ct);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Signal(ProcessSignal signal)
    {
        ThrowIfDisposed();
        // No-op for fake
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        ThrowIfDisposed();
        // No-op for fake
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _inputPipe.Writer.CompleteAsync().ConfigureAwait(false);
        await _outputPipe.Reader.CompleteAsync().ConfigureAwait(false);
        await _errorPipe.Reader.CompleteAsync().ConfigureAwait(false);
        _resultTcs.TrySetResult(_result);
    }
}
