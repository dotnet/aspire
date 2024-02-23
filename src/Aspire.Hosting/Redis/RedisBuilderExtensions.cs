// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
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
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 6379))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "7.2.4" })
                      .PublishAsContainer();
    }

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="containerName"></param>
    /// <param name="hostPort"></param>
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
                                  .WithAnnotation(new ContainerImageAnnotation { Image = "rediscommander/redis-commander", Tag = "latest" })
                                  .WithHttpEndpoint(containerPort: 8081, hostPort: hostPort, name: containerName)
                                  .ExcludeFromManifest();

        return builder;
    }
}
