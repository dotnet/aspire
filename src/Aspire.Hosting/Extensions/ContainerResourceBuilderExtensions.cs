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
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
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
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, string name, string image, string tag)
    {
        var container = new ContainerResource(name);
        return builder.AddResource(container)
                      .WithAnnotation(new ContainerImageAnnotation { Image = image, Tag = tag });
    }

    [Obsolete("WithServiceBinding has been renamed to WithEndpoint. Use WithEndpoint instead.")]
    public static IResourceBuilder<T> WithServiceBinding<T>(this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? scheme = null, string? name = null, string? env = null) where T : IResource
    {
        return builder.WithEndpoint(containerPort: containerPort, hostPort: hostPort, scheme: scheme, name: name, env: env);
    }

    /// <summary>
    /// Exposes an endpoint on a resource. This binding reference can be retrieved using <see cref="ResourceBuilderExtensions.GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The endpoint name will be the scheme name if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="containerPort">The container port.</param>
    /// <param name="hostPort">An optional host port.</param>
    /// <param name="scheme">An optional scheme e.g. (http/https). Defaults to "tcp" if not specified.</param>
    /// <param name="name">An optional name of the endpoint. Defaults to the scheme name if not specified.</param>
    /// <param name="env">An optional name of the environment variable to inject.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? scheme = null, string? name = null, string? env = null) where T : IResource
    {
        if (builder.Resource.Annotations.OfType<EndpointAnnotation>().Any(sb => string.Equals(sb.Name, name, StringComparisons.EndpointAnnotationName)))
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
    /// Exposes an HTTP endpoint on a resource. This binding reference can be retrieved using <see cref="ResourceBuilderExtensions.GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The binding name will be "http" if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="containerPort">The container port.</param>
    /// <param name="hostPort">An optional host port.</param>
    /// <param name="name">An optional name of the endpoint. Defaults to "http" if not specified.</param>
    /// <param name="env">An optional name of the environment variable to inject.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithHttpEndpoint<T>(this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? name = null, string? env = null) where T : IResource
    {
        return builder.WithEndpoint(containerPort: containerPort, hostPort: hostPort, scheme: "http", name: name, env: env);
    }

    /// <summary>
    /// Exposes an HTTPS endpoint on a resource. This binding reference can be retrieved using <see cref="ResourceBuilderExtensions.GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The binding name will be "https" if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="containerPort">The container port.</param>
    /// <param name="hostPort">An optional host port.</param>
    /// <param name="name">An optional name of the endpoint. Defaults to "https" if not specified.</param>
    /// <param name="env">An optional name of the environment variable to inject.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithHttpsEndpoint<T>(this IResourceBuilder<T> builder, int containerPort, int? hostPort = null, string? name = null, string? env = null) where T : IResource
    {
        return builder.WithEndpoint(containerPort: containerPort, hostPort: hostPort, scheme: "https", name: name, env: env);
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
