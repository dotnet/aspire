// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
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
        var address = uri.GetLeftPart(UriPartial.Authority).Remove(0, uri.GetLeftPart(UriPartial.Scheme).Length);

        var httpService = new HttpServiceResource(name);
        return builder.AddResource(httpService)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(writer => WriteHttpServiceResourceToManifest(writer, uri)))
                      .WithAnnotation(new AllocatedEndpointAnnotation(name, ProtocolType.Tcp, address, uri.Port, uri.Scheme));

        // Should this be done with ServiceBindingAnnotation or AllocatedEndpointAnnotation??
        //.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: uri.Scheme, port: uri.Port, isExternal: true));
    }

    private static void WriteHttpServiceResourceToManifest(Utf8JsonWriter jsonWriter, Uri uri)
    {
        jsonWriter.WriteString("type", "httpservice.v0");
        jsonWriter.WriteString("url", uri.ToString());
    }
}
