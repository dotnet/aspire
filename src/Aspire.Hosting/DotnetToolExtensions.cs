// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dotnet Tool resources to the application model.
/// </summary>
[Experimental("ASPIREDOTNETTOOL", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class DotNetToolExtensions
{
    private const string ArgumentSeperator = "--";

    /// <summary>
    /// Adds a .NET tool resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="packageId">The package id of the tool.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DotnetToolResource> AddDotnetTool(this IDistributedApplicationBuilder builder, [ResourceName] string name, string packageId)
        => builder.AddDotnetTool(new DotnetToolResource(name, packageId));

    /// <summary>
    /// Adds a .NET tool resource to the distributed application builder, configuring it for execution via the 'dotnet
    /// tool exec' command.
    /// </summary>
    /// <typeparam name="T">The type of the .NET tool resource to add. Must inherit from <see cref="DotnetToolResource"/>.</typeparam>
    /// <param name="builder">The distributed application builder to which the .NET tool resource will be added.</param>
    /// <param name="resource">The .NET tool resource instance to add and configure.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> AddDotnetTool<T>(this IDistributedApplicationBuilder builder, T resource)
        where T : DotnetToolResource
    {
        return builder.AddResource(resource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "Tool",
                Properties = []
            })
            .WithIconName("Toolbox")
            .WithCommand("dotnet")
            .WithArgs(x =>
            {
                if (!x.Resource.TryGetLastAnnotation<DotnetToolAnnotation>(out var toolConfig))
                {
                    // If the annotation has been removed, don't add any dotnet tool arguments.
                    return;
                }

                x.Args.Add("tool");
                x.Args.Add("exec");
                x.Args.Add(toolConfig.PackageId);

                var sourceArg = toolConfig.IgnoreExistingFeeds ? "--source" : "--add-source";

                foreach (var source in toolConfig.Sources)
                {
                    x.Args.Add(sourceArg);
                    x.Args.Add(source);
                }

                if (toolConfig.IgnoreFailedSources)
                {
                    x.Args.Add("--ignore-failed-sources");
                }

                if (toolConfig.Version is not null)
                {
                    x.Args.Add("--version");
                    x.Args.Add(toolConfig.Version);
                }
                else if (toolConfig.Prerelease)
                {
                    x.Args.Add("--prerelease");
                }

                x.Args.Add("--yes");
                x.Args.Add("--");
            })
            .OnInitializeResource(async (resource, evt, ct) =>
            {
                var rns = evt.Services.GetRequiredService<ResourceNotificationService>();
                _ = Task.Run(async () =>
                {
                    await foreach (var x in rns.WatchAsync(ct).ConfigureAwait(false))
                    {
                        if (x.Resource != resource)
                        {
                            continue;
                        }

                        var toolConfig = resource.ToolConfiguration;
                        if (toolConfig == null)
                        {
                            continue;
                        }

                        var expectedPath = toolConfig.PackageId;

                        var existingPathProp = x.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Executable.Path);
                        if (existingPathProp != null && existingPathProp.Value as string != expectedPath)
                        {
                            await rns.PublishUpdateAsync(resource, x =>
                            {
                                // Existing Path could have changed in the meantime, so make sure to get the updated version
                                var existingPathProp = x.Properties.FirstOrDefault(p => p.Name == KnownProperties.Executable.Path);
                                if (existingPathProp == null)
                                {
                                    return x;
                                }

                                var argsProperty = x.Properties.FirstOrDefault(x => x.Name == KnownProperties.Resource.AppArgs);
                                var argsSensitivityProperty = x.Properties.FirstOrDefault(x => x.Name == KnownProperties.Resource.AppArgsSensitivity);

                                if (argsProperty?.Value is not ImmutableArray<string> originalArgs
                                    || argsSensitivityProperty?.Value is not ImmutableArray<int> originalSensitivity)
                                {
                                    return x; ;
                                }

                                var argSeperatorPosition = originalArgs.IndexOf(ArgumentSeperator);
                                if (argSeperatorPosition == 0)
                                {
                                    return x;
                                }

                                var firstArgToDisplay = argSeperatorPosition + 1;
                                var trimmedArgs = originalArgs[firstArgToDisplay..];
                                var trimmedSensitivity = originalSensitivity[firstArgToDisplay..];

                                return x with
                                {
                                    Properties = x.Properties
                                            .Replace(existingPathProp, existingPathProp with { Value = expectedPath })
                                            .Replace(argsSensitivityProperty, argsSensitivityProperty with { Value = trimmedSensitivity })
                                            //TODO: This could be overly sensitive if any of the `dotnet tool exec` args are sensitive
                                            // But I don't see how else to get se
                                            // I also don't see how you could mark a sens
                                            .Replace(argsProperty, argsProperty with { Value = trimmedArgs })
                                };
                            }).ConfigureAwait(false);
                        }
                    }

                }, ct);
            });
    }

    /// <summary>
    /// Sets the package identifier for the tool configuration associated with the resource builder.
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="packageId">The package identifier to assign to the tool configuration. Cannot be null.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithPackageId<T>(this IResourceBuilder<T> builder, string packageId)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.PackageId = packageId;
        return builder;
    }

    /// <summary>
    /// Set the package version for a tool to use
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="version">The package version to use</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithPackageVersion<T>(this IResourceBuilder<T> builder, string version)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.Version = version;
        return builder;
    }

    /// <summary>
    /// Configures the resource builder to mark the associated .NET tool package as a prerelease version.
    /// </summary>
    /// <typeparam name="T">The type of resource being built. Must inherit from DotnetToolResource.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithPackagePrerelease<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.Prerelease = true;
        return builder;
    }

    /// <summary>
    /// Adds a package source to 
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="source"></param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithPackageSource<T>(this IResourceBuilder<T> builder, string source)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.Sources.Add(source);
        return builder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithPackageIgnoreExistingFeeds<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.IgnoreExistingFeeds = true;
        return builder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithPackageIgnoreFailedSources<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.IgnoreFailedSources = true;
        return builder;
    }
}
