// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AppConfiguration resources to the application model.
/// </summary>
public static class AzureAppConfigurationExtensions
{
    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureAppConfigurationResource(name);
        return builder.AddResource(resource)
                .WithParameter("configName", resource.CreateBicepResourceName())
                .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
