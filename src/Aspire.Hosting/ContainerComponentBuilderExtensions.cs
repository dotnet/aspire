// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class ContainerComponentBuilderExtensions
{
    public static IDistributedApplicationComponentBuilder<ContainerComponent> AddContainer(this IDistributedApplicationBuilder builder, string name,  string image)
    {
        return builder.AddContainer(name, image, "latest");
    }

    public static IDistributedApplicationComponentBuilder<ContainerComponent> AddContainer(this IDistributedApplicationBuilder builder, string name, string image, string tag)
    {
        var container = new ContainerComponent();
        var componentBuilder = builder.AddComponent(name, container);
        componentBuilder.WithAnnotation(new ContainerImageAnnotation { Image = image, Tag = tag });

        return componentBuilder;
    }

    public static IDistributedApplicationComponentBuilder<T> WithServiceBinding<T>(this IDistributedApplicationComponentBuilder<T> builder, int containerPort, int? hostPort = null, string? scheme = null, string? name = null) where T: IDistributedApplicationComponent
    {
        if (builder.Component.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.Name == name))
        {
            throw new DistributedApplicationException($"Service binding with name '{name}' already exists");
        }

        var annotation = new ServiceBindingAnnotation(ProtocolType.Tcp, scheme, name, hostPort, containerPort);
        return builder.WithAnnotation(annotation);
    }

    public static IDistributedApplicationComponentBuilder<T> WithVolumeMount<T>(this IDistributedApplicationComponentBuilder<T> builder, string source, string target, VolumeMountType type = default, bool isReadOnly = false) where T: ContainerComponent
    {
        var annotation = new VolumeMountAnnotation(source, target, type, isReadOnly);
        return builder.WithAnnotation(annotation);
    }
}
