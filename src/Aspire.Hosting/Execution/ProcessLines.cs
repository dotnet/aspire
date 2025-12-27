// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides line-based streaming access to a running process's output.
/// Implements <see cref="IProcessLines"/>.
/// </summary>
internal sealed partial class ProcessLines : IProcessLines
{
    private readonly System.Diagnostics.Process _process;
    private readonly PipeWriter _input;
    private readonly PipeReader _output;
    private readonly PipeReader _error;
    private readonly TaskCompletionSource<ProcessResult> _resultTcs;
    private readonly CancellationTokenSource _disposeCts;
    private volatile ProcessExitReason _exitReason = ProcessExitReason.Exited;
    private bool _disposed;
    private bool _linesEnumerated;

    internal ProcessLines(System.Diagnostics.Process process)
    {
        _process = process;

        // Wrap process streams with Pipelines
        _input = PipeWriter.Create(_process.StandardInput.BaseStream, new StreamPipeWriterOptions(leaveOpen: false));
        _output = PipeReader.Create(_process.StandardOutput.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));
        _error = PipeReader.Create(_process.StandardError.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));

        _resultTcs = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _disposeCts = new CancellationTokenSource();

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
            await _process.WaitForExitAsync(_disposeCts.Token).ConfigureAwait(false);

            // Create the result (no captured output - caller reads via ReadLinesAsync)
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
    public async IAsyncEnumerable<OutputLine> ReadLinesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_linesEnumerated)
        {
            throw new InvalidOperationException("ReadLinesAsync can only be called once per ProcessLines instance.");
        }
        _linesEnumerated = true;

        var channel = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Start reading from both stdout and stderr concurrently
        var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token).Token;
        var stdoutTask = ReadLinesFromReaderAsync(_output, isStdErr: false, channel.Writer, linkedCt);
        var stderrTask = ReadLinesFromReaderAsync(_error, isStdErr: true, channel.Writer, linkedCt);

        // Complete the channel when both readers are done
        _ = Task.WhenAll(stdoutTask, stderrTask).ContinueWith(
            _ => channel.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        await foreach (var line in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return line;
        }
    }

    private static async Task ReadLinesFromReaderAsync(
        PipeReader reader,
        bool isStdErr,
        ChannelWriter<OutputLine> writer,
        CancellationToken ct)
    {
        try
        {
            while (true)
            {
                ReadResult result;
                try
                {
                    result = await reader.ReadAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    await writer.WriteAsync(new OutputLine(isStdErr, line), ct).ConfigureAwait(false);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    // Process any remaining content as a final line
                    if (buffer.Length > 0)
                    {
                        var remainingLine = GetString(buffer);
                        if (!string.IsNullOrEmpty(remainingLine))
                        {
                            await writer.WriteAsync(new OutputLine(isStdErr, remainingLine), ct).ConfigureAwait(false);
                        }
                    }
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out string line)
    {
        var reader = new SequenceReader<byte>(buffer);

        if (reader.TryReadTo(out ReadOnlySpan<byte> lineBytes, (byte)'\n'))
        {
            // Remove trailing \r if present
            if (lineBytes.Length > 0 && lineBytes[^1] == '\r')
            {
                lineBytes = lineBytes[..^1];
            }

            line = Encoding.UTF8.GetString(lineBytes);
            buffer = buffer.Slice(reader.Position);
            return true;
        }

        line = string.Empty;
        return false;
    }

    private static string GetString(ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.FirstSpan;
            // Remove trailing \r if present
            if (span.Length > 0 && span[^1] == '\r')
            {
                span = span[..^1];
            }
            return Encoding.UTF8.GetString(span);
        }

        return Encoding.UTF8.GetString(buffer.ToArray()).TrimEnd('\r');
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
            // On Windows, just kill for now (no .NET 10 p/invoke complexity)
            KillCore();
        }
        else
        {
            // On Unix, send SIGINT
            SendUnixSignal(2);
        }
    }

    private void SendTerminate()
    {
        if (OperatingSystem.IsWindows())
        {
            KillCore();
        }
        else
        {
            // On Unix, send SIGTERM
            SendUnixSignal(15);
        }
    }

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

    [System.Runtime.InteropServices.LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
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
