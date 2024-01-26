// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Data.Cosmos;
using Azure;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class AzureCosmosDBProvisioner(ILogger<AzureCosmosDBProvisioner> logger) : AzureResourceProvisioner<AzureCosmosDBResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureCosmosDBResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string connectionString)
        {
            if (resource.Databases.Count == 0)
            {
                resource.ConnectionString = connectionString;
                return true;
            }

            foreach (var database in resource.Databases)
            {
                if (configuration.GetConnectionString(database.Name) is string databaseConnectionString)
                {
                    database.ConnectionString = databaseConnectionString;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        AzureCosmosDBResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
    {
        context.ResourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not CosmosDBAccountResource)
        {
            logger.LogWarning("Resource {resourceName} is not a Cosmos DB resource. Deleting it.", resource.Name);

            await context.ArmClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var cosmosResource = azureResource as CosmosDBAccountResource;

        if (cosmosResource is null)
        {
            var cosmosDbName = Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20];

            logger.LogInformation("Creating CosmosDB {cosmosDbName} in {location}...", cosmosDbName, context.Location);

            var cosmosDbCreateOrUpdateContent = new CosmosDBAccountCreateOrUpdateContent(
                context.Location,
                new CosmosDBAccountLocation[]
                {
                    new CosmosDBAccountLocation()
                    {
                        LocationName = context.Location.Name,
                        FailoverPriority = 0
                    }
                }
                );
            cosmosDbCreateOrUpdateContent.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

            var sw = ValueStopwatch.StartNew();

            var operation = await context.ResourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(WaitUntil.Completed, cosmosDbName, cosmosDbCreateOrUpdateContent, cancellationToken).ConfigureAwait(false);
            cosmosResource = operation.Value;

            logger.LogInformation("Cosmos DB {cosmosDbName} created in {elapsed}", cosmosResource.Data.Name, sw.GetElapsedTime());
        }

        // This must be an explicit call to get the keys
        var keysOperation = await cosmosResource.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var keys = keysOperation.Value;

        // REVIEW: Do we need to use the port?
        resource.ConnectionString = $"AccountEndpoint={cosmosResource.Data.DocumentEndpoint};AccountKey={keys.PrimaryMasterKey};";

        var connectionStrings = context.UserSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.ConnectionString;

        var existingDatabases = cosmosResource.GetCosmosDBSqlDatabases().GetAllAsync(cancellationToken);

        await foreach (var existingDatabase in existingDatabases)
        {
            if (!resource.Databases.Any(d => d.Name == existingDatabase.Data.Name))
            {
                logger.LogInformation("Deleting database {DatabaseName}", existingDatabase.Data.Name);
                await existingDatabase.DeleteAsync(WaitUntil.Completed, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var database in resource.Databases)
        {
            await CreateDatabaseIfNotExists(cosmosResource, database, cancellationToken).ConfigureAwait(false);
            connectionStrings[database.Name] = resource.ConnectionString; // Same as parent resource.
        }
    }

    private async Task CreateDatabaseIfNotExists(
        CosmosDBAccountResource cosmosResource,
        AzureCosmosDBDatabaseResource database,
        CancellationToken cancellationToken)
    {

        var exists = await cosmosResource.GetCosmosDBSqlDatabases().ExistsAsync(database.Name, cancellationToken).ConfigureAwait(false);

        if (!exists)
        {
            logger.LogInformation("Creating Cosmos DB SQL database {CosmosAccountName}/{CosmosDatabaseName} in {Location}", cosmosResource.Data.Name, database.Name, cosmosResource.Data.Location);

            var cosmosDatabaseContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(cosmosResource.Data.Location, new CosmosDBSqlDatabaseResourceInfo(database.Name));
            cosmosDatabaseContent.Tags.Add(AzureProvisioner.AspireResourceNameTag, database.Name);

            var sw = ValueStopwatch.StartNew();
            var operation = await cosmosResource.GetCosmosDBSqlDatabases()
                                                .CreateOrUpdateAsync(WaitUntil.Completed, database.Name, cosmosDatabaseContent, cancellationToken)
                                                .ConfigureAwait(false);
            var cosmosDatabaseResource = operation.Value;

            logger.LogInformation("Cosmos DB SQL database {CosmosAccountName}/{CosmosDatabaseName} created in {Elapsed}", cosmosResource.Data.Name, cosmosDatabaseResource.Data.Name, sw.GetElapsedTime());
        }
    }
}
