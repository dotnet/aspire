// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

public static class KeycloakBuilderExtensions
{
    private const int DefaultContainerPort = 8080;

    public static IResourceBuilder<KeycloakContainerResource> AddKeycloakContainer(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var keycloakContainer = new KeycloakContainerResource(name);

        return builder
            .AddResource(keycloakContainer)
            .WithManifestPublishingCallback(WriteKeycloakContainerToManifest)
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port, containerPort: DefaultContainerPort))
            .WithAnnotation(new ContainerImageAnnotation { Image = "quay.io/keycloak/keycloak", Tag = "latest" })
            .WithEnvironment("KEYCLOAK_ADMIN","admin")
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD","admin")
            .WithArgs("start-dev");
    }

    private static void WriteKeycloakContainerToManifest(this ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "keycloak.server.v0");
    }
}