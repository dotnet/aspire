// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Packages;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding package executable resources to the application model.
/// </summary>
/// <remarks>
/// These helpers let an app host restore and execute a runnable NuGet package without requiring the package source code to
/// be present in the solution. Package restore uses the app host directory as its configuration root so local NuGet.Config
/// files and feed settings are applied consistently.
/// </remarks>
public static class PackageExecutableResourceBuilderExtensions
{
    /// <summary>
    /// Adds a package-backed executable resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="packageId">The package identifier that contains the runnable executable.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// Call <see cref="WithPackageVersion{T}(IResourceBuilder{T}, string)"/> to select the package version before running the
    /// resource. The package is restored relative to <see cref="IDistributedApplicationBuilder.AppHostDirectory"/> so NuGet
    /// configuration is resolved the same way as other app-host-relative resource inputs.
    /// Runnable assets can be restored from either the package <c>lib</c> or <c>tools</c> layout.
    /// Packages that place a framework-dependent entry point under <c>lib</c> should only rely on assemblies that are
    /// already present in the target shared runtime, or otherwise included in the package.
    /// If the application requires additional managed dependencies, package authors should include published output under
    /// <c>tools/&lt;tfm&gt;/any</c> or another supported <c>tools</c> layout and select the entry point with
    /// <see cref="WithPackageExecutable{T}(IResourceBuilder{T}, string)"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var packageExecutable = builder.AddPackageExecutable("formatter", "Contoso.PackageExecutables.Formatter")
    ///     .WithPackageVersion("1.2.3")
    ///     .WithPackageExecutable("formatter.dll")
    ///     .WithArgs("--watch");
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="packageId"/> is invalid.</exception>
    [AspireExport("addPackageExecutable", Description = "Adds an executable resource backed by a NuGet package")]
    public static IResourceBuilder<PackageExecutableResource> AddPackageExecutable(this IDistributedApplicationBuilder builder, [ResourceName] string name, string packageId)
        => builder.AddPackageExecutable(new PackageExecutableResource(name, packageId));

    /// <summary>
    /// Adds a package-backed executable resource to the application model.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="resource">The resource to add.</param>
    /// <returns>The resource builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="resource"/> is null.</exception>
    public static IResourceBuilder<T> AddPackageExecutable<T>(this IDistributedApplicationBuilder builder, T resource)
        where T : PackageExecutableResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resource);

        AnchorWorkingDirectoryToAppHost(builder, resource);

        builder.Services.TryAddSingleton<IPackageExecutableResolver, PackageExecutableResolver>();

        var resourceBuilder = builder.AddResource(resource)
            .WithCommand("dotnet")
            .WithArgs(context =>
            {
                if (resource.ResolvedExecutable is { } resolved)
                {
                    context.Args.AddRange(GetResolvedArguments(resource, resolved, context.ExecutionContext?.IsPublishMode == true));
                }
            })
            .OnBeforeResourceStarted(BeforeResourceStarted);

        return resourceBuilder.PublishAsContainer();

        static async Task BeforeResourceStarted(T resource, BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
        {
            var resolver = @event.Services.GetRequiredService<IPackageExecutableResolver>();
            var notifications = @event.Services.GetRequiredService<ResourceNotificationService>();
            var resolved = await resolver.ResolveAsync(resource, cancellationToken).ConfigureAwait(false);

            resource.WithResolvedExecutable(resolved);

            await notifications.PublishUpdateAsync(resource, snapshot => snapshot with
            {
                Properties = snapshot.Properties.SetResourcePropertyRange([
                    new(KnownProperties.Tool.Package, resolved.PackageId),
                    new(KnownProperties.Tool.Version, resolved.PackageVersion),
                    new(KnownProperties.Resource.Source, resolved.PackageId),
                    new(KnownProperties.Resource.Type, KnownResourceTypes.PackageExecutable)
                ])
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Converts a package executable resource to a container resource during publish.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">Optional container configuration callback.</param>
    /// <returns>The original resource builder.</returns>
    /// <remarks>
    /// Publishing package executable resources currently supports managed <c>.dll</c> entry points that run on the .NET
    /// runtime image. Runtime execution does not require the .NET SDK once the app host has been built, but the target
    /// environment must provide the appropriate .NET shared framework for both the app host and the packaged executable.
    /// </remarks>
    public static IResourceBuilder<T> PublishAsContainer<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<ContainerResource>>? configure = null)
        where T : PackageExecutableResource
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        if (builder.ApplicationBuilder.TryCreateResourceBuilder<PackageExecutableContainerResource>(builder.Resource.Name, out var existingBuilder))
        {
            configure?.Invoke(existingBuilder);
            return builder;
        }

        builder.ApplicationBuilder.Resources.Remove(builder.Resource);

        var publishContextPath = Path.Combine(
            Path.GetTempPath(),
            "aspire-package-executables",
            "publish",
            builder.Resource.Name,
            Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(publishContextPath);
        var container = new PackageExecutableContainerResource(builder.Resource, publishContextPath);
        var containerBuilder = builder.ApplicationBuilder.AddResource(container);

        containerBuilder.WithImage(builder.Resource.Name)
                        .WithEntrypoint("dotnet")
                        .WithDockerfileFactory(publishContextPath, context => CreatePublishDockerfileAsync(builder.Resource, publishContextPath, context));

        configure?.Invoke(containerBuilder);

        return builder.WithManifestPublishingCallback(context => context.WriteContainerAsync(container));
    }

    /// <summary>
    /// Sets the package version to restore.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="version">The package version.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// Package executable resources require an explicit version so restore behavior remains deterministic.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="version"/> is null, empty, or whitespace.</exception>
    [AspireExport("withPackageVersion", Description = "Sets the package version")]
    public static IResourceBuilder<T> WithPackageVersion<T>(this IResourceBuilder<T> builder, string version)
        where T : PackageExecutableResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        builder.Resource.PackageConfiguration!.Version = version;
        return builder;
    }

    /// <summary>
    /// Adds a package source used during restore.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The package source.</param>
    /// <returns>The resource builder.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is null, empty, or whitespace.</exception>
    [AspireExport("withPackageSource", Description = "Adds a package source")]
    public static IResourceBuilder<T> WithPackageSource<T>(this IResourceBuilder<T> builder, string source)
        where T : PackageExecutableResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        builder.Resource.PackageConfiguration!.Sources.Add(source);
        return builder;
    }

    /// <summary>
    /// Adds multiple package sources used during restore.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="sources">The package sources.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<T> WithPackageSources<T>(this IResourceBuilder<T> builder, params string[] sources)
        where T : PackageExecutableResource
    {
        ArgumentNullException.ThrowIfNull(sources);

        foreach (var source in sources)
        {
            builder.WithPackageSource(source);
        }

        return builder;
    }

    /// <summary>
    /// Selects a specific executable file from the restored package contents.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="executableName">The executable file name or base name.</param>
    /// <returns>The resource builder.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="executableName"/> is null, empty, or whitespace.</exception>
    [AspireExport("withPackageExecutable", Description = "Selects the executable inside the package")]
    public static IResourceBuilder<T> WithPackageExecutable<T>(this IResourceBuilder<T> builder, string executableName)
        where T : PackageExecutableResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);
        builder.Resource.PackageConfiguration!.ExecutableName = executableName;
        return builder;
    }

    /// <summary>
    /// Configures the restore to ignore existing feeds declared in NuGet configuration.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    [AspireExport("withPackageIgnoreExistingFeeds", Description = "Ignores existing feeds during package restore")]
    public static IResourceBuilder<T> WithPackageIgnoreExistingFeeds<T>(this IResourceBuilder<T> builder)
        where T : PackageExecutableResource
    {
        builder.Resource.PackageConfiguration!.IgnoreExistingFeeds = true;
        return builder;
    }

    /// <summary>
    /// Configures the restore to continue when one package source fails but another source can satisfy the package.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// When enabled, Aspire first attempts restore with the full source set and then retries individual sources if the
    /// combined restore fails. This keeps successful sources usable when one configured feed is unavailable.
    /// </remarks>
    [AspireExport("withPackageIgnoreFailedSources", Description = "Allows failed package sources during restore")]
    public static IResourceBuilder<T> WithPackageIgnoreFailedSources<T>(this IResourceBuilder<T> builder)
        where T : PackageExecutableResource
    {
        builder.Resource.PackageConfiguration!.IgnoreFailedSources = true;
        return builder;
    }

    /// <summary>
    /// Sets the working directory for the executable relative to the restored package directory.
    /// </summary>
    /// <typeparam name="T">The package executable resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="workingDirectory">The working directory override.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// The path must remain relative to the restored package contents. Rooted paths and directory traversal outside the
    /// package are rejected when the package is resolved.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workingDirectory"/> is null, empty, or whitespace.</exception>
    [AspireExport("withPackageWorkingDirectory", Description = "Sets the package working directory")]
    public static IResourceBuilder<T> WithPackageWorkingDirectory<T>(this IResourceBuilder<T> builder, string workingDirectory)
        where T : PackageExecutableResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        builder.Resource.PackageConfiguration!.WorkingDirectory = workingDirectory;
        return builder;
    }

    private static void AnchorWorkingDirectoryToAppHost<T>(IDistributedApplicationBuilder builder, T resource)
        where T : PackageExecutableResource
    {
        if (resource.Annotations.OfType<ExecutableAnnotation>().LastOrDefault() is not { } executableAnnotation)
        {
            return;
        }

        if (Path.IsPathRooted(executableAnnotation.WorkingDirectory))
        {
            return;
        }

        executableAnnotation.WorkingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, executableAnnotation.WorkingDirectory));
    }

    private static void WithResolvedExecutable(this PackageExecutableResource resource, PackageExecutableResolutionResult resolved)
    {
        var existingAnnotations = resource.Annotations.OfType<ResolvedPackageExecutableAnnotation>().ToArray();
        foreach (var existingAnnotation in existingAnnotations)
        {
            resource.Annotations.Remove(existingAnnotation);
        }

        var resolvedAnnotation = new ResolvedPackageExecutableAnnotation
        {
            PackageId = resolved.PackageId,
            PackageVersion = resolved.PackageVersion,
            PackageDirectory = resolved.PackageDirectory,
            ExecutablePath = resolved.ExecutablePath,
            Command = resolved.Command,
            WorkingDirectory = resolved.WorkingDirectory,
        };

        resolvedAnnotation.Arguments.AddRange(resolved.Arguments);
        resource.Annotations.Add(resolvedAnnotation);

        if (resource.Annotations.OfType<ExecutableAnnotation>().LastOrDefault() is { } executableAnnotation)
        {
            executableAnnotation.Command = resolved.Command;
            executableAnnotation.WorkingDirectory = resolved.WorkingDirectory;
        }
    }

    private static IReadOnlyList<string> GetResolvedArguments(PackageExecutableResource resource, ResolvedPackageExecutableAnnotation resolved, bool isPublishMode)
    {
        if (!isPublishMode)
        {
            return resolved.Arguments;
        }

        if (!string.Equals(resolved.Command, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            throw new DistributedApplicationException($"Publishing package executable resource '{resource.Name}' currently supports managed .dll executables only.");
        }

        var relativeExecutablePath = Path.GetRelativePath(resolved.WorkingDirectory, resolved.ExecutablePath)
            .Replace('\\', '/');

        var publishArguments = new List<string>
        {
            relativeExecutablePath
        };

        if (resolved.Arguments.Count > 1)
        {
            publishArguments.AddRange(resolved.Arguments.Skip(1));
        }

        return publishArguments;
    }

    private static async Task<string> CreatePublishDockerfileAsync(PackageExecutableResource resource, string publishContextPath, DockerfileFactoryContext context)
    {
        var resolver = context.Services.GetRequiredService<IPackageExecutableResolver>();
        var resolved = await resolver.ResolveAsync(resource, context.CancellationToken).ConfigureAwait(false);

        if (!string.Equals(resolved.Command, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            throw new DistributedApplicationException($"Publishing package executable resource '{resource.Name}' currently supports managed .dll executables only.");
        }

        resource.WithResolvedExecutable(resolved);

        var packageOutputPath = Path.Combine(publishContextPath, "package");
        ResetDirectory(packageOutputPath);
        CopyDirectory(resolved.PackageDirectory, packageOutputPath);

        var workingDirectoryRelativePath = Path.GetRelativePath(resolved.PackageDirectory, resolved.WorkingDirectory)
            .Replace('\\', '/');
        var containerWorkingDirectory = string.Equals(workingDirectoryRelativePath, ".", StringComparison.Ordinal)
            ? "/app"
            : $"/app/{workingDirectoryRelativePath}";

        return string.Join("\n", [
            "FROM mcr.microsoft.com/dotnet/runtime:10.0",
            "WORKDIR /app",
            "COPY package/ /app/",
            $"WORKDIR {containerWorkingDirectory}"
        ]);
    }

    private static void ResetDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, directory);
            Directory.CreateDirectory(Path.Combine(destinationPath, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var destinationFile = Path.Combine(destinationPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }

    private sealed class PackageExecutableContainerResource(PackageExecutableResource packageResource, string publishContextPath) : ContainerResource(packageResource.Name)
    {
        public string PublishContextPath { get; } = publishContextPath;

        public override ResourceAnnotationCollection Annotations => packageResource.Annotations;
    }
}