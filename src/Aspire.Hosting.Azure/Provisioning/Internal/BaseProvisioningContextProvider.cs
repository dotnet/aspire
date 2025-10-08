#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Aspire.Hosting.Azure.Resources;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Base implementation for <see cref="IProvisioningContextProvider"/> that provides common functionality.
/// </summary>
internal abstract partial class BaseProvisioningContextProvider(
    IInteractionService interactionService,
    IOptions<AzureProvisionerOptions> options,
    IHostEnvironment environment,
    ILogger logger,
    IArmClientProvider armClientProvider,
    IUserPrincipalProvider userPrincipalProvider,
    ITokenCredentialProvider tokenCredentialProvider,
    DistributedApplicationExecutionContext distributedApplicationExecutionContext) : IProvisioningContextProvider
{
    internal const string LocationName = "Location";
    internal const string SubscriptionIdName = "SubscriptionId";
    internal const string ResourceGroupName = "ResourceGroup";

    protected readonly IInteractionService _interactionService = interactionService;
    protected readonly AzureProvisionerOptions _options = options.Value;
    protected readonly IHostEnvironment _environment = environment;
    protected readonly ILogger _logger = logger;
    protected readonly IArmClientProvider _armClientProvider = armClientProvider;
    protected readonly IUserPrincipalProvider _userPrincipalProvider = userPrincipalProvider;
    protected readonly ITokenCredentialProvider _tokenCredentialProvider = tokenCredentialProvider;
    protected readonly DistributedApplicationExecutionContext _distributedApplicationExecutionContext = distributedApplicationExecutionContext;

    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.\(\)]+$")]
    private static partial Regex ResourceGroupValidCharacters();

    protected static bool IsValidResourceGroupName(string? name)
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

    public virtual async Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject deploymentState, CancellationToken cancellationToken = default)
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
            // Create a unique resource group name and save it in deployment state
            resourceGroupName = GetDefaultResourceGroupName();

            createIfAbsent = true;

            deploymentState.Prop("Azure")["ResourceGroup"] = resourceGroupName;
        }
        else
        {
            resourceGroupName = _options.ResourceGroup;
            createIfAbsent = _options.AllowResourceGroupCreation ?? false;
        }

        // In publish mode, always allow resource group creation
        if (_distributedApplicationExecutionContext.IsPublishMode)
        {
            createIfAbsent = true;
            _options.AllowResourceGroupCreation = true;
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

        // Persist the provisioning options to deployment state so they can be reused in the future
        var azureSection = deploymentState.Prop("Azure");
        azureSection["Location"] = _options.Location;
        azureSection["SubscriptionId"] = _options.SubscriptionId;
        azureSection["ResourceGroup"] = resourceGroupName;
        if (_options.AllowResourceGroupCreation.HasValue)
        {
            azureSection["AllowResourceGroupCreation"] = _options.AllowResourceGroupCreation.Value;
        }

        return new ProvisioningContext(
                    credential,
                    armClient,
                    subscriptionResource,
                    resourceGroup,
                    tenantResource,
                    location,
                    principal,
                    deploymentState,
                    _distributedApplicationExecutionContext);
    }

    protected abstract string GetDefaultResourceGroupName();

    /// <summary>
    /// Prompts for subscription using dynamic options when available, falls back to manual input.
    /// </summary>
    protected async Task PromptForSubscriptionAsync(CancellationToken cancellationToken)
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
    protected async Task PromptForLocationAndResourceGroupAsync(CancellationToken cancellationToken)
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
    protected async Task RetrieveAzureProvisioningOptionsAsync(CancellationToken cancellationToken = default)
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
}
