// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides advanced control over a running process, including streaming I/O
/// and sending signals.
/// </summary>
public sealed partial class RunningProcess : IRunningProcess
{
    private readonly Process _process;
    private readonly PipeWriter _input;
    private readonly PipeReader _output;
    private readonly PipeReader _error;
    private readonly TaskCompletionSource<ProcessResult> _resultTcs;
    private readonly ExecSpec _spec;
    private readonly StringBuilder? _stdoutCapture;
    private readonly StringBuilder? _stderrCapture;
    private readonly CancellationTokenSource _disposeCts;
    private readonly Task? _stdoutCaptureTask;
    private readonly Task? _stderrCaptureTask;

    private volatile ProcessExitReason _exitReason = ProcessExitReason.Exited;
    private bool _disposed;

    internal RunningProcess(
        Process process,
        ExecSpec spec,
        bool captureOutput)
    {
        _process = process;
        _spec = spec;

        // Wrap process streams with Pipelines
        // Note: leaveOpen: false for stdin so completing the writer closes stdin
        _input = PipeWriter.Create(_process.StandardInput.BaseStream, new StreamPipeWriterOptions(leaveOpen: false));
        _output = PipeReader.Create(_process.StandardOutput.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));
        _error = PipeReader.Create(_process.StandardError.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));

        _resultTcs = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _disposeCts = new CancellationTokenSource();

        if (captureOutput)
        {
            _stdoutCapture = new StringBuilder();
            _stderrCapture = new StringBuilder();

            // Start background tasks to capture output
            _stdoutCaptureTask = CaptureOutputAsync(_output, _stdoutCapture);
            _stderrCaptureTask = CaptureOutputAsync(_error, _stderrCapture);
        }

        SetupExitHandler();
    }

    /// <inheritdoc />
    public PipeWriter Input => _input;

    /// <inheritdoc />
    public PipeReader Output => _output;

    /// <inheritdoc />
    public PipeReader Error => _error;

    private async Task CaptureOutputAsync(PipeReader reader, StringBuilder capture)
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(_disposeCts.Token).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (buffer.Length > 0)
                {
                    foreach (var segment in buffer)
                    {
                        capture.Append(Encoding.UTF8.GetString(segment.Span));
                    }
                }

                reader.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            // Process is being disposed
        }
        catch
        {
            // Stream may have been closed
        }
    }

    private void SetupExitHandler()
    {
        _ = WaitForExitAndSetResultAsync();
    }

    private async Task WaitForExitAndSetResultAsync()
    {
        try
        {
            // Wait for process to exit
            await _process.WaitForExitAsync(_disposeCts.Token).ConfigureAwait(false);

            // Wait for capture tasks to complete if capturing
            if (_stdoutCaptureTask is not null && _stderrCaptureTask is not null)
            {
                await Task.WhenAll(_stdoutCaptureTask, _stderrCaptureTask).ConfigureAwait(false);
            }

            // Create the result
            var result = new ProcessResult(
                _process.ExitCode,
                _stdoutCapture?.ToString().TrimEnd(),
                _stderrCapture?.ToString().TrimEnd(),
                _exitReason);

            _resultTcs.TrySetResult(result);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            // Process is being disposed, complete with what we have
            var result = new ProcessResult(
                _process.HasExited ? _process.ExitCode : -1,
                _stdoutCapture?.ToString().TrimEnd(),
                _stderrCapture?.ToString().TrimEnd(),
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
