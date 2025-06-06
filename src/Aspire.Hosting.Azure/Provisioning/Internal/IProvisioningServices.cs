// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Azure.Core;
using Azure.ResourceManager;
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
    /// Gets the user secrets path for the current application.
    /// </summary>
    string? GetUserSecretsPath();

    /// <summary>
    /// Loads user secrets from the specified path.
    /// </summary>
    Task<JsonObject> LoadUserSecretsAsync(string? userSecretsPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves user secrets to the specified path.
    /// </summary>
    Task SaveUserSecretsAsync(string userSecretsPath, JsonObject userSecrets, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides provisioning context creation functionality.
/// </summary>
internal interface IProvisioningContextProvider
{
    /// <summary>
    /// Creates a provisioning context for Azure resource operations.
    /// </summary>
    Task<ProvisioningContext> CreateProvisioningContextAsync(
        TokenCredentialHolder tokenCredentialHolder,
        Lazy<Task<JsonObject>> userSecretsLazy,
        CancellationToken cancellationToken = default);
}