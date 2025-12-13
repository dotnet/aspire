// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.VirtualShell.Internal;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Represents a command that can be configured fluently and executed.
/// </summary>
public sealed class Command : ICommand
{
    private readonly ShellState _shellState;
    private readonly TimeSpan? _shellDefaultTimeout;
    private readonly IExecutableResolver _resolver;
    private readonly IProcessRunner _runner;
    private readonly string _fileName;
    private readonly IReadOnlyList<string> _args;

    // Per-command settings
    private Stdin? _stdin;
    private TimeSpan? _timeout;
    private bool _captureOutput = true;
    private int? _maxCaptureBytes;
    private CancellationMode _cancellationMode = CancellationMode.KillTree;

    internal Command(
        ShellState shellState,
        TimeSpan? shellDefaultTimeout,
        IExecutableResolver resolver,
        IProcessRunner runner,
        string fileName,
        IReadOnlyList<string> args)
    {
        _shellState = shellState;
        _shellDefaultTimeout = shellDefaultTimeout;
        _resolver = resolver;
        _runner = runner;
        _fileName = fileName;
        _args = args;
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
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }
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
        if (maxBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes), "Max capture bytes must be positive.");
        }
        _maxCaptureBytes = maxBytes;
        return this;
    }

    /// <inheritdoc />
    public ICommand WithCancellationMode(CancellationMode mode)
    {
        _cancellationMode = mode;
        return this;
    }

    /// <inheritdoc />
    public Task<CliResult> ExecuteAsync(CancellationToken ct = default)
    {
        var spec = CreateSpec(captureByDefault: true);
        var exePath = _resolver.ResolveOrThrow(_fileName, _shellState);
        return _runner.RunAsync(exePath, _args, spec, _shellState, ct);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OutputLine> LinesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var streamRun = Stream();
        await using (streamRun.ConfigureAwait(false))
        {
            await foreach (var line in streamRun.Lines(ct).ConfigureAwait(false))
            {
                yield return line;
            }
        }
    }

    /// <inheritdoc />
    public IStreamRun Stream()
    {
        var spec = CreateSpec(captureByDefault: false);
        var exePath = _resolver.ResolveOrThrow(_fileName, _shellState);
        return _runner.Start(exePath, _args, spec, _shellState);
    }

    private ExecSpec CreateSpec(bool captureByDefault)
    {
        var spec = new ExecSpec
        {
            WorkingDirectory = _shellState.WorkingDirectory,
            Timeout = _timeout ?? _shellDefaultTimeout,
            CaptureOutput = _captureOutput && captureByDefault,
            MaxCaptureBytes = _maxCaptureBytes,
            Stdin = _stdin,
            KillProcessTree = _cancellationMode == CancellationMode.KillTree
        };

        return spec;
    }
}
