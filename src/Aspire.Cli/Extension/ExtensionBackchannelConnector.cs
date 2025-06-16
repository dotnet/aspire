// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using Aspire.Cli.Backchannel;
using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Extension;

internal sealed class ExtensionBackchannelConnector(ILogger<ExtensionBackchannelConnector> logger, IExtensionBackchannel extensionBackchannel) : BackgroundService
{
    private readonly TaskCompletionSource _connectionSetupTcs = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = new Activity(nameof(ExtensionBackchannelConnector));

        var endpoint = Environment.GetEnvironmentVariable(KnownConfigNames.ExtensionEndpoint);
        Debug.Assert(endpoint is not null);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        var connectionAttempts = 0;
        logger.LogDebug("Starting backchannel connection to AppHost at {Endpoint}", endpoint);

        var startTime = DateTimeOffset.UtcNow;

        do
        {
            connectionAttempts++;

            try
            {
                await extensionBackchannel.ConnectAsync(endpoint, stoppingToken).ConfigureAwait(false);
                logger.LogDebug("Connected to ExtensionBackchannel at {Endpoint}", endpoint);
                _connectionSetupTcs.SetResult();
            }
            catch (SocketException ex)
            {
                var waitingFor = DateTimeOffset.UtcNow - startTime;
                if (waitingFor > TimeSpan.FromSeconds(10))
                {
                    logger.LogDebug("Slow polling for backchannel connection (attempt {ConnectionAttempts}), {SocketException}", connectionAttempts, ex);
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    // We don't want to spam the logs with our early connection attempts.
                }
            }
            catch (IncompatibilityException ex)
            {
                logger.LogError(
                    "The Aspire extension is incompatible with the CLI and must be updated to a version that supports the {RequiredCapability} capability.",
                    ex.RequiredCapability
                    );

                // If the app host is incompatible then there is no point
                // trying to reconnect, we should propogate the exception
                // up to the code that needs to back channel so it can display
                // and error message to the user.
                _connectionSetupTcs.SetException(ex);

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while trying to connect to the backchannel.");
                _connectionSetupTcs.SetException(ex);
                throw;
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);

        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<IExtensionBackchannel> WaitForConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancellationTask = Task.Delay(Timeout.Infinite, linkedCts.Token);
        var completedTask = Task.WhenAny(_connectionSetupTcs.Task, cancellationTask).ConfigureAwait(false);

        await completedTask;
        return extensionBackchannel;
    }
}
