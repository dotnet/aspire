// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
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
    /// <returns>The <see cref="IResourceBuilder{ExecutableResource}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, string name, string command, string workingDirectory, params string[]? args)
    {
        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));

        var executable = new ExecutableResource(name, command, workingDirectory, args);
        return builder.AddResource(executable);
    }

    /// <summary>
    /// Adds annotation to <see cref="ExecutableResource" /> to support containerization during deployment.
    /// </summary>
    /// <typeparam name="T">Type of executable resource</typeparam>
    /// <param name="builder">Resource builder</param>
    /// <returns></returns>
    public static IResourceBuilder<T> AsDockerfileInManifest<T>(this IResourceBuilder<T> builder) where T : ExecutableResource
    {
        return builder.WithManifestPublishingCallback(context => WriteExecutableAsDockerfileResource(context, builder.Resource));
    }

    /// <summary>
    /// Adds the arguments to be passed to an executable resource when the executable is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The arguments to be passed to the executable when it is started.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, params string[] args) where T : ExecutableResource
    {
        var annotation = new ExecutableArgsCallbackAnnotation(updatedArgs =>
        {
            updatedArgs.AddRange(args);
        });
        return builder.WithAnnotation(annotation);
    }

    private static void WriteExecutableAsDockerfileResource(ManifestPublishingContext context, ExecutableResource executable)
    {
        context.Writer.WriteString("type", "dockerfile.v0");

        var appHostRelativePathToDockerfile = Path.Combine(executable.WorkingDirectory, "Dockerfile");
        var manifestFileRelativePathToDockerfile = context.GetManifestRelativePath(appHostRelativePathToDockerfile);
        context.Writer.WriteString("path", manifestFileRelativePathToDockerfile);

        var manifestFileRelativePathToContextDirectory = context.GetManifestRelativePath(executable.WorkingDirectory);
        context.Writer.WriteString("context", manifestFileRelativePathToContextDirectory);

        context.WriteEnvironmentVariables(executable);
        context.WriteBindings(executable, emitContainerPort: true);
    }
}
