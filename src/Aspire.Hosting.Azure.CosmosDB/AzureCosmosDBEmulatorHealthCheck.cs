// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.CosmosDB;

/// <summary>
/// This health check also creates default databases and containers for the Azure CosmosDB Emulator.
/// </summary>
internal sealed class AzureCosmosDBEmulatorHealthCheck : IHealthCheck
{
    private readonly Func<CosmosClient> _clientFactory;
    private readonly Func<CosmosDBDatabase[]> _databasesFactory;
    private bool _resourcesCreated;

    public AzureCosmosDBEmulatorHealthCheck(Func<CosmosClient> clientFactory, Func<CosmosDBDatabase[]> databasesFactory)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(databasesFactory);

        _clientFactory = clientFactory;
        _databasesFactory = databasesFactory;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var cosmosClient = _clientFactory();

            await cosmosClient.ReadAccountAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

            // Create the databases and containers if they do not exist. This is only performed once.
            if (!_resourcesCreated)
            {
                var databases = _databasesFactory();

                foreach (var database in databases)
                {
                    var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync(database.Name, cancellationToken: cancellationToken).ConfigureAwait(false)).Database;

                    foreach (var container in database.Containers)
                    {
                        await db.CreateContainerIfNotExistsAsync(container.Name, container.PartitionKeyPath, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }

                _resourcesCreated = true;
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
