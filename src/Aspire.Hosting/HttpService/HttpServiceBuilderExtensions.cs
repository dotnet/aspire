// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Http Service resources to the application model.
/// </summary>
public static class HttpServiceBuilderExtensions
{
    /// <summary>
    /// Adds a Http Service to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="uri"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{HttpServiceResource}"/>.</returns>
    public static IResourceBuilder<HttpServiceResource> AddHttpService(this IDistributedApplicationBuilder builder, string name, string uri)
    {
        return builder.AddHttpService(name, new Uri(uri, UriKind.Absolute));
    }

    /// <summary>
    /// Adds a Http Service to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="uri"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{HttpServiceResource}"/>.</returns>
    public static IResourceBuilder<HttpServiceResource> AddHttpService(this IDistributedApplicationBuilder builder, string name, Uri uri)
    {
        var httpService = new HttpServiceResource(name, uri);
        return builder.AddResource(httpService)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(writer => WriteHttpServiceResourceToManifest(writer, uri)));
    }

    private static void WriteHttpServiceResourceToManifest(Utf8JsonWriter jsonWriter, Uri uri)
    {
        jsonWriter.WriteString("type", "httpservice.v0");
        jsonWriter.WriteString("Uri", uri.ToString());
    }
}
