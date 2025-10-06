// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIREPUBLISHERS001

using System.Reflection;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Resources;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Publishing;
using Azure.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Publish mode implementation of <see cref="IProvisioningContextProvider"/>.
/// Uses enhanced prompting logic with dynamic subscription and location fetching.
/// </summary>
internal sealed class PublishModeProvisioningContextProvider(
    IInteractionService interactionService,
    IHostEnvironment environment,
    ILogger<PublishModeProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider,
    DistributedApplicationExecutionContext distributedApplicationExecutionContext,
    IPublishingActivityReporter activityReporter,
    IUserSecretsManager userSecretsManager,
    IOptions<PublishingOptions> publishingOptions) : BaseProvisioningContextProvider(
        interactionService,
        environment,
        logger,
        armClientProvider,
        userPrincipalProvider,
        tokenCredentialProvider,
        distributedApplicationExecutionContext,
        userSecretsManager)
{
    private readonly string _deploymentKey = userSecretsManager.GetDeploymentKey() ?? throw new InvalidOperationException("Deployment key is required for publish mode provisioning.");
    private readonly IOptions<PublishingOptions> _publishingOptions = publishingOptions;

    protected override string GetDefaultResourceGroupName()
    {
        var prefix = "rg-aspire";

        if (!string.IsNullOrWhiteSpace(ResourceGroupPrefix))
        {
            prefix = ResourceGroupPrefix;
        }

        var maxApplicationNameSize = ResourceGroupNameHelpers.MaxResourceGroupNameLength - prefix.Length - 1; // extra '-'

        var normalizedApplicationName = ResourceGroupNameHelpers.NormalizeResourceGroupName(_environment.ApplicationName.ToLowerInvariant());
        if (normalizedApplicationName.Length > maxApplicationNameSize)
        {
            normalizedApplicationName = normalizedApplicationName[..maxApplicationNameSize];
        }

        // Publish mode doesn't include random suffix for consistency
        return $"{prefix}-{normalizedApplicationName}";
    }

    public override async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        try
        {
            await RetrieveAzureProvisioningOptions(userSecrets, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Azure provisioning options have been handled successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
        }

        return await base.CreateProvisioningContextAsync(userSecrets, cancellationToken).ConfigureAwait(false);
    }

    private async Task RetrieveAzureProvisioningOptions(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        var subscriptionKey = $"Azure:{_deploymentKey}:SubscriptionId";
        var locationKey = $"Azure:{_deploymentKey}:Location";
        var resourceGroupKey = $"Azure:{_deploymentKey}:ResourceGroup";

        string? existingSubscriptionId = null;
        string? existingLocation = null;
        string? existingResourceGroup = null;

        // Only load from user secrets if NoCache is not set
        if (!_publishingOptions.Value.NoCache)
        {
            existingSubscriptionId = userSecrets[subscriptionKey]?.GetValue<string>();
            existingLocation = userSecrets[locationKey]?.GetValue<string>();
            existingResourceGroup = userSecrets[resourceGroupKey]?.GetValue<string>();

            // Set options only from user secrets values
            if (!string.IsNullOrEmpty(existingSubscriptionId))
            {
                SubscriptionId = existingSubscriptionId;
            }

            if (!string.IsNullOrEmpty(existingLocation))
            {
                Location = existingLocation;
            }

            if (!string.IsNullOrEmpty(existingResourceGroup))
            {
                ResourceGroup = existingResourceGroup;
                AllowResourceGroupCreation = true;
            }
        }

        while (Location == null || SubscriptionId == null)
        {
            if (SubscriptionId == null)
            {
                await PromptForSubscriptionAsync(cancellationToken).ConfigureAwait(false);
                if (SubscriptionId == null)
                {
                    continue;
                }
            }

            if (Location == null)
            {
                await PromptForLocationAndResourceGroupAsync(cancellationToken).ConfigureAwait(false);
                if (Location == null)
                {
                    continue;
                }
            }
        }

        // Only save to user secrets if NoCache is not set
        if (!_publishingOptions.Value.NoCache)
        {
            if (SubscriptionId != existingSubscriptionId)
            {
                userSecrets[subscriptionKey] = SubscriptionId;
            }

            if (Location != existingLocation)
            {
                userSecrets[locationKey] = Location;
            }

            if (ResourceGroup != existingResourceGroup)
            {
                userSecrets[resourceGroupKey] = ResourceGroup;
            }
        }
    }

    private async Task PromptForSubscriptionAsync(CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, string>>? subscriptionOptions = null;
        var fetchSucceeded = false;

        var step = await activityReporter.CreateStepAsync(
            "Retrieving Azure subscription information",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var task = await step.CreateTaskAsync("Fetching available subscriptions", cancellationToken).ConfigureAwait(false);

                await using (task.ConfigureAwait(false))
                {
                    try
                    {
                        var credential = _tokenCredentialProvider.TokenCredential;
                        var armClient = _armClientProvider.GetArmClient(credential);
                        var availableSubscriptions = await armClient.GetAvailableSubscriptionsAsync(cancellationToken).ConfigureAwait(false);
                        var subscriptionList = availableSubscriptions.ToList();

                        if (subscriptionList.Count > 0)
                        {
                            subscriptionOptions = [.. subscriptionList
                                .Select(sub => KeyValuePair.Create(sub.Id.SubscriptionId ?? "", $"{sub.DisplayName ?? sub.Id.SubscriptionId} ({sub.Id.SubscriptionId})"))
                                .OrderBy(kvp => kvp.Value)];
                            fetchSucceeded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to enumerate available subscriptions. Falling back to manual input.");
                    }
                }

                if (fetchSucceeded)
                {
                    await step.SucceedAsync($"Found {subscriptionOptions!.Count} available subscription(s)", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await step.WarnAsync("Failed to fetch subscriptions, falling back to manual entry", cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure subscription information.");
                await step.FailAsync($"Failed to retrieve subscription information: {ex.Message}", cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        if (subscriptionOptions?.Count > 0)
        {
            var result = await _interactionService.PromptInputsAsync(
                AzureProvisioningStrings.SubscriptionDialogTitle,
                AzureProvisioningStrings.SubscriptionSelectionMessage,
                [
                    new InteractionInput
                    {
                        Name = SubscriptionIdName,
                        InputType = InputType.Choice,
                        Label = AzureProvisioningStrings.SubscriptionIdLabel,
                        Required = true,
                        Options = [..subscriptionOptions]
                    }
                ],
                new InputsDialogInteractionOptions
                {
                    EnableMessageMarkdown = false
                },
                cancellationToken).ConfigureAwait(false);

            if (!result.Canceled)
            {
                SubscriptionId = result.Data[SubscriptionIdName].Value;
                return;
            }
        }

        var manualResult = await _interactionService.PromptInputsAsync(
            AzureProvisioningStrings.SubscriptionDialogTitle,
            AzureProvisioningStrings.SubscriptionManualEntryMessage,
            [
                new InteractionInput
                {
                    Name = SubscriptionIdName,
                    InputType = InputType.SecretText,
                    Label = AzureProvisioningStrings.SubscriptionIdLabel,
                    Placeholder = AzureProvisioningStrings.SubscriptionIdPlaceholder,
                    Required = true
                }
            ],
            new InputsDialogInteractionOptions
            {
                EnableMessageMarkdown = false,
                ValidationCallback = static (validationContext) =>
                {
                    var subscriptionInput = validationContext.Inputs[SubscriptionIdName];
                    if (!Guid.TryParse(subscriptionInput.Value, out var _))
                    {
                        validationContext.AddValidationError(subscriptionInput, AzureProvisioningStrings.ValidationSubscriptionIdInvalid);
                    }
                    return Task.CompletedTask;
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (!manualResult.Canceled)
        {
            SubscriptionId = manualResult.Data[SubscriptionIdName].Value;
        }
    }

    private async Task PromptForLocationAndResourceGroupAsync(CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, string>>? locationOptions = null;
        var fetchSucceeded = false;

        var step = await activityReporter.CreateStepAsync(
            "Retrieving Azure region information",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var task = await step.CreateTaskAsync("Fetching supported regions", cancellationToken).ConfigureAwait(false);

                await using (task.ConfigureAwait(false))
                {
                    try
                    {
                        var credential = _tokenCredentialProvider.TokenCredential;
                        var armClient = _armClientProvider.GetArmClient(credential);
                        var availableLocations = await armClient.GetAvailableLocationsAsync(SubscriptionId!, cancellationToken).ConfigureAwait(false);
                        var locationList = availableLocations.ToList();

                        if (locationList.Count > 0)
                        {
                            locationOptions = [.. locationList.Select(loc => KeyValuePair.Create(loc.Name, loc.DisplayName))];
                            fetchSucceeded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to enumerate available locations. Falling back to manual input.");
                    }
                }

                if (fetchSucceeded)
                {
                    await step.SucceedAsync($"Found {locationOptions!.Count} available region(s)", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await step.WarnAsync("Failed to fetch regions, falling back to manual entry", cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure region information.");
                await step.FailAsync($"Failed to retrieve region information: {ex.Message}", cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        if (locationOptions?.Count > 0)
        {
            var result = await _interactionService.PromptInputsAsync(
                AzureProvisioningStrings.LocationDialogTitle,
                AzureProvisioningStrings.LocationSelectionMessage,
                [
                    new InteractionInput
                    {
                        Name = LocationName,
                        InputType = InputType.Choice,
                        Label = AzureProvisioningStrings.LocationLabel,
                        Required = true,
                        Options = [..locationOptions]
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
                    EnableMessageMarkdown = false,
                    ValidationCallback = static (validationContext) =>
                    {
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
                Location = result.Data[LocationName].Value;
                ResourceGroup = result.Data[ResourceGroupName].Value;
                AllowResourceGroupCreation = true;

                return;
            }
        }

        var locations = typeof(AzureLocation).GetProperties(BindingFlags.Public | BindingFlags.Static)
                            .Where(p => p.PropertyType == typeof(AzureLocation))
                            .Select(p => (AzureLocation)p.GetValue(null)!)
                            .Select(location => KeyValuePair.Create(location.Name, location.DisplayName ?? location.Name))
                            .OrderBy(kvp => kvp.Value)
                            .ToList();

        var manualResult = await _interactionService.PromptInputsAsync(
            AzureProvisioningStrings.LocationDialogTitle,
            AzureProvisioningStrings.LocationSelectionMessage,
            [
                new InteractionInput
                {
                    Name = LocationName,
                    InputType = InputType.Choice,
                    Label = AzureProvisioningStrings.LocationLabel,
                    Required = true,
                    Options = [..locations]
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
                EnableMessageMarkdown = false,
                ValidationCallback = static (validationContext) =>
                {
                    var resourceGroupInput = validationContext.Inputs[ResourceGroupName];
                    if (!IsValidResourceGroupName(resourceGroupInput.Value))
                    {
                        validationContext.AddValidationError(resourceGroupInput, AzureProvisioningStrings.ValidationResourceGroupNameInvalid);
                    }
                    return Task.CompletedTask;
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (!manualResult.Canceled)
        {
            Location = manualResult.Data[LocationName].Value;
            ResourceGroup = manualResult.Data[ResourceGroupName].Value;
            AllowResourceGroupCreation = true;
        }
    }
}
