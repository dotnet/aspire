#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
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
    InteractionService interactionService,
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<DefaultProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider) : IProvisioningContextProvider
{
    private readonly AzureProvisionerOptions _options = options.Value;

    private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>();

    public void AddProvisioningCommand(IAzureResource resource)
    {
        resource.Annotations.Add(new ResourceCommandAnnotation(
            "azure-provision",
            "Set Azure Configuration",
            ShowConfigCommand,
            PromptForConfiguration,
            "Configure Azure provisioning settings",
            parameter: null,
            confirmationMessage: null,
            iconName: null,
            iconVariant: null,
            isHighlighted: false
            ));
    }

    private async Task<ExecuteCommandResult> PromptForConfiguration(ExecuteCommandContext context)
    {
        await Ask(context.CancellationToken).ConfigureAwait(false);

        return new ExecuteCommandResult
        {
            Success = true
        };
    }

    private async Task Ask(CancellationToken cancellationToken)
    {
        var locations = (typeof(AzureLocation).GetProperty("PublicCloudLocations", BindingFlags.NonPublic | BindingFlags.Static)
                            ?.GetValue(null) as Dictionary<string, AzureLocation> ?? [])
                            .Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.DisplayName ?? kvp.Value.Name))
                            .ToList();

        var result = await interactionService.PromptInputsAsync("Azure Provisioning",
                """
                The model contains Azure resources that require an Azure Subscription. 
                Please provide the required Azure settings.
                <br /><br />
                If you do not have an Azure subscription, you can create a <a href="https://azure.com/free" target="_blank">free account</a>.
                """,
                [
                    new InteractionInput { InputType = InputType.Select, Label = "Location", Placeholder = "Select Location", Required = true, Options = [..locations] },
                    new InteractionInput { InputType = InputType.Password, Label = "Subscription ID", Placeholder = "Select Subscription ID", Required = true },
                ],
                new InputsDialogInteractionOptions { ShowDismiss = false, EscapeMessageHtml = false },
                cancellationToken).ConfigureAwait(false);

        _options.Location = result.Data?[0].Value;
        _options.SubscriptionId = result.Data?[1].Value;

        _channel.Writer.TryWrite(true);
    }

    private ResourceCommandState ShowConfigCommand(UpdateCommandStateContext context)
    {
        if (_options.Location == null || _options.SubscriptionId == null)
        {
            return ResourceCommandState.Enabled;
        }

        return ResourceCommandState.Hidden;
    }

    public async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        await Ask(cancellationToken).ConfigureAwait(false);

        await foreach (var _ in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
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
            catch (MissingConfigurationException ex)
            {
                logger.LogError(ex, "Missing configuration for Azure provisioning.");
            }
            catch (Exception ex)
            {
                _channel.Writer.TryComplete(ex);
            }
        }

        throw new DistributedApplicationException("Provisioning context creation was cancelled or failed.");
    }
}