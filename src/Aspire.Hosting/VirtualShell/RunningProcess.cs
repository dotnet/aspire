// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Provides advanced control over a running process, including streaming output,
/// writing to stdin, and sending signals.
/// </summary>
public sealed partial class RunningProcess : IRunningProcess
{
    private readonly Process _process;
    private readonly Channel<OutputLine> _outputChannel;
    private readonly TaskCompletionSource<CliResult> _resultTcs;
    private readonly ExecSpec _spec;
    private readonly StringBuilder? _stdoutCapture;
    private readonly StringBuilder? _stderrCapture;
    private readonly CancellationTokenSource _disposeCts;
    private readonly bool _killProcessTree;

    private volatile CliExitReason _exitReason = CliExitReason.Exited;
    private bool _stdinCompleted;
    private bool _disposed;

    internal RunningProcess(
        Process process,
        ExecSpec spec,
        bool captureOutput,
        bool killProcessTree)
    {
        _process = process;
        _spec = spec;
        _killProcessTree = killProcessTree;
        _outputChannel = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
        _resultTcs = new TaskCompletionSource<CliResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _disposeCts = new CancellationTokenSource();

        if (captureOutput)
        {
            _stdoutCapture = new StringBuilder();
            _stderrCapture = new StringBuilder();
        }

        SetupOutputHandlers();
        SetupExitHandler();
    }

    private void SetupOutputHandlers()
    {
        _process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            _stdoutCapture?.AppendLine(e.Data);
            _outputChannel.Writer.TryWrite(new OutputLine(IsStdErr: false, e.Data));
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            _stderrCapture?.AppendLine(e.Data);
            _outputChannel.Writer.TryWrite(new OutputLine(IsStdErr: true, e.Data));
        };

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private void SetupExitHandler()
    {
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) =>
        {
            // Complete the output channel
            _outputChannel.Writer.TryComplete();

            // Create the result
            var result = new CliResult(
                _process.ExitCode,
                _stdoutCapture?.ToString().TrimEnd(),
                _stderrCapture?.ToString().TrimEnd(),
                _exitReason);

            _resultTcs.TrySetResult(result);
        };
    }

    /// <summary>
    /// Streams the output lines from the process.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    public async IAsyncEnumerable<OutputLine> Lines([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);

        await foreach (var line in _outputChannel.Reader.ReadAllAsync(linkedCts.Token).ConfigureAwait(false))
        {
            yield return line;
        }
    }

    /// <summary>
    /// Gets the exit code when the process completes.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The exit code of the process.</returns>
    public async Task<int> ExitCodeAsync(CancellationToken ct = default)
    {
        var result = await ResultAsync(ct).ConfigureAwait(false);
        return result.ExitCode;
    }

    /// <summary>
    /// Gets the full result when the process completes.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the process execution.</returns>
    public async Task<CliResult> ResultAsync(CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);

        try
        {
            return await _resultTcs.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _exitReason = CliExitReason.Canceled;
            Kill();
            throw;
        }
    }

    /// <summary>
    /// Ensures the process completed successfully, throwing if it did not.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process did not complete successfully.
    /// </exception>
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

    /// <summary>
    /// Writes text to the process's stdin.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public async Task WriteAsync(ReadOnlyMemory<char> text, CancellationToken ct = default)
    {
        ThrowIfStdinCompleted();
        await _process.StandardInput.WriteAsync(text, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a line of text to the process's stdin.
    /// </summary>
    /// <param name="line">The line to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public async Task WriteLineAsync(string line, CancellationToken ct = default)
    {
        ThrowIfStdinCompleted();
        await _process.StandardInput.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes stdin, signaling to the process that no more input is coming.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    public Task CompleteStdinAsync(CancellationToken ct = default)
    {
        ThrowIfStdinCompleted();
        _stdinCompleted = true;
        _process.StandardInput.Close();
        return Task.CompletedTask;
    }

    private void ThrowIfStdinCompleted()
    {
        if (_stdinCompleted)
        {
            throw new InvalidOperationException("Stdin has already been completed.");
        }
    }

    /// <summary>
    /// Sends a signal to the process.
    /// </summary>
    /// <param name="signal">The signal to send.</param>
    public void Signal(CliSignal signal)
    {
        if (_process.HasExited)
        {
            return;
        }

        _exitReason = CliExitReason.Signaled;

        switch (signal)
        {
            case CliSignal.Interrupt:
                SendInterrupt();
                break;
            case CliSignal.Terminate:
                SendTerminate();
                break;
            case CliSignal.Kill:
                Kill();
                break;
        }
    }

    /// <summary>
    /// Kills the process immediately.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
    public void Kill(bool entireProcessTree = true)
    {
        if (_process.HasExited)
        {
            return;
        }

        _exitReason = CliExitReason.Killed;

        try
        {
            _process.Kill(entireProcessTree && _killProcessTree);
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }

    private void SendInterrupt()
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows, try to close the main window first
            if (!_process.CloseMainWindow())
            {
                Kill();
            }
        }
        else
        {
            // On Unix, send SIGINT
            SendSignal(2); // SIGINT
        }
    }

    private void SendTerminate()
    {
        if (OperatingSystem.IsWindows())
        {
            Kill();
        }
        else
        {
            // On Unix, send SIGTERM
            SendSignal(15); // SIGTERM
        }
    }

    private void SendSignal(int sig)
    {
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                SysKill(_process.Id, sig);
            }
            catch
            {
                // If signal fails, fall back to kill
                Kill();
            }
        }
    }

    [LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static partial int SysKill(int pid, int sig);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _disposeCts.Cancel();

        if (!_process.HasExited)
        {
            // Give the process a chance to exit gracefully
            Signal(CliSignal.Interrupt);

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Timeout - force kill
                Kill();
            }
        }

        _process.Dispose();
        _disposeCts.Dispose();
    }
}
