// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

// Provisions azure resources for development purposes
internal sealed class AzureProvisioner(IOptions<PublishingOptions> options, IConfiguration configuration, IHostEnvironment environment, ILogger<AzureProvisioner> logger) : IDistributedApplicationLifecycleHook
{
    private const string Key = "aspire-resource-name";

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

        logger.LogInformation("Getting default subscription...");

        var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscription.Data.DisplayName, subscription.Id);

        // Name of the resource group to create based on the machine name and application name
        var (resourceGroupName, createIfNoExists) = configuration["Azure:ResourceGroup"] switch
        {
            null => ($"{Environment.MachineName.ToLowerInvariant()}-{environment.ApplicationName.ToLowerInvariant()}-rg", true),
            string rg => (rg, false)
        };

        var resourceGroups = subscription.GetResourceGroups();
        ResourceGroupResource? resourceGroup;

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

            var rgData = new ResourceGroupData(location);
            rgData.Tags.Add("aspire", "true");
            var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
            resourceGroup = operation.Value;

            logger.LogInformation("Resource group {rgName} created.", resourceGroup.Data.Name);
        }

        var principalId = Guid.Parse(await GetUserPrincipalAsync(credential, cancellationToken).ConfigureAwait(false));

        // Create a dictionary from resource name to StorageAccountResource using the tag aspire-resource-name to find the storage account
        var storageAccounts = resourceGroup.GetStorageAccounts();
        var resourceNameToStorageAccountMap = new Dictionary<string, StorageAccountResource>();

        await foreach (var a in storageAccounts.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (a.Data.Tags.TryGetValue("aspire-resource-name", out var aspireName))
            {
                resourceNameToStorageAccountMap.Add(aspireName, a);
            }
        }

        var serviceBusNamespaces = resourceGroup.GetServiceBusNamespaces();
        var resourceNameToServiceBusNamespaceMap = new Dictionary<string, ServiceBusNamespaceResource>();

        await foreach (var ns in serviceBusNamespaces.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (ns.Data.Tags.TryGetValue("aspire-resource-name", out var aspireName))
            {
                resourceNameToServiceBusNamespaceMap.Add(aspireName, ns);
            }
        }

        var keyVaults = resourceGroup.GetKeyVaults();
        var resourceNameToKeyVaultMap = new Dictionary<string, KeyVaultResource>();

        await foreach (var kv in keyVaults.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (kv.Data.Tags.TryGetValue(Key, out var aspireName))
            {
                resourceNameToKeyVaultMap.Add(aspireName, kv);
            }
        }

        var tasks = new List<Task>();

        foreach (var c in azureResources)
        {
            if (c is AzureStorageResource storage)
            {
                var task = CreateStorageAccountAsync(armClient,
                    subscription,
                    storageAccounts,
                    resourceNameToStorageAccountMap,
                    location,
                    storage,
                    principalId,
                    cancellationToken);

                tasks.Add(task);

                resourceNameToStorageAccountMap.Remove(c.Name);
            }

            if (c is AzureServiceBusResource serviceBus)
            {
                var task = CreateServiceBusAsync(armClient,
                    subscription,
                    serviceBusNamespaces,
                    resourceNameToServiceBusNamespaceMap,
                    location,
                    serviceBus,
                    principalId,
                    cancellationToken);

                tasks.Add(task);

                resourceNameToServiceBusNamespaceMap.Remove(c.Name);
            }

            if (c is AzureKeyVaultResource keyVault)
            {
                var task = CreateKeyVaultAsync(armClient,
                    subscription,
                    keyVaults,
                    resourceNameToKeyVaultMap,
                    location,
                    keyVault,
                    principalId,
                    cancellationToken);

                tasks.Add(task);

                resourceNameToKeyVaultMap.Remove(c.Name);
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Clean up any left over resources that are no longer in the model
        foreach (var (name, sa) in resourceNameToStorageAccountMap)
        {
            logger.LogInformation("Deleting storage account {accountName} which maps to resource name {name}.", sa.Id, name);

            await sa.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        foreach (var (name, ns) in resourceNameToServiceBusNamespaceMap)
        {
            logger.LogInformation("Deleting service bus namespace {namespaceName} which maps to resource name {name}.", ns.Id, name);

            await ns.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        foreach (var (name, kv) in resourceNameToKeyVaultMap)
        {
            logger.LogInformation("Deleting key vault {keyVaultName} which maps to resource name {name}.", kv.Id, name);

            await kv.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CreateKeyVaultAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        KeyVaultCollection keyVaults,
        Dictionary<string, KeyVaultResource> resourceNameToKeyVaultMap,
        AzureLocation location,
        AzureKeyVaultResource keyVault,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        resourceNameToKeyVaultMap.TryGetValue(keyVault.Name, out var keyVaultResource);

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
            parameters.Tags.Add("aspire-resource-name", keyVault.Name);

            var operation = await keyVaults.CreateOrUpdateAsync(WaitUntil.Completed, vaultName, parameters, cancellationToken).ConfigureAwait(false);
            keyVaultResource = operation.Value;

            logger.LogInformation("Key vault {vaultName} created.", keyVaultResource.Data.Name);
        }

        keyVault.VaultUri = keyVaultResource.Data.Properties.VaultUri;

        // Key Vault Administrator
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#key-vault-administrator
        var roleDefinitionId = CreateRoleDefinitionId(subscription, "00482a5a-887f-4fb3-b363-3b7fe8e74483");

        await DoRoleAssignmentAsync(armClient, keyVaultResource.Id, principalId, roleDefinitionId, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateServiceBusAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ServiceBusNamespaceCollection serviceBusNamespaces,
        Dictionary<string, ServiceBusNamespaceResource> resourceNameToServiceBusNamespaceMap,
        AzureLocation location,
        AzureServiceBusResource resource,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        resourceNameToServiceBusNamespaceMap.TryGetValue(resource.Name, out var serviceBusNamespace);

        if (serviceBusNamespace is null)
        {
            // ^[a-zA-Z][a-zA-Z0-9-]*$
            var namespaceName = Guid.NewGuid().ToString();

            logger.LogInformation("Creating service bus namespace {namespace} in {location}...", namespaceName, location);

            var parameters = new ServiceBusNamespaceData(location);
            parameters.Tags.Add("aspire-resource-name", resource.Name);

            // Now we can create a storage account with defined account name and parameters
            var operation = await serviceBusNamespaces.CreateOrUpdateAsync(WaitUntil.Completed, namespaceName, parameters, cancellationToken).ConfigureAwait(false);
            serviceBusNamespace = operation.Value;

            logger.LogInformation("Service bus namespace {namespace} created.", serviceBusNamespace.Data.Name);
        }

        // This is the full uri to the service bus namespace e.g https://namespace.servicebus.windows.net:443/
        // the connection strings for the app need the host name only
        resource.ServiceBusEndpoint = new Uri(serviceBusNamespace.Data.ServiceBusEndpoint).Host;

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
        StorageAccountCollection storageAccounts,
        Dictionary<string, StorageAccountResource> resourceNameToStorageAccountMap,
        AzureLocation location,
        AzureStorageResource resource,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        resourceNameToStorageAccountMap.TryGetValue(resource.Name, out var storageAccount);

        if (storageAccount is null)
        {
            //  Storage account name must be between 3 and 24 characters in length and use numbers and lower-case letters only.
            var accountName = Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20];

            logger.LogInformation("Creating storage account {accountName} in {location}...", accountName, location);

            // First we need to define the StorageAccountCreateParameters
            var sku = new StorageSku(StorageSkuName.StandardGrs);
            var kind = StorageKind.Storage;
            var parameters = new StorageAccountCreateOrUpdateContent(sku, kind, location);
            parameters.Tags.Add("aspire-resource-name", resource.Name);

            // Now we can create a storage account with defined account name and parameters
            var accountCreateOperation = await storageAccounts.CreateOrUpdateAsync(WaitUntil.Completed, accountName, parameters, cancellationToken).ConfigureAwait(false);
            storageAccount = accountCreateOperation.Value;

            logger.LogInformation("Storage account {accountName} created.", storageAccount.Data.Name);
        }

        resource.BlobUri = storageAccount.Data.PrimaryEndpoints.BlobUri;
        resource.TableUri = storageAccount.Data.PrimaryEndpoints.TableUri;
        resource.QueueUri = storageAccount.Data.PrimaryEndpoints.QueueUri;

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
