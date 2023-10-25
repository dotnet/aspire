// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using RedisArmResource = Azure.ResourceManager.Redis.RedisResource;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class AzureRedisProvisioner(ILogger<AzureRedisProvisioner> logger) : AzureResourceProvisioner<AzureRedisResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureRedisResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string connectionString)
        {
            resource.ConnectionString = connectionString;
            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        AzureRedisResource resource,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {

        resourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not RedisArmResource)
        {
            logger.LogWarning("Resource {resourceName} is not a redis resource. Deleting it.", resource.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var redisResource = azureResource as RedisArmResource;

        if (redisResource is null)
        {
            var redisName = Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20];

            logger.LogInformation("Creating redis {redisName} in {location}...", redisName, location);

            var redisCreateOrUpdateContent = new RedisCreateOrUpdateContent(location, new RedisSku(RedisSkuName.Basic, RedisSkuFamily.BasicOrStandard, 0));
            redisCreateOrUpdateContent.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

            var sw = Stopwatch.StartNew();
            var operation = await resourceGroup.GetAllRedis().CreateOrUpdateAsync(WaitUntil.Completed, redisName, redisCreateOrUpdateContent, cancellationToken).ConfigureAwait(false);
            redisResource = operation.Value;
            sw.Stop();

            logger.LogInformation("Redis {redisName} created in {elapsed}", redisResource.Data.Name, sw.Elapsed);
        }

        // This must be an explicit call to get the keys
        var keysOperation = await redisResource.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var keys = keysOperation.Value;

        // REVIEW: Do we need to use the port?
        resource.ConnectionString = $"{redisResource.Data.HostName},ssl=true,password={keys.PrimaryKey}";

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.ConnectionString;
    }
}
