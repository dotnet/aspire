#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Hosting.DeploymentState;
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
    IDeploymentStateProvider deploymentStateProvider,
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
    protected readonly IDeploymentStateProvider _deploymentStateProvider = deploymentStateProvider;
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

    public virtual async Task<ProvisioningContext> CreateProvisioningContextAsync(CancellationToken cancellationToken = default)
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

            var state = await _deploymentStateProvider.LoadAsync(cancellationToken).ConfigureAwait(false);
            state.Prop("Azure")["ResourceGroup"] = resourceGroupName;
            await _deploymentStateProvider.SaveAsync(state, cancellationToken).ConfigureAwait(false);
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
                    _deploymentStateProvider,
                    _distributedApplicationExecutionContext);
    }

    protected abstract string GetDefaultResourceGroupName();
}
