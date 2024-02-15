// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
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
        if (GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion is string informationalVersion)
        {
            // Display version and commit like 8.0.0-preview.2.23619.3+17dd83f67c6822954ec9a918ef2d048a78ad4697
            logger.LogInformation("Version: {InformationalVersion}", informationalVersion);
        }

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
