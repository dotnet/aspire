// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Hex1b;

namespace Aspire.Cli.UI;

/// <summary>
/// A terminal workload adapter that streams console logs from an Aspire resource
/// via the backchannel and renders them in an embedded terminal widget.
/// </summary>
internal sealed class AspireResourceConsoleLogWorkload : IHex1bTerminalWorkloadAdapter
{
    private readonly Channel<byte[]> _outputChannel;
    private readonly IAppHostAuxiliaryBackchannel _connection;
    private CancellationTokenSource? _streamCts;
    private bool _disposed;
    private string? _currentResourceName;

    public AspireResourceConsoleLogWorkload(IAppHostAuxiliaryBackchannel connection)
    {
        _connection = connection;
        _outputChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <inheritdoc />
    public event Action? Disconnected;

    /// <inheritdoc />
    public async ValueTask<ReadOnlyMemory<byte>> ReadOutputAsync(CancellationToken ct = default)
    {
        if (_disposed)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        try
        {
            if (await _outputChannel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                if (_outputChannel.Reader.TryRead(out var data))
                {
                    return data;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }

        return ReadOnlyMemory<byte>.Empty;
    }

    /// <inheritdoc />
    public ValueTask WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        // Read-only log view — input is ignored
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ResizeAsync(int width, int height, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Starts streaming logs for the specified resource. Cancels any previous stream.
    /// </summary>
    public void StartStreaming(string resourceName, CancellationToken cancellationToken)
    {
        StopStreaming();

        _currentResourceName = resourceName;
        _streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _streamCts.Token;

        // Clear terminal and show header
        WriteOutput($"\x1b[2J\x1b[H\x1b[1;35m── Logs: {resourceName} ──\x1b[0m\r\n\r\n");

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var logLine in _connection.GetResourceLogsAsync(resourceName, follow: true, cancellationToken: ct).ConfigureAwait(false))
                {
                    var prefix = logLine.IsError
                        ? "\x1b[31m"  // red for errors
                        : "\x1b[0m";  // default
                    WriteOutput($"{prefix}{logLine.Content}\x1b[0m\r\n");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                WriteOutput($"\r\n\x1b[31mLog stream error: {ex.Message}\x1b[0m\r\n");
            }
        }, ct);
    }

    /// <summary>
    /// Stops the current log stream.
    /// </summary>
    public void StopStreaming()
    {
        if (_streamCts is not null)
        {
            _streamCts.Cancel();
            _streamCts.Dispose();
            _streamCts = null;
        }

        _currentResourceName = null;
    }

    /// <summary>
    /// Gets the name of the resource currently being streamed, if any.
    /// </summary>
    public string? CurrentResourceName => _currentResourceName;

    private void WriteOutput(string text)
    {
        if (_disposed)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        _outputChannel.Writer.TryWrite(bytes);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopStreaming();
            _outputChannel.Writer.TryComplete();
            Disconnected?.Invoke();
        }

        return ValueTask.CompletedTask;
    }
}
