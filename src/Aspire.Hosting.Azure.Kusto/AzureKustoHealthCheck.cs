// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// A health check to validate that the Kusto service is available and responsive.
/// </summary>
internal sealed class AzureKustoHealthCheck : IHealthCheck
{
    private readonly KustoConnectionStringBuilder _kcsb;

    public AzureKustoHealthCheck(KustoConnectionStringBuilder connectionStringBuilder)
    {
        ArgumentNullException.ThrowIfNull(connectionStringBuilder);

        _kcsb = connectionStringBuilder;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            const string query = "print message = \"Hello, World!\"";
            var clientRequestProperties = new ClientRequestProperties()
            {
                ClientRequestId = Guid.NewGuid().ToString(),
            };
            var client = KustoClientFactory.CreateCslQueryProvider(_kcsb);
            using var reader = await client.ExecuteQueryAsync(client.DefaultDatabaseName, query, clientRequestProperties, cancellationToken).ConfigureAwait(false);

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
