using Microsoft.Extensions.DependencyInjection;

namespace DotnetTool.AppHost;

/// <summary>
/// Provides extension methods for adding Dotnet Tool resources to the application model.
/// </summary>
public static class DotNetToolExtensions
{
    public static IResourceBuilder<DotnetToolResource> AddDotnetTool(this IDistributedApplicationBuilder builder, string name, string command, string packageId, Action<IResourceBuilder<DotnetToolResource>> configure)
    {
        var tool = builder.AddDotnetTool(name, packageId, command);
        configure(tool);
        return tool;
    }

    public static IResourceBuilder<DotnetToolResource> AddDotnetTool(this IDistributedApplicationBuilder builder, string name, string command, string packageId)
        => builder.AddDotnetTool(new DotnetToolResource(name, packageId, command));

    public static IResourceBuilder<T> AddDotnetTool<T>(this IDistributedApplicationBuilder builder, T resource)
        where T: DotnetToolResource
    {
        var tool = builder.AddResource(resource)
           .WithIconName("Toolbox");

        var installer = BuildInstaller();
        RewriteToolCommand();

        return tool.WaitForCompletion(installer);

        void RewriteToolCommand()
        {
            // To avoid excess redownloading, want to set the tool path to a 
            // .Net 10's `dotnet tool exec` would handle a lot of that natively
            // Although https://github.com/dotnet/sdk/issues/50579 is a complication
            // In the meantime, download tool to a path based on IAspireStore
            //
            // Using BeforeStartEvent rather than BeforeResoruceStart as the latter event
            // gets called multiple times, and prepending would break the path
            builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
            {
                if (Path.IsPathFullyQualified(resource.Command))
                {
                    throw new ArgumentException("Executable must not have an absolute path to run as a tool", nameof(builder));
                }

                var toolDirectory = GetToolDirectory(evt.Services, tool);
                tool.WithCommand(Path.Combine(toolDirectory, resource.Command));

                return Task.CompletedTask;
            });
        }

        IResourceBuilder<DotnetToolInstaller> BuildInstaller()
        {
            var installerResource = new DotnetToolInstaller($"{tool.Resource.Name}-installer", "dotnet") { Parent = tool.Resource };

            return builder
                .AddResource(installerResource)
                .WithArgs(x =>
                {
                    var toolDirectory = GetToolDirectory(x.ExecutionContext.ServiceProvider, tool);
                    var toolConfig = tool.Resource.ToolConfiguration;

                    x.Args.Add("tool");
                    x.Args.Add("install");
                    x.Args.Add(toolConfig.PackageId);
                    x.Args.Add("--tool-path");
                    x.Args.Add(toolDirectory);

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

                    if (toolConfig.AllowDowngrade)
                    {
                        x.Args.Add("--allow-downgrade");
                    }

                    x.Args.Add("--verbosity");
                    x.Args.Add("detailed");
                })
                .WithIconName("ArrowDownload")
                .WithParentRelationship(tool)
                .WithOfflineFallback();
        }

        string GetToolDirectory(IServiceProvider serviceProvider, IResourceBuilder<DotnetToolResource> tool)
        {
            var builder = tool.ApplicationBuilder;
            var explicitPath = builder.Configuration["ASPIRE_TOOLBASEPATH"];

            if (!string.IsNullOrEmpty(explicitPath))
            {
                return Path.Combine(explicitPath, builder.Environment.ApplicationName, tool.Resource.Name);
            }
            else
            {
                var store = serviceProvider.GetRequiredService<IAspireStore>();
                return Path.Combine(store.BasePath, "tools", tool.Resource.Name);
            }
        }
    }

    public static IResourceBuilder<T> WithPackageId<T>(this IResourceBuilder<T> builder, string packageId)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration.PackageId = packageId;
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
        builder.Resource.ToolConfiguration.Version = version;
        return builder;
    }

    public static IResourceBuilder<T> WithPackagePrerelease<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration.Prerelease = true;
        return builder;
    }

    public static IResourceBuilder<T> WithPackageSource<T>(this IResourceBuilder<T> builder, string source)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration.Sources.Add(source);
        return builder;
    }

    public static IResourceBuilder<T> WithPackageIgnoreExistingFeeds<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration.IgnoreExistingFeeds = true;
        return builder;
    }

    public static IResourceBuilder<T> WithPackageIgnoreFailedSources<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration.IgnoreFailedSources = true;
        return builder;
    }

    public static IResourceBuilder<T> WithPackageAllowDowngrade<T>(this IResourceBuilder<T> builder)
        where T : DotnetToolResource
    {
        builder.Resource.ToolConfiguration.AllowDowngrade = true;
        return builder;
    }

    private static IResourceBuilder<DotnetToolInstaller> WithOfflineFallback(this IResourceBuilder<DotnetToolInstaller> builder)
    {
        return builder.WithArgs(x =>
        {
            var settings = NuGet.Configuration.Settings.LoadDefaultSettings(root: builder.Resource.WorkingDirectory);
            var packagesPath = NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(settings);

            builder.ApplicationBuilder.CreateResourceBuilder(builder.Resource.Parent)
                .WithPackageSource(packagesPath)
                .WithPackageIgnoreFailedSources();
        });
    }
}
