// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding executable resources to the <see cref="IDistributedApplicationBuilder"/> application model.
/// </summary>
public static class ContainerExecutableResourceBuilderExtensions
{
    /// <summary>
    /// Adds a container executable resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for <see cref="ContainerResource"/> command will be running in.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="command">The executable path. This can be a fully qualified path or a executable to run from the shell/command line.</param>
    /// <param name="workingDirectory">The working directory of the executable in the container.</param>
    /// <param name="args">The arguments to the executable.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for <see cref="ContainerExecutableResource"/>.</returns>
    /// <example>
    /// Adds an anonymous volume that will be mounted in the container's file system at the path <c>/usr/data</c>:
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .AddContainerExecutable("mycommand", "ls");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ContainerExecutableResource> AddContainerExecutable<T>(this IResourceBuilder<T> builder, [ResourceName] string name, string command, string? workingDirectory = null, params object[]? args)
        where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(command);

        var containerExecutable = new ContainerExecutableResource(name, builder.Resource, command, workingDirectory);

        return builder.ApplicationBuilder
            .AddResource(containerExecutable)
            .WithArgs(context =>
            {
                if (args is not null)
                {
                    context.Args.AddRange(args);
                }
            });
    }
}
