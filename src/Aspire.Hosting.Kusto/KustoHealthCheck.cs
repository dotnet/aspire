// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Kusto;

/// <summary>
/// A health check to validate that the Kusto service is available and responsive.
/// </summary>
internal sealed class KustoHealthCheck : IHealthCheck
{
    private readonly ICslQueryProvider _queryProvider;

    public KustoHealthCheck(ICslQueryProvider queryProvider)
    {
        ArgumentNullException.ThrowIfNull(queryProvider);

        _queryProvider = queryProvider;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            var clientRequestProperties = new ClientRequestProperties()
            {
                ClientRequestId = Guid.NewGuid().ToString(),
            };

            const string query = "print message = \"Hello, World!\"";
            using var reader = await _queryProvider.ExecuteQueryAsync(_queryProvider.DefaultDatabaseName, query, clientRequestProperties, cancellationToken).ConfigureAwait(false);

            if (reader.Read())
            {
                return HealthCheckResult.Healthy();
            }

            return HealthCheckResult.Unhealthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
