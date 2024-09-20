// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Meilisearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Meilisearch;

internal sealed class MeilisearchHealthCheck : IHealthCheck
{
    private readonly MeilisearchClient _meilisearchClient;

    public MeilisearchHealthCheck(MeilisearchClient meilisearchClient)
    {
        ArgumentNullException.ThrowIfNull(meilisearchClient, nameof(meilisearchClient));
        _meilisearchClient = meilisearchClient;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _meilisearchClient.IsHealthyAsync(cancellationToken).ConfigureAwait(false);

            return isHealthy
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
