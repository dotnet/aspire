// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.SignalR;
internal sealed class AzureSignalRHealthCheck : IHealthCheck
{
    private readonly ServiceManager _signalRClient;

    public AzureSignalRHealthCheck(ServiceManager signalRClient)
    {
        ArgumentNullException.ThrowIfNull(signalRClient ,nameof(signalRClient));
        _signalRClient = signalRClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _signalRClient.IsServiceHealthy(cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
