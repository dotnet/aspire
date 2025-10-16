// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIREPUBLISHERS001

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Resources;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Publishing;
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
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<PublishModeProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider,
    DistributedApplicationExecutionContext distributedApplicationExecutionContext,
    IPublishingActivityReporter activityReporter) : BaseProvisioningContextProvider(
        interactionService,
        options,
        environment,
        logger,
        armClientProvider,
        userPrincipalProvider,
        tokenCredentialProvider,
        distributedApplicationExecutionContext)
{
    protected override string GetDefaultResourceGroupName()
    {
        var prefix = "rg-aspire";

        if (!string.IsNullOrWhiteSpace(_options.ResourceGroupPrefix))
        {
            prefix = _options.ResourceGroupPrefix;
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
            await RetrieveAzureProvisioningOptions(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Azure provisioning options have been handled successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
        }

        return await base.CreateProvisioningContextAsync(userSecrets, cancellationToken).ConfigureAwait(false);
    }

    private async Task RetrieveAzureProvisioningOptions(CancellationToken cancellationToken = default)
    {
        while (_options.Location == null || _options.SubscriptionId == null)
        {
            if (_options.SubscriptionId == null)
            {
                await PromptForSubscriptionAsync(cancellationToken).ConfigureAwait(false);
                if (_options.SubscriptionId == null)
                {
                    continue;
                }
            }

            if (_options.Location == null)
            {
                await PromptForLocationAndResourceGroupAsync(cancellationToken).ConfigureAwait(false);
                if (_options.Location == null)
                {
                    continue;
                }
            }
        }
    }

    private async Task PromptForSubscriptionAsync(CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, string>>? subscriptionOptions = null;
        var fetchSucceeded = false;

        var step = await activityReporter.CreateStepAsync(
            "fetch-subscription",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var task = await step.CreateTaskAsync("Fetching available subscriptions", cancellationToken).ConfigureAwait(false);

                await using (task.ConfigureAwait(false))
                {
                    (subscriptionOptions, fetchSucceeded) = await TryGetSubscriptionsAsync(cancellationToken).ConfigureAwait(false);
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
                _options.SubscriptionId = result.Data[SubscriptionIdName].Value;
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
            _options.SubscriptionId = manualResult.Data[SubscriptionIdName].Value;
        }
    }

    private async Task PromptForLocationAndResourceGroupAsync(CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, string>>? locationOptions = null;
        var fetchSucceeded = false;

        var step = await activityReporter.CreateStepAsync(
            "fetch-regions",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var task = await step.CreateTaskAsync("Fetching supported regions", cancellationToken).ConfigureAwait(false);

                await using (task.ConfigureAwait(false))
                {
                    (locationOptions, fetchSucceeded) = await TryGetLocationsAsync(_options.SubscriptionId!, cancellationToken).ConfigureAwait(false);
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
            _options.Location = result.Data[LocationName].Value;
            _options.ResourceGroup = result.Data[ResourceGroupName].Value;
            _options.AllowResourceGroupCreation = true;
        }
    }
}
