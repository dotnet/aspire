// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azurite;

namespace Aspire.Hosting;

public static class AzuriteBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<AzuriteContainerResource> AddAzuriteContainer(
        this IDistributedApplicationBuilder builder,
        string name,
        int? blobPort = null,
        int? queuePort = null,
        int? tablePort = null)
    {
        var Azurite = new AzuriteContainerResource(name);
        return builder.AddResource(Azurite)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzuriteResourceToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, name: "blob", port: blobPort, containerPort: 10000))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, name: "queue", port: queuePort, containerPort: 10001))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, name: "table", port: tablePort, containerPort: 10002))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/azure-storage/azurite", Tag = "latest" });
    }

    public static IDistributedApplicationResourceBuilder<AzuriteResource> AddAzurite(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var azurite = new AzuriteResource(name, connectionString);
        return builder.AddResource(azurite)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                        WriteAzuriteResourceToManifest(jsonWriter, azurite.GetConnectionString())));
    }

    private static void WriteAzuriteResourceToManifest(Utf8JsonWriter jsonWriter) =>
        WriteAzuriteResourceToManifest(jsonWriter, null);

    private static void WriteAzuriteResourceToManifest(Utf8JsonWriter jsonWriter, string? connectionString)
    {
        jsonWriter.WriteString("type", "Azurite.v1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }
}
