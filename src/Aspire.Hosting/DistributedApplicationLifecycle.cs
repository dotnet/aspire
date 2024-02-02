// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationLifecycle(ILogger<DistributedApplication> logger, IConfiguration configuration, IOptions<PublishingOptions> publishingOptions) : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = logger;
        _ = publishingOptions;
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        if (publishingOptions.Value.Publisher != "manifest")
        {
            logger.LogInformation("Distributed application started. Press CTRL-C to stop.");
        }

        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        if (publishingOptions.Value.Publisher != "manifest")
        {
            logger.LogInformation("Distributed application starting.");
            logger.LogInformation("Application host directory is: {AppHostDirectory}", configuration["AppHost:Directory"]);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
