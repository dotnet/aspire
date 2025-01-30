// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// TODO: Use health check from AspNetCore.Diagnostics.HealthChecks once following PR released:
// https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/pull/2244
namespace Aspire.Elastic.Clients.Elasticsearch;

internal sealed class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _elasticsearchClient;

    public ElasticsearchHealthCheck(ElasticsearchClient elasticsearchClient)
    {
        ArgumentNullException.ThrowIfNull(elasticsearchClient, nameof(elasticsearchClient));
        _elasticsearchClient = elasticsearchClient;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var pingResult = await _elasticsearchClient.PingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            bool isSuccess = pingResult.ApiCallDetails.HttpStatusCode == 200;

            return isSuccess
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
