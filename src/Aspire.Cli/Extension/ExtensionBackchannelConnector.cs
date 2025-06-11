// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Extension;

internal sealed class ExtensionBackchannelConnector(ILogger<ExtensionBackchannelConnector> logger, ExtensionBackchannel extensionBackchannel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = new Activity(nameof(ExtensionBackchannelConnector));
        var endpoint = Environment.GetEnvironmentVariable(KnownConfigNames.ExtensionEndpoint);

        Debug.Assert(endpoint is not null);
        logger.LogInformation("Attempting to connect to ExtensionBackchannel at {Endpoint}", endpoint);

        try
        {
            await extensionBackchannel.ConnectAsync(endpoint, stoppingToken).ConfigureAwait(false);
            logger.LogInformation("Connected to ExtensionBackchannel at {Endpoint}", endpoint);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to ExtensionBackchannel at {Endpoint}", endpoint);
        }
    }
}
