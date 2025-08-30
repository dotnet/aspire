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

internal sealed class BackchannelService(
    ILogger<BackchannelService> logger,
    IConfiguration configuration,
    AppHostRpcTarget appHostRpcTarget,
    IDistributedApplicationEventing eventing,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly List<JsonRpc> _activeConnections = [];
    private readonly object _connectionsLock = new();
    
    public bool IsBackchannelExpected => configuration.GetValue<string>(KnownConfigNames.UnixSocketPath) is {};

    private readonly TaskCompletionSource _backchannelConnectedTcs = new();

    public Task BackchannelConnected => _backchannelConnectedTcs.Task;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Socket? serverSocket = null;
        try
        {
            var unixSocketPath = configuration.GetValue<string>(KnownConfigNames.UnixSocketPath);

            if (string.IsNullOrEmpty(unixSocketPath))
            {
                logger.LogDebug("Backchannel socket path was not specified.");
                return;
            }

            logger.LogDebug("Listening for backchannel connection on socket path: {SocketPath}", unixSocketPath);
            serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(unixSocketPath);
            serverSocket.Bind(endpoint);
            serverSocket.Listen();

            var backchannelReadyEvent = new BackchannelReadyEvent(serviceProvider, unixSocketPath);
            await eventing.PublishAsync(
                backchannelReadyEvent,
                EventDispatchBehavior.NonBlockingConcurrent,
                stoppingToken).ConfigureAwait(false);

            var firstConnection = true;

            // Accept multiple connections in a loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var clientSocket = await serverSocket.AcceptAsync(stoppingToken).ConfigureAwait(false);
                    logger.LogDebug("Accepted new backchannel connection");

                    // Handle each connection in the background
                    _ = Task.Run(async () => await HandleConnectionAsync(clientSocket).ConfigureAwait(false), stoppingToken);

                    if (firstConnection)
                    {
                        firstConnection = false;
                        // NOTE: The DistributedApplicationRunner will await this TCS
                        //       when a backchannel is expected, and will not stop
                        //       the application itself - it will instead wait for
                        //       the CLI to stop the application explicitly.
                        _backchannelConnectedTcs.SetResult();

                        var backchannelConnectedEvent = new BackchannelConnectedEvent(serviceProvider, unixSocketPath);
                        await eventing.PublishAsync(
                            backchannelConnectedEvent,
                            EventDispatchBehavior.NonBlockingConcurrent,
                            stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Socket was disposed, likely due to cancellation
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Operation was cancelled
                    break;
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            // This exception is expected when the service is shut down whilst waiting for
            // a socket and just means that we don't need to wait anymore.
            logger.LogDebug("Backchannel service was cancelled: {Message}", ex.Message);
            return;
        }
        finally
        {
            serverSocket?.Dispose();
        }
    }

    private async Task HandleConnectionAsync(Socket clientSocket)
    {
        JsonRpc? rpc = null;
        try
        {
            var stream = new NetworkStream(clientSocket, true);
            rpc = JsonRpc.Attach(stream, appHostRpcTarget);

            lock (_connectionsLock)
            {
                _activeConnections.Add(rpc);
            }

            logger.LogDebug("Backchannel connection established");

            // Wait for the connection to close
            await rpc.Completion.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error handling backchannel connection");
        }
        finally
        {
            if (rpc is not null)
            {
                lock (_connectionsLock)
                {
                    _activeConnections.Remove(rpc);
                }

                rpc.Dispose();
            }

            logger.LogDebug("Backchannel connection closed");
        }
    }

    public override void Dispose()
    {
        lock (_connectionsLock)
        {
            foreach (var connection in _activeConnections)
            {
                connection.Dispose();
            }
            _activeConnections.Clear();
        }

        base.Dispose();
    }
}
