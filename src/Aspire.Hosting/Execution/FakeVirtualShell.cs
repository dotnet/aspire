// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
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
    internal readonly ConcurrentQueue<FakeCommand> _createdCommands = new();
    internal readonly ConcurrentDictionary<string, ProcessResult> _responses = new(StringComparer.OrdinalIgnoreCase);
    internal readonly ConcurrentDictionary<string, Func<CapturedCommand, ProcessResult>> _responseHandlers = new(StringComparer.OrdinalIgnoreCase);
    internal ProcessResult _defaultResult = new(0, "", "", ProcessExitReason.Exited);

    internal string? _workingDirectory;
    internal readonly Dictionary<string, string?> _environment = new(StringComparer.OrdinalIgnoreCase);
    internal readonly Dictionary<string, string> _secrets = new(StringComparer.Ordinal);
    internal readonly HashSet<string> _secretEnvKeys = new(StringComparer.OrdinalIgnoreCase);
    internal string? _tag;

    /// <summary>
    /// Gets all commands that have been executed (after calling RunAsync or Start).
    /// </summary>
    public IReadOnlyList<CapturedCommand> ExecutedCommands => _executedCommands.ToArray();

    /// <summary>
    /// Gets all commands that have been created (via Command method), including their current configuration.
    /// </summary>
    public IReadOnlyList<FakeCommand> CreatedCommands => _createdCommands.ToArray();

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
        _createdCommands = parent._createdCommands;
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
        while (_createdCommands.TryDequeue(out _)) { }
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

    /// <inheritdoc />
    public ICommand Command(string commandLine)
    {
        var parts = commandLine.Split(' ', 2);
        var fileName = parts[0];
        var args = parts.Length > 1 ? [parts[1]] : Array.Empty<string>();
        return Command(fileName, args);
    }

    /// <inheritdoc />
    public ICommand Command(string fileName, IReadOnlyList<string> args)
    {
        var command = new FakeCommand(this, fileName, args);
        _createdCommands.Enqueue(command);
        return command;
    }

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(string commandLine, CancellationToken ct = default)
    {
        return Command(commandLine).RunAsync(ct);
    }

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        return Command(fileName, args).RunAsync(ct);
    }

    /// <inheritdoc />
    public IRunningProcess Start(string commandLine)
    {
        return Command(commandLine).WithCaptureOutput(false).Start();
    }

    /// <inheritdoc />
    public IRunningProcess Start(string fileName, IReadOnlyList<string> args)
    {
        return Command(fileName, args).WithCaptureOutput(false).Start();
    }
}

/// <summary>
/// Represents a command that was captured by <see cref="FakeVirtualShell"/>.
/// </summary>
/// <param name="FileName">The executable name or path.</param>
/// <param name="Arguments">The arguments passed to the command.</param>
/// <param name="WorkingDirectory">The working directory at time of execution.</param>
/// <param name="Environment">The environment variables at time of execution.</param>
/// <param name="Stdin">The stdin source, if configured.</param>
/// <param name="CaptureOutput">Whether output capture was enabled.</param>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed record CapturedCommand(
    string FileName,
    string[] Arguments,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string?> Environment,
    Stdin? Stdin,
    bool CaptureOutput)
{
    /// <summary>
    /// Gets the command line as a single string.
    /// </summary>
    public string CommandLine => Arguments.Length == 0 ? FileName : $"{FileName} {string.Join(" ", Arguments)}";
}

/// <summary>
/// A fake implementation of <see cref="ICommand"/> for testing purposes.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeCommand : ICommand
{
    private readonly FakeVirtualShell _shell;
    private readonly string _fileName;
    private readonly IReadOnlyList<string> _args;

    private Stdin? _stdin;
    private bool _captureOutput = true;

    internal FakeCommand(FakeVirtualShell shell, string fileName, IReadOnlyList<string> args)
    {
        _shell = shell;
        _fileName = fileName;
        _args = args;
    }

    /// <summary>
    /// Gets the current command state as a JSON object for test assertions.
    /// </summary>
    /// <returns>A JSON object containing the command state.</returns>
    public JsonObject GetStateAsJson()
    {
        var json = new JsonObject
        {
            ["fileName"] = _fileName
        };

        if (_args.Count > 0)
        {
            var argsArray = new JsonArray();
            foreach (var arg in _args)
            {
                argsArray.Add(arg);
            }
            json["args"] = argsArray;
        }

        if (_shell._workingDirectory is not null)
        {
            json["workingDirectory"] = _shell._workingDirectory;
        }

        if (_shell._environment.Count > 0)
        {
            var envObj = new JsonObject();
            foreach (var kvp in _shell._environment.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                envObj[kvp.Key] = kvp.Value;
            }
            json["environment"] = envObj;
        }

        if (_stdin is not null)
        {
            json["stdin"] = _stdin.ToString();
        }

        if (!_captureOutput)
        {
            json["captureOutput"] = false;
        }

        if (_shell._tag is not null)
        {
            json["tag"] = _shell._tag;
        }

        return json;
    }

    /// <inheritdoc />
    public ICommand WithStdin(Stdin stdin)
    {
        _stdin = stdin;
        return this;
    }

    /// <inheritdoc />
    public ICommand WithStdin() => WithStdin(Stdin.CreatePipe());

    /// <inheritdoc />
    public ICommand WithCaptureOutput(bool capture)
    {
        _captureOutput = capture;
        return this;
    }

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(CancellationToken ct = default)
    {
        var captured = CreateCapturedCommand();
        _shell._executedCommands.Enqueue(captured);
        var result = GetResult(captured);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public IRunningProcess Start()
    {
        var captured = CreateCapturedCommand();
        _shell._executedCommands.Enqueue(captured);
        var result = GetResult(captured);
        return new FakeRunningProcess(result);
    }

    private CapturedCommand CreateCapturedCommand()
    {
        return new CapturedCommand(
            _fileName,
            _args.ToArray(),
            _shell._workingDirectory,
            new Dictionary<string, string?>(_shell._environment),
            _stdin,
            _captureOutput);
    }

    private ProcessResult GetResult(CapturedCommand command)
    {
        // Check for response handler first
        if (_shell._responseHandlers.TryGetValue(command.FileName, out var handler))
        {
            return handler(command);
        }

        // Check for static response
        if (_shell._responses.TryGetValue(command.FileName, out var result))
        {
            return result;
        }

        return _shell._defaultResult;
    }
}

/// <summary>
/// A fake implementation of <see cref="IRunningProcess"/> for testing purposes.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class FakeRunningProcess : IRunningProcess
{
    private readonly ProcessResult _result;
    private readonly Pipe _inputPipe;
    private readonly Pipe _outputPipe;
    private readonly Pipe _errorPipe;
    private readonly TaskCompletionSource<ProcessResult> _resultTcs;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="FakeRunningProcess"/> with the specified result.
    /// </summary>
    /// <param name="result">The result to return when the process completes.</param>
    public FakeRunningProcess(ProcessResult result)
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
