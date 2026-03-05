// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class WatchStatusWriter : IAsyncDisposable
{
    private readonly Channel<WatchStatusEvent> _eventChannel = Channel.CreateUnbounded<WatchStatusEvent>(new()
    {
        SingleReader = true,
        SingleWriter = false
    });

    private readonly string? _pipeName;
    private readonly NamedPipeClientStream _pipe;
    private readonly ILogger _logger;
    private readonly Task _channelReader;
    private readonly CancellationTokenSource _disposalCancellationSource = new();

    public WatchStatusWriter(string pipeName, ILogger logger)
    {
        _pipe = new NamedPipeClientStream(
            serverName: ".",
            pipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        _pipeName = pipeName;
        _logger = logger;
        _channelReader = StartChannelReaderAsync(_disposalCancellationSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing status pipe.");

        _disposalCancellationSource.Cancel();
        await _channelReader;

        try
        {
            await _pipe.DisposeAsync();
        }
        catch (IOException)
        {
            // Pipe may already be broken if the server disconnected
        }

        _disposalCancellationSource.Dispose();
    }

    private async Task StartChannelReaderAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Connecting to status pipe '{PipeName}'...", _pipeName);

            await _pipe.ConnectAsync(cancellationToken);

            using var streamWriter = new StreamWriter(_pipe) { AutoFlush = true };

            await foreach (var statusEvent in _eventChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(statusEvent);
                await streamWriter.WriteLineAsync(json.AsMemory(), cancellationToken);
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or IOException)
        {
            // expected when disposing or if the server disconnects
        }
        catch (Exception e)
        {
            _logger.LogError("Unexpected error reading status event: {Exception}", e);
        }
    }

    public void WriteEvent(WatchStatusEvent statusEvent)
        => _eventChannel.Writer.TryWrite(statusEvent);
}
