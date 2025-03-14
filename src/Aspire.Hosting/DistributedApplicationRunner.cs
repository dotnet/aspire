// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(ApplicationOrchestrator orchestrator, DcpHost dcpHost, IHostApplicationLifetime lifetime, DistributedApplicationExecutionContext executionContext, DistributedApplicationModel model, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (executionContext.IsRunMode)
        {
            await dcpHost.StartAsync(stoppingToken).ConfigureAwait(false);
            await orchestrator.RunApplicationAsync(stoppingToken).ConfigureAwait(false);
        }
        else if (executionContext.IsPublishMode)
        {
            var publisher = serviceProvider.GetRequiredKeyedService<IDistributedApplicationPublisher>(executionContext.PublisherName);
            await publisher.PublishAsync(model, stoppingToken).ConfigureAwait(false);
            lifetime.StopApplication();
        }
        else if (executionContext.IsInspectMode)
        {
            // No op for now.
        }
        else
        {
            throw new DistributedApplicationException($"Unexpected mode: {executionContext.Operation}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (executionContext.IsRunMode)
        {
            await orchestrator.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
