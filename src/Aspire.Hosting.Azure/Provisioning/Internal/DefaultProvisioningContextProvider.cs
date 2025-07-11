#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
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
    IInteractionService interactionService,
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<DefaultProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider) : IProvisioningContextProvider
{
    private readonly AzureProvisionerOptions _options = options.Value;

    private readonly TaskCompletionSource _provisioningOptionsAvailable = new();

    private void EnsureProvisioningOptions(JsonObject userSecrets)
    {
        if (!string.IsNullOrEmpty(_options.Location) && !string.IsNullOrEmpty(_options.SubscriptionId))
        {
            // If both options are already set, we can skip the prompt
            _provisioningOptionsAvailable.TrySetResult();
            return;
        }

        if (interactionService.IsAvailable)
        {
            // Start the loop that will allow the user to specify the Azure provisioning options
            _ = Task.Run(async () =>
            {
                try
                {
                    await RetrieveAzureProvisioningOptions(userSecrets).ConfigureAwait(false);

                    logger.LogDebug("Azure provisioning options have been handled successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
                }
            });
        }
    }
    private async Task RetrieveAzureProvisioningOptions(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        while (_options.Location == null || _options.SubscriptionId == null)
        {
            var locations = typeof(AzureLocation).GetProperties(BindingFlags.Public | BindingFlags.Static)
                            .Where(p => p.PropertyType == typeof(AzureLocation))
                            .Select(p => (AzureLocation)p.GetValue(null)!)
                            .Select(location => KeyValuePair.Create(location.Name, location.DisplayName ?? location.Name))
                            .OrderBy(kvp => kvp.Value)
                            .ToList();

            var messageBarResult = await interactionService.PromptMessageBarAsync(
                 "Azure Provisioning",
                 "The model contains Azure resources that require an Azure Subscription.",
                 new MessageBarInteractionOptions
                 {
                     Intent = MessageIntent.Warning,
                     PrimaryButtonText = "Enter values"
                 },
                 cancellationToken)
                 .ConfigureAwait(false);

            if (messageBarResult.Canceled)
            {
                // User canceled the prompt, so we exit the loop
                _provisioningOptionsAvailable.SetException(new MissingConfigurationException("Azure provisioning options were not provided."));
                return;
            }

            if (messageBarResult.Data)
            {
                var result = await interactionService.PromptInputsAsync(
                    "Azure Provisioning",
                    """
                    The model contains Azure resources that require an Azure Subscription. 
                    Please provide the required Azure settings.

                    If you do not have an Azure subscription, you can create a [free account](https://azure.com/free).
                    """,
                    [
                        new InteractionInput { InputType = InputType.Choice, Label = "Location", Placeholder = "Select Location", Required = true, Options = [..locations] },
                        new InteractionInput { InputType = InputType.SecretText, Label = "Subscription ID", Placeholder = "Select Subscription ID", Required = true },
                        new InteractionInput { InputType = InputType.Text, Label = "Resource Group", Value = GetDefaultResourceGroupName()},
                    ],
                    new InputsDialogInteractionOptions { ShowDismiss = false, EnableMessageMarkdown = true },
                    cancellationToken).ConfigureAwait(false);

                if (!result.Canceled)
                {
                    _options.Location = result.Data?[0].Value;
                    _options.SubscriptionId = result.Data?[1].Value;
                    _options.ResourceGroup = result.Data?[2].Value;
                    _options.AllowResourceGroupCreation = true; // Allow the creation of the resource group if it does not exist.

                    // Persist the parameter value to user secrets so they can be reused in the future
                    userSecrets.Prop("Azure")["Location"] = _options.Location;
                    userSecrets.Prop("Azure")["SubscriptionId"] = _options.SubscriptionId;
                    userSecrets.Prop("Azure")["ResourceGroup"] = _options.ResourceGroup;

                    _provisioningOptionsAvailable.SetResult();
                }
            }
        }
    }

    public async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        EnsureProvisioningOptions(userSecrets);

        await _provisioningOptionsAvailable.Task.ConfigureAwait(false);

        var subscriptionId = _options.SubscriptionId ?? throw new MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

        var credential = tokenCredentialProvider.TokenCredential;

        if (tokenCredentialProvider is DefaultTokenCredentialProvider defaultProvider)
        {
            defaultProvider.LogCredentialType();
        }

        var armClient = armClientProvider.GetArmClient(credential, subscriptionId);

        logger.LogInformation("Getting default subscription and tenant...");

        var (subscriptionResource, tenantResource) = await armClient.GetSubscriptionAndTenantAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscriptionResource.DisplayName, subscriptionResource.Id);
        logger.LogInformation("Tenant: {tenantId}", tenantResource.TenantId);

        if (string.IsNullOrEmpty(_options.Location))
        {
            throw new MissingConfigurationException("An azure location/region is required. Set the Azure:Location configuration value.");
        }

        string resourceGroupName;
        bool createIfAbsent;

        if (string.IsNullOrEmpty(_options.ResourceGroup))
        {
            // Generate an resource group name since none was provided
            // Create a unique resource group name and save it in user secrets
            resourceGroupName = GetDefaultResourceGroupName();

            createIfAbsent = true;

            userSecrets.Prop("Azure")["ResourceGroup"] = resourceGroupName;
        }
        else
        {
            resourceGroupName = _options.ResourceGroup;
            createIfAbsent = _options.AllowResourceGroupCreation ?? false;
        }

        var resourceGroups = subscriptionResource.GetResourceGroups();

        IResourceGroupResource? resourceGroup;

        var location = new AzureLocation(_options.Location);
        try
        {
            var response = await resourceGroups.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
            resourceGroup = response.Value;

            logger.LogInformation("Using existing resource group {rgName}.", resourceGroup.Name);
        }
        catch (Exception)
        {
            if (!createIfAbsent)
            {
                throw;
            }

            // REVIEW: Is it possible to do this without an exception?

            logger.LogInformation("Creating resource group {rgName} in {location}...", resourceGroupName, location);

            var rgData = new ResourceGroupData(location);
            rgData.Tags.Add("aspire", "true");
            var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
            resourceGroup = operation.Value;

            logger.LogInformation("Resource group {rgName} created.", resourceGroup.Name);
        }

        var principal = await userPrincipalProvider.GetUserPrincipalAsync(cancellationToken).ConfigureAwait(false);

        return new ProvisioningContext(
                    credential,
                    armClient,
                    subscriptionResource,
                    resourceGroup,
                    tenantResource,
                    location,
                    principal,
                    userSecrets);
    }

    private string GetDefaultResourceGroupName()
    {
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

        return $"{prefix}-{normalizedApplicationName}-{suffix}";
    }
}
