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
public static class DotnetToolExtensions
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
            .WithArgs(BuildToolExecArguments)
            .OnInitializeResource(UpdateSourceColumnForDashboard)
            .OnBeforeResourceStarted(BuildToolProperties);

        void BuildToolExecArguments(CommandLineArgsCallbackContext x)
        {
            var toolConfig = resource.ToolConfiguration;
            if (toolConfig == null)
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
            x.Args.Add(ArgumentSeperator);
        }

        static Task UpdateSourceColumnForDashboard(T resource, InitializeResourceEvent evt, CancellationToken ct)
        {
            var rns = evt.Services.GetRequiredService<ResourceNotificationService>();
            // `DcpExecutor` will keep on ovewriting these properties every time there is an internal update
            // so subscribe to every Resource update so we can undo DcpExecutor's changes.
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

                    // Update the executable `path` property as this is what the dashboard uses to render the primary text in the "Source" column
                    // changing `dotnet` to `TOOL NAME`
                    var properties = x.Snapshot.Properties;
                    var expectedPath = toolConfig.PackageId;
                    var existingPath = properties.FirstOrDefault(p => p.Name == KnownProperties.Executable.Path)?.Value;

                    if (existingPath as string != expectedPath)
                    {
                        await rns.PublishUpdateAsync(resource, x => x with
                        {
                            Properties = [
                              ..x.Properties.RemoveAll(p => p.Name is KnownProperties.Executable.Path),
                           new(KnownProperties.Executable.Path, expectedPath)
                           ]
                        }).ConfigureAwait(false);
                        continue;
                    }

                    // For resource args strip out the "tool exec <packageId> ... --" portion and only show the args for the tool itself
                    // But for diagnostics, put the original properties back in the ToolExecArgs property
                    var argsProperty = properties.FirstOrDefault(x => x.Name == KnownProperties.Resource.AppArgs);
                    var argsSensitivityProperty = properties.FirstOrDefault(x => x.Name == KnownProperties.Resource.AppArgsSensitivity);

                    if (argsProperty?.Value is not ImmutableArray<string> originalArgs
                        || argsSensitivityProperty?.Value is not ImmutableArray<int> originalSensitivity)
                    {
                        continue;
                    }

                    // If the first args are not "tool" or "exec", then assume we've already removed the args
                    if (originalArgs.Length < 2 || originalArgs[0] != "tool" || originalArgs[1] != "exec")
                    {
                        continue;
                    }

                    var argSeperatorPosition = originalArgs.IndexOf(ArgumentSeperator);
                    if (argSeperatorPosition == -1)
                    {
                        continue;
                    }

                    var firstToolArg = argSeperatorPosition + 1;
                    var toolArgs = originalArgs[firstToolArg..];
                    var toolSensitivity = originalSensitivity[firstToolArg..];

                    var execArgs = originalArgs[..argSeperatorPosition];

                    await rns.PublishUpdateAsync(resource, x => x with
                    {
                        Properties = [
                            ..x.Properties.RemoveAll(p => p.Name is KnownProperties.Resource.AppArgs or KnownProperties.Resource.AppArgsSensitivity or KnownProperties.Tool.ExecArgs),
                            new(KnownProperties.Resource.AppArgs, toolArgs),
                            new(KnownProperties.Resource.AppArgsSensitivity, toolSensitivity),
                            new(KnownProperties.Tool.ExecArgs, toolArgs){ IsSensitive = true }
                            ]
                    }).ConfigureAwait(false);
                }
            }, ct);

            return Task.CompletedTask;
        }

        //TODO: Move to WithConfigurationFinalizer once merged - https://github.com/dotnet/aspire/pull/13200
        async Task BuildToolProperties(T resource, BeforeResourceStartedEvent evt, CancellationToken ct)
        {
            var rns = evt.Services.GetRequiredService<ResourceNotificationService>();
            var toolConfig = resource.ToolConfiguration;
            if (toolConfig == null)
            {
                return;
            }

            await rns.PublishUpdateAsync(resource, x => x with
            {
                Properties = [
                        ..x.Properties,
                        new (KnownProperties.Tool.Package, toolConfig.PackageId),
                        new (KnownProperties.Tool.Version, toolConfig.Version)
                        ]
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sets the package identifier for the tool configuration associated with the resource builder.
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="packageId">The package identifier to assign to the tool configuration. Cannot be null.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithToolPackage<T>(this IResourceBuilder<T> builder, string packageId)
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
    public static IResourceBuilder<T> WithToolVersion<T>(this IResourceBuilder<T> builder, string version)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.Version = version;
        return builder;
    }

    /// <summary>
    /// Allow prerelease versions of the tool to be used
    /// </summary>
    /// <typeparam name="T">The type of resource being built. Must inherit from DotnetToolResource.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithToolPrerelease<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.Prerelease = true;
        return builder;
    }

    /// <summary>
    /// Adds a package source to get a tool from
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="source"></param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithToolSource<T>(this IResourceBuilder<T> builder, string source)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.Sources.Add(source);
        return builder;
    }

    /// <summary>
    /// Only use the specified package sources, rather than using them in addition to the existing sources.
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithToolIgnoreExistingFeeds<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.IgnoreExistingFeeds = true;
        return builder;
    }

    /// <summary>
    /// Treat package source failures as warnings.
    /// </summary>
    /// <typeparam name="T">The Dotnet Tool resource type</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<T> WithToolIgnoreFailedSources<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration?.IgnoreFailedSources = true;
        return builder;
    }
}
