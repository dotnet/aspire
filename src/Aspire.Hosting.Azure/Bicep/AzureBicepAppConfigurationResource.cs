// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents Azure App Configuration.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepAppConfigurationResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.appconfig.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string template for the manifest for the Azure App Configuration resource.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Name}.outputs.appConfigEndpoint}}";

    /// <summary>
    /// Gets the connection string for the Azure App Configuration resource.
    /// </summary>
    /// <returns>The connection string for the Azure App Configuration resource.</returns>
    public string? GetConnectionString()
    {
        return Outputs["appConfigEndpoint"];
    }
}

/// <summary>
/// Provides extension methods for adding the Azure AppConfiguration resources to the application model.
/// </summary>
public static class AzureBicepAppConfigurationExtensions
{
    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepAppConfigurationResource> AddBicepAppConfiguration(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepAppConfigurationResource(name);
        return builder.AddResource(resource)
                .WithParameter("configName", resource.CreateBicepResourceName())
                .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
