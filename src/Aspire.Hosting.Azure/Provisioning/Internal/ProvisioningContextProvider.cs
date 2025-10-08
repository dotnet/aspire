#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Aspire.Hosting.Azure.Resources;
using Aspire.Hosting.Azure.Utils;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Unified implementation for <see cref="IProvisioningContextProvider"/> that handles both run mode and publish mode scenarios.
/// Uses enhanced prompting logic with dynamic subscription and location fetching.
/// </summary>
internal sealed partial class ProvisioningContextProvider(
    IInteractionService interactionService,
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger<ProvisioningContextProvider> logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider,
    DistributedApplicationExecutionContext distributedApplicationExecutionContext) : IProvisioningContextProvider
{
    internal const string LocationName = "Location";
    internal const string SubscriptionIdName = "SubscriptionId";
    internal const string ResourceGroupName = "ResourceGroup";

    private readonly IInteractionService _interactionService = interactionService;
    private readonly AzureProvisionerOptions _options = options.Value;
    private readonly IHostEnvironment _environment = environment;
    private readonly ILogger _logger = logger;
    private readonly IArmClientProvider _armClientProvider = armClientProvider;
    private readonly IUserPrincipalProvider _userPrincipalProvider = userPrincipalProvider;
    private readonly ITokenCredentialProvider _tokenCredentialProvider = tokenCredentialProvider;
    private readonly DistributedApplicationExecutionContext _distributedApplicationExecutionContext = distributedApplicationExecutionContext;

    // For run mode - tracks when provisioning options are available
    private readonly TaskCompletionSource _provisioningOptionsAvailable = new(TaskCreationOptions.RunContinuationsAsynchronously);

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

    private string GetDefaultResourceGroupName()
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

        // Add random suffix for run mode to ensure uniqueness, but not for publish mode for consistency
        if (_distributedApplicationExecutionContext.IsPublishMode)
        {
            return $"{prefix}-{normalizedApplicationName}";
        }
        else
        {
            var suffix = RandomNumberGenerator.GetHexString(8, lowercase: true);
            var maxAppNameWithSuffix = maxApplicationNameSize - suffix.Length - 1; // extra '-'

            if (normalizedApplicationName.Length > maxAppNameWithSuffix)
            {
                normalizedApplicationName = normalizedApplicationName[..maxAppNameWithSuffix];
            }

            return $"{prefix}-{normalizedApplicationName}-{suffix}";
        }
    }

    public async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        if (_distributedApplicationExecutionContext.IsPublishMode)
        {
            // Publish mode: Direct prompting
            try
            {
                await RetrieveAzureProvisioningOptions(userSecrets, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Azure provisioning options have been handled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
                throw;
            }
        }
        else
        {
            // Run mode: Async prompting with task completion source
            EnsureProvisioningOptions(userSecrets);
            await _provisioningOptionsAvailable.Task.ConfigureAwait(false);
        }

        return await CreateProvisioningContextInternalAsync(userSecrets, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureProvisioningOptions(JsonObject userSecrets)
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
                await RetrieveAzureProvisioningOptions(userSecrets).ConfigureAwait(false);

                _logger.LogDebug("Azure provisioning options have been handled successfully.");
                _provisioningOptionsAvailable.SetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure provisioning options.");
                _provisioningOptionsAvailable.SetException(ex);
            }
        });
    }

    private async Task RetrieveAzureProvisioningOptions(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        if (_distributedApplicationExecutionContext.IsPublishMode)
        {
            // Publish mode: Direct dynamic prompting
            await RetrieveAzureProvisioningOptionsAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Run mode: Notification prompt first, then dynamic prompting
            await RetrieveAzureProvisioningOptionsForRunMode(userSecrets, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RetrieveAzureProvisioningOptionsForRunMode(JsonObject userSecrets, CancellationToken cancellationToken = default)
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
                throw new MissingConfigurationException("Azure provisioning options were not provided.");
            }

            if (messageBarResult.Data)
            {
                // Use the shared dynamic prompting method
                await RetrieveAzureProvisioningOptionsAsync(cancellationToken).ConfigureAwait(false);

                if (_options.Location != null && _options.SubscriptionId != null && _options.ResourceGroup != null)
                {
                    var azureSection = userSecrets.Prop("Azure");

                    // Persist the parameter value to user secrets so they can be reused in the future
                    azureSection["Location"] = _options.Location;
                    azureSection["SubscriptionId"] = _options.SubscriptionId;
                    azureSection["ResourceGroup"] = _options.ResourceGroup;

                    break; // Exit the loop
                }
            }
        }
    }

    /// <summary>
    /// Prompts for subscription using dynamic options when available, falls back to manual input.
    /// </summary>
    private async Task PromptForSubscriptionAsync(CancellationToken cancellationToken)
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
                    AllowCustomChoice = true,
                    Placeholder = AzureProvisioningStrings.SubscriptionIdPlaceholder,
                    DynamicOptions = new DynamicInputOptions
                    {
                        AlwaysUpdateOnStart = true,
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
                }
            ],
            new InputsDialogInteractionOptions
            {
                EnableMessageMarkdown = false,
                ValidationCallback = static (validationContext) =>
                {
                    var subscriptionInput = validationContext.Inputs[SubscriptionIdName];
                    if (!string.IsNullOrWhiteSpace(subscriptionInput.Value) && !Guid.TryParse(subscriptionInput.Value, out var _))
                    {
                        validationContext.AddValidationError(subscriptionInput, AzureProvisioningStrings.ValidationSubscriptionIdInvalid);
                    }
                    return Task.CompletedTask;
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (!result.Canceled)
        {
            _options.SubscriptionId = result.Data[SubscriptionIdName].Value;
        }
    }

    /// <summary>
    /// Prompts for location and resource group using dynamic options when available, falls back to static locations.
    /// </summary>
    private async Task PromptForLocationAndResourceGroupAsync(CancellationToken cancellationToken)
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
                    DynamicOptions = new DynamicInputOptions
                    {
                        AlwaysUpdateOnStart = true,
                        UpdateInputCallback = async (context) =>
                        {
                            try
                            {
                                var credential = _tokenCredentialProvider.TokenCredential;
                                var armClient = _armClientProvider.GetArmClient(credential);
                                var availableLocations = await armClient.GetAvailableLocationsAsync(_options.SubscriptionId!, context.CancellationToken).ConfigureAwait(false);
                                var locationList = availableLocations.ToList();

                                if (locationList.Count > 0)
                                {
                                    var locationOptions = locationList
                                        .Select(loc => KeyValuePair.Create(loc.Name, loc.DisplayName))
                                        .OrderBy(kvp => kvp.Value)
                                        .ToList();

                                    context.Input.Options = locationOptions;
                                }
                                else
                                {
                                    // Fall back to static locations from AzureLocation
                                    var staticLocations = GetStaticAzureLocations();
                                    context.Input.Options = staticLocations;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to enumerate available locations. Falling back to static locations.");

                                // Fall back to static locations from AzureLocation
                                var staticLocations = GetStaticAzureLocations();
                                context.Input.Options = staticLocations;
                            }
                        }
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

    /// <summary>
    /// Gets static Azure locations as fallback when dynamic loading fails.
    /// </summary>
    private static List<KeyValuePair<string, string>> GetStaticAzureLocations()
    {
        return typeof(AzureLocation).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(AzureLocation))
            .Select(p => (AzureLocation)p.GetValue(null)!)
            .Select(location => KeyValuePair.Create(location.Name, location.DisplayName ?? location.Name))
            .OrderBy(kvp => kvp.Value)
            .ToList();
    }

    /// <summary>
    /// Common method to retrieve Azure provisioning options with dynamic prompts.
    /// </summary>
    private async Task RetrieveAzureProvisioningOptionsAsync(CancellationToken cancellationToken = default)
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

    private async Task<ProvisioningContext> CreateProvisioningContextInternalAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        var subscriptionId = _options.SubscriptionId ?? throw new MissingConfigurationException("An Azure subscription id is required. Set the Azure:SubscriptionId configuration value.");

        var credential = _tokenCredentialProvider.TokenCredential;

        if (_tokenCredentialProvider is DefaultTokenCredentialProvider defaultProvider)
        {
            defaultProvider.LogCredentialType();
        }

        var armClient = _armClientProvider.GetArmClient(credential, subscriptionId);

        _logger.LogInformation("Getting default subscription and tenant...");

        var (subscriptionResource, tenantResource) = await armClient.GetSubscriptionAndTenantAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Default subscription: {name} ({subscriptionId})", subscriptionResource.DisplayName, subscriptionResource.Id);
        _logger.LogInformation("Tenant: {tenantId}", tenantResource.TenantId);

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

            _logger.LogInformation("Using existing resource group {rgName}.", resourceGroup.Name);
        }
        catch (Exception)
        {
            if (!createIfAbsent)
            {
                throw;
            }

            // REVIEW: Is it possible to do this without an exception?

            _logger.LogInformation("Creating resource group {rgName} in {location}...", resourceGroupName, location);

            var rgData = new ResourceGroupData(location);
            rgData.Tags.Add("aspire", "true");
            var operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData, cancellationToken).ConfigureAwait(false);
            resourceGroup = operation.Value;

            _logger.LogInformation("Resource group {rgName} created.", resourceGroup.Name);
        }

        var principal = await _userPrincipalProvider.GetUserPrincipalAsync(cancellationToken).ConfigureAwait(false);

        return new ProvisioningContext(
                    credential,
                    armClient,
                    subscriptionResource,
                    resourceGroup,
                    tenantResource,
                    location,
                    principal,
                    userSecrets,
                    _distributedApplicationExecutionContext);
    }
}
