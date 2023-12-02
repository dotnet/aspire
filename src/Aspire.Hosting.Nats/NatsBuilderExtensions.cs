// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Nats;

public static class NatsBuilderExtensions
{
    public static IResourceBuilder<NatsContainerResource> AddNatsContainer(
        this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var nats = new NatsContainerResource(name);
        return builder.AddResource(nats)
            .WithManifestPublishingCallback(WriteNatsResourceToManifest)
            .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: "nats", port: port, containerPort: 4222))
            .WithAnnotation(new ContainerImageAnnotation { Image = "nats", Tag = "latest" });
    }

    private static void WriteNatsResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "nats.v0");
    }
}
