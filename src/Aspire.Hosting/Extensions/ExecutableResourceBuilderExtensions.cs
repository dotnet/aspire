// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

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
        var executable = new ExecutableResource(name, command, workingDirectory, args);
        return builder.AddResource(executable);
    }

    /// <summary>
    /// Adds annotation to <see cref="ExecutableResource" /> to support containerization during deployment.
    /// </summary>
    /// <typeparam name="T">Type of executable resource</typeparam>
    /// <param name="builder">Resource builder</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithDockerfile<T>(this IResourceBuilder<T> builder) where T : ExecutableResource
    {
        return builder.WithAnnotation(
            new ManifestPublishingCallbackAnnotation(context => WriteExecutableAsDockerfileResource(context, builder.Resource))
            );
    }

    private static void WriteExecutableAsDockerfileResource(ManifestPublishingContext context, ExecutableResource executable)
    {
        context.Writer.WriteString("type", "dockerfile.v0");
        context.Writer.WriteString("path", Path.Combine(executable.WorkingDirectory, "Dockerfile"));
        context.Writer.WriteString("context", executable.WorkingDirectory);

        ManifestPublisher.WriteEnvironmentVariables(executable, context);
        ManifestPublisher.WriteBindings(executable, context, true);
    }
}
