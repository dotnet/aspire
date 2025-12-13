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
    private readonly SemaphoreSlim _stdinLock = new(1, 1);

    private volatile CliExitReason _exitReason = CliExitReason.Exited;
    private bool _stdinCompleted;
    private bool _disposed;
    private bool _readLinesStarted;

    internal RunningProcess(
        Process process,
        ExecSpec spec,
        bool captureOutput)
    {
        _process = process;
        _spec = spec;
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
        // Use WaitForExitAsync which waits for both process exit AND async output reading to complete.
        // The Exited event fires as soon as the process terminates but before all output is read.
        _ = WaitForExitAndSetResultAsync();
    }

    private async Task WaitForExitAndSetResultAsync()
    {
        try
        {
            // WaitForExitAsync waits for both process termination and all redirected output to be read.
            // Use the dispose token so that if DisposeAsync is called, this task can complete.
            await _process.WaitForExitAsync(_disposeCts.Token).ConfigureAwait(false);

            // Complete the output channel
            _outputChannel.Writer.TryComplete();

            // Create the result
            var result = new CliResult(
                _process.ExitCode,
                _stdoutCapture?.ToString().TrimEnd(),
                _stderrCapture?.ToString().TrimEnd(),
                _exitReason);

            _resultTcs.TrySetResult(result);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            // Process is being disposed, complete with what we have
            _outputChannel.Writer.TryComplete();

            var result = new CliResult(
                _process.HasExited ? _process.ExitCode : -1,
                _stdoutCapture?.ToString().TrimEnd(),
                _stderrCapture?.ToString().TrimEnd(),
                CliExitReason.Killed);

            _resultTcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            _outputChannel.Writer.TryComplete(ex);
            _resultTcs.TrySetException(ex);
        }
    }

    /// <summary>
    /// Streams the output lines from the process.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    /// <exception cref="InvalidOperationException">Thrown if ReadLines has already been called.</exception>
    public async IAsyncEnumerable<OutputLine> ReadLines([EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_readLinesStarted)
        {
            throw new InvalidOperationException("ReadLines can only be called once per process.");
        }
        _readLinesStarted = true;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);

        await foreach (var line in _outputChannel.Reader.ReadAllAsync(linkedCts.Token).ConfigureAwait(false))
        {
            yield return line;
        }
    }

    /// <summary>
    /// Gets the full result when the process completes.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the process execution.</returns>
    /// <remarks>
    /// When the cancellation token is triggered, this method throws <see cref="OperationCanceledException"/>
    /// but does NOT automatically kill the process. The caller is responsible for calling <see cref="Kill"/>
    /// if desired.
    /// </remarks>
    public async Task<CliResult> WaitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);

        return await _resultTcs.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
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
        ThrowIfDisposed();

        var result = await WaitAsync(ct).ConfigureAwait(false);
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
        ThrowIfDisposed();
        await _stdinLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfStdinCompleted();
            await _process.StandardInput.WriteAsync(text, ct).ConfigureAwait(false);
        }
        finally
        {
            _stdinLock.Release();
        }
    }

    /// <summary>
    /// Writes a line of text to the process's stdin.
    /// </summary>
    /// <param name="line">The line to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public async Task WriteLineAsync(string line, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _stdinLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfStdinCompleted();
            await _process.StandardInput.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
        }
        finally
        {
            _stdinLock.Release();
        }
    }

    /// <summary>
    /// Completes stdin, signaling to the process that no more input is coming.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    public async Task CompleteStdinAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _stdinLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfStdinCompleted();
            _stdinCompleted = true;
            _process.StandardInput.Close();
        }
        finally
        {
            _stdinLock.Release();
        }
    }

    private void ThrowIfStdinCompleted()
    {
        if (_stdinCompleted)
        {
            throw new InvalidOperationException("Stdin has already been completed.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Sends a signal to the process.
    /// </summary>
    /// <param name="signal">The signal to send.</param>
    public void Signal(CliSignal signal)
    {
        ThrowIfDisposed();
        SignalCore(signal);
    }

    private void SignalCore(CliSignal signal)
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
                KillCore();
                break;
        }
    }

    /// <summary>
    /// Kills the process immediately.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
    public void Kill(bool entireProcessTree = true)
    {
        ThrowIfDisposed();
        KillCore(entireProcessTree);
    }

    private void KillCore(bool entireProcessTree = true)
    {
        if (_process.HasExited)
        {
            return;
        }

        _exitReason = CliExitReason.Killed;

        try
        {
            _process.Kill(entireProcessTree);
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
#if NET10_0_OR_GREATER
            // On Windows with .NET 10+, send CTRL+C via GenerateConsoleCtrlEvent
            // Process must be started with CreateNewProcessGroup = true
            SendWindowsCtrlEvent(CtrlEvent.CtrlC);
#else
            // On older .NET, try to close the main window first
            if (!_process.CloseMainWindow())
            {
                KillCore();
            }
#endif
        }
        else
        {
            // On Unix, send SIGINT
            SendUnixSignal(2); // SIGINT
        }
    }

    private void SendTerminate()
    {
        if (OperatingSystem.IsWindows())
        {
#if NET10_0_OR_GREATER
            // On Windows with .NET 10+, send CTRL+BREAK (similar to SIGTERM)
            SendWindowsCtrlEvent(CtrlEvent.CtrlBreak);
#else
            // On older .NET, just kill the process
            KillCore();
#endif
        }
        else
        {
            // On Unix, send SIGTERM
            SendUnixSignal(15); // SIGTERM
        }
    }

#if NET10_0_OR_GREATER
    private void SendWindowsCtrlEvent(CtrlEvent ctrlEvent)
    {
        try
        {
            // When CreateNewProcessGroup is true, the process group ID equals the process ID
            if (!GenerateConsoleCtrlEvent((uint)ctrlEvent, (uint)_process.Id))
            {
                // If signal fails, fall back to kill
                KillCore();
            }
        }
        catch
        {
            // If signal fails, fall back to kill
            KillCore();
        }
    }

    private enum CtrlEvent : uint
    {
        CtrlC = 0,
        CtrlBreak = 1
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
#endif

    private void SendUnixSignal(int sig)
    {
        try
        {
            SysKill(_process.Id, sig);
        }
        catch
        {
            // If signal fails, fall back to kill
            KillCore();
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
            SignalCore(CliSignal.Interrupt);

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Timeout - force kill
                KillCore();
            }
        }

        _stdinLock.Dispose();
        _process.Dispose();
        _disposeCts.Dispose();
    }
}
