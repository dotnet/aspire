// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class KeyVaultProvisioner(ILogger<KeyVaultProvisioner> logger) : AzureResourceProvisioner<AzureKeyVaultResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureKeyVaultResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string vaultUrl)
        {
            resource.VaultUri = new(vaultUrl);
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
            parameters.Tags.Add(AzureProvisioner.AspireResourceNameTag, keyVault.Name);

            var operation = await resourceGroup.GetKeyVaults().CreateOrUpdateAsync(WaitUntil.Completed, vaultName, parameters, cancellationToken).ConfigureAwait(false);
            keyVaultResource = operation.Value;

            logger.LogInformation("Key vault {vaultName} created.", keyVaultResource.Data.Name);
        }
        keyVault.VaultUri = keyVaultResource.Data.Properties.VaultUri;

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[keyVault.Name] = keyVault.VaultUri.ToString();

        // Key Vault Administrator
        // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#key-vault-administrator
        var roleDefinitionId = CreateRoleDefinitionId(subscription, "00482a5a-887f-4fb3-b363-3b7fe8e74483");

        await DoRoleAssignmentAsync(armClient, keyVaultResource.Id, principalId, roleDefinitionId, cancellationToken).ConfigureAwait(false);
    }
}
