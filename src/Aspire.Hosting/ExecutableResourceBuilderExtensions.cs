// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding executable resources to the <see cref="IDistributedApplicationBuilder"/> application model.
/// </summary>
public static class ExecutableResourceBuilderExtensions
{
    /// <summary>
    /// Adds an executable resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="command">The executable path. This can be a fully qualified path or a executable to run from the shell/command line.</param>
    /// <param name="workingDirectory">The working directory of the executable.</param>
    /// <param name="args">The arguments to the executable.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// You can run any executable command using its full path.
    /// As a security feature, Aspire doesn't run executable unless the command is located in a path listed in the PATH environment variable.
    /// <para/> 
    /// To run an executable file that's in the current directory, specify the full path or use the relative path <c>./</c> to represent the current directory.
    /// </remarks>
    public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, [ResourceName] string name, string command, string workingDirectory, params string[]? args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        return AddExecutable(builder, name, command, workingDirectory, (object[]?)args);
    }

    /// <summary>
    /// Adds an executable resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="command">The executable path. This can be a fully qualified path or a executable to run from the shell/command line.</param>
    /// <param name="workingDirectory">The working directory of the executable.</param>
    /// <param name="args">The arguments to the executable.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, [ResourceName] string name, string command, string workingDirectory, params object[]? args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));

        var executable = new ExecutableResource(name, command, workingDirectory);
        return builder.AddResource(executable)
                      .WithArgs(context =>
                      {
                          if (args is not null)
                          {
                              context.Args.AddRange(args);
                          }
                      });
    }

    /// <summary>
    /// Adds annotation to <see cref="ExecutableResource" /> to support containerization during deployment.
    /// </summary>
    /// <typeparam name="T">Type of executable resource</typeparam>
    /// <param name="builder">Resource builder</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> PublishAsDockerFile<T>(this IResourceBuilder<T> builder) where T : ExecutableResource
    {
        return builder.PublishAsDockerFile(c => { });
    }

    /// <summary>
    /// Adds annotation to <see cref="ExecutableResource" /> to support containerization during deployment.
    /// The resulting container image is built, and when the optional <paramref name="buildArgs"/> are provided
    /// they're used with <c>docker build --build-arg</c>.
    /// </summary>
    /// <typeparam name="T">Type of executable resource</typeparam>
    /// <param name="builder">Resource builder</param>
    /// <param name="buildArgs">The optional build arguments, used with <c>docker build --build-args</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("Use builder.PublishAsDockerFile(c => c.WithBuildArg(name, value)) instead.")]
    public static IResourceBuilder<T> PublishAsDockerFile<T>(this IResourceBuilder<T> builder, IEnumerable<DockerBuildArg>? buildArgs) where T : ExecutableResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.PublishAsDockerFile(c =>
        {
            foreach (var arg in buildArgs ?? [])
            {
                c.WithBuildArg(arg.Name, arg.Value);
            }
        });
    }

    /// <summary>
    /// Adds support for containerizing this <see cref="ExecutableResource"/> during deployment.
    /// The resulting container image is built, and when the optional <paramref name="configure"/> action is provided,
    /// it is used to configure the container resource.
    /// </summary>
    /// <remarks>
    /// When the executable resource is converted to a container resource, the arguments to the executable
    /// are not used. This is because arguments to the executable often contain physical paths that are not valid
    /// in the container. The container can be set up with the correct arguments using the <paramref name="configure"/> action.
    /// </remarks>
    /// <typeparam name="T">Type of executable resource</typeparam>
    /// <param name="builder">Resource builder</param>
    /// <param name="configure">Optional action to configure the container resource</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> PublishAsDockerFile<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<ContainerResource>>? configure)
        where T : ExecutableResource
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // The implementation here is less than ideal, but we don't have a clean way of building resource types
        // that change their behavior based on the context. In this case, we want to change the behavior of the
        // resource from an ExecutableResource to a ContainerResource. We do this by removing the ExecutableResource
        // from the application model and adding a new ContainerResource in its place in publish mode.

        // There are still dangling references to the original ExecutableResource in the application model, but
        // in publish mode, it won't be used. This is a limitation of the current design.
        builder.ApplicationBuilder.Resources.Remove(builder.Resource);

        var container = new ExecutableContainerResource(builder.Resource);
        var cb = builder.ApplicationBuilder.AddResource(container);
        // WithImage makes this a container resource (adding the annotation)
        cb.WithImage(builder.Resource.Name);
        cb.WithDockerfile(contextPath: builder.Resource.WorkingDirectory);
        // Arguments to the executable often contain physical paths that are not valid in the container
        // Clear them out so that the container can be set up with the correct arguments
        cb.WithArgs(c => c.Args.Clear());

        configure?.Invoke(cb);

        // Even through we're adding a ContainerResource
        // update the manifest publishing callback on the original ExecutableResource
        // so that the container resource is written to the manifest
        return builder.WithManifestPublishingCallback(context =>
            context.WriteContainerAsync(container));
    }

    // Allows us to mirror annotations from ExecutableResource to ContainerResource
    private sealed class ExecutableContainerResource(ExecutableResource er) : ContainerResource(er.Name)
    {
        public override ResourceAnnotationCollection Annotations => er.Annotations;
    }
}
