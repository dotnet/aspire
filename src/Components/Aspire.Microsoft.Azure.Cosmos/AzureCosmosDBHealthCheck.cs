// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Microsoft.Azure.Cosmos;

/// <summary>
/// A health check for Azure CosmosDB.
/// </summary>
internal sealed class AzureCosmosDBHealthCheck : IHealthCheck
{
    private readonly AzureDataCosmosSettings _settings;

    public AzureCosmosDBHealthCheck(AzureDataCosmosSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using (CosmosClient cosmosClient = new CosmosClient(_settings.ConnectionString))
            {
                var accountProperties = await cosmosClient.ReadAccountAsync().ConfigureAwait(false);
                if (accountProperties is not null)
                {
                    return HealthCheckResult.Healthy();
                }

                return new HealthCheckResult(context.Registration.FailureStatus, description: "DB account properties result is null");
            }

        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
