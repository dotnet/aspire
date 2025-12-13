// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// A fake implementation of <see cref="IVirtualShell"/> for testing purposes.
/// Captures all commands executed and allows configuring responses.
/// </summary>
public sealed class FakeVirtualShell : IVirtualShell
{
    private readonly ConcurrentQueue<CapturedCommand> _commands = new();
    private readonly ConcurrentDictionary<string, CliResult> _responses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Func<CapturedCommand, CliResult>> _responseHandlers = new(StringComparer.OrdinalIgnoreCase);
    private CliResult _defaultResult = new(0, "", "", CliExitReason.Exited);

    private string? _workingDirectory;
    private readonly Dictionary<string, string?> _environment = new(StringComparer.OrdinalIgnoreCase);
    private TimeSpan _timeout = TimeSpan.FromMinutes(2);
    private string? _tag;

    /// <summary>
    /// Gets all commands that have been executed.
    /// </summary>
    public IReadOnlyList<CapturedCommand> Commands => _commands.ToArray();

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
    /// Creates a new instance of <see cref="FakeVirtualShell"/>.
    /// </summary>
    public FakeVirtualShell()
    {
    }

    private FakeVirtualShell(FakeVirtualShell parent)
    {
        _commands = parent._commands;
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
        while (_commands.TryDequeue(out _)) { }
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
    public Task<CliResult> Run(string commandLine, Action<ExecSpec>? perCall = null, CancellationToken ct = default)
    {
        var parts = commandLine.Split(' ', 2);
        var fileName = parts[0];
        var args = parts.Length > 1 ? [parts[1]] : Array.Empty<string>();
        return Run(fileName, args, perCall, ct);
    }

    /// <inheritdoc />
    public Task<CliResult> Run(string fileName, IReadOnlyList<string> args, Action<ExecSpec>? perCall = null, CancellationToken ct = default)
    {
        var spec = new ExecSpec();
        perCall?.Invoke(spec);

        var command = new CapturedCommand(
            fileName,
            args.ToArray(),
            _workingDirectory,
            new Dictionary<string, string?>(_environment),
            spec);

        _commands.Enqueue(command);

        var result = GetResult(command);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<OutputLine> Lines(string commandLine, Action<ExecSpec>? perCall = null, CancellationToken ct = default)
    {
        var parts = commandLine.Split(' ', 2);
        var fileName = parts[0];
        var args = parts.Length > 1 ? [parts[1]] : Array.Empty<string>();
        return Lines(fileName, args, perCall, ct);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OutputLine> Lines(string fileName, IReadOnlyList<string> args, Action<ExecSpec>? perCall = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var result = await Run(fileName, args, perCall, ct).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(result.Stdout))
        {
            foreach (var line in result.Stdout.Split('\n'))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    yield return new OutputLine(IsStdErr: false, line.TrimEnd('\r'));
                }
            }
        }

        if (!string.IsNullOrEmpty(result.Stderr))
        {
            foreach (var line in result.Stderr.Split('\n'))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    yield return new OutputLine(IsStdErr: true, line.TrimEnd('\r'));
                }
            }
        }
    }

    /// <inheritdoc />
    public IStreamRun Stream(string commandLine, Action<ExecSpec>? perCall = null)
    {
        var parts = commandLine.Split(' ', 2);
        var fileName = parts[0];
        var args = parts.Length > 1 ? [parts[1]] : Array.Empty<string>();
        return Stream(fileName, args, perCall);
    }

    /// <inheritdoc />
    public IStreamRun Stream(string fileName, IReadOnlyList<string> args, Action<ExecSpec>? perCall = null)
    {
        var spec = new ExecSpec();
        perCall?.Invoke(spec);

        var command = new CapturedCommand(
            fileName,
            args.ToArray(),
            _workingDirectory,
            new Dictionary<string, string?>(_environment),
            spec);

        _commands.Enqueue(command);

        var result = GetResult(command);
        return new FakeStreamRun(result);
    }

    private CliResult GetResult(CapturedCommand command)
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
/// Represents a command that was captured by <see cref="FakeVirtualShell"/>.
/// </summary>
/// <param name="FileName">The executable name or path.</param>
/// <param name="Arguments">The arguments passed to the command.</param>
/// <param name="WorkingDirectory">The working directory at time of execution.</param>
/// <param name="Environment">The environment variables at time of execution.</param>
/// <param name="Spec">The execution spec used.</param>
public sealed record CapturedCommand(
    string FileName,
    string[] Arguments,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string?> Environment,
    ExecSpec Spec)
{
    /// <summary>
    /// Gets the command line as a single string.
    /// </summary>
    public string CommandLine => Arguments.Length == 0 ? FileName : $"{FileName} {string.Join(" ", Arguments)}";
}

/// <summary>
/// A fake implementation of <see cref="IStreamRun"/> for testing purposes.
/// </summary>
public sealed class FakeStreamRun : IStreamRun
{
    private readonly CliResult _result;
    private readonly Channel<OutputLine> _channel;
    private readonly TaskCompletionSource<CliResult> _resultTcs;
    private bool _disposed;
    private bool _stdinCompleted;

    /// <summary>
    /// Creates a new instance of <see cref="FakeStreamRun"/> with the specified result.
    /// </summary>
    /// <param name="result">The result to return when the process completes.</param>
    public FakeStreamRun(CliResult result)
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
    public async Task<int> ExitCodeAsync(CancellationToken ct = default)
    {
        var result = await ResultAsync(ct).ConfigureAwait(false);
        return result.ExitCode;
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
