// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Nats;

public static class NatsBuilderExtensions
{
    public static IResourceBuilder<NatsContainerResource> AddNatsContainer(
        this IDistributedApplicationBuilder builder, string name, int? port = null, bool enableJetStream = false, string? mountPath = null)
    {
        var nats = new NatsContainerResource(name);
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

                    if (mountPath != null)
                    {
                        updatedArgs.Add("-sd");
                        updatedArgs.Add("/data");
                    }
                }));

            if (mountPath != null)
            {
                resourceBuilder.WithAnnotation(new VolumeMountAnnotation(mountPath, "/data"));
            }
        }

        return resourceBuilder;
    }

    private static void WriteNatsResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "nats.v0");
    }
}
