// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IDistributedApplicationBuilder"/> to add container resources to the application.
/// </summary>
public static class ContainerResourceBuilderExtensions
{
    /// <summary>
    /// Adds a container resource to the application. Uses the "latest" tag.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="image">The container image name. The tag is assumed to be "latest".</param>
    /// <returns>The <see cref="IResourceBuilder{ContainerResource}"/> for chaining.</returns>
    public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, string name, string image)
    {
        return builder.AddContainer(name, image, "latest");
    }

    /// <summary>
    /// Adds a container resource to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="image">The container image name.</param>
    /// <param name="tag">The container image tag.</param>
    /// <returns>The <see cref="IResourceBuilder{ContainerResource}"/> for chaining.</returns>
    public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, string name, string image, string tag)
    {
        var container = new ContainerResource(name);
        return builder.AddResource(container)
                      .WithAnnotation(new ContainerImageAnnotation { Image = image, Tag = tag });
    }

    /// <summary>
    /// Adds a binding to expose an endpoint on a resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="containerPort">The container port.</param>
    /// <param name="hostPort">The host machine port.</param>
    /// <param name="scheme">The scheme e.g http/https/amqp</param>
    /// <param name="name">The name of the binding.</param>
    /// <param name="env">The name of the environment variable to inject.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? scheme = null, string? name = null, string? env = null) where T : IResource
    {
        if (builder.Resource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.Name == name))
        {
            throw new DistributedApplicationException($"Endpoint with name '{name}' already exists");
        }

        var annotation = new EndpointAnnotation(
            protocol: ProtocolType.Tcp,
            uriScheme: scheme,
            name: name,
            port: hostPort,
            containerPort: containerPort,
            env: env);

        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Adds a volume mount to a container resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source path of the volume. This is the physical location on the host.</param>
    /// <param name="target">The target path in the container.</param>
    /// <param name="type">The type of volume mount.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithVolumeMount<T>(this IResourceBuilder<T> builder, string source, string target, VolumeMountType type = default, bool isReadOnly = false) where T : ContainerResource
    {
        var annotation = new VolumeMountAnnotation(source, target, type, isReadOnly);
        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Adds the arguments to be passed to a container resource when the container is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The arguments to be passed to the container when it is started.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>    
    public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, params string[] args) where T : ContainerResource
    {
        var annotation = new ExecutableArgsCallbackAnnotation(updatedArgs =>
        {
            updatedArgs.AddRange(args);
        });
        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Sets the Entrypoint for the container.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="entrypoint">The new entrypoint for the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEntrypoint<T>(this IResourceBuilder<T> builder, string entrypoint) where T : ContainerResource
    {
        builder.Resource.Entrypoint = entrypoint;
        return builder;
    }
}

internal static class IListExtensions
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
    {
        foreach (var item in collection)
        {
            list.Add(item);
        }
    }
}
