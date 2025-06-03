// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Azure.Core;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Provides ARM client operations for Azure provisioning.
/// </summary>
internal interface IArmClientWrapper
{
    Task<SubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<TenantResource> GetTenantsAsync(CancellationToken cancellationToken);
    Task<ResourceGroupResource> GetResourceGroupAsync(SubscriptionResource subscription, string resourceGroupName, CancellationToken cancellationToken);
    Task<ResourceGroupResource> CreateResourceGroupAsync(SubscriptionResource subscription, string resourceGroupName, AzureLocation location, CancellationToken cancellationToken);
}

/// <summary>
/// Provides SecretClient operations for Azure Key Vault.
/// </summary>
internal interface ISecretClientWrapper
{
    Task<string> GetSecretValueAsync(string secretName, CancellationToken cancellationToken);
}

/// <summary>
/// Provides bicep CLI operations.
/// </summary>
internal interface IBicepCliInvoker
{
    Task<string> CompileTemplateAsync(string bicepFilePath, CancellationToken cancellationToken);
}

/// <summary>
/// Provides user secrets management operations.
/// </summary>
internal interface IUserSecretsManager
{
    Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken);
    Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken);
}

/// <summary>
/// Provides provisioning context for Azure resources.
/// </summary>
internal interface IProvisioningContextProvider
{
    Task<ProvisioningContext> GetProvisioningContextAsync(CancellationToken cancellationToken);
}