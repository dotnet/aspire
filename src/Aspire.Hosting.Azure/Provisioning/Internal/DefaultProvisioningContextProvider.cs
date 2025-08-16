#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Aspire.Hosting.Azure.Resources;
using Aspire.Hosting.Azure.Utils;
using Aspire.Hosting.Publishing;
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
internal sealed partial class DefaultProvisioningContextProvider(
    IInteractionService interactionService,
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<DefaultProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider,
    DistributedApplicationExecutionContext distributedApplicationExecutionContext,
    IOptions<PublishingOptions> publishingOptions) : IProvisioningContextProvider
{
    private readonly AzureProvisionerOptions _options = options.Value;
    private readonly PublishingOptions _publishingOptions = publishingOptions.Value;

    private readonly TaskCompletionSource _provisioningOptionsAvailable = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private void EnsureProvisioningOptions(JsonObject userSecrets)
    {
        if (!interactionService.IsAvailable ||
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
                await RetrieveAzureProvisioningOptions(userSecrets).ConfigureAwait(false);

                logger.LogDebug("Azure provisioning options have been handled successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
                _provisioningOptionsAvailable.SetException(ex);
            }
        });
    }

    private async Task RetrieveAzureProvisioningOptions(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        while (_options.Location == null || _options.SubscriptionId == null)
        {
            var messageBarResult = await interactionService.PromptNotificationAsync(
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
                var selectedSubscriptionId = await PromptForSubscriptionAsync(cancellationToken).ConfigureAwait(false);

                if (selectedSubscriptionId == null)
                {
                    return;
                }

                var (selectedLocation, selectedResourceGroup) = await PromptForLocationAndResourceGroupAsync(selectedSubscriptionId, cancellationToken).ConfigureAwait(false);

                if (selectedLocation == null)
                {
                    return;
                }

                // Set the options
                _options.SubscriptionId = selectedSubscriptionId;
                _options.Location = selectedLocation;
                _options.ResourceGroup = selectedResourceGroup;
                _options.AllowResourceGroupCreation = true; // Allow the creation of the resource group if it does not exist.

                var azureSection = userSecrets.Prop("Azure");

                // Persist the parameter value to user secrets so they can be reused in the future
                azureSection["SubscriptionId"] = _options.SubscriptionId;
                azureSection["Location"] = _options.Location;
                azureSection["ResourceGroup"] = _options.ResourceGroup;

                _provisioningOptionsAvailable.SetResult();
            }
        }
    }

    private async Task<string?> PromptForSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        // Get the available subscriptions based on the current credential or fallback to text input
        try
        {
            var credential = tokenCredentialProvider.TokenCredential;
            var armClient = armClientProvider.GetArmClient(credential);

            logger.LogInformation("Fetching available Azure subscriptions...");
            var availableSubscriptions = await armClient.GetAvailableSubscriptionsAsync(cancellationToken).ConfigureAwait(false);

            if (availableSubscriptions.Count == 0)
            {
                throw new InvalidOperationException("No Azure subscriptions are available for the authenticated user.");
            }

            var subscriptionOptions = availableSubscriptions
                .Select(s => KeyValuePair.Create(s.SubscriptionId, $"{s.DisplayName} ({s.SubscriptionId})"))
                .ToList();

            var result = await interactionService.PromptInputsAsync(
                AzureProvisioningStrings.SubscriptionInputsTitle,
                AzureProvisioningStrings.SubscriptionInputsMessage,
                [
                    new InteractionInput
                    {
                        InputType = InputType.Choice,
                        Label = AzureProvisioningStrings.SubscriptionLabel,
                        Placeholder = AzureProvisioningStrings.SubscriptionPlaceholder,
                        Required = true,
                        Options = [..subscriptionOptions]
                    }
                ],
                new InputsDialogInteractionOptions
                {
                    EnableMessageMarkdown = true
                },
                cancellationToken).ConfigureAwait(false);

            return result.Canceled ? null : result.Data?[0].Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Azure subscriptions. Falling back to manual subscription ID entry.");

            var result = await interactionService.PromptInputsAsync(
                AzureProvisioningStrings.SubscriptionInputsTitle,
                "Unable to automatically fetch subscriptions. Please enter your subscription ID manually.",
                [
                    new InteractionInput
                    {
                        InputType = InputType.SecretText,
                        Label = AzureProvisioningStrings.SubscriptionIdLabel,
                        Placeholder = AzureProvisioningStrings.SubscriptionIdPlaceholder,
                        Required = true
                    }
                ],
                new InputsDialogInteractionOptions
                {
                    EnableMessageMarkdown = true,
                    ValidationCallback = static (validationContext) =>
                    {
                        var subscriptionInput = validationContext.Inputs[0];
                        if (!Guid.TryParse(subscriptionInput.Value, out var _))
                        {
                            validationContext.AddValidationError(subscriptionInput, AzureProvisioningStrings.ValidationSubscriptionIdInvalid);
                        }
                        return Task.CompletedTask;
                    }
                },
                cancellationToken).ConfigureAwait(false);

            return result.Canceled ? null : result.Data?[0].Value;
        }
    }

    private async Task<(string? location, string? resourceGroup)> PromptForLocationAndResourceGroupAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        // Get available locations for the subscription or fallback to static lookup
        List<KeyValuePair<string, string>> locations;
        try
        {
            var armClient = armClientProvider.GetArmClient(tokenCredentialProvider.TokenCredential);
            var availableLocations = await armClient.GetAvailableLocationsAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
            locations = [.. availableLocations
                .Select(location => KeyValuePair.Create(location.Name, location.DisplayName))
                .OrderBy(kvp => kvp.Value)];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve available locations for subscription {SubscriptionId}. Using static location list.", subscriptionId);
            locations = [.. typeof(AzureLocation).GetProperties(BindingFlags.Public | BindingFlags.Static)
                                .Where(p => p.PropertyType == typeof(AzureLocation))
                                .Select(p => (AzureLocation)p.GetValue(null)!)
                                .Select(location => KeyValuePair.Create(location.Name, location.DisplayName ?? location.Name))
                                .OrderBy(kvp => kvp.Value)];
        }

        var result = await interactionService.PromptInputsAsync(
            AzureProvisioningStrings.LocationAndResourceGroupInputsTitle,
            AzureProvisioningStrings.LocationAndResourceGroupInputsMessage,
            [
                new InteractionInput { InputType = InputType.Choice, Label = AzureProvisioningStrings.LocationLabel, Placeholder = AzureProvisioningStrings.LocationPlaceholder, Required = true, Options = [..locations] },
                new InteractionInput { InputType = InputType.Text, Label = AzureProvisioningStrings.ResourceGroupLabel, Value = GetDefaultResourceGroupName() },
            ],
            new InputsDialogInteractionOptions
            {
                EnableMessageMarkdown = true,
                ValidationCallback = static (validationContext) =>
                {
                    var resourceGroupInput = validationContext.Inputs[1];
                    if (!IsValidResourceGroupName(resourceGroupInput.Value))
                    {
                        validationContext.AddValidationError(resourceGroupInput, AzureProvisioningStrings.ValidationResourceGroupNameInvalid);
                    }

                    return Task.CompletedTask;
                }
            },
            cancellationToken).ConfigureAwait(false);

        return result.Canceled ? (null, null) : (result.Data?[0].Value, result.Data?[1].Value);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.\(\)]+$")]
    private static partial Regex ResourceGroupValidCharacters();

    private static bool IsValidResourceGroupName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 90)
        {
            return false;
        }

        // Only allow valid characters - letters, digits, underscores, hyphens, periods, and parentheses
        if (!ResourceGroupValidCharacters().IsMatch(name))
        {
            return false;
        }

        // Must start with a letter
        if (!char.IsLetter(name[0]))
        {
            return false;
        }

        // Cannot end with a period
        if (name.EndsWith('.'))
        {
            return false;
        }

        // No consecutive periods
        return !name.Contains("..");
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
        var outputPath = _publishingOptions.OutputPath is { } outputPathValue ? Path.GetFullPath(outputPathValue) : null;

        return new ProvisioningContext(
                    credential,
                    armClient,
                    subscriptionResource,
                    resourceGroup,
                    tenantResource,
                    location,
                    principal,
                    userSecrets,
                    distributedApplicationExecutionContext,
                    outputPath);
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

        return distributedApplicationExecutionContext.IsPublishMode
            ? $"{prefix}-{normalizedApplicationName}"
            : $"{prefix}-{normalizedApplicationName}-{suffix}";
    }
}
