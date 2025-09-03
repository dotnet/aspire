// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for Docker Compose publishing customizations on container resources.
/// </summary>
public static class DockerComposeContainerExtensions
{
    /// <summary>
    /// Marks a bind mount to skip copying during Docker Compose publishing. The bind mount will keep its absolute path in the compose file.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source path that should not be copied during Docker Compose publishing.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This is useful for system files, sockets (like Docker socket), or other resources that should remain as absolute paths
    /// in the compose file and not be copied to the output directory during publishing.
    /// </remarks>
    /// <example>
    /// Bind mount the Docker socket but skip copying it during publishing:
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddContainer("mycontainer", "myimage")
    ///        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
    ///        .WithSkipBindMountCopying("/var/run/docker.sock");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithSkipBindMountCopying<T>(this IResourceBuilder<T> builder, string source) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        // If the source is a rooted path, use it directly without resolution  
        var sourcePath = Path.IsPathRooted(source) ? source : Path.GetFullPath(source, builder.ApplicationBuilder.AppHostDirectory);
        var annotation = new Docker.SkipBindMountCopyingAnnotation(sourcePath);
        return builder.WithAnnotation(annotation);
    }
}