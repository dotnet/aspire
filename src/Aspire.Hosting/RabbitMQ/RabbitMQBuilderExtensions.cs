// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding RabbitMQ resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class RabbitMQBuilderExtensions
{
    /// <summary>
    /// Adds a RabbitMQ resource to the application. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port that the underlying container is bound to when running locally.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RabbitMQServerResource> AddRabbitMQ(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var password = PasswordGenerator.GeneratePassword(6, 6, 2, 2);

        var rabbitMq = new RabbitMQServerResource(name, password);
        return builder.AddResource(rabbitMq)
                       .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 5672))
                       .WithAnnotation(new ContainerImageAnnotation { Image = "rabbitmq", Tag = "3" })
                       .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                       .WithEnvironment(context =>
                       {
                           if (context.ExecutionContext.IsPublishMode)
                           {
                               context.EnvironmentVariables.Add("RABBITMQ_DEFAULT_PASS", $"{{{rabbitMq.Name}.inputs.password}}");
                           }
                           else
                           {
                               context.EnvironmentVariables.Add("RABBITMQ_DEFAULT_PASS", rabbitMq.Password);
                           }
                       })
                       .PublishAsContainer();
    }

    /// <summary>
    /// Changes the RabbitMQ resource to be published as a container in the manifest.
    /// </summary>
    /// <param name="builder">Resource builder for <see cref="RabbitMQServerResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RabbitMQServerResource> PublishAsContainer(this IResourceBuilder<RabbitMQServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(builder.Resource.WriteToManifest);
    }
}
