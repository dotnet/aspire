// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Cli;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(ILogger<DistributedApplicationRunner> logger, IHostApplicationLifetime lifetime, DistributedApplicationExecutionContext executionContext, DistributedApplicationModel model, IServiceProvider serviceProvider, IPublishingActivityProgressReporter activityReporter, IDistributedApplicationEventing eventing, BackchannelService backchannelService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (executionContext.IsPublishMode)
        {
            // If we are running in publish mode and are being driven by the
            // CLI we need to wait for the backchannel from the CLI to the
            // apphost to be connected so we can stream back publishing progress.
            // This code detects that a backchannel is expected - and if so
            // we block until the backchannel is connected and bound to the RPC target.
            if (backchannelService.IsBackchannelExpected)
            {
                logger.LogDebug("Waiting for backchannel connection before publishing.");
                await backchannelService.BackchannelConnected.ConfigureAwait(false);
            }

            var publishingActivity = await activityReporter.CreateActivityAsync(
                "publishing-artifacts",
                $"Publishing artifacts",
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

                await activityReporter.UpdateActivityStatusAsync(
                    publishingActivity,
                    (status) => status with { IsComplete = true },
                    stoppingToken).ConfigureAwait(false);

                // If we are running in publish mode and a backchannel is being
                // used then we don't want to stop the app host. Instead the
                // CLI will tell the app host to stop when it is done - and
                // if the CLI crashes then the orphan detector will kick in
                // and stop the app host.
                if (!backchannelService.IsBackchannelExpected)
                {
                    lifetime.StopApplication();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish the distributed application.");
                await activityReporter.UpdateActivityStatusAsync(
                    publishingActivity,
                    (status) => status with { IsError = true },
                    stoppingToken).ConfigureAwait(false);

                if (!backchannelService.IsBackchannelExpected)
                {
                     throw new DistributedApplicationException($"Publishing failed exception message: {ex.Message}", ex);
                }
            }
        }
    }
}
