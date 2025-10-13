#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Resources;
using Aspire.Hosting.Azure.Utils;
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
    DistributedApplicationExecutionContext distributedApplicationExecutionContext) : BaseProvisioningContextProvider(
        interactionService,
        options,
        environment,
        logger,
        armClientProvider,
        userPrincipalProvider,
        tokenCredentialProvider,
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
            // if both options are already set, we can skip the prompt
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

    public override async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        EnsureProvisioningOptions();

        await _provisioningOptionsAvailable.Task.ConfigureAwait(false);

        return await base.CreateProvisioningContextAsync(userSecrets, cancellationToken).ConfigureAwait(false);
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
                var result = await _interactionService.PromptInputsAsync(
                    AzureProvisioningStrings.InputsTitle,
                    AzureProvisioningStrings.InputsMessage,
                    [
                        new InteractionInput
                         {
                             Name = SubscriptionIdName,
                             InputType = InputType.Choice,
                             Label = AzureProvisioningStrings.SubscriptionIdLabel,
                             Required = true,
                             AllowCustomChoice = true,
                             Placeholder = AzureProvisioningStrings.SubscriptionIdPlaceholder,
                             DynamicOptions = new DynamicInputOptions
                             {
                                 UpdateInputCallback = async (context) =>
                                 {
                                     try
                                     {
                                         var credential = _tokenCredentialProvider.TokenCredential;
                                         var armClient = _armClientProvider.GetArmClient(credential);
                                         var availableSubscriptions = await armClient.GetAvailableSubscriptionsAsync(context.CancellationToken).ConfigureAwait(false);
                                         var subscriptionList = availableSubscriptions.ToList();

                                         if (subscriptionList.Count > 0)
                                         {
                                             var subscriptionOptions = subscriptionList
                                                 .Select(sub => KeyValuePair.Create(sub.Id.SubscriptionId ?? "", $"{sub.DisplayName ?? sub.Id.SubscriptionId} ({sub.Id.SubscriptionId})"))
                                                 .OrderBy(kvp => kvp.Value)
                                                 .ToList();

                                             context.Input.Options = subscriptionOptions;
                                         }
                                         else
                                         {
                                             context.Input.Options = [];
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         _logger.LogWarning(ex, "Failed to enumerate available subscriptions. Allowing manual input.");
                                         context.Input.Options = [];
                                     }
                                 }
                             }
                         },
                         new InteractionInput
                         {
                             Name = LocationName,
                             InputType = InputType.Choice,
                             Label = AzureProvisioningStrings.LocationLabel,
                             Placeholder = "Select location",
                             Required = true,
                             Disabled = true,
                             DynamicOptions = new DynamicInputOptions
                             {
                                 UpdateInputCallback = async (context) =>
                                 {
                                     try
                                     {
                                         var subscriptionId = context.AllInputs[SubscriptionIdName].Value ?? string.Empty;

                                         var credential = _tokenCredentialProvider.TokenCredential;
                                         var armClient = _armClientProvider.GetArmClient(credential);
                                         var availableLocations = await armClient.GetAvailableLocationsAsync(subscriptionId, context.CancellationToken).ConfigureAwait(false);
                                         var locationList = availableLocations.ToList();

                                         if (locationList.Count > 0)
                                         {
                                             var locationOptions = locationList
                                                 .Select(loc => KeyValuePair.Create(loc.Name, loc.DisplayName))
                                                 .OrderBy(kvp => kvp.Value)
                                                 .ToList();

                                             context.Input.Options = locationOptions;
                                             context.Input.Disabled = false;
                                         }
                                         else
                                         {
                                             // Fall back to static locations from AzureLocation
                                             var staticLocations = GetStaticAzureLocations();
                                             context.Input.Options = staticLocations;
                                             context.Input.Disabled = false;
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         _logger.LogWarning(ex, "Failed to enumerate available locations. Falling back to static locations.");

                                         // Fall back to static locations from AzureLocation
                                         var staticLocations = GetStaticAzureLocations();
                                         context.Input.Options = staticLocations;
                                         context.Input.Disabled = false;
                                     }
                                 },
                                 DependsOnInputs = [SubscriptionIdName]
                             }
                         },
                         new InteractionInput
                         {
                             Name = ResourceGroupName,
                             InputType = InputType.Text,
                             Label = AzureProvisioningStrings.ResourceGroupLabel,
                             Value = GetDefaultResourceGroupName()
                         }
                    ],
                    new InputsDialogInteractionOptions
                    {
                        EnableMessageMarkdown = true,
                        ValidationCallback = static (validationContext) =>
                        {
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
                    _options.Location = result.Data[LocationName].Value;
                    _options.SubscriptionId = result.Data[SubscriptionIdName].Value;
                    _options.ResourceGroup = result.Data[ResourceGroupName].Value;
                    _options.AllowResourceGroupCreation = true; // Allow the creation of the resource group if it does not exist.

                    _provisioningOptionsAvailable.SetResult();
                }
            }
        }
    }
}
