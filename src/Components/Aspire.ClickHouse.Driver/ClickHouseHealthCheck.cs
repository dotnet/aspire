// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.ClickHouse.Driver;

internal sealed class ClickHouseHealthCheck : IHealthCheck
{
    private readonly ClickHouseDataSource _dataSource;

    public ClickHouseHealthCheck(ClickHouseDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dataSource.GetClient().PingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return result
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("ClickHouse ping returned unsuccessful response.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ClickHouse health check failed.", ex);
        }
    }
}
