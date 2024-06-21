// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Garnet;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Garnet resources to the application model.
/// </summary>
public static class GarnetBuilderExtensions
{
    private const string GarnetContainerDataDirectory = "/data";

    /// <summary>
    /// Adds a Garnet container to the application model.
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var garnet = builder.AddGarnet("garnet");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api)
    ///                  .WithReference(garnet);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <example>
    /// Use in Api with Aspire.StackExchange.Redis
    /// <code lang="csharp">
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddRedisClient("garnet");
    ///
    /// var multiplexer = builder.Services.BuildServiceProvider()
    ///                                   .GetRequiredService&lt;IConnectionMultiplexer&gt;();
    /// 
    /// var db = multiplexer.GetDatabase();
    /// db.HashSet("key", [new HashEntry("hash", "value")]);
    /// var value = db.HashGet("key", "hash");
    /// </code>
    /// </example>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> AddGarnet(this IDistributedApplicationBuilder builder, string name,
        int? port = null)
    {
        var garnet = new GarnetResource(name);
        return builder.AddResource(garnet)
            .WithEndpoint(port: port, targetPort: 6379, name: GarnetResource.PrimaryEndpointName)
            .WithImage(GarnetContainerImageTags.Image, GarnetContainerImageTags.Tag)
            .WithImageRegistry(GarnetContainerImageTags.Registry);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Garnet container resource and enables Garnet persistence.
    /// </summary>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{GarnetResource}, TimeSpan?, long)"/> to adjust Garnet persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddGarnet("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable Garnet persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithDataVolume(this IResourceBuilder<GarnetResource> builder,
        string? name = null, bool isReadOnly = false)
    {
        builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), GarnetContainerDataDirectory,
            isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Garnet container resource and enables Garnet persistence.
    /// </summary>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{GarnetResource}, TimeSpan?, long)"/> to adjust Garnet persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var garnet = builder.AddGarnet("garnet")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable Garnet persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithDataBindMount(this IResourceBuilder<GarnetResource> builder,
        string source, bool isReadOnly = false)
    {
        builder.WithBindMount(source, GarnetContainerDataDirectory, isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Configures a Garnet container resource for persistence.
    /// </summary>
    /// <example>
    /// Use with <see cref="WithDataBindMount(IResourceBuilder{GarnetResource}, string, bool)"/>
    /// or <see cref="WithDataVolume(IResourceBuilder{GarnetResource}, string?, bool)"/> to persist Garnet data across sessions with custom persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddGarnet("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithPersistence(this IResourceBuilder<GarnetResource> builder,
        TimeSpan? interval = null, long keysChangedThreshold = 1)
        => builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(context =>
        {
            context.Args.Add("--save");
            context.Args.Add((interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            context.Args.Add(keysChangedThreshold.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }), ResourceAnnotationMutationBehavior.Replace);
}
