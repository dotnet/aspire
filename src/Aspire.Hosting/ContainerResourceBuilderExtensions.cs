// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

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
    public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, [ResourceName] string name, string image)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(image);

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
    public static IResourceBuilder<ContainerResource> AddContainer(this IDistributedApplicationBuilder builder, [ResourceName] string name, string image, string tag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(tag);

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
    public static IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, string? name, string target, bool isReadOnly = false) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(target);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(target);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(entrypoint);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(tag);

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
    public static IResourceBuilder<T> WithImageRegistry<T>(this IResourceBuilder<T> builder, string? registry) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(tag);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(sha256);

        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } existingImageAnnotation)
        {
            existingImageAnnotation.SHA256 = sha256;
            return builder;
        }

        return ThrowResourceIsNotContainer(builder);
    }

    /// <summary>
    /// Adds a callback to be executed with a list of arguments to add to the container runtime run command when a container resource is started.
    /// </summary>
    /// <remarks>
    /// This is intended to pass additional arguments to the underlying container runtime run command to enable advanced features such as exposing GPUs to the container. To pass runtime arguments to the actual container, use the <see cref="ResourceBuilderExtensions.WithArgs{T}(IResourceBuilder{T}, string[])"/> method.
    /// </remarks>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="args">The arguments to be passed to the container runtime run command when the container resource is started.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithContainerRuntimeArgs<T>(this IResourceBuilder<T> builder, params string[] args) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithContainerRuntimeArgs(context => context.Args.AddRange(args));
    }

    /// <summary>
    /// Adds a callback to be executed with a list of arguments to add to the container runtime run command when a container resource is started.
    /// </summary>
    /// <remarks>
    /// This is intended to pass additional arguments to the underlying container runtime run command to enable advanced features such as exposing GPUs to the container. To pass runtime arguments to the actual container, use the <see cref="ResourceBuilderExtensions.WithArgs{T}(IResourceBuilder{T}, Action{CommandLineArgsCallbackContext})"/> method.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing arguments. This runs after resources have been allocation by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithContainerRuntimeArgs<T>(this IResourceBuilder<T> builder, Action<ContainerRuntimeArgsCallbackContext> callback) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.WithContainerRuntimeArgs(context =>
        {
            callback(context);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Adds a callback to be executed with a list of arguments to add to the container runtime run command when a container resource is started.
    /// </summary>
    /// <remarks>
    /// This is intended to pass additional arguments to the underlying container runtime run command to enable advanced features such as exposing GPUs to the container. To pass runtime arguments to the actual container, use the <see cref="ResourceBuilderExtensions.WithArgs{T}(IResourceBuilder{T}, Func{CommandLineArgsCallbackContext, Task})"/> method.
    /// </remarks>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing arguments. This runs after resources have been allocation by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithContainerRuntimeArgs<T>(this IResourceBuilder<T> builder, Func<ContainerRuntimeArgsCallbackContext, Task> callback) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        var annotation = new ContainerRuntimeArgsCallbackAnnotation(callback);
        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Sets the lifetime behavior of the container resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">Builder for the container resource.</param>
    /// <param name="lifetime">The lifetime behavior of the container resource. The defaults behavior is <see cref="ContainerLifetime.Session"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Marking a container resource to have a <see cref="ContainerLifetime.Persistent"/> lifetime.
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .WithContainerLifetime(ContainerLifetimeType.Persistent);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithLifetime<T>(this IResourceBuilder<T> builder, ContainerLifetime lifetime) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new ContainerLifetimeAnnotation { Lifetime = lifetime }, ResourceAnnotationMutationBehavior.Replace);
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
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithManifestPublishingCallback(context => context.WriteContainerAsync(builder.Resource));
    }

    /// <summary>
    /// Causes .NET Aspire to build the specified container image from a Dockerfile.
    /// </summary>
    /// <typeparam name="T">Type parameter specifying any type derived from <see cref="ContainerResource"/>/</typeparam>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
    /// <param name="contextPath">Path to be used as the context for the container image build.</param>
    /// <param name="dockerfilePath">Override path for the Dockerfile if it is not in the <paramref name="contextPath"/>.</param>
    /// <param name="stage">The stage representing the image to be published in a multi-stage Dockerfile.</param>
    /// <returns>A <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// When this method is called an annotation is added to the <see cref="ContainerResource"/> that specifies the context path and
    /// Dockerfile path to be used when building the container image. These details are then used by the orchestrator to build the image
    /// before using that image to start the container.
    /// </para>
    /// <para>
    /// Both the <paramref name="contextPath"/> and <paramref name="dockerfilePath"/> are relative to the AppHost directory unless
    /// they are fully qualified. If the <paramref name="dockerfilePath"/> is not provided, the path is assumed to be Dockerfile relative
    /// to the <paramref name="contextPath"/>.
    /// </para>
    /// <para>
    /// When generating the manifest for deployment tools, the <see cref="ContainerResourceBuilderExtensions.WithDockerfile{T}(IResourceBuilder{T}, string, string?, string?)"/>
    /// method results in an additional attribute being added to the `container.v0` resource type which contains the configuration
    /// necessary to allow the deployment tool to build the container image prior to deployment.
    /// </para>
    /// </remarks>
    /// <example>
    /// Creates a container called <c>mycontainer</c> with an image called <c>myimage</c>.
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .WithDockerfile("path/to/context");
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithDockerfile<T>(this IResourceBuilder<T> builder, string contextPath, string? dockerfilePath = null, string? stage = null) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(contextPath);

        var fullyQualifiedContextPath = Path.GetFullPath(contextPath, builder.ApplicationBuilder.AppHostDirectory);

        dockerfilePath ??= "Dockerfile";

        var fullyQualifiedDockerfilePath = Path.GetFullPath(dockerfilePath, fullyQualifiedContextPath);

        if (!Directory.Exists(fullyQualifiedContextPath))
        {
            throw new DirectoryNotFoundException($"Context path not found at '{fullyQualifiedContextPath}'.");
        }

        if (!File.Exists(fullyQualifiedDockerfilePath))
        {
            throw new FileNotFoundException($"Dockerfile not found at '{fullyQualifiedDockerfilePath}'.");
        }

        var imageName = builder.GenerateImageName();
        var annotation = new DockerfileBuildAnnotation(fullyQualifiedContextPath, fullyQualifiedDockerfilePath, stage);
        return builder.WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Replace)
                      .WithImageRegistry(registry: null)
                      .WithImage(imageName)
                      .WithImageTag("latest");
    }

    /// <summary>
    /// Adds a Dockerfile to the application model that can be treated like a container resource.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="contextPath">Path to be used as the context for the container image build.</param>
    /// <param name="dockerfilePath">Override path for the Dockerfile if it is not in the <paramref name="contextPath"/>.</param>
    /// <param name="stage">The stage representing the image to be published in a multi-stage Dockerfile.</param>
    /// <returns>A <see cref="IResourceBuilder{ContainerResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// Both the <paramref name="contextPath"/> and <paramref name="dockerfilePath"/> are relative to the AppHost directory unless
    /// they are fully qualified. If the <paramref name="dockerfilePath"/> is not provided, the path is assumed to be Dockerfile relative
    /// to the <paramref name="contextPath"/>.
    /// </para>
    /// <para>
    /// When generating the manifest for deployment tools, the <see cref="AddDockerfile(IDistributedApplicationBuilder, string, string, string?, string?)"/>
    /// method results in an additional attribute being added to the `container.v1` resource type which contains the configuration
    /// necessary to allow the deployment tool to build the container image prior to deployment.
    /// </para>
    /// </remarks>
    /// <example>
    /// Creates a container called <c>mycontainer</c> based on a Dockerfile in the context path <c>path/to/context</c>.
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// builder.AddDockerfile("mycontainer", "path/to/context");
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ContainerResource> AddDockerfile(this IDistributedApplicationBuilder builder, [ResourceName] string name, string contextPath, string? dockerfilePath = null, string? stage = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(contextPath);

        return builder.AddContainer(name, "placeholder") // Image name will be replaced by WithDockerfile.
                      .WithDockerfile(contextPath, dockerfilePath, stage);
    }

    /// <summary>
    /// Overrides the default container name for this resource. By default Aspire generates a unique container name based on the
    /// resource name and a random postfix (or a postfix based on a hash of the AppHost project path for persistent container resources).
    /// This method allows you to override that behavior with a custom name, but could lead to naming conflicts if the specified name is not unique.
    /// </summary>
    /// <remarks>
    /// Combining this with <see cref="ContainerLifetime.Persistent"/> will allow Aspire to re-use an existing container that was not
    /// created by an Aspire AppHost.
    /// </remarks>
    /// <typeparam name="T">The type of container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource.</param>
    /// <param name="name">The desired container name. Must be a valid container name or your runtime will report an error.</param>
    /// <returns>The resource bulder for the container resource.</returns>
    public static IResourceBuilder<T> WithContainerName<T>(this IResourceBuilder<T> builder, string name) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.WithAnnotation(new ContainerNameAnnotation { Name = name }, ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Adds a build argument when the container is build from a Dockerfile.
    /// </summary>
    /// <typeparam name="T">The type of container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource.</param>
    /// <param name="name">The name of the build argument.</param>
    /// <param name="value">The value of the build argument.</param>
    /// <returns>The resource builder for the container resource.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ContainerResourceBuilderExtensions.WithBuildArg{T}(IResourceBuilder{T}, string, object)"/> is
    /// called before <see cref="ContainerResourceBuilderExtensions.WithDockerfile{T}(IResourceBuilder{T}, string, string?, string?)"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <see cref="ContainerResourceBuilderExtensions.WithBuildArg{T}(IResourceBuilder{T}, string, object)"/> extension method
    /// adds an additional build argument the container resource to be used when the image is built. This method must be called after
    /// <see cref="ContainerResourceBuilderExtensions.WithDockerfile{T}(IResourceBuilder{T}, string, string?, string?)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Adding a static build argument.
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .WithDockerfile("../mycontainer")
    ///        .WithBuildArg("CUSTOM_BRANDING", "/app/static/branding/custom");
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithBuildArg<T>(this IResourceBuilder<T> builder, string name, object value) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        var annotation = builder.Resource.Annotations.OfType<DockerfileBuildAnnotation>().SingleOrDefault();

        if (annotation is null)
        {
            throw new InvalidOperationException("The resource does not have a Dockerfile build annotation. Call WithDockerfile before calling WithBuildArg.");
        }

        annotation.BuildArguments[name] = value;

        return builder;
    }

    /// <summary>
    /// Adds a build argument when the container is built from a Dockerfile.
    /// </summary>
    /// <typeparam name="T">The type of container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource.</param>
    /// <param name="name">The name of the build argument.</param>
    /// <param name="value">The resource builder for a parameter resource.</param>
    /// <returns>The resource builder for the container resource.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ContainerResourceBuilderExtensions.WithBuildArg{T}(IResourceBuilder{T}, string, IResourceBuilder{ParameterResource})"/> is
    /// called before <see cref="ContainerResourceBuilderExtensions.WithDockerfile{T}(IResourceBuilder{T}, string, string?, string?)"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <see cref="ContainerResourceBuilderExtensions.WithBuildArg{T}(IResourceBuilder{T}, string, IResourceBuilder{ParameterResource})"/> extension method
    /// adds an additional build argument the container resource to be used when the image is built. This method must be called after
    /// <see cref="ContainerResourceBuilderExtensions.WithDockerfile{T}(IResourceBuilder{T}, string, string?, string?)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Adding a build argument based on a parameter..
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var branding = builder.AddParameter("branding");
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .WithDockerfile("../mycontainer")
    ///        .WithBuildArg("CUSTOM_BRANDING", branding);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithBuildArg<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<ParameterResource> value) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        if (value.Resource.Secret)
        {
            throw new InvalidOperationException("Cannot add secret parameter as a build argument. Use WithSecretBuildArg instead.");
        }

        return builder.WithBuildArg(name, value.Resource);
    }

    /// <summary>
    /// Adds a secret build argument when the container is built from a Dockerfile.
    /// </summary>
    /// <typeparam name="T">The type of container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource.</param>
    /// <param name="name">The name of the secret build argument.</param>
    /// <param name="value">The resource builder for a parameter resource.</param>
    /// <returns>The resource builder for the container resource.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ContainerResourceBuilderExtensions.WithBuildSecret{T}(IResourceBuilder{T}, string, IResourceBuilder{ParameterResource})"/> is
    /// called before <see cref="ContainerResourceBuilderExtensions.WithDockerfile{T}(IResourceBuilder{T}, string, string?, string?)"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <see cref="ContainerResourceBuilderExtensions.WithBuildSecret{T}(IResourceBuilder{T}, string, IResourceBuilder{ParameterResource})"/> extension method
    /// results in a <c>--secret</c> argument being appended to the <c>docker build</c> or <c>podman build</c> command. This overload results in an environment
    /// variable-based secret being passed to the build process. The value of the environment variable is the value of the secret referenced by the <see cref="ParameterResource"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Adding a build secret based on a parameter.
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var accessToken = builder.AddParameter("accessToken", secret: true);
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .WithDockerfile("../mycontainer")
    ///        .WithBuildSecret("ACCESS_TOKEN", accessToken);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithBuildSecret<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<ParameterResource> value) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        var annotation = builder.Resource.Annotations.OfType<DockerfileBuildAnnotation>().SingleOrDefault();

        if (annotation is null)
        {
            throw new InvalidOperationException("The resource does not have a Dockerfile build annotation. Call WithDockerfile before calling WithSecretBuildArg.");
        }

        annotation.BuildSecrets[name] = value.Resource;

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
