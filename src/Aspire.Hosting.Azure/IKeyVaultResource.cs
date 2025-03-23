// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Security.KeyVault.Secrets;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a resource that represents an Azure Key Vault.
/// </summary>
public interface IKeyVaultResource : IResource, IAzureResource
{
    /// <summary>
    /// Gets the output reference that represents the vault uri for the Azure Key Vault resource.
    /// </summary>
    BicepOutputReference VaultUriOutputReference { get; }

    /// <summary>
    /// Gets the output reference that represents the name of Azure Key Vault resource.
    /// </summary>
    BicepOutputReference NameOutputReference { get; }

    /// <summary>
    /// the output reference that represents the resource id for the Azure Key Vault resource.
    /// </summary>
    BicepOutputReference IdOutputReference { get; }

    /// <summary>
    /// The secret client used to access the Azure Key Vault in run mode.
    /// </summary>
    SecretClient? SecretClient { get; set; }

    /// <summary>
    /// Gets a secret reference for the specified secret name.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <returns>A reference to the secret.</returns>
    IKeyVaultSecretReference GetSecretReference(string secretName);
}