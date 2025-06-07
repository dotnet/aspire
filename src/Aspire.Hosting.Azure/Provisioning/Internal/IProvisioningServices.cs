// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Secrets;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Provides access to Azure ARM client functionality.
/// </summary>
internal interface IArmClientProvider
{
    /// <summary>
    /// Gets the ARM client for Azure resource management.
    /// </summary>
    ArmClient GetArmClient(TokenCredential credential, string subscriptionId);
}

/// <summary>
/// Provides access to Azure Key Vault secret client functionality.
/// </summary>
internal interface ISecretClientProvider
{
    /// <summary>
    /// Gets a secret client for the specified vault URI.
    /// </summary>
    SecretClient GetSecretClient(Uri vaultUri, TokenCredential credential);
}

/// <summary>
/// Provides bicep CLI execution functionality.
/// </summary>
internal interface IBicepCliExecutor
{
    /// <summary>
    /// Compiles a bicep file to ARM template JSON.
    /// </summary>
    Task<string> CompileBicepToArmAsync(string bicepFilePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides user secrets management functionality.
/// </summary>
internal interface IUserSecretsManager
{
    /// <summary>
    /// Loads user secrets from the current application.
    /// </summary>
    Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves user secrets to the current application.
    /// </summary>
    Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides provisioning context creation functionality.
/// </summary>
internal interface IProvisioningContextProvider
{
    /// <summary>
    /// Creates a provisioning context for Azure resource operations.
    /// </summary>
    Task<ProvisioningContext> CreateProvisioningContextAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for Azure ArmClient.
/// </summary>
internal interface IArmClient
{
    /// <summary>
    /// Gets the default subscription.
    /// </summary>
    Task<ISubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all tenants.
    /// </summary>
    IAsyncEnumerable<ITenantResource> GetTenantsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for Azure SubscriptionResource.
/// </summary>
internal interface ISubscriptionResource
{
    /// <summary>
    /// Gets the subscription resource identifier.
    /// </summary>
    ResourceIdentifier Id { get; }
    
    /// <summary>
    /// Gets subscription data.
    /// </summary>
    ISubscriptionData Data { get; }
    
    /// <summary>
    /// Gets a resource group.
    /// </summary>
    Task<IResourceGroupResource> GetResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets resource groups collection.
    /// </summary>
    IResourceGroupCollection GetResourceGroups();
}

/// <summary>
/// Abstraction for Azure SubscriptionData.
/// </summary>
internal interface ISubscriptionData
{
    /// <summary>
    /// Gets the subscription ID.
    /// </summary>
    ResourceIdentifier Id { get; }
    
    /// <summary>
    /// Gets the subscription display name.
    /// </summary>
    string? DisplayName { get; }
    
    /// <summary>
    /// Gets the tenant ID.
    /// </summary>
    Guid? TenantId { get; }
}

/// <summary>
/// Abstraction for Azure ResourceGroupCollection.
/// </summary>
internal interface IResourceGroupCollection
{
    /// <summary>
    /// Gets a resource group.
    /// </summary>
    Task<Response<IResourceGroupResource>> GetAsync(string resourceGroupName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates or updates a resource group.
    /// </summary>
    Task<ArmOperation<IResourceGroupResource>> CreateOrUpdateAsync(WaitUntil waitUntil, string resourceGroupName, ResourceGroupData data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for Azure ResourceGroupResource.
/// </summary>
internal interface IResourceGroupResource
{
    /// <summary>
    /// Gets the resource group resource identifier.
    /// </summary>
    ResourceIdentifier Id { get; }
    
    /// <summary>
    /// Gets resource group data.
    /// </summary>
    IResourceGroupData Data { get; }
    
    /// <summary>
    /// Gets ARM deployments collection.
    /// </summary>
    IArmDeploymentCollection GetArmDeployments();
}

/// <summary>
/// Abstraction for Azure ResourceGroupData.
/// </summary>
internal interface IResourceGroupData
{
    /// <summary>
    /// Gets the resource group name.
    /// </summary>
    string Name { get; }
}

/// <summary>
/// Abstraction for Azure ArmDeploymentCollection.
/// </summary>
internal interface IArmDeploymentCollection
{
    /// <summary>
    /// Creates or updates a deployment.
    /// </summary>
    Task<ArmOperation<ArmDeploymentResource>> CreateOrUpdateAsync(
        WaitUntil waitUntil, 
        string deploymentName, 
        ArmDeploymentContent content, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for Azure TenantResource.
/// </summary>
internal interface ITenantResource
{
    /// <summary>
    /// Gets tenant data.
    /// </summary>
    ITenantData Data { get; }
}

/// <summary>
/// Abstraction for Azure TenantData.
/// </summary>
internal interface ITenantData
{
    /// <summary>
    /// Gets the tenant ID.
    /// </summary>
    Guid? TenantId { get; }
    
    /// <summary>
    /// Gets the default domain.
    /// </summary>
    string? DefaultDomain { get; }
}

/// <summary>
/// Abstraction for Azure AzureLocation.
/// </summary>
internal interface IAzureLocation
{
    /// <summary>
    /// Gets the location name.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Returns the string representation of the location.
    /// </summary>
    string ToString();
}

/// <summary>
/// Provides user principal retrieval functionality.
/// </summary>
internal interface IUserPrincipalProvider
{
    /// <summary>
    /// Gets the user principal from the provided token credential.
    /// </summary>
    Task<UserPrincipal> GetUserPrincipalAsync(TokenCredential credential, CancellationToken cancellationToken = default);
}