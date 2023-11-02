// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding RabbitMQ resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class RabbitMQBuilderExtensions
{
    /// <summary>
    /// Adds a RabbitMQ container to the application. The default image name is "rabbitmq" and the default tag is "3-management".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port of RabbitMQ.</param>
    /// <param name="password">The password for RabbitMQ. The default is "guest".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RabbitMQContainerResource}"/>.</returns>
    public static IResourceBuilder<RabbitMQContainerResource> AddRabbitMQContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
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

    /// <summary>
    /// Adds a RabbitMQ connection resource to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">A RabbitMQ connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RabbitMQConnectionResource}"/>.</returns>
    public static IResourceBuilder<RabbitMQConnectionResource> AddRabbitMQConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var rabbitMqConnection = new RabbitMQConnectionResource(name, connectionString);

        return builder.AddResource(rabbitMqConnection)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation((json) => WriteRabbitMQConnectionToManifest(json, rabbitMqConnection)));
    }
    private static void WriteRabbitMQContainerToManifest(Utf8JsonWriter json)
    {
        json.WriteString("type", "rabbitmq.server.v0");
    }

    private static void WriteRabbitMQConnectionToManifest(Utf8JsonWriter json, RabbitMQConnectionResource rabbitMqConnection)
    {
        json.WriteString("type", "rabbitmq.connection.v0");
        json.WriteString("connectionString", rabbitMqConnection.GetConnectionString());
    }
}
