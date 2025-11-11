// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Search.Documents;

/// <summary>
/// Performs a health check for an Azure Cognitive Search indexer by verifying the availability of the search service.
/// </summary>
/// <remarks>This health check is intended for use with ASP.NET Core health monitoring. It attempts to retrieve
/// service statistics from the Azure Cognitive Search service to determine if the indexer is accessible and
/// operational. If the service is unavailable or an error occurs, the health check reports an unhealthy
/// status.</remarks>
internal sealed class AzureSearchIndexerHealthCheck : IHealthCheck
{
    private readonly SearchIndexerClient _searchIndexerClient;

    public AzureSearchIndexerHealthCheck(SearchIndexerClient indexerClient)
    {
#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
        ArgumentNullException.ThrowIfNull(indexerClient, nameof(indexerClient));
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly
        _searchIndexerClient = indexerClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _searchIndexerClient.GetDataSourceConnectionNamesAsync(cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    exception: ex
                );
        }
    }
}
