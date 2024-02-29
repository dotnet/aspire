// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure resources to the application model.
/// </summary>
public static class AzureResourceExtensions
{
    /// <summary>
    /// Changes the resource to be published as a connection string reference in the manifest.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The configured <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> PublishAsConnectionString<T>(this IResourceBuilder<T> builder)
        where T : IAzureResource, IResourceWithConnectionString
    {
        ParameterResourceBuilderExtensions.ConfigureConnectionStringManifestPublisher((IResourceBuilder<IResourceWithConnectionString>)builder);
        return builder;
    }
}
