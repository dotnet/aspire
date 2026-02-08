// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class WatchStatusWriter : IAsyncDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private readonly StreamWriter _writer;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _connected;

    private WatchStatusWriter(NamedPipeClientStream pipe, StreamWriter writer, ILogger logger)
    {
        _pipe = pipe;
        _writer = writer;
        _logger = logger;
        _connected = true;
    }

    public static async Task<WatchStatusWriter?> TryConnectAsync(string pipeName, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var pipe = new NamedPipeClientStream(
                serverName: ".",
                pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            await pipe.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);

            var writer = new StreamWriter(pipe) { AutoFlush = true };
            logger.LogDebug("Connected to status pipe '{PipeName}'.", pipeName);
            return new WatchStatusWriter(pipe, writer, logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("Failed to connect to status pipe '{PipeName}': {Message}. Status events will not be reported.", pipeName, ex.Message);
            return null;
        }
    }

    public async Task WriteEventAsync(WatchStatusEvent statusEvent)
    {
        if (!_connected)
        {
            return;
        }

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_connected)
            {
                return;
            }

            var json = JsonSerializer.Serialize(statusEvent);
            await _writer.WriteLineAsync(json).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            _logger.LogDebug("Status pipe disconnected: {Message}", ex.Message);
            _connected = false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _connected = false;
        try
        {
            await _writer.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Pipe may already be broken if the server disconnected
        }
        await _pipe.DisposeAsync().ConfigureAwait(false);
    }
}
