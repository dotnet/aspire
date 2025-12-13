// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// A fake implementation of <see cref="IVirtualShell"/> for testing purposes.
/// Captures all commands executed and allows configuring responses.
/// </summary>
public sealed class FakeVirtualShell : IVirtualShell
{
    internal readonly ConcurrentQueue<CapturedCommand> _executedCommands = new();
    internal readonly ConcurrentQueue<FakeCommand> _createdCommands = new();
    internal readonly ConcurrentDictionary<string, CliResult> _responses = new(StringComparer.OrdinalIgnoreCase);
    internal readonly ConcurrentDictionary<string, Func<CapturedCommand, CliResult>> _responseHandlers = new(StringComparer.OrdinalIgnoreCase);
    internal CliResult _defaultResult = new(0, "", "", CliExitReason.Exited);

    internal string? _workingDirectory;
    internal readonly Dictionary<string, string?> _environment = new(StringComparer.OrdinalIgnoreCase);
    internal TimeSpan _timeout = TimeSpan.FromMinutes(2);
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
    /// Gets the current timeout.
    /// </summary>
    public TimeSpan CurrentTimeout => _timeout;

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
        _timeout = parent._timeout;
        _tag = parent._tag;
    }

    /// <summary>
    /// Sets the default result for commands that don't have a specific response configured.
    /// </summary>
    /// <param name="result">The default result.</param>
    /// <returns>This instance for chaining.</returns>
    public FakeVirtualShell WithDefaultResult(CliResult result)
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
    public FakeVirtualShell WithResponse(string command, CliResult result)
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
    public FakeVirtualShell WithResponseHandler(string command, Func<CapturedCommand, CliResult> handler)
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
    public IVirtualShell Timeout(TimeSpan timeout)
    {
        var clone = new FakeVirtualShell(this)
        {
            _timeout = timeout
        };
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
    public Task<CliResult> Run(string commandLine, CancellationToken ct = default)
    {
        return Command(commandLine).RunAsync(ct);
    }

    /// <inheritdoc />
    public Task<CliResult> Run(string fileName, IReadOnlyList<string> args, CancellationToken ct = default)
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
/// <param name="Timeout">The timeout, if configured.</param>
/// <param name="CaptureOutput">Whether output capture was enabled.</param>
/// <param name="MaxCaptureBytes">The max capture bytes, if configured.</param>
public sealed record CapturedCommand(
    string FileName,
    string[] Arguments,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string?> Environment,
    Stdin? Stdin,
    TimeSpan? Timeout,
    bool CaptureOutput,
    int? MaxCaptureBytes)
{
    /// <summary>
    /// Gets the command line as a single string.
    /// </summary>
    public string CommandLine => Arguments.Length == 0 ? FileName : $"{FileName} {string.Join(" ", Arguments)}";
}

/// <summary>
/// A fake implementation of <see cref="ICommand"/> for testing purposes.
/// </summary>
public sealed class FakeCommand : ICommand
{
    private readonly FakeVirtualShell _shell;
    private readonly string _fileName;
    private readonly IReadOnlyList<string> _args;

    private Stdin? _stdin;
    private TimeSpan? _timeout;
    private bool _captureOutput = true;
    private int? _maxCaptureBytes;

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

        if (_timeout is not null)
        {
            json["timeout"] = _timeout.Value.ToString();
        }

        if (!_captureOutput)
        {
            json["captureOutput"] = false;
        }

        if (_maxCaptureBytes is not null)
        {
            json["maxCaptureBytes"] = _maxCaptureBytes.Value;
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
    public ICommand WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <inheritdoc />
    public ICommand WithCaptureOutput(bool capture)
    {
        _captureOutput = capture;
        return this;
    }

    /// <inheritdoc />
    public ICommand WithMaxCaptureBytes(int maxBytes)
    {
        _maxCaptureBytes = maxBytes;
        return this;
    }

    /// <inheritdoc />
    public Task<CliResult> RunAsync(CancellationToken ct = default)
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
            _timeout,
            _captureOutput,
            _maxCaptureBytes);
    }

    private CliResult GetResult(CapturedCommand command)
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
public sealed class FakeRunningProcess : IRunningProcess
{
    private readonly CliResult _result;
    private readonly Channel<OutputLine> _channel;
    private readonly TaskCompletionSource<CliResult> _resultTcs;
    private bool _disposed;
    private bool _stdinCompleted;

    /// <summary>
    /// Creates a new instance of <see cref="FakeRunningProcess"/> with the specified result.
    /// </summary>
    /// <param name="result">The result to return when the process completes.</param>
    public FakeRunningProcess(CliResult result)
    {
        _result = result;
        _channel = Channel.CreateUnbounded<OutputLine>();
        _resultTcs = new TaskCompletionSource<CliResult>();

        // Queue up the output lines
        _ = Task.Run(async () =>
        {
            if (!string.IsNullOrEmpty(result.Stdout))
            {
                foreach (var line in result.Stdout.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        await _channel.Writer.WriteAsync(new OutputLine(IsStdErr: false, line.TrimEnd('\r'))).ConfigureAwait(false);
                    }
                }
            }

            if (!string.IsNullOrEmpty(result.Stderr))
            {
                foreach (var line in result.Stderr.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        await _channel.Writer.WriteAsync(new OutputLine(IsStdErr: true, line.TrimEnd('\r'))).ConfigureAwait(false);
                    }
                }
            }

            _channel.Writer.Complete();
            _resultTcs.SetResult(result);
        });
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OutputLine> Lines([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var line in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return line;
        }
    }

    /// <inheritdoc />
    public Task<CliResult> ResultAsync(CancellationToken ct = default)
    {
        return _resultTcs.Task.WaitAsync(ct);
    }

    /// <inheritdoc />
    public async Task EnsureSuccessAsync(CancellationToken ct = default)
    {
        var result = await ResultAsync(ct).ConfigureAwait(false);
        if (!result.Success)
        {
            var message = $"Process exited with code {result.ExitCode} (reason: {result.Reason})";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }
    }

    /// <inheritdoc />
    public Task WriteAsync(ReadOnlyMemory<char> text, CancellationToken ct = default)
    {
        ThrowIfStdinCompleted();
        // No-op for fake
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteLineAsync(string line, CancellationToken ct = default)
    {
        ThrowIfStdinCompleted();
        // No-op for fake
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CompleteStdinAsync(CancellationToken ct = default)
    {
        ThrowIfStdinCompleted();
        _stdinCompleted = true;
        return Task.CompletedTask;
    }

    private void ThrowIfStdinCompleted()
    {
        if (_stdinCompleted)
        {
            throw new InvalidOperationException("Stdin has already been completed.");
        }
    }

    /// <inheritdoc />
    public void Signal(CliSignal signal)
    {
        // No-op for fake
    }

    /// <inheritdoc />
    public void Kill(bool entireProcessTree = true)
    {
        // No-op for fake
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _channel.Writer.TryComplete();
        _resultTcs.TrySetResult(_result);
        return ValueTask.CompletedTask;
    }
}
