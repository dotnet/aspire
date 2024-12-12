// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private readonly Func<IEnumerable<string>> _queueNamesFactory;
    private readonly Func<IEnumerable<string>> _topicNamesFactory;

    private ServiceBusClient? _serviceBusClient;

    public ServiceBusHealthCheck(Func<ServiceBusClient> clientFactory, Func<IEnumerable<string>> queueNamesFactory, Func<IEnumerable<string>> topicNamesFactory)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(queueNamesFactory);
        ArgumentNullException.ThrowIfNull(topicNamesFactory);

        _clientFactory = clientFactory;
        _queueNamesFactory = queueNamesFactory;
        _topicNamesFactory = topicNamesFactory;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // ServiceBusAdministrationClient is not available in the emulator

        try
        {
            _serviceBusClient ??= _clientFactory();

            foreach (var queueName in _queueNamesFactory())
            {
                var receiver = _serviceBusClient.CreateReceiver(queueName);
                await receiver.PeekMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            foreach (var topicName in _topicNamesFactory())
            {
                var receiver = _serviceBusClient.CreateReceiver(topicName);
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
