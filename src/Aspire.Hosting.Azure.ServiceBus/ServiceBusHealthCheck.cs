// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.ServiceBus;

/// <summary>
/// The standard ServiceBus health check is not sufficient since it requires predefined knowledge that there is at least one queue or one topic.
/// At the time we register the health check, we may not know if there are any of each.
/// </summary>
internal sealed class ServiceBusHealthCheck : IHealthCheck
{
    private readonly Func<ServiceBusClient> _clientFactory;
    private readonly Func<Resource?> _nameFactory;

    public ServiceBusHealthCheck(Func<ServiceBusClient> clientFactory, Func<Resource?> nameFactory)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(nameFactory);

        _clientFactory = clientFactory;
        _nameFactory = nameFactory;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // The emulator doesn't support ServiceBusAdministrationClient which would be the preferred method to check the health of the service.
        // A ServiceBusClient is used instead.

        try
        {
            var serviceBusClient = _clientFactory();

            // We can assume that if a queue/topic is up and working then all queues or topics are too

            var queueOrTopicName = _nameFactory();

            if (queueOrTopicName is null)
            {
                return HealthCheckResult.Healthy();
            }

            if (queueOrTopicName is AzureServiceBusQueueResource azureServiceBusQueueResource)
            {
                var receiver = serviceBusClient.CreateReceiver(azureServiceBusQueueResource.QueueName);
                await receiver.PeekMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            if (queueOrTopicName is AzureServiceBusTopicResource azureServiceBusTopicResource
                && azureServiceBusTopicResource.Subscriptions.Count != 0)
            {
                var receiver = serviceBusClient.CreateReceiver(azureServiceBusTopicResource.TopicName,
                    azureServiceBusTopicResource.Subscriptions[0].SubscriptionName);

                await receiver.PeekMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
