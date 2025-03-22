// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a resource that represents an Azure Key Vault.
/// </summary>
public interface IKeyVaultResource : IResource, IAzureResource
{
    /// <summary>
    /// Gets the "vaultUri" output reference for the Azure Key Vault resource.
    /// </summary>
    BicepOutputReference VaultUri { get; }

    /// <summary>
    /// Gets the "name" output reference for the Azure Key Vault resource.
    /// </summary>
    BicepOutputReference NameOutputReference { get; }

    /// <summary>
    /// The secrets for the Azure Key Vault resource. Used in run mode to resolve
    /// the secrets from the Key Vault.
    /// </summary>
    IDictionary<string, string> Secrets { get; }

    /// <summary>
    /// Gets a secret reference for the specified secret name.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <returns>A reference to the secret.</returns>
    IKeyVaultSecretReference GetSecretReference(string secretName);
}
