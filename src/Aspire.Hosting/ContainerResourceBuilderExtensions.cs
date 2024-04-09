// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                      .WithImage(image, tag);
    }

    /// <summary>
    /// Adds a volume to a container resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume.</param>
    /// <param name="target">The target path where the volume is mounted in the container.</param>
    /// <param name="isReadOnly">A flag that indicates if the volume should be mounted as read-only.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, string name, string target, bool isReadOnly = false) where T : ContainerResource
    {
        var annotation = new ContainerMountAnnotation(name, target, ContainerMountType.Volume, isReadOnly);
        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Adds an anonymous volume to a container resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="target">The target path where the volume is mounted in the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, string target) where T : ContainerResource
    {
        var annotation = new ContainerMountAnnotation(null, target, ContainerMountType.Volume, false);
        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Adds a bind mount to a container resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source path of the mount. This is the path to the file or directory on the host.</param>
    /// <param name="target">The target path where the file or directory is mounted in the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithBindMount<T>(this IResourceBuilder<T> builder, string source, string target, bool isReadOnly = false) where T : ContainerResource
    {
        var annotation = new ContainerMountAnnotation(Path.GetFullPath(source, builder.ApplicationBuilder.AppHostDirectory), target, ContainerMountType.BindMount, isReadOnly);
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

    /// <summary>
    /// Allows overriding the image tag on a container.
    /// </summary>
    /// <typeparam name="T">Type of container resource.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="tag">Tag value.</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithImageTag<T>(this IResourceBuilder<T> builder, string tag) where T : ContainerResource
    {
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } existingImageAnnotation)
        {
            existingImageAnnotation.Tag = tag;
            return builder;
        }

        return ThrowResourceIsNotContainer(builder);
    }

    /// <summary>
    /// Allows overriding the image registry on a container.
    /// </summary>
    /// <typeparam name="T">Type of container resource.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="registry">Registry value.</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithImageRegistry<T>(this IResourceBuilder<T> builder, string registry) where T : ContainerResource
    {
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } existingImageAnnotation)
        {
            existingImageAnnotation.Registry = registry;
            return builder;
        }

        return ThrowResourceIsNotContainer(builder);
    }

    /// <summary>
    /// Allows overriding the image on a container.
    /// </summary>
    /// <typeparam name="T">Type of container resource.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="image">Image value.</param>
    /// <param name="tag">Tag value.</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithImage<T>(this IResourceBuilder<T> builder, string image, string tag = "latest") where T : ContainerResource
    {
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } existingImageAnnotation)
        {
            existingImageAnnotation.Image = image;
            existingImageAnnotation.Tag = tag;
            return builder;
        }

        // if the annotation doesn't exist, create it with the given image and add it to the collection
        var containerImageAnnotation = new ContainerImageAnnotation() { Image = image, Tag = tag };
        builder.Resource.Annotations.Add(containerImageAnnotation);
        return builder;
    }

    /// <summary>
    /// Allows setting the image to a specific sha256 on a container.
    /// </summary>
    /// <typeparam name="T">Type of container resource.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="sha256">Registry value.</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithImageSHA256<T>(this IResourceBuilder<T> builder, string sha256) where T : ContainerResource
    {
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } existingImageAnnotation)
        {
            existingImageAnnotation.SHA256 = sha256;
            return builder;
        }

        return ThrowResourceIsNotContainer(builder);
    }

    /// <summary>
    /// Adds a callback to be executed with a list of arguments to add to the container run command when a container resource is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The arguments to be passed to the container run command when the container resource is started.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithContainerRunArgs<T>(this IResourceBuilder<T> builder, params string[] args) where T : ContainerResource
    {
        return builder.WithContainerRunArgs(context => context.Args.AddRange(args));
    }

    /// <summary>
    /// Adds a callback to be executed with a list of arguments to add to the container run command when a container resource is started.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing arguments. This runs after resources have been allocation by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithContainerRunArgs<T>(this IResourceBuilder<T> builder, Action<ContainerRunArgsCallbackContext> callback) where T : ContainerResource
    {
        return builder.WithContainerRunArgs(context =>
        {
            callback(context);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Adds a callback to be executed with a list of arguments to add to the container run command when a container resource is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing arguments. This runs after resources have been allocation by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithContainerRunArgs<T>(this IResourceBuilder<T> builder, Func<ContainerRunArgsCallbackContext, Task> callback) where T : ContainerResource
    {
        var annotation = new ContainerRunArgsCallbackAnnotation(callback);
        return builder.WithAnnotation(annotation);
    }

    private static IResourceBuilder<T> ThrowResourceIsNotContainer<T>(IResourceBuilder<T> builder) where T : ContainerResource
    {
        throw new InvalidOperationException($"The resource '{builder.Resource.Name}' does not have a container image specified. Use WithImage to specify the container image and tag.");
    }

    /// <summary>
    /// Changes the resource to be published as a container in the manifest.
    /// </summary>
    /// <param name="builder">Resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> PublishAsContainer<T>(this IResourceBuilder<T> builder) where T : ContainerResource
    {
        return builder.WithManifestPublishingCallback(context => context.WriteContainerAsync(builder.Resource));
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
