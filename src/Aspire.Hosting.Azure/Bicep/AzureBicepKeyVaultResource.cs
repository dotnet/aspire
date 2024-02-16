// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Key Vault.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepKeyVaultResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.keyvault.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Key Vault resource.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Name}.outputs.vaultUri}}";

    /// <summary>
    /// Gets the connection string for the Azure Key Vault resource.
    /// </summary>
    /// <returns>The connection string for the Azure Key Vault resource.</returns>
    public string? GetConnectionString()
    {
        return Outputs["vaultUri"];
    }
}

/// <summary>
/// Provides extension methods for adding the Azure Key Vault resources to the application model.
/// </summary>
public static class AzureBicepKeyVaultResourceExtensions
{
    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepKeyVaultResource> AddBicepKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepKeyVaultResource(name);
        return builder.AddResource(resource)
                    .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                    .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                    .WithParameter("vaultName", resource.CreateBicepResourceName())
                    .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
