// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class StorageProvisioner(ILogger<StorageProvisioner> logger) : AzureResourceProvisioner<AzureStorageResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureStorageResource resource)
    {
        // Storage isn't a connection string because it has multiple endpoints
        var storageSection = configuration.GetSection($"Azure:Storage:{resource.Name}");
        var tableUrl = storageSection["TableUri"];
        var blobUrl = storageSection["BlobUri"];
        var queueUrl = storageSection["QueueUri"];

        // If any of these is null then we need to create/get the storage account
        if (tableUrl is not null && blobUrl is not null && queueUrl is not null)
        {
            resource.TableUri = new Uri(tableUrl);
            resource.BlobUri = new Uri(blobUrl);
            resource.QueueUri = new Uri(queueUrl);

            return true;
        }
        return false;
    }

    public override bool ShouldProvision(IConfiguration configuration, AzureStorageResource resource) =>
        !resource.IsEmulator;

    public override async Task GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
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
            parameters.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

            // Now we can create a storage account with defined account name and parameters
            var accountCreateOperation = await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(WaitUntil.Completed, accountName, parameters, cancellationToken).ConfigureAwait(false);
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
        // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#storage-queue-data-contributor
        var storageQueueDataContributorId = CreateRoleDefinitionId(subscription, "974c5e8b-45b9-4653-ba55-5f855dd0fb88");

        // Storage Table Data Contributor
        // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#storage-table-data-contributor
        var storageDataContributorId = CreateRoleDefinitionId(subscription, "0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3");

        // Storage Blob Data Contributor
        // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor
        var storageBlobDataContributorId = CreateRoleDefinitionId(subscription, "81a9662b-bebf-436f-a333-f67b29880f12");

        var t0 = DoRoleAssignmentAsync(armClient, storageAccount.Id, principalId, storageQueueDataContributorId, cancellationToken);
        var t1 = DoRoleAssignmentAsync(armClient, storageAccount.Id, principalId, storageDataContributorId, cancellationToken);
        var t2 = DoRoleAssignmentAsync(armClient, storageAccount.Id, principalId, storageBlobDataContributorId, cancellationToken);

        await Task.WhenAll(t0, t1, t2).ConfigureAwait(false);
    }
}
