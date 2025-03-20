// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Hosting.Cli;

internal sealed class BackchannelService(ILogger<BackchannelService> logger, IConfiguration configuration, AppHostRpcTarget appHostRpcTarget, IDistributedApplicationEventing eventing) : BackgroundService
{
    private const string UnixSocketPathEnvironmentVariable = "ASPIRE_BACKCHANNEL_PATH";
    private readonly List<JsonRpc> _rpcs = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var unixSocketPath = configuration.GetValue<string>(UnixSocketPathEnvironmentVariable);

        if (string.IsNullOrEmpty(unixSocketPath))
        {
            logger.LogDebug("Backchannel socket path was not specified.");
            return;
        }

        logger.LogDebug("Listening for backchannel connection on socket path: {SocketPath}", unixSocketPath);
        var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(unixSocketPath);
        serverSocket.Bind(endpoint);
        serverSocket.Listen();

        var backchannelReadyEvent = new BackchannelReadyEvent(unixSocketPath);
        await eventing.PublishAsync(
            backchannelReadyEvent,
            EventDispatchBehavior.NonBlockingConcurrent,
            stoppingToken).ConfigureAwait(false);

        do
        {
            var clientSocket = await serverSocket.AcceptAsync(stoppingToken).ConfigureAwait(false);
            var stream = new NetworkStream(clientSocket, true);
            var rpc = JsonRpc.Attach(stream, appHostRpcTarget);
            _rpcs.Add(rpc);

            var backchannelConnectedEvent = new BackchannelConnectedEvent(unixSocketPath);
            await eventing.PublishAsync(
                backchannelConnectedEvent,
                EventDispatchBehavior.NonBlockingConcurrent,
                stoppingToken).ConfigureAwait(false);

            logger.LogDebug("Accepted backchannel connection from {RemoteEndPoint}", clientSocket.RemoteEndPoint);
        } while (!stoppingToken.IsCancellationRequested);
    }
}
