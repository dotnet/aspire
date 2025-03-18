// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Devcontainers.Codespaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Hosting.Cli;

internal sealed class CliBackchannel(ILogger<CliBackchannel> logger, IConfiguration configuration, AppHostRpcTarget appHostRpcTarget, ResourceNotificationService resourceNotificationService, CodespacesUrlRewriter urlRewriter) : BackgroundService
{
    private const string UnixSocketPathEnvironmentVariable = "ASPIRE_LAUNCHER_BACKCHANNEL_PATH";

    private bool _dashboardUrlsReady;
    private bool _dashboardUrlsSent;

    private string? _dashboardBaseUrl;

    private string? _dashboardLoginUrl;

    public void SetDashboardUrls(string baseUrl, string? browserToken)
    {
        // Always store the base URL without the token, we use
        // this for the resource table in the CLI to make sure
        // that port forwarding in Codespaces works correctly.
        _dashboardBaseUrl = baseUrl;

        var uri = new Uri(baseUrl);
        var dashboardLoginUrl = browserToken switch {
            { } token => $"{uri.GetLeftPart(UriPartial.Authority)}/login?t={token}",
            _ => baseUrl
        };

        _dashboardLoginUrl = urlRewriter.RewriteUrl(dashboardLoginUrl);
        _dashboardUrlsReady = true;
    }

    private async Task ForwardSundriesAsync(JsonRpc rpc, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        do
        {
            if (_dashboardUrlsReady && !_dashboardUrlsSent)
            {
                logger.LogDebug(
                    "Sending dashboard base URL of {BaseUrl} and dashboard login URL of {LoginUrl} via CLI backchannel.",
                    _dashboardBaseUrl,
                    _dashboardLoginUrl
                );

                await rpc.InvokeAsync(
                    "UpdateDashboardUrlsAsync",
                    _dashboardBaseUrl,
                    _dashboardLoginUrl).ConfigureAwait(false);

                _dashboardUrlsSent = true;
            }
        } while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    private async Task ForwardResourceStatesAsync(JsonRpc rpc, CancellationToken cancellationToken)
    {
        var resourceEvents = resourceNotificationService.WatchAsync(cancellationToken);

        await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
        {
            if (resourceEvent.Resource.Name == KnownResourceNames.AspireDashboard)
            {
                logger.LogDebug("Ignoring resource event for {ResourceName}", KnownResourceNames.AspireDashboard);
                continue;
            }

            await rpc.InvokeAsync(
                "UpdateResourceAsync",
                resourceEvent.Resource.Name,
                resourceEvent.Resource.GetType().Name,
                resourceEvent.Snapshot?.State?.Text ?? string.Empty,
                resourceEvent.Snapshot?.Urls.Select(u => u.Url).ToArray()).ConfigureAwait(false);
        }
    }

    private async Task SendPingsAsync(JsonRpc rpc, CancellationToken cancellationToken)
    {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            do
            {
                var sendTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                logger.LogDebug("Sending PingAsync to CLI backchannel.");

                var responseTimestamp = await rpc.InvokeAsync<long>("PingAsync", sendTimestamp).ConfigureAwait(false);
                Debug.Assert(sendTimestamp == responseTimestamp);
                var roundtripMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sendTimestamp;

                logger.LogDebug("CLI PingAsync round trip time is {RoundTripDuration} ms", roundtripMilliseconds);
            } while(await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

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

            logger.LogDebug("Connecting to backchannel socket at {SocketPath}", unixSocketPath);

            await socket.ConnectAsync(endpoint, stoppingToken).ConfigureAwait(false);

            logger.LogDebug("Connected to backchannel socket at {SocketPath}", unixSocketPath);

            using var stream = new NetworkStream(socket, true);
            var rpc = JsonRpc.Attach(stream, appHostRpcTarget);

            var pendingSendPings = SendPingsAsync(rpc, stoppingToken);
            var pendingForwardResourceStates = ForwardResourceStatesAsync(rpc, stoppingToken);
            var pendingForwardSundries = ForwardSundriesAsync(rpc, stoppingToken);

            await Task.WhenAny(
                pendingSendPings, // Independent of anything else.
                pendingForwardResourceStates, // Dedicated to resource events.
                pendingForwardSundries // Everything else.
                ).ConfigureAwait(false);
        }
        catch (StreamJsonRpc.ConnectionLostException ex) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug(ex, "Ignoring ConnectionLostException because of cancellation.");
            return;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Shutting down CLI backchannel because of cancellation.");
            return;
        }
    }
}
