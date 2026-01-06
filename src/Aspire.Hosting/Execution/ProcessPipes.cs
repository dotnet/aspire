// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides low-level pipe access to a running process.
/// The caller is responsible for reading from the output pipes to avoid deadlock.
/// Implements <see cref="IProcessPipes"/>.
/// </summary>
internal sealed partial class ProcessPipes : IProcessPipes
{
    private readonly System.Diagnostics.Process _process;
    private readonly PipeWriter _input;
    private readonly PipeReader _output;
    private readonly PipeReader _error;
    private readonly TaskCompletionSource<ProcessResult> _resultTcs;
    private readonly CancellationTokenSource _disposeCts;
    private volatile ProcessExitReason _exitReason = ProcessExitReason.Exited;
    private bool _disposed;

    internal ProcessPipes(System.Diagnostics.Process process)
    {
        _process = process;

        // Wrap process streams with Pipelines
        // leaveOpen: false for stdin so completing the writer closes stdin
        _input = PipeWriter.Create(_process.StandardInput.BaseStream, new StreamPipeWriterOptions(leaveOpen: false));
        _output = PipeReader.Create(_process.StandardOutput.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));
        _error = PipeReader.Create(_process.StandardError.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));

        _resultTcs = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _disposeCts = new CancellationTokenSource();

        SetupExitHandler();
    }

    /// <inheritdoc />
    public PipeWriter Input => _input;

    /// <inheritdoc />
    public PipeReader Output => _output;

    /// <inheritdoc />
    public PipeReader Error => _error;

    private void SetupExitHandler()
    {
        _ = WaitForExitAndSetResultAsync();
    }

    private async Task WaitForExitAndSetResultAsync()
    {
        try
        {
            await _process.WaitForExitAsync(_disposeCts.Token).ConfigureAwait(false);

            // Create the result (no captured output - caller reads via pipes)
            var result = new ProcessResult(
                _process.ExitCode,
                null,
                null,
                _exitReason);

            _resultTcs.TrySetResult(result);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            var result = new ProcessResult(
                _process.HasExited ? _process.ExitCode : -1,
                null,
                null,
                ProcessExitReason.Killed);

            _resultTcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            _resultTcs.TrySetException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ProcessResult> WaitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);

        return await _resultTcs.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Signal(ProcessSignal signal)
    {
        ThrowIfDisposed();
        SignalCore(signal);
    }

    private void SignalCore(ProcessSignal signal)
    {
        if (_process.HasExited)
        {
            return;
        }

        _exitReason = ProcessExitReason.Signaled;

        switch (signal)
        {
            case ProcessSignal.Interrupt:
                SendInterrupt();
                break;
            case ProcessSignal.Terminate:
                SendTerminate();
                break;
            case ProcessSignal.Kill:
                KillCore();
                break;
        }
    }

    /// <inheritdoc />
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

        _exitReason = ProcessExitReason.Killed;

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

        // Complete the input pipe to signal end of input
        await _input.CompleteAsync().ConfigureAwait(false);

        // Cancel background tasks
        await _disposeCts.CancelAsync().ConfigureAwait(false);

        if (!_process.HasExited)
        {
            // Give the process a chance to exit gracefully
            SignalCore(ProcessSignal.Interrupt);

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

        // Complete output pipe readers
        await _output.CompleteAsync().ConfigureAwait(false);
        await _error.CompleteAsync().ConfigureAwait(false);

        _process.Dispose();
        _disposeCts.Dispose();
    }
}
