// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.RabbitMQ;

namespace Aspire.Hosting;

public static class RabbitMQBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<RabbitMQContainerResource> AddRabbitMQContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        var rabbitMq = new RabbitMQContainerResource(name, password);
        return builder.AddResource(rabbitMq)
                       .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5672))
                       .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "management", port: null, containerPort: 15672))
                       .WithAnnotation(new ContainerImageAnnotation { Image = "rabbitmq", Tag = "3-management" })
                       .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteRabbitMQContainerToManifest))
                       .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                       .WithEnvironment("RABBITMQ_DEFAULT_PASS", () => rabbitMq.Password ?? "guest");
    }

    public static IDistributedApplicationResourceBuilder<RabbitMQConnectionResource> AddRabbitMQConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var rabbitMqConnection = new RabbitMQConnectionResource(name, connectionString);

        return builder.AddResource(rabbitMqConnection)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation((json) => WriteRabbitMQConnectionToManifest(json, rabbitMqConnection)));
    }
    private static void WriteRabbitMQContainerToManifest(Utf8JsonWriter json)
    {
        json.WriteString("type", "rabbitmq.server.v1");
    }

    private static void WriteRabbitMQConnectionToManifest(Utf8JsonWriter json, RabbitMQConnectionResource rabbitMqConnection)
    {
        json.WriteString("type", "rabbitmq.connection.v1");
        json.WriteString("connectionString", rabbitMqConnection.GetConnectionString());
    }
}
