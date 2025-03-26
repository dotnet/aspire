// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(ILogger<DistributedApplicationRunner> logger, IHostApplicationLifetime lifetime, DistributedApplicationExecutionContext executionContext, DistributedApplicationModel model, IServiceProvider serviceProvider, IPublishingActivityProgressReporter activityReporter) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (executionContext.IsPublishMode)
        {
            var publishingActivity = await activityReporter.CreateActivityAsync(
                "publishing-artifacts",
                $"Executing publisher {executionContext.PublisherName}",
                isPrimary: true,
                stoppingToken).ConfigureAwait(false);

            try
            {
                var publisher = serviceProvider.GetRequiredKeyedService<IDistributedApplicationPublisher>(executionContext.PublisherName);
                await publisher.PublishAsync(model, stoppingToken).ConfigureAwait(false);

                publishingActivity.IsComplete = true;
                await activityReporter.UpdateActivityAsync(publishingActivity, stoppingToken).ConfigureAwait(false);

                lifetime.StopApplication();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish the distributed application.");
                publishingActivity.IsError = true;
                await activityReporter.UpdateActivityAsync(publishingActivity, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
