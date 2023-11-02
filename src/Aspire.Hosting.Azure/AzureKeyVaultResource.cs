// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Key Vault resource that can be deployed to an Azure resource group.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureKeyVaultResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the URI of the Azure Key Vault.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets the connection string for the Azure Key Vault resource.
    /// </summary>
    /// <returns>The connection string for the Azure Key Vault resource.</returns>
    public string? GetConnectionString() => VaultUri?.ToString();
}
