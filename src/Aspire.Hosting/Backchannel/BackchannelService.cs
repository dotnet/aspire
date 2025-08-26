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
    private JsonRpc? _rpc;
    private string? _socketPath;
    
    public bool IsBackchannelExpected => configuration.GetValue<string>(KnownConfigNames.UnixSocketPath) is {};

    private readonly TaskCompletionSource _backchannelConnectedTcs = new();

    public Task BackchannelConnected => _backchannelConnectedTcs.Task;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var unixSocketPath = configuration.GetValue<string>(KnownConfigNames.UnixSocketPath);

            if (string.IsNullOrEmpty(unixSocketPath))
            {
                logger.LogDebug("Backchannel socket path was not specified.");
                return;
            }

            _socketPath = unixSocketPath;

            logger.LogDebug("Listening for backchannel connection on socket path: {SocketPath}", unixSocketPath);
            var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(unixSocketPath);
            serverSocket.Bind(endpoint);
            serverSocket.Listen();

            var backchannelReadyEvent = new BackchannelReadyEvent(serviceProvider, unixSocketPath);
            await eventing.PublishAsync(
                backchannelReadyEvent,
                EventDispatchBehavior.NonBlockingConcurrent,
                stoppingToken).ConfigureAwait(false);

            // Accept the first connection and mark backchannel as ready
            var firstClientSocket = await serverSocket.AcceptAsync(stoppingToken).ConfigureAwait(false);
            var firstStream = new NetworkStream(firstClientSocket, true);
            var firstRpc = JsonRpc.Attach(firstStream, appHostRpcTarget);
            _rpc = firstRpc;

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

            // Continue accepting additional connections for transient commands
            // We don't await this task to avoid blocking the first connection
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var clientSocket = await serverSocket.AcceptAsync(stoppingToken).ConfigureAwait(false);
                        logger.LogDebug("Accepted additional backchannel connection");
                        
                        // Handle each additional connection on a separate task
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var stream = new NetworkStream(clientSocket, true);
                                var rpc = JsonRpc.Attach(stream, appHostRpcTarget);
                                await rpc.Completion.ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger.LogDebug(ex, "Additional backchannel connection closed");
                            }
                        }, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Stopped accepting additional backchannel connections");
                }
            }, stoppingToken);
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
            // Clean up socket file when service stops
            if (_socketPath is not null)
            {
                try
                {
                    if (File.Exists(_socketPath))
                    {
                        File.Delete(_socketPath);
                        logger.LogDebug("Cleaned up backchannel socket file: {SocketPath}", _socketPath);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to clean up backchannel socket file: {SocketPath}", _socketPath);
                }
            }
        }
    }
}
