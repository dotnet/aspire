// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.VirtualShell.Internal;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Default implementation of <see cref="IVirtualShell"/> that provides portable
/// command execution without invoking a shell.
/// </summary>
public sealed class VirtualShell : IVirtualShell
{
    private readonly ShellState _state;
    private readonly TimeSpan? _defaultTimeout;
    private readonly string? _tag;
    private readonly ICommandLineParser _parser;
    private readonly IExecutableResolver _resolver;
    private readonly IProcessRunner _runner;

    /// <summary>
    /// Creates a new VirtualShell instance with default settings.
    /// </summary>
    public VirtualShell()
        : this(ShellState.Default, null, null, new CommandLineParser(), new ExecutableResolver(), new ProcessRunner())
    {
    }

    /// <summary>
    /// Creates a new VirtualShell instance with the specified dependencies.
    /// </summary>
    /// <param name="parser">The command line parser.</param>
    /// <param name="resolver">The executable resolver.</param>
    /// <param name="runner">The process runner.</param>
    internal VirtualShell(
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner)
        : this(ShellState.Default, null, null, parser, resolver, runner)
    {
    }

    private VirtualShell(
        ShellState state,
        TimeSpan? defaultTimeout,
        string? tag,
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner)
    {
        _state = state;
        _defaultTimeout = defaultTimeout;
        _tag = tag;
        _parser = parser;
        _resolver = resolver;
        _runner = runner;
    }

    /// <inheritdoc />
    public IVirtualShell Cd(string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        return new VirtualShell(
            _state with { WorkingDirectory = Path.GetFullPath(workingDirectory) },
            _defaultTimeout,
            _tag,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public IVirtualShell Env(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new VirtualShell(
            _state.WithEnv(key, value),
            _defaultTimeout,
            _tag,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public IVirtualShell Env(IReadOnlyDictionary<string, string?> vars)
    {
        ArgumentNullException.ThrowIfNull(vars);
        return new VirtualShell(
            _state.WithEnv(vars),
            _defaultTimeout,
            _tag,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public IVirtualShell Timeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }
        return new VirtualShell(
            _state,
            timeout,
            _tag,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public IVirtualShell Tag(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return new VirtualShell(
            _state,
            _defaultTimeout,
            category,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public Task<CliResult> Run(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default)
    {
        var (fileName, args) = _parser.Parse(commandLine);
        return Run(fileName, args, perCall, ct);
    }

    /// <inheritdoc />
    public Task<CliResult> Run(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default)
    {
        var spec = CreateSpec(perCall, captureByDefault: true);
        var exePath = _resolver.ResolveOrThrow(fileName, _state);
        return _runner.RunAsync(exePath, args, spec, _state, ct);
    }

    /// <inheritdoc />
    public async Task<string> Cap(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default)
    {
        var (fileName, args) = _parser.Parse(commandLine);
        return await Cap(fileName, args, perCall, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> Cap(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default)
    {
        var result = await Run(fileName, args, spec =>
        {
            spec.CaptureOutput = true;
            perCall?.Invoke(spec);
        }, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            var message = $"Command '{fileName}' failed with exit code {result.ExitCode}";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }

        return result.Stdout?.TrimEnd() ?? string.Empty;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<OutputLine> Lines(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default)
    {
        var (fileName, args) = _parser.Parse(commandLine);
        return Lines(fileName, args, perCall, ct);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OutputLine> Lines(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var streamRun = Stream(fileName, args, spec =>
        {
            spec.CaptureOutput = false; // Streaming default: don't capture
            perCall?.Invoke(spec);
        });
        await using (streamRun.ConfigureAwait(false))
        {
            await foreach (var line in streamRun.Lines(ct).ConfigureAwait(false))
            {
                yield return line;
            }
        }
    }

    /// <inheritdoc />
    public IStreamRun Stream(
        string commandLine,
        Action<ExecSpec>? perCall = null)
    {
        var (fileName, args) = _parser.Parse(commandLine);
        return Stream(fileName, args, perCall);
    }

    /// <inheritdoc />
    public IStreamRun Stream(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null)
    {
        var spec = CreateSpec(perCall, captureByDefault: false);
        var exePath = _resolver.ResolveOrThrow(fileName, _state);
        return _runner.Start(exePath, args, spec, _state);
    }

    private ExecSpec CreateSpec(Action<ExecSpec>? perCall, bool captureByDefault)
    {
        var spec = new ExecSpec
        {
            WorkingDirectory = _state.WorkingDirectory,
            Timeout = _defaultTimeout,
            CaptureOutput = captureByDefault
        };

        // Apply per-call customization
        perCall?.Invoke(spec);

        return spec;
    }
}
