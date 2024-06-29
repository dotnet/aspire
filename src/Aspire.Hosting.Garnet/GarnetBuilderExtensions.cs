// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Garnet;
using Aspire.Hosting.Utils.Cache;

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
    public static IResourceBuilder<GarnetResource> AddGarnet(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var garnetResource = new GarnetResource(name);
        var garnetContainerImageTags = new GarnetContainerImageTags();
        return builder.AddCache(garnetResource,
            garnetContainerImageTags,
            6379,
            port);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Cache container resource and enables Cache persistence.
    /// </summary>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{GarnetResource}, TimeSpan?, long)"/> to adjust Cache persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddCache("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable Cache persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithDataVolume(this IResourceBuilder<GarnetResource> builder,
        string? name = null,
        bool isReadOnly = false)
        => builder.WithDataVolume(GarnetContainerDataDirectory, name, isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Cache container resource and enables Cache persistence.
    /// </summary>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{GarnetResource}, TimeSpan?, long)"/> to adjust Cache persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddCache("cache")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable Cache persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithDataBindMount(this IResourceBuilder<GarnetResource> builder,
        string source,
        bool isReadOnly = false)
        => builder.WithDataBindMount(source, GarnetContainerDataDirectory, isReadOnly);

    /// <summary>
    /// Configures a Cache container resource for persistence.
    /// </summary>
    /// <example>
    /// Use with <see cref="WithDataBindMount(IResourceBuilder{GarnetResource}, string, bool)"/>
    /// or <see cref="WithDataVolume(IResourceBuilder{GarnetResource}, string?, bool)"/> to persist Cache data across sessions with custom persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddCache("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithPersistence(this IResourceBuilder<GarnetResource> builder,
        TimeSpan? interval = null,
        long keysChangedThreshold = 1)
        => CacheBuilderExtensions.WithPersistence(builder, interval, keysChangedThreshold);
}
