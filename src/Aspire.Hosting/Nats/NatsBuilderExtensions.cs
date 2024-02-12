// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Nats;

public static class NatsBuilderExtensions
{
    /// <summary>
    /// Adds a NATS server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for NATS server.</param>
    /// <param name="enableJetStream">Enable JetStream.</param>
    /// <param name="srcMountPath">JetStream data source mount path to persist streams between restarts.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> AddNats(
        this IDistributedApplicationBuilder builder, string name, int? port = null, bool enableJetStream = false, string? srcMountPath = null)
    {
        var nats = new NatsServerResource(name);
        var resourceBuilder = builder.AddResource(nats)
            .WithManifestPublishingCallback(WriteNatsResourceToManifest)
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "nats", port: port, containerPort: 4222))
            .WithAnnotation(new ContainerImageAnnotation { Image = "nats", Tag = "latest" });

        if (enableJetStream)
        {
            resourceBuilder
                .WithAnnotation(new ExecutableArgsCallbackAnnotation(updatedArgs =>
                {
                    if (enableJetStream)
                    {
                        updatedArgs.Add("-js");
                    }

                    if (srcMountPath != null)
                    {
                        updatedArgs.Add("-sd");
                        updatedArgs.Add("/data");
                    }
                }));

            if (srcMountPath != null)
            {
                resourceBuilder.WithAnnotation(new VolumeMountAnnotation(srcMountPath, "/data"));
            }
        }

        return resourceBuilder;
    }

    private static void WriteNatsResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "nats.v0");
    }
}
