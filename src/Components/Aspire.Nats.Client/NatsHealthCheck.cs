// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using NATS.Client.Core;

namespace Aspire.Nats.Client;

public class NatsHealthCheck(INatsConnection connection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(connection.ConnectionState == NatsConnectionState.Open
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy());
    }
}
