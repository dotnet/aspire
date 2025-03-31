// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(ILogger<DistributedApplicationRunner> logger, IHostApplicationLifetime lifetime, DistributedApplicationExecutionContext executionContext, DistributedApplicationModel model, IServiceProvider serviceProvider, IPublishingActivityProgressReporter activityReporter, IDistributedApplicationEventing eventing) : BackgroundService
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
                await eventing.PublishAsync<BeforePublishEvent>(
                    new BeforePublishEvent(serviceProvider, model), stoppingToken
                    ).ConfigureAwait(false);

                var publisher = serviceProvider.GetRequiredKeyedService<IDistributedApplicationPublisher>(executionContext.PublisherName);
                await publisher.PublishAsync(model, stoppingToken).ConfigureAwait(false);

                await eventing.PublishAsync<AfterPublishEvent>(
                    new AfterPublishEvent(serviceProvider, model), stoppingToken
                    ).ConfigureAwait(false);

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
