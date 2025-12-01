#pragma warning disable IDE0005 // Using directive is unnecessary (needed when file is linked to test project)
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
#pragma warning restore IDE0005

namespace DotnetTool.AppHost;

/// <summary>
/// Provides extension methods for adding Dotnet Tool resources to the application model.
/// </summary>
public static class DotNetToolExtensions
{
    public static IResourceBuilder<DotnetToolResource> AddDotnetTool(this IDistributedApplicationBuilder builder, string name, string packageId)
        => builder.AddDotnetTool(new DotnetToolResource(name, packageId));

    public static IResourceBuilder<T> AddDotnetTool<T>(this IDistributedApplicationBuilder builder, T resource)
        where T : DotnetToolResource
    {
        return builder.AddResource(resource)
           .WithIconName("Toolbox")
           .WithCommand("dotnet")
           .WithArgs(x =>
           {
               if (!x.Resource.TryGetLastAnnotation<DotNetToolAnnotation>(out var toolConfig))
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

               x.Args.Add("--verbosity");
               x.Args.Add("detailed");
               x.Args.Add("--yes");
               x.Args.Add("--");
           });
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
}
