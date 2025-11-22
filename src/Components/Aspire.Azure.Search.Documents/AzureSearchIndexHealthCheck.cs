// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Search.Documents;

/// <summary>
/// Performs a health check on an Azure Cognitive Search index by verifying the availability of the search service.
/// </summary>
/// <remarks>This health check attempts to retrieve service statistics from the associated Azure Cognitive Search
/// index to determine if the service is responsive. It is intended for use with health monitoring frameworks to report
/// the operational status of the search index. The check does not validate the existence or state of specific documents
/// within the index.</remarks>
internal sealed class AzureSearchIndexHealthCheck : IHealthCheck
{
    private readonly SearchIndexClient _searchIndexClient;

    public AzureSearchIndexHealthCheck(SearchIndexClient indexClient)
    {
#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
        ArgumentNullException.ThrowIfNull(indexClient, nameof(indexClient));
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly
        _searchIndexClient = indexClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _searchIndexClient.GetServiceStatisticsAsync(cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
