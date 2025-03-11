// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Hosting.Cli;

internal class CliBackchannel(ILogger<CliBackchannel> logger, IConfiguration configuration, AppHostRpcTarget appHostRpcTarget) : BackgroundService
{
    private const string UnixSocketPathEnvironmentVariable = "ASPIRE_LAUNCHER_BACKCHANNEL_PATH";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var unixSocketPath = configuration.GetValue<string>(UnixSocketPathEnvironmentVariable);

        if (string.IsNullOrEmpty(unixSocketPath))
        {
            logger.LogDebug("Aspire CLI backchannel socket path was not specified.");
            return;
        }

        logger.LogDebug("Aspire CLI backchannel socket path: {SocketPath}", unixSocketPath);
        
        // Forcing to background.
        await Task.Yield();

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(unixSocketPath);
            await socket.ConnectAsync(endpoint, stoppingToken).ConfigureAwait(false);
            using var stream = new NetworkStream(socket, true);
            var rpc = JsonRpc.Attach(stream, appHostRpcTarget);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            do
            {
                var sendTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var responseTimestamp = await rpc.InvokeAsync<long>("PingAsync", sendTimestamp).ConfigureAwait(false);
                Debug.Assert(sendTimestamp == responseTimestamp);
                var roundtripMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sendTimestamp;
                logger.LogDebug("PingAsync round trip time is {RoundTripDuration} ms", roundtripMilliseconds);
            } while(await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (StreamJsonRpc.ConnectionLostException && stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Ignoring ConnectionLostException because of cancellation.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Shutting down CLI backchannel because of cancellation.");
            return;
        }
    }
}
