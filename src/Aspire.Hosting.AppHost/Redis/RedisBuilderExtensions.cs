// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Redis;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Redis resources to the application model.
/// </summary>
public static class RedisBuilderExtensions
{
    /// <summary>
    /// Adds a Redis container to the application model. The default image is "redis" and tag is "latest". This version the package defaults to the 7.2.4 tag of the redis container image
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var redis = new RedisResource(name);
        return builder.AddResource(redis)
                      .WithEndpoint(hostPort: port, containerPort: 6379, name: RedisResource.PrimaryEndpointName)
                      .WithImage(ContainerImageTags.Redis.Image, ContainerImageTags.Redis.Tag);
    }

    /// <summary>
    /// Configures a container resource for Redis Commander which is pre-configured to connect to the <see cref="RedisResource"/> that this method is used on.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="RedisResource"/>.</param>
    /// <param name="containerName">Override the container name used for Redis Commander.</param>
    /// <param name="hostPort">The host port to expose the Redis Commander container image on.</param>
    /// <returns></returns>
    public static IResourceBuilder<RedisResource> WithRedisCommander(this IResourceBuilder<RedisResource> builder, string? containerName = null, int? hostPort = null)
    {
        if (builder.ApplicationBuilder.Resources.OfType<RedisCommanderResource>().Any())
        {
            return builder;
        }

        builder.ApplicationBuilder.Services.TryAddLifecycleHook<RedisCommanderConfigWriterHook>();

        containerName ??= $"{builder.Resource.Name}-commander";

        var resource = new RedisCommanderResource(containerName);
        builder.ApplicationBuilder.AddResource(resource)
                                  .WithImage("rediscommander/redis-commander", "latest")
                                  .WithHttpEndpoint(containerPort: 8081, hostPort: hostPort, name: containerName)
                                  .ExcludeFromManifest();

        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Redis container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the resource name. </param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> WithDataVolume(this IResourceBuilder<RedisResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? $"{builder.Resource.Name}-data", "/data", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Redis container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> WithDataBindMount(this IResourceBuilder<RedisResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/data", isReadOnly);
}
