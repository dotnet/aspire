// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for configuring provider-specific mount options for container mounts.
/// </summary>
public static class ContainerMountOptionsExtensions
{
    /// <summary>
    /// Sets provider / platform specific mount <paramref name="options"/> on the mount whose target path
    /// matches <paramref name="targetPath"/>. Throws if no existing mount with that target is found.
    /// </summary>
    /// <typeparam name="T">A container resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="targetPath">The mount's target path inside the container (e.g. "/var/lib/postgresql/data").</param>
    /// <param name="options">The provider-specific options string (opaque to Aspire).</param>
    /// <returns>The same builder for chaining.</returns>
    public static IResourceBuilder<T> WithMountOptions<T>(this IResourceBuilder<T> builder, string targetPath, string options)
        where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        if (string.IsNullOrEmpty(targetPath))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(targetPath));
        }
        
        if (string.IsNullOrEmpty(options))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(options));
        }

        var mount = builder.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => string.Equals(m.Target, targetPath, StringComparison.Ordinal));

        if (mount is null)
        {
            throw new InvalidOperationException($"No container mount with target '{targetPath}' was found on resource '{builder.Resource.Name}'.");
        }

        mount.Options = options;
        return builder;
    }
}