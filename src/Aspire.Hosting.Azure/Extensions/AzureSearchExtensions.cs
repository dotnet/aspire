// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Search resources to the application model.
/// </summary>
public static class AzureSearchExtensions
{
    /// <summary>
    /// Adds an Azure Search resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSearchResource}"/>.</returns>
    public static IResourceBuilder<AzureSearchResource> AddAzureSearch(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureSearchResource(name);
        return builder.AddResource(resource)
                .WithParameter("name", resource.CreateBicepResourceName())
                .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
