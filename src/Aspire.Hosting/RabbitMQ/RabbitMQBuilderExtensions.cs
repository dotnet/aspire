// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

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
        password ??= Guid.NewGuid().ToString("N");
        var rabbitMq = new RabbitMQContainerResource(name, password);
        return builder.AddResource(rabbitMq)
                       .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 5672))
                       .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "management", port: null, containerPort: 15672))
                       .WithAnnotation(new ContainerImageAnnotation { Image = "rabbitmq", Tag = "3-management" })
                       .WithManifestPublishingCallback(context => WriteRabbitMQContainerToManifest(context, rabbitMq))
                       .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                       .WithEnvironment(context =>
                       {
                           if (context.PublisherName == "manifest")
                           {
                               context.EnvironmentVariables.Add("RABBITMQ_DEFAULT_PASS", $"{{{rabbitMq.Name}.inputs.password}}");
                           }
                           else
                           {
                               context.EnvironmentVariables.Add("RABBITMQ_DEFAULT_PASS", rabbitMq.Password);
                           }
                       });

    }

    /// <summary>
    /// Adds a RabbitMQ resource to the application. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RabbitMQContainerResource}"/>.</returns>
    public static IResourceBuilder<RabbitMQServerResource> AddRabbitMQ(this IDistributedApplicationBuilder builder, string name)
    {
        var password = Guid.NewGuid().ToString("N");
        var rabbitMq = new RabbitMQServerResource(name, password);
        return builder.AddResource(rabbitMq)
                       .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, containerPort: 5672))
                       .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "management", port: null, containerPort: 15672))
                       .WithAnnotation(new ContainerImageAnnotation { Image = "rabbitmq", Tag = "3-management" })
                       .WithManifestPublishingCallback(WriteRabbitMQServerToManifest)
                       .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                       .WithEnvironment("RABBITMQ_DEFAULT_PASS", () => rabbitMq.Password);
    }

    private static void WriteRabbitMQServerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "rabbitmq.server.v0");
    }

    private static void WriteRabbitMQContainerToManifest(ManifestPublishingContext context, RabbitMQContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"amqp://guest:{{{resource.Name}.inputs.password}}@{{{resource.Name}.bindings.management.host}}:{{{resource.Name}.bindings.management.port}}");
        context.Writer.WriteStartObject("inputs");      // "inputs": {
        context.Writer.WriteStartObject("password");    //   "password": {
        context.Writer.WriteString("type", "string");   //     "type": "string",
        context.Writer.WriteBoolean("secret", true);    //     "secret": true,
        context.Writer.WriteStartObject("default");     //     "default": {
        context.Writer.WriteStartObject("generate");    //       "generate": {
        context.Writer.WriteNumber("minLength", 10);    //         "minLength": 10,
        context.Writer.WriteEndObject();                //       }
        context.Writer.WriteEndObject();                //     }
        context.Writer.WriteEndObject();                //   }
        context.Writer.WriteEndObject();                // }
    }
}
