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
    SecretClient GetSecretClient(Uri vaultUri);
}

/// <summary>
/// Provides bicep CLI execution functionality.
/// </summary>
internal interface IBicepCompiler
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
    Task<ProvisioningContext> CreateProvisioningContextAsync(JsonObject userSecrets, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides user principal retrieval functionality.
/// </summary>
internal interface IUserPrincipalProvider
{
    /// <summary>
    /// Gets the user principal.
    /// </summary>
    Task<UserPrincipal> GetUserPrincipalAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides access to Azure token credentials.
/// </summary>
internal interface ITokenCredentialProvider
{
    /// <summary>
    /// Gets the token credential for Azure authentication.
    /// </summary>
    TokenCredential TokenCredential { get; }
}
