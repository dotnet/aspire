// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Options for configuring container registry mirrors used during container image builds.
/// </summary>
/// <remarks>
/// Aspire uses these options when invoking .NET SDK container build targets (for example via <c>dotnet publish</c>)
/// to rewrite the computed container base image registry.
/// This is useful in environments where base images must be pulled through an internal proxy or pull-through cache.
/// </remarks>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ContainerRegistryMirrorOptions
{
    /// <summary>
    /// Gets the dictionary of registry mirrors, where the key is the source registry
    /// and the value is the mirror registry to use instead.
    /// </summary>
    /// <value>
    /// A case-insensitive dictionary for configuration convenience. Keys are treated as literal text when applying
    /// replacements during the build.
    /// </value>
    public Dictionary<string, string> Mirrors { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Extension methods for configuring container registry mirrors.
/// </summary>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class ContainerRegistryMirrorExtensions
{
    /// <summary>
    /// Configures a container registry mirror to be used when building container images.
    /// When the SDK computes a base image from the source registry, it will be replaced
    /// with the mirror registry.
    /// </summary>
    /// <remarks>
    /// This configuration is applied during Aspire's container image build flow. It rewrites the MSBuild
    /// <c>ContainerBaseImage</c> property after it is computed by the SDK.
    ///
    /// The replacement is performed as a literal string substitution and is case-sensitive. For best results,
    /// configure <paramref name="sourceRegistry"/> using the same casing as the computed base images
    /// (typically lower-case).
    /// </remarks>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="sourceRegistry">The source registry to replace (e.g., "mcr.microsoft.com").</param>
    /// <param name="mirrorRegistry">The mirror registry to use instead.</param>
    /// <returns>The distributed application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceRegistry"/> or <paramref name="mirrorRegistry"/> is empty or whitespace.</exception>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// // Use an internal Artifactory mirror for MCR images
    /// builder.WithContainerRegistryMirror(
    ///     "mcr.microsoft.com",
    ///     "docker.artifactory.example.com/mcr-remote");
    ///
    /// builder.AddProject&lt;Projects.MyApi&gt;("api");
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IDistributedApplicationBuilder WithContainerRegistryMirror(
        this IDistributedApplicationBuilder builder,
        string sourceRegistry,
        string mirrorRegistry)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRegistry);
        ArgumentException.ThrowIfNullOrWhiteSpace(mirrorRegistry);

        builder.Services.Configure<ContainerRegistryMirrorOptions>(options =>
            options.Mirrors[sourceRegistry] = mirrorRegistry);

        return builder;
    }

    /// <summary>
    /// Configures multiple container registry mirrors to be used when building container images.
    /// </summary>
    /// <remarks>
    /// This overload is useful when sourcing mirrors from configuration.
    /// </remarks>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="mirrors">A dictionary of source registries to mirror registries.</param>
    /// <returns>The distributed application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="mirrors"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.WithContainerRegistryMirrors(new Dictionary&lt;string, string&gt;
    /// {
    ///     ["mcr.microsoft.com"] = "docker.artifactory.example.com/mcr-remote",
    ///     ["ghcr.io"] = "docker.artifactory.example.com/ghcr-remote",
    /// });
    ///
    /// builder.AddProject&lt;Projects.MyApi&gt;("api");
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IDistributedApplicationBuilder WithContainerRegistryMirrors(
        this IDistributedApplicationBuilder builder,
        IDictionary<string, string> mirrors)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(mirrors);

        builder.Services.Configure<ContainerRegistryMirrorOptions>(options =>
        {
            foreach (var kvp in mirrors)
            {
                options.Mirrors[kvp.Key] = kvp.Value;
            }
        });

        return builder;
    }
}
