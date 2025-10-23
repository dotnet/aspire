#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Aspire.Hosting.Azure.Resources;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Run mode implementation of <see cref="IProvisioningContextProvider"/>.
/// </summary>
internal sealed class RunModeProvisioningContextProvider(
    IInteractionService interactionService,
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<RunModeProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider,
    IDeploymentStateManager deploymentStateManager,
    DistributedApplicationExecutionContext distributedApplicationExecutionContext) : BaseProvisioningContextProvider(
        interactionService,
        options,
        environment,
        logger,
        armClientProvider,
        userPrincipalProvider,
        tokenCredentialProvider,
        deploymentStateManager,
        distributedApplicationExecutionContext)
{
    private readonly TaskCompletionSource _provisioningOptionsAvailable = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override string GetDefaultResourceGroupName()
    {
        var prefix = "rg-aspire";

        if (!string.IsNullOrWhiteSpace(_options.ResourceGroupPrefix))
        {
            prefix = _options.ResourceGroupPrefix;
        }

        var suffix = RandomNumberGenerator.GetHexString(8, lowercase: true);

        var maxApplicationNameSize = ResourceGroupNameHelpers.MaxResourceGroupNameLength - prefix.Length - suffix.Length - 2; // extra '-'s

        var normalizedApplicationName = ResourceGroupNameHelpers.NormalizeResourceGroupName(_environment.ApplicationName.ToLowerInvariant());
        if (normalizedApplicationName.Length > maxApplicationNameSize)
        {
            normalizedApplicationName = normalizedApplicationName[..maxApplicationNameSize];
        }

        // Run mode always includes random suffix for uniqueness
        return $"{prefix}-{normalizedApplicationName}-{suffix}";
    }

    private void EnsureProvisioningOptions()
    {
        if (!_interactionService.IsAvailable ||
            (!string.IsNullOrEmpty(_options.Location) && !string.IsNullOrEmpty(_options.SubscriptionId)))
        {
            // If the interaction service is not available, or
            // if all options are already set, we can skip the prompt
            _provisioningOptionsAvailable.TrySetResult();
            return;
        }

        // Start the loop that will allow the user to specify the Azure provisioning options
        _ = Task.Run(async () =>
        {
            try
            {
                await RetrieveAzureProvisioningOptions().ConfigureAwait(false);

                _logger.LogDebug("Azure provisioning options have been handled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
                _provisioningOptionsAvailable.SetException(ex);
            }
        });
    }

    public override async Task<ProvisioningContext> CreateProvisioningContextAsync(CancellationToken cancellationToken = default)
    {
        EnsureProvisioningOptions();

        await _provisioningOptionsAvailable.Task.ConfigureAwait(false);

        return await base.CreateProvisioningContextAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RetrieveAzureProvisioningOptions(CancellationToken cancellationToken = default)
    {
        while (_options.Location == null || _options.SubscriptionId == null)
        {
            var messageBarResult = await _interactionService.PromptNotificationAsync(
                 AzureProvisioningStrings.NotificationTitle,
                 AzureProvisioningStrings.NotificationMessage,
                 new NotificationInteractionOptions
                 {
                     Intent = MessageIntent.Warning,
                     PrimaryButtonText = AzureProvisioningStrings.NotificationPrimaryButtonText
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
                var inputs = new List<InteractionInput>();

                // Skip tenant prompting if subscription ID is already set
                if (string.IsNullOrEmpty(_options.SubscriptionId))
                {
                    inputs.Add(new InteractionInput
                    {
                        Name = TenantName,
                        InputType = InputType.Choice,
                        Label = AzureProvisioningStrings.TenantLabel,
                        Required = true,
                        AllowCustomChoice = true,
                        Placeholder = AzureProvisioningStrings.TenantPlaceholder,
                        DynamicLoading = new InputLoadOptions
                        {
                            LoadCallback = async (context) =>
                            {
                                var (tenantOptions, fetchSucceeded) =
                                    await TryGetTenantsAsync(cancellationToken).ConfigureAwait(false);

                                context.Input.Options = fetchSucceeded
                                    ? tenantOptions!
                                    : [];
                            }
                        }
                    });
                }

                // If the subscription ID is already set
                // show the value as from the configuration and disable the input
                // there should be no option to change it

                inputs.Add(new InteractionInput
                {
                    Name = SubscriptionIdName,
                    InputType = string.IsNullOrEmpty(_options.SubscriptionId) ? InputType.Choice : InputType.Text,
                    Label = AzureProvisioningStrings.SubscriptionIdLabel,
                    Required = true,
                    AllowCustomChoice = true,
                    Placeholder = AzureProvisioningStrings.SubscriptionIdPlaceholder,
                    Disabled = !string.IsNullOrEmpty(_options.SubscriptionId),
                    Value = _options.SubscriptionId,
                    DynamicLoading = new InputLoadOptions
                    {
                        LoadCallback = async (context) =>
                        {
                            if (!string.IsNullOrEmpty(_options.SubscriptionId))
                            {
                                // If subscription ID is not set, we don't need to load options
                                return;
                            }

                            // Get tenant ID from input if tenant selection is enabled, otherwise use configured value
                            var tenantId = context.AllInputs[TenantName].Value ?? string.Empty;

                            var (subscriptionOptions, fetchSucceeded) =
                                await TryGetSubscriptionsAsync(tenantId, cancellationToken).ConfigureAwait(false);

                            context.Input.Options = fetchSucceeded
                                ? subscriptionOptions!
                                : [];
                            context.Input.Disabled = false;
                        },
                        DependsOnInputs = string.IsNullOrEmpty(_options.SubscriptionId) ? [TenantName] : []
                    }
                });

                inputs.Add(new InteractionInput
                {
                    Name = ResourceGroupName,
                    InputType = InputType.Choice,
                    Label = AzureProvisioningStrings.ResourceGroupLabel,
                    Placeholder = AzureProvisioningStrings.ResourceGroupPlaceholder,
                    Value = GetDefaultResourceGroupName(),
                    AllowCustomChoice = true,
                    Disabled = true,
                    DynamicLoading = new InputLoadOptions
                    {
                        LoadCallback = async (context) =>
                        {
                            var subscriptionId = context.AllInputs[SubscriptionIdName].Value ?? string.Empty;

                            var (resourceGroupOptions, fetchSucceeded) = await TryGetResourceGroupsWithLocationAsync(subscriptionId, cancellationToken).ConfigureAwait(false);

                            if (fetchSucceeded && resourceGroupOptions is not null)
                            {
                                context.Input.Options = resourceGroupOptions.Select(rg => KeyValuePair.Create(rg.Name, rg.Name)).ToList();
                            }
                            else
                            {
                                context.Input.Options = [];
                            }
                            context.Input.Disabled = false;
                        },
                        DependsOnInputs = [SubscriptionIdName]
                    }
                });

                inputs.Add(new InteractionInput
                {
                    Name = LocationName,
                    InputType = InputType.Choice,
                    Label = AzureProvisioningStrings.LocationLabel,
                    Placeholder = AzureProvisioningStrings.LocationPlaceholder,
                    Required = true,
                    Disabled = true,
                    DynamicLoading = new InputLoadOptions
                    {
                        LoadCallback = async (context) =>
                        {
                            var subscriptionId = context.AllInputs[SubscriptionIdName].Value ?? string.Empty;
                            var resourceGroupName = context.AllInputs[ResourceGroupName].Value ?? string.Empty;

                            // Check if the selected resource group is an existing one
                            var (resourceGroupOptions, fetchSucceeded) = await TryGetResourceGroupsWithLocationAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                            
                            if (fetchSucceeded && resourceGroupOptions is not null)
                            {
                                var existingResourceGroup = resourceGroupOptions.FirstOrDefault(rg => rg.Name.Equals(resourceGroupName, StringComparison.OrdinalIgnoreCase));
                                if (existingResourceGroup != default)
                                {
                                    // Use location from existing resource group
                                    context.Input.Options = [KeyValuePair.Create(existingResourceGroup.Location, existingResourceGroup.Location)];
                                    context.Input.Value = existingResourceGroup.Location;
                                    context.Input.Disabled = true; // Make it read-only since it's from existing RG
                                    return;
                                }
                            }

                            // For new resource groups, load all locations
                            var (locationOptions, _) = await TryGetLocationsAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                            context.Input.Options = locationOptions;
                            context.Input.Disabled = false;
                        },
                        DependsOnInputs = [SubscriptionIdName, ResourceGroupName]
                    }
                });

                var result = await _interactionService.PromptInputsAsync(
                    AzureProvisioningStrings.InputsTitle,
                    AzureProvisioningStrings.InputsMessage,
                    inputs,
                    new InputsDialogInteractionOptions
                    {
                        EnableMessageMarkdown = true,
                        ValidationCallback = (validationContext) =>
                        {
                            // Only validate tenant if it's included in the inputs
                            if (validationContext.Inputs.TryGetByName(TenantName, out var tenantInput))
                            {
                                if (!string.IsNullOrWhiteSpace(tenantInput.Value) && !Guid.TryParse(tenantInput.Value, out _))
                                {
                                    validationContext.AddValidationError(tenantInput, AzureProvisioningStrings.ValidationTenantIdInvalid);
                                }
                            }

                            var subscriptionInput = validationContext.Inputs[SubscriptionIdName];
                            if (!string.IsNullOrWhiteSpace(subscriptionInput.Value) && !Guid.TryParse(subscriptionInput.Value, out _))
                            {
                                validationContext.AddValidationError(subscriptionInput, AzureProvisioningStrings.ValidationSubscriptionIdInvalid);
                            }

                            var resourceGroupInput = validationContext.Inputs[ResourceGroupName];
                            if (!IsValidResourceGroupName(resourceGroupInput.Value))
                            {
                                validationContext.AddValidationError(resourceGroupInput, AzureProvisioningStrings.ValidationResourceGroupNameInvalid);
                            }

                            return Task.CompletedTask;
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                if (!result.Canceled)
                {
                    // Only set tenant ID if it was part of the input (when subscription ID wasn't already set)
                    if (result.Data.TryGetByName(TenantName, out var tenantInput))
                    {
                        _options.TenantId = tenantInput.Value;
                    }
                    _options.Location = result.Data[LocationName].Value;
                    _options.SubscriptionId ??= result.Data[SubscriptionIdName].Value;
                    _options.ResourceGroup = result.Data[ResourceGroupName].Value;
                    _options.AllowResourceGroupCreation = true; // Allow the creation of the resource group if it does not exist.

                    _provisioningOptionsAvailable.SetResult();
                }
            }
        }
    }
}
