// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class WatchControlReader : IAsyncDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private readonly StreamReader _reader;
    private readonly ILogger _logger;

    private WatchControlReader(NamedPipeClientStream pipe, StreamReader reader, ILogger logger)
    {
        _pipe = pipe;
        _reader = reader;
        _logger = logger;
    }

    public static async Task<WatchControlReader?> TryConnectAsync(string pipeName, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var pipe = new NamedPipeClientStream(
                serverName: ".",
                pipeName,
                PipeDirection.In,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            await pipe.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);

            var reader = new StreamReader(pipe);
            logger.LogDebug("Connected to control pipe '{PipeName}'.", pipeName);
            return new WatchControlReader(pipe, reader, logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("Failed to connect to control pipe '{PipeName}': {Message}. External rebuild commands will not be available.", pipeName, ex.Message);
            return null;
        }
    }

    public async Task<WatchControlCommand?> ReadCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            var line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<WatchControlCommand>(line);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            _logger.LogDebug("Control pipe disconnected: {Message}", ex.Message);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _reader.Dispose();
        }
        catch (IOException)
        {
            // Pipe may already be broken if the server disconnected
        }
        await _pipe.DisposeAsync().ConfigureAwait(false);
    }
}
