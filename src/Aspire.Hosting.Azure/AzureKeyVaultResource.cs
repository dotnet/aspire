// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Key Vault.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureKeyVaultResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.keyvault.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "vaultUri" output reference for the Azure Key Vault resource.
    /// </summary>
    public BicepOutputReference VaultUri => new("vaultUri", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Key Vault resource.
    /// </summary>
    public string ConnectionStringExpression => VaultUri.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure Key Vault resource.
    /// </summary>
    /// <returns>The connection string for the Azure Key Vault resource.</returns>
    public string? GetConnectionString() => VaultUri.Value;
}
