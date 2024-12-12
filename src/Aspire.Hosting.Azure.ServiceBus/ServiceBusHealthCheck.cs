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
    private readonly Func<string> _connectionStringFactory;
    private readonly Func<IEnumerable<string>> _queueNamesFactory;
    private readonly Func<IEnumerable<string>> _topicNamesFactory;

    public ServiceBusHealthCheck(Func<string> connectionStringFactory, Func<IEnumerable<string>> queueNamesFactory, Func<IEnumerable<string>> topicNamesFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionStringFactory);
        ArgumentNullException.ThrowIfNull(queueNamesFactory);
        ArgumentNullException.ThrowIfNull(topicNamesFactory);

        _connectionStringFactory = connectionStringFactory;
        _queueNamesFactory = queueNamesFactory;
        _topicNamesFactory = topicNamesFactory;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // ServiceBusAdministrationClient is not available in the emulator

        try
        {
            var serviceBusClient = new ServiceBusClient(_connectionStringFactory());

            foreach (var queueName in _queueNamesFactory())
            {
                var receiver = serviceBusClient.CreateReceiver(queueName);
                await receiver.PeekMessageAsync(1, cancellationToken).ConfigureAwait(false);
            }

            foreach (var topicName in _topicNamesFactory())
            {
                var receiver = serviceBusClient.CreateReceiver(topicName);
                await receiver.PeekMessageAsync(1, cancellationToken).ConfigureAwait(false);
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
