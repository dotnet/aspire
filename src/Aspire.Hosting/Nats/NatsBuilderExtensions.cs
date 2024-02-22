// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding NATS resources to the application model.
/// </summary>
public static class NatsBuilderExtensions
{
    /// <summary>
    /// Adds a NATS server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for NATS server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> AddNats(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var nats = new NatsServerResource(name);
        return builder.AddResource(nats)
                      .WithManifestPublishingCallback(WriteNatsResourceToManifest)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 4222))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "nats", Tag = "latest" });
    }

    /// <summary>
    /// Adds JetStream support to the NATS server resource.
    /// </summary>
    /// <param name="builder">NATS resource builder.</param>
    /// <param name="srcMountPath">Optional mount path providing persistence between restarts.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> WithJetStream(this IResourceBuilder<NatsServerResource> builder, string? srcMountPath = null)
    {
        builder.WithAnnotation(new ExecutableArgsCallbackAnnotation(updatedArgs =>
        {
            updatedArgs.Add("-js");
            if (srcMountPath != null)
            {
                updatedArgs.Add("-sd");
                updatedArgs.Add("/data");
            }
        }));

        if (srcMountPath != null)
        {
            builder.WithAnnotation(new VolumeMountAnnotation(srcMountPath, "/data"));
        }

        return builder;
    }

    /// <summary>
    /// Changes the NATS resource to be published as a container in the manifest.
    /// </summary>
    /// <param name="builder">Resource builder for <see cref="NatsServerResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> PublishAsContainer(this IResourceBuilder<NatsServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(context => WriteNatsContainerResourceToManifest(context, builder.Resource));
    }

    private static void WriteNatsResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "nats.v0");
    }

    private static void WriteNatsContainerResourceToManifest(ManifestPublishingContext context, NatsServerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"nats://{{{resource.Name}.bindings.tcp.host}}:{{{resource.Name}.bindings.tcp.port}}");
    }
}
