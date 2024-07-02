// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Redis resources to the application model.
/// </summary>
public static class RedisBuilderExtensions
{
    private const string RedisContainerDataDirectory = "/data";

    /// <summary>
    /// Adds a Redis container to the application model.
    /// </summary>
    /// <remarks>
    /// The default image is "redis" and the tag is "7.2.4".
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var redisResource = new RedisResource(name);
        return builder.AddCache(redisResource,
            RedisContainerImageTags.Registry,
            RedisContainerImageTags.Image,
            RedisContainerImageTags.Tag,
            6379,
            port);
    }

    /// <summary>
    /// Configures a container resource for Redis Commander which is pre-configured to connect to the <see cref="RedisResource"/> that this method is used on.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="RedisResource"/>.</param>
    /// <param name="configureContainer">Configuration callback for Redis Commander container resource.</param>
    /// <param name="containerName">Override the container name used for Redis Commander.</param>
    /// <returns></returns>
    public static IResourceBuilder<RedisResource> WithRedisCommander(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<RedisCommanderResource>>? configureContainer = null, string? containerName = null)
    {
        if (builder.ApplicationBuilder.Resources.OfType<RedisCommanderResource>().SingleOrDefault() is { } existingRedisCommanderResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingRedisCommanderResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            builder.ApplicationBuilder.Services.TryAddLifecycleHook<RedisCommanderConfigWriterHook>();
            containerName ??= $"{builder.Resource.Name}-commander";

            var resource = new RedisCommanderResource(containerName);
            var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
                                      .WithImage(RedisContainerImageTags.RedisCommanderImage, RedisContainerImageTags.RedisCommanderTag)
                                      .WithImageRegistry(RedisContainerImageTags.RedisCommanderRegistry)
                                      .WithHttpEndpoint(targetPort: 8081, name: "http")
                                      .ExcludeFromManifest();

            configureContainer?.Invoke(resourceBuilder);

            return builder;
        }
    }

    /// <summary>
    /// Configures the host port that the Redis Commander resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Redis Commander.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for PGAdmin.</returns>
    public static IResourceBuilder<RedisCommanderResource> WithHostPort(this IResourceBuilder<RedisCommanderResource> builder, int? port)
    {
        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Redis container resource and enables Redis persistence.
    /// </summary>
    /// <remarks>
    /// Use <see cref="WithPersistence(IResourceBuilder{RedisResource}, TimeSpan?, long)"/> to adjust Redis persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddRedis("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable Redis persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> WithDataVolume(this IResourceBuilder<RedisResource> builder,
        string? name = null,
        bool isReadOnly = false)
        => builder.WithDataVolume(RedisContainerDataDirectory, name, isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Redis container resource and enables Redis persistence.
    /// </summary>
    /// <remarks>
    /// Use <see cref="WithPersistence(IResourceBuilder{RedisResource}, TimeSpan?, long)"/> to adjust Redis persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddRedis("cache")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable Redis persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> WithDataBindMount(this IResourceBuilder<RedisResource> builder,
        string source,
        bool isReadOnly = false)
        => builder.WithDataBindMount(source, RedisContainerDataDirectory, isReadOnly);

    /// <summary>
    /// Configures a Redis container resource for persistence.
    /// </summary>
    /// <remarks>
    /// Use with <see cref="WithDataBindMount(IResourceBuilder{RedisResource}, string, bool)"/>
    /// or <see cref="WithDataVolume(IResourceBuilder{RedisResource}, string?, bool)"/> to persist Redis data across sessions with custom persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddRedis("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> WithPersistence(this IResourceBuilder<RedisResource> builder,
        TimeSpan? interval = null,
        long keysChangedThreshold = 1)
        => CacheBuilderExtensions.WithPersistence(builder, interval, keysChangedThreshold);

}
