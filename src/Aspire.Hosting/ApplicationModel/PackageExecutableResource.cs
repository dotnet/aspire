// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an executable restored from a NuGet package.
/// </summary>
/// <remarks>
/// Package executable resources restore a specific package version at runtime and then execute a runnable asset from the
/// restored package contents. Use <see cref="PackageExecutableResourceBuilderExtensions.WithPackageVersion{T}(IResourceBuilder{T}, string)"/>
/// to pin the package version before the resource is started.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}, Package = {PackageConfiguration?.PackageId}")]
public class PackageExecutableResource : ExecutableResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackageExecutableResource"/> class.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="packageId">The package identifier that contains the executable.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="packageId"/> is null, empty, or whitespace.</exception>
    public PackageExecutableResource(string name, string packageId)
        : base(name, "dotnet", ".")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId, nameof(packageId));

        Annotations.Add(new PackageExecutableAnnotation
        {
            PackageId = packageId
        });
    }

    internal PackageExecutableAnnotation? PackageConfiguration
    {
        get
        {
            this.TryGetLastAnnotation<PackageExecutableAnnotation>(out var configuration);
            return configuration;
        }
    }

    internal ResolvedPackageExecutableAnnotation? ResolvedExecutable
    {
        get
        {
            this.TryGetLastAnnotation<ResolvedPackageExecutableAnnotation>(out var resolvedExecutable);
            return resolvedExecutable;
        }
    }
}