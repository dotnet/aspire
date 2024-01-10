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
            resource.ConnectionString = connectionString;
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
    }
}
