// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A health check to validate that the Kusto service is available and responsive.
/// </summary>
internal sealed class AzureKustoHealthCheck : IHealthCheck
{
    private readonly KustoConnectionStringBuilder _kcsb;
    private readonly bool _isClusterCheck;

    private static readonly ClientRequestProperties s_defaultClientRequestProperties = GetClientRequestProperties();

    public AzureKustoHealthCheck(KustoConnectionStringBuilder connectionStringBuilder, bool isClusterCheck)
    {
        _kcsb = connectionStringBuilder;
        _isClusterCheck = isClusterCheck;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (_isClusterCheck)
            {
                return await CheckClusterHealthAsync().ConfigureAwait(false);
            }
            else
            {
                return await CheckDatabaseHealthAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }

    private async Task<HealthCheckResult> CheckClusterHealthAsync()
    {
        using var queryProvider = KustoClientFactory.CreateCslAdminProvider(_kcsb);

        var results = await queryProvider.ExecuteControlCommandAsync<string>(".show version", s_defaultClientRequestProperties).ConfigureAwait(false);
        if (results.Any())
        {
            return HealthCheckResult.Healthy();
        }

        return HealthCheckResult.Unhealthy();
    }

    private async Task<HealthCheckResult> CheckDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        const string query = "print message = \"Hello, World!\"";

        var client = KustoClientFactory.CreateCslQueryProvider(_kcsb);
        using var reader = await client.ExecuteQueryAsync(client.DefaultDatabaseName, query, s_defaultClientRequestProperties, cancellationToken).ConfigureAwait(false);
        if (reader.Read())
        {
            return HealthCheckResult.Healthy();
        }
        return HealthCheckResult.Unhealthy();
    }

    private static ClientRequestProperties GetClientRequestProperties()
    {
        var clientRequestProps = new ClientRequestProperties();
        clientRequestProps.SetOption("client_timeout", TimeSpan.FromSeconds(30));
        return clientRequestProps;
    }
}
