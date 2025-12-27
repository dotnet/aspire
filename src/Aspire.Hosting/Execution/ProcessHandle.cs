// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides control over a running process with custom output handling via ProcessOutput.
/// Implements <see cref="IProcessHandle"/>.
/// </summary>
internal sealed partial class ProcessHandle : IProcessHandle
{
    private readonly System.Diagnostics.Process _process;
    private readonly PipeWriter _input;
    private readonly PipeReader _output;
    private readonly PipeReader _error;
    private readonly TaskCompletionSource<ProcessResult> _resultTcs;
    private readonly ProcessOutput _stdoutOutput;
    private readonly ProcessOutput _stderrOutput;
    private readonly CancellationTokenSource _disposeCts;
    private readonly Task<string?>? _stdoutDrainTask;
    private readonly Task<string?>? _stderrDrainTask;
    private volatile ProcessExitReason _exitReason = ProcessExitReason.Exited;
    private bool _disposed;

    internal ProcessHandle(System.Diagnostics.Process process, ProcessOutput stdout, ProcessOutput stderr)
    {
        _process = process;
        _stdoutOutput = stdout;
        _stderrOutput = stderr;

        // Wrap process streams with Pipelines
        _input = PipeWriter.Create(_process.StandardInput.BaseStream, new StreamPipeWriterOptions(leaveOpen: false));
        _output = PipeReader.Create(_process.StandardOutput.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));
        _error = PipeReader.Create(_process.StandardError.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));

        _resultTcs = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _disposeCts = new CancellationTokenSource();

        // Start draining immediately using polymorphism
        _stdoutDrainTask = _stdoutOutput.DrainAsync(_output, _disposeCts.Token);
        _stderrDrainTask = _stderrOutput.DrainAsync(_error, _disposeCts.Token);

        SetupExitHandler();
    }

    internal PipeWriter Input => _input;

    private void SetupExitHandler()
    {
        _ = WaitForExitAndSetResultAsync();
    }

    private async Task WaitForExitAndSetResultAsync()
    {
        try
        {
            // Wait for output to be drained
            string? stdout = null;
            string? stderr = null;

            if (_stdoutDrainTask is not null && _stderrDrainTask is not null)
            {
                var results = await Task.WhenAll(_stdoutDrainTask, _stderrDrainTask).ConfigureAwait(false);
                stdout = results[0];
                stderr = results[1];
            }

            // Wait for process to exit
            await _process.WaitForExitAsync(_disposeCts.Token).ConfigureAwait(false);

            // Create the result
            var result = new ProcessResult(
                _process.ExitCode,
                stdout,
                stderr,
                _exitReason);

            _resultTcs.TrySetResult(result);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            // Process is being disposed, complete with what we have
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
            SendWindowsCtrlEvent(CtrlEvent.CtrlC);
#else
            if (!_process.CloseMainWindow())
            {
                KillCore();
            }
#endif
        }
        else
        {
            SendUnixSignal(2); // SIGINT
        }
    }

    private void SendTerminate()
    {
        if (OperatingSystem.IsWindows())
        {
#if NET10_0_OR_GREATER
            SendWindowsCtrlEvent(CtrlEvent.CtrlBreak);
#else
            KillCore();
#endif
        }
        else
        {
            SendUnixSignal(15); // SIGTERM
        }
    }

#if NET10_0_OR_GREATER
    private void SendWindowsCtrlEvent(CtrlEvent ctrlEvent)
    {
        try
        {
            if (!GenerateConsoleCtrlEvent((uint)ctrlEvent, (uint)_process.Id))
            {
                KillCore();
            }
        }
        catch
        {
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
            SignalCore(ProcessSignal.Interrupt);

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                KillCore();
            }
        }

        await _output.CompleteAsync().ConfigureAwait(false);
        await _error.CompleteAsync().ConfigureAwait(false);

        _process.Dispose();
        _disposeCts.Dispose();
    }
}
