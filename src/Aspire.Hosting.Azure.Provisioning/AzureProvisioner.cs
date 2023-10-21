// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

// Provisions azure resources for development purposes
internal sealed class AzureProvisioner(IOptions<PublishingOptions> options, IConfiguration configuration, IHostEnvironment environment, ILogger<AzureProvisioner> logger) : IDistributedApplicationLifecycleHook
{
    private const string AspireResourceNameTag = "aspire-resource-name";

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // TODO: Make this more general purpose
        if (options.Value.Publisher == "manifest")
        {
            return;
        }

        var azureResources = appModel.Resources.OfType<IAzureResource>();
        if (!azureResources.OfType<IAzureResource>().Any())
        {
            return;
        }

        try
        {
            await ProvisionAzureResources(configuration, environment, logger, azureResources, cancellationToken).ConfigureAwait(false);
        }
        catch (MissingConfigurationException ex)
        {
            logger.LogWarning(ex, "Required configuration is missing.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error provisioning Azure resources.");
        }
    }

    private async Task ProvisionAzureResources(IConfiguration configuration, IHostEnvironment environment, ILogger<AzureProvisioner> logger, IEnumerable<IAzureResource> azureResources, CancellationToken cancellationToken)
    {
        var credential = new DefaultAzureCredential();

        var subscriptionId = configuration["Azure:SubscriptionId"] ?? throw new MissingConfigurationException("An azure subscription id is required. Set the Azure:SubscriptionId configuration value.");
        var location = configuration["Azure:Location"] switch
        {
            null => throw new MissingConfigurationException("An azure location/region is required. Set the Azure:Location configuration value."),
            string loc => new AzureLocation(loc)
        };

        var armClient = new ArmClient(credential, subscriptionId);

        var subscriptionLazy = new Lazy<Task<SubscriptionResource>>(async () =>
        {
            logger.LogInformation("Getting default subscription...");

            var value = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Default subscription: {name} ({subscriptionId})", value.Data.DisplayName, value.Id);

            return value;
        });

        Lazy<Task<(ResourceGroupResource, AzureLocation)>> resourceGroupAndLocationLazy = new(async () =>
        {
            // Name of the resource group to create based on the machine name and application name
            var (resourceGroupName, createIfNoExists) = configuration["Azure:ResourceGroup"] switch
            {
                null => ($"{Environment.MachineName.ToLowerInvariant()}-{environment.ApplicationName.ToLowerInvariant()}-rg", true),
                string rg => (rg, false)
            };

            var subscription = await subscriptionLazy.Value.ConfigureAwait(false);

            var resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource? resourceGroup = null;
            AzureLocation? location = null;
            try
            {
                var response = await resourceGroups.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
                resourceGroup = response.Value;
                location = resourceGroup.Data.Location;

                logger.LogInformation("Using existing resource group {rgName}.", resourceGroup.Data.Name);
            }
            catch (Exception)
            {
                if (!createIfNoExists)
                {
                    throw;
                }

                // REVIEW: Is it possible to do this without an exception?

                logger.LogInformation("Creating resource group {rgName} in {location}...", resourceGroupName, location);

                var rgData = new ResourceGroupData(location!.Value);
                rgData.Tags.Add("aspire", "true");
                var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
                resourceGroup = operation.Value;

                logger.LogInformation("Resource group {rgName} created.", resourceGroup.Data.Name);
            }

            return (resourceGroup, location.Value);
        });

        var principalIdLazy = new Lazy<Task<Guid>>(async () => Guid.Parse(await GetUserPrincipalAsync(credential, cancellationToken).ConfigureAwait(false)));

        var resourceMapLazy = new Lazy<Task<Dictionary<string, ArmResource>>>(async () =>
        {
            var resourceMap = new Dictionary<string, ArmResource>();

            var (resourceGroup, _) = await resourceGroupAndLocationLazy.Value.ConfigureAwait(false);

            await PopulateExistingAspireResources(
                 resourceGroup,
                 (rg, token) => rg.GetKeyVaults().GetAllAsync(cancellationToken: token),
                 kv => kv.Data.Tags,
                 resourceMap,
                 cancellationToken).ConfigureAwait(false);

            await PopulateExistingAspireResources(
                resourceGroup,
                (rg, token) => rg.GetServiceBusNamespaces().GetAllAsync(cancellationToken: token),
                ns => ns.Data.Tags,
                resourceMap,
                cancellationToken).ConfigureAwait(false);

            await PopulateExistingAspireResources(
                resourceGroup,
                (rg, token) => rg.GetStorageAccounts().GetAllAsync(cancellationToken: token),
                sa => sa.Data.Tags,
                resourceMap,
                cancellationToken).ConfigureAwait(false);

            return resourceMap;
        });

        var tasks = new List<Task>();

        // Try to find the user secrets path
        // we're going to cache access tokens in the user secrets file
        // to speed up credential acquisition.
        static string? GetUserSecretsPath()
        {
            return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
            {
                null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
                string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
            };
        }

        var userSecretsPath = GetUserSecretsPath();

        ResourceGroupResource? resourceGroup = null;
        SubscriptionResource? subscription = null;
        Dictionary<string, ArmResource>? resourceMap = null;
        Guid? principalId = default;
        var usedResources = new HashSet<string>();

        var userSecrets = userSecretsPath is null ? [] : JsonNode.Parse(File.ReadAllText(userSecretsPath))!.AsObject();

        foreach (var c in azureResources)
        {
            usedResources.Add(c.Name);

            if (c is AzureStorageResource storage)
            {
                // Storage isn't a connection string because it has multiple endpoints
                var tableUrl = configuration[$"Azure:Storage:{storage.Name}:TableUri"];
                var blobUrl = configuration[$"Azure:Storage:{storage.Name}:BlobUri"];
                var queueUrl = configuration[$"Azure:Storage:{storage.Name}:QueueUri"];

                // If any of these is null then we need to create/get the storage account
                if (tableUrl is not null && blobUrl is not null && queueUrl is not null)
                {
                    logger.LogInformation("Using connection information stored in user secrets for {storageName}.", storage.Name);

                    storage.TableUri = new Uri(tableUrl);
                    storage.BlobUri = new Uri(blobUrl);
                    storage.QueueUri = new Uri(queueUrl);

                    continue;
                }

                subscription ??= await subscriptionLazy.Value.ConfigureAwait(false);

                if (resourceGroup is null)
                {
                    (resourceGroup, location) = await resourceGroupAndLocationLazy.Value.ConfigureAwait(false);
                }

                resourceMap ??= await resourceMapLazy.Value.ConfigureAwait(false);
                principalId ??= await principalIdLazy.Value.ConfigureAwait(false);

                var task = CreateStorageAccountAsync(armClient,
                    subscription,
                    resourceGroup,
                    resourceMap,
                    location,
                    storage,
                    principalId.Value,
                    userSecrets,
                    cancellationToken);

                tasks.Add(task);
            }

            if (c is AzureServiceBusResource serviceBus)
            {
                var serviceBusEndpoint = configuration.GetConnectionString(serviceBus.Name);

                if (serviceBusEndpoint is not null)
                {
                    logger.LogInformation("Using connection information stored in user secrets for {serviceBusName}.", serviceBus.Name);

                    serviceBus.ServiceBusEndpoint = serviceBusEndpoint;

                    continue;
                }

                subscription ??= await subscriptionLazy.Value.ConfigureAwait(false);

                if (resourceGroup is null)
                {
                    (resourceGroup, location) = await resourceGroupAndLocationLazy.Value.ConfigureAwait(false);
                }

                resourceMap ??= await resourceMapLazy.Value.ConfigureAwait(false);
                principalId ??= await principalIdLazy.Value.ConfigureAwait(false);

                var task = CreateServiceBusAsync(armClient,
                    await subscriptionLazy.Value.ConfigureAwait(false),
                    resourceGroup,
                    resourceMap,
                    location,
                    serviceBus,
                    principalId.Value,
                    userSecrets,
                    cancellationToken);

                tasks.Add(task);
            }

            if (c is AzureKeyVaultResource keyVault)
            {
                var vaultUri = configuration.GetConnectionString(keyVault.Name);

                if (vaultUri is not null)
                {
                    logger.LogInformation("Using connection information stored in user secrets for {keyVaultName}.", keyVault.Name);

                    keyVault.VaultUri = new(vaultUri);

                    continue;
                }

                subscription ??= await subscriptionLazy.Value.ConfigureAwait(false);

                if (resourceGroup is null)
                {
                    (resourceGroup, location) = await resourceGroupAndLocationLazy.Value.ConfigureAwait(false);
                }

                resourceMap ??= await resourceMapLazy.Value.ConfigureAwait(false);
                principalId ??= await principalIdLazy.Value.ConfigureAwait(false);

                var task = CreateKeyVaultAsync(armClient,
                    subscription,
                    resourceGroup,
                    resourceMap,
                    location,
                    keyVault,
                    principalId.Value,
                    userSecrets,
                    cancellationToken);

                tasks.Add(task);
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // If we created any resources then save the user secrets
            if (userSecretsPath is not null)
            {
                File.WriteAllText(userSecretsPath, userSecrets.ToString());

                logger.LogInformation("Azure resource connection strings saved to user secrets.");
            }
        }

        // Do this in the background to avoid blocking startup
        _ = Task.Run(async () =>
        {
            logger.LogInformation("Cleaning up unused resources...");

            resourceMap ??= await resourceMapLazy.Value.ConfigureAwait(false);

            // Clean up any left over resources that are no longer in the model
            foreach (var (name, sa) in resourceMap)
            {
                if (usedResources.Contains(name))
                {
                    continue;
                }

                var response = await armClient.GetGenericResources().GetAsync(sa.Id, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Deleting unused resource {keyVaultName} which maps to resource name {name}.", sa.Id, name);

                await response.Value.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
            }
        },
        cancellationToken);
    }

    private static async Task PopulateExistingAspireResources<TResource>(
        ResourceGroupResource resourceGroup,
        Func<ResourceGroupResource, CancellationToken, IAsyncEnumerable<TResource>> getCollection,
        Func<TResource, IDictionary<string, string>> getTags,
        Dictionary<string, ArmResource> map,
        CancellationToken cancellationToken)
        where TResource : ArmResource
    {
        await foreach (var r in getCollection(resourceGroup, cancellationToken))
        {
            var tags = getTags(r);
            if (tags.TryGetValue(AspireResourceNameTag, out var aspireResourceName))
            {
                map[aspireResourceName] = r;
            }
        }
    }

    private async Task CreateKeyVaultAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        AzureKeyVaultResource keyVault,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {
        resourceMap.TryGetValue(keyVault.Name, out var azureResource);

        if (azureResource is not null && azureResource is not KeyVaultResource)
        {
            logger.LogWarning("Resource {resourceName} is not a key vault resource. Deleting it.", keyVault.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var keyVaultResource = azureResource as KeyVaultResource;

        if (keyVaultResource is null)
        {
            // A vault's name must be between 3-24 alphanumeric characters. The name must begin with a letter, end with a letter or digit, and not contain consecutive hyphens.
            // Follow this link for more information: https://go.microsoft.com/fwlink/?linkid=2147742
            var vaultName = $"v{Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20]}";

            logger.LogInformation("Creating key vault {vaultName} in {location}...", vaultName, location);

            var properties = new KeyVaultProperties(subscription.Data.TenantId!.Value, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
            {
                EnabledForTemplateDeployment = true,
                EnableRbacAuthorization = true
            };
            var parameters = new KeyVaultCreateOrUpdateContent(location, properties);
            parameters.Tags.Add(AspireResourceNameTag, keyVault.Name);

            var operation = await resourceGroup.GetKeyVaults().CreateOrUpdateAsync(WaitUntil.Completed, vaultName, parameters, cancellationToken).ConfigureAwait(false);
            keyVaultResource = operation.Value;

            logger.LogInformation("Key vault {vaultName} created.", keyVaultResource.Data.Name);
        }

        keyVault.VaultUri = keyVaultResource.Data.Properties.VaultUri;

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[keyVault.Name] = keyVault.VaultUri.ToString();

        // Key Vault Administrator
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#key-vault-administrator
        var roleDefinitionId = CreateRoleDefinitionId(subscription, "00482a5a-887f-4fb3-b363-3b7fe8e74483");

        await DoRoleAssignmentAsync(armClient, keyVaultResource.Id, principalId, roleDefinitionId, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateServiceBusAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        AzureServiceBusResource resource,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {
        resourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not ServiceBusNamespaceResource)
        {
            logger.LogWarning("Resource {resourceName} is not a service bus namespace. Deleting it.", resource.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var serviceBusNamespace = azureResource as ServiceBusNamespaceResource;

        if (serviceBusNamespace is null)
        {
            // ^[a-zA-Z][a-zA-Z0-9-]*$
            var namespaceName = Guid.NewGuid().ToString();

            logger.LogInformation("Creating service bus namespace {namespace} in {location}...", namespaceName, location);

            var parameters = new ServiceBusNamespaceData(location);
            parameters.Tags.Add(AspireResourceNameTag, resource.Name);

            // Now we can create a storage account with defined account name and parameters
            var operation = await resourceGroup.GetServiceBusNamespaces().CreateOrUpdateAsync(WaitUntil.Completed, namespaceName, parameters, cancellationToken).ConfigureAwait(false);
            serviceBusNamespace = operation.Value;

            logger.LogInformation("Service bus namespace {namespace} created.", serviceBusNamespace.Data.Name);
        }

        // This is the full uri to the service bus namespace e.g https://namespace.servicebus.windows.net:443/
        // the connection strings for the app need the host name only
        resource.ServiceBusEndpoint = new Uri(serviceBusNamespace.Data.ServiceBusEndpoint).Host;

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.ServiceBusEndpoint;

        // Now create the queues
        var queues = serviceBusNamespace.GetServiceBusQueues();
        var topics = serviceBusNamespace.GetServiceBusTopics();

        var queuesToCreate = new HashSet<string>(resource.QueueNames);
        var topicsToCreate = new HashSet<string>(resource.TopicNames);

        // Delete unused queues
        await foreach (var sbQueue in queues.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (!resource.QueueNames.Contains(sbQueue.Data.Name))
            {
                logger.LogInformation("Deleting queue {queueName}", sbQueue.Data.Name);

                await sbQueue.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
            }

            // Don't need to create this queue
            queuesToCreate.Remove(sbQueue.Data.Name);
        }

        await foreach (var sbTopic in topics.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (!resource.TopicNames.Contains(sbTopic.Data.Name))
            {
                logger.LogInformation("Deleting topic {topicName}", sbTopic.Data.Name);

                await sbTopic.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
            }

            // Don't need to create this topic
            topicsToCreate.Remove(sbTopic.Data.Name);
        }

        // Create the remaining queues
        foreach (var queueName in queuesToCreate)
        {
            logger.LogInformation("Creating queue {queueName}...", queueName);

            await queues.CreateOrUpdateAsync(WaitUntil.Completed, queueName, new ServiceBusQueueData(), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Queue {queueName} created.", queueName);
        }

        // Create the remaining topics
        foreach (var topicName in topicsToCreate)
        {
            logger.LogInformation("Creating topic {topicName}...", topicName);

            await topics.CreateOrUpdateAsync(WaitUntil.Completed, topicName, new ServiceBusTopicData(), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Topic {topicName} created.", topicName);
        }

        // Azure Service Bus Data Owner
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#azure-service-bus-data-owner
        var roleDefinitionId = CreateRoleDefinitionId(subscription, "090c5cfd-751d-490a-894a-3ce6f1109419");

        await DoRoleAssignmentAsync(armClient, serviceBusNamespace.Id, principalId, roleDefinitionId, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateStorageAccountAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroupResource,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        AzureStorageResource resource,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {
        resourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not StorageAccountResource)
        {
            logger.LogWarning("Resource {resourceName} is not a storage account. Deleting it.", resource.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var storageAccount = azureResource as StorageAccountResource;

        if (storageAccount is null)
        {
            //  Storage account name must be between 3 and 24 characters in length and use numbers and lower-case letters only.
            var accountName = Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20];

            logger.LogInformation("Creating storage account {accountName} in {location}...", accountName, location);

            // First we need to define the StorageAccountCreateParameters
            var sku = new StorageSku(StorageSkuName.StandardGrs);
            var kind = StorageKind.Storage;
            var parameters = new StorageAccountCreateOrUpdateContent(sku, kind, location);
            parameters.Tags.Add(AspireResourceNameTag, resource.Name);

            // Now we can create a storage account with defined account name and parameters
            var accountCreateOperation = await resourceGroupResource.GetStorageAccounts().CreateOrUpdateAsync(WaitUntil.Completed, accountName, parameters, cancellationToken).ConfigureAwait(false);
            storageAccount = accountCreateOperation.Value;

            logger.LogInformation("Storage account {accountName} created.", storageAccount.Data.Name);
        }

        resource.BlobUri = storageAccount.Data.PrimaryEndpoints.BlobUri;
        resource.TableUri = storageAccount.Data.PrimaryEndpoints.TableUri;
        resource.QueueUri = storageAccount.Data.PrimaryEndpoints.QueueUri;

        var resourceEntry = userSecrets.Prop("Azure").Prop("Storage").Prop(resource.Name);
        resourceEntry["BlobUri"] = resource.BlobUri.ToString();
        resourceEntry["TableUri"] = resource.TableUri.ToString();
        resourceEntry["QueueUri"] = resource.QueueUri.ToString();

        // Storage Queue Data Contributor
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-queue-data-contributor
        var storageQueueDataContributorId = CreateRoleDefinitionId(subscription, "974c5e8b-45b9-4653-ba55-5f855dd0fb88");

        // Storage Table Data Contributor
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-table-data-contributor
        var storageDataContributorId = CreateRoleDefinitionId(subscription, "0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3");

        // Storage Blob Data Contributor
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor
        var storageBlobDataContributorId = CreateRoleDefinitionId(subscription, "81a9662b-bebf-436f-a333-f67b29880f12");

        var t0 = DoRoleAssignmentAsync(armClient, storageAccount.Id, principalId, storageQueueDataContributorId, cancellationToken);
        var t1 = DoRoleAssignmentAsync(armClient, storageAccount.Id, principalId, storageDataContributorId, cancellationToken);
        var t2 = DoRoleAssignmentAsync(armClient, storageAccount.Id, principalId, storageBlobDataContributorId, cancellationToken);

        await Task.WhenAll(t0, t1, t2).ConfigureAwait(false);
    }

    private static ResourceIdentifier CreateRoleDefinitionId(SubscriptionResource subscription, string roleDefinitionId) =>
        new($"{subscription.Id}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}");

    private async Task DoRoleAssignmentAsync(
        ArmClient armClient,
        ResourceIdentifier resourceId,
        Guid principalId,
        ResourceIdentifier roleDefinitionId,
        CancellationToken cancellationToken)
    {
        var roleAssignments = armClient.GetRoleAssignments(resourceId);
        await foreach (var ra in roleAssignments.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (ra.Data.PrincipalId == principalId &&
                ra.Data.RoleDefinitionId.Equals(roleDefinitionId))
            {
                return;
            }
        }

        logger.LogInformation("Assigning role {role} to {principalId}...", roleDefinitionId, principalId);

        var roleAssignmentInfo = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

        var roleAssignmentId = Guid.NewGuid().ToString();
        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, roleAssignmentInfo, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Role {role} assigned to {principalId}.", roleDefinitionId, principalId);
    }

    internal async Task<string> GetUserPrincipalAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        var response = await credential.GetTokenAsync(new(["https://graph.windows.net/.default"]), cancellationToken).ConfigureAwait(false);

        static string ParseToken(in AccessToken response)
        {
            // Parse the access token to get the user's object id (this is their principal id)

            var parts = response.Token.Split('.');
            var part = parts[1];
            var convertedToken = part.ToString().Replace('_', '/').Replace('-', '+');

            switch (part.Length % 4)
            {
                case 2:
                    convertedToken += "==";
                    break;
                case 3:
                    convertedToken += "=";
                    break;
            }
            var bytes = Convert.FromBase64String(convertedToken);
            Utf8JsonReader reader = new(bytes);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var header = reader.GetString();
                    if (header == "oid")
                    {
                        reader.Read();
                        return reader.GetString()!;
                    }
                    reader.Read();
                }
            }
            return string.Empty;
        }

        return ParseToken(response);
    }

    sealed class MissingConfigurationException(string message) : Exception(message)
    {

    }
}
