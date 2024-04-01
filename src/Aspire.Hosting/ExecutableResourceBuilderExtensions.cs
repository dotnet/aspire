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
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, string name, string command, string workingDirectory, params string[]? args)
    {
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
    [Obsolete("Use PublishAsDockerFile instead")]
    public static IResourceBuilder<T> AsDockerfileInManifest<T>(this IResourceBuilder<T> builder) where T : ExecutableResource
    {
        return builder.PublishAsDockerFile();
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
    public static IResourceBuilder<T> PublishAsDockerFile<T>(this IResourceBuilder<T> builder, IEnumerable<DockerBuildArg>? buildArgs = null) where T : ExecutableResource
    {
        return builder.WithManifestPublishingCallback(context => WriteExecutableAsDockerfileResourceAsync(context, builder.Resource, buildArgs));
    }

    private static async Task WriteExecutableAsDockerfileResourceAsync(ManifestPublishingContext context, ExecutableResource executable, IEnumerable<DockerBuildArg>? buildArgs = null)
    {
        context.Writer.WriteString("type", "dockerfile.v0");

        var appHostRelativePathToDockerfile = Path.Combine(executable.WorkingDirectory, "Dockerfile");
        var manifestFileRelativePathToDockerfile = context.GetManifestRelativePath(appHostRelativePathToDockerfile);
        context.Writer.WriteString("path", manifestFileRelativePathToDockerfile);

        var manifestFileRelativePathToContextDirectory = context.GetManifestRelativePath(executable.WorkingDirectory);
        context.Writer.WriteString("context", manifestFileRelativePathToContextDirectory);

        if (buildArgs is not null)
        {
            context.WriteDockerBuildArgs(buildArgs);
        }

        await context.WriteEnvironmentVariablesAsync(executable).ConfigureAwait(false);
        context.WriteBindings(executable);
    }
}
