// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Utils;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IProvisioningContextProvider"/>.
/// </summary>
internal sealed class DefaultProvisioningContextProvider(
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<DefaultProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider) : IProvisioningContextProvider
{
    private readonly AzureProvisionerOptions _options = options.Value;

    public async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        return await CreateProvisioningContextAsync(userSecrets, null, null, null, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, string? subscriptionId = null, string? resourceGroup = null, string? location = null, bool? allowResourceGroupCreation = null, CancellationToken cancellationToken = default)
    {
        var actualSubscriptionId = subscriptionId ?? _options.SubscriptionId ?? throw new MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

        var credential = tokenCredentialProvider.TokenCredential;

        if (tokenCredentialProvider is DefaultTokenCredentialProvider defaultProvider)
        {
            defaultProvider.LogCredentialType();
        }

        var armClient = armClientProvider.GetArmClient(credential, actualSubscriptionId);

        logger.LogInformation("Getting default subscription and tenant...");

        var (subscriptionResource, tenantResource) = await armClient.GetSubscriptionAndTenantAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscriptionResource.DisplayName, subscriptionResource.Id);
        logger.LogInformation("Tenant: {tenantId}", tenantResource.TenantId);

        var actualLocation = location ?? _options.Location;
        if (string.IsNullOrEmpty(actualLocation))
        {
            throw new MissingConfigurationException("An azure location/region is required. Set the Azure:Location configuration value.");
        }

        string resourceGroupName;
        bool createIfAbsent;

        var actualResourceGroup = resourceGroup ?? _options.ResourceGroup;
        if (string.IsNullOrEmpty(actualResourceGroup))
        {
            // Generate an resource group name since none was provided

            var prefix = "rg-aspire";

            if (!string.IsNullOrWhiteSpace(_options.ResourceGroupPrefix))
            {
                prefix = _options.ResourceGroupPrefix;
            }

            var suffix = RandomNumberGenerator.GetHexString(8, lowercase: true);

            var maxApplicationNameSize = ResourceGroupNameHelpers.MaxResourceGroupNameLength - prefix.Length - suffix.Length - 2; // extra '-'s

            var normalizedApplicationName = ResourceGroupNameHelpers.NormalizeResourceGroupName(environment.ApplicationName.ToLowerInvariant());
            if (normalizedApplicationName.Length > maxApplicationNameSize)
            {
                normalizedApplicationName = normalizedApplicationName[..maxApplicationNameSize];
            }

            // Create a unique resource group name and save it in user secrets
            resourceGroupName = $"{prefix}-{normalizedApplicationName}-{suffix}";

            createIfAbsent = true;

            userSecrets.Prop("Azure")["ResourceGroup"] = resourceGroupName;
        }
        else
        {
            resourceGroupName = actualResourceGroup;
            createIfAbsent = allowResourceGroupCreation ?? _options.AllowResourceGroupCreation ?? false;
        }

        var resourceGroups = subscriptionResource.GetResourceGroups();

        IResourceGroupResource? resourceGroupResource;

        var azureLocation = new AzureLocation(actualLocation);
        try
        {
            var response = await resourceGroups.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
            resourceGroupResource = response.Value;

            logger.LogInformation("Using existing resource group {rgName}.", resourceGroupResource.Name);
        }
        catch (Exception)
        {
            if (!createIfAbsent)
            {
                throw;
            }

            // REVIEW: Is it possible to do this without an exception?

            logger.LogInformation("Creating resource group {rgName} in {location}...", resourceGroupName, azureLocation);

            var rgData = new ResourceGroupData(azureLocation);
            rgData.Tags.Add("aspire", "true");
            var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
            resourceGroupResource = operation.Value;

            logger.LogInformation("Resource group {rgName} created.", resourceGroupResource.Name);
        }

        var principal = await userPrincipalProvider.GetUserPrincipalAsync(cancellationToken).ConfigureAwait(false);

        return new ProvisioningContext(
                    credential,
                    armClient,
                    subscriptionResource,
                    resourceGroupResource,
                    tenantResource,
                    azureLocation,
                    principal,
                    userSecrets);
    }
}
