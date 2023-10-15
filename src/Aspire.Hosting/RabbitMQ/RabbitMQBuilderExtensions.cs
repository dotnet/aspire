// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.RabbitMQ;

namespace Aspire.Hosting;

public static class RabbitMQBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<RabbitMQContainerResource> AddRabbitMQContainer(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var rabbitMq = new RabbitMQContainerResource(name);
        var resourceBuilder = builder.AddResource(rabbitMq);
        resourceBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5672)); // Internal port is always 5672.
        resourceBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "management", port: null, containerPort: 15672));
        resourceBuilder.WithAnnotation(new ContainerImageAnnotation { Image = "rabbitmq", Tag = "3-management" });
        return resourceBuilder;
    }
}
