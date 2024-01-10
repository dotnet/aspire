// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Redis;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Redis resources to the application model.
/// </summary>
public static class RedisBuilderExtensions
{
    /// <summary>
    /// Adds a Redis container to the application model. The default image is "redis" and tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for the redis server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisContainerResource}"/>.</returns>
    public static IResourceBuilder<RedisContainerResource> AddRedisContainer(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var redis = new RedisContainerResource(name);
        return builder.AddResource(redis)
                      .WithManifestPublishingCallback(context => WriteRedisContainerResourceToManifest(context, redis))
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 6379))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "latest" });
    }

    /// <summary>
    /// Adds a Redis container to the application model. The default image is "redis" and tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisContainerResource}"/>.</returns>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, string name)
    {
        var redis = new RedisResource(name);
        return builder.AddResource(redis)
                      .WithManifestPublishingCallback(WriteRedisResourceToManifest)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, containerPort: 6379))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "latest" });
    }

    public static IResourceBuilder<T> WithRedisCommander<T>(this IResourceBuilder<T> builder, string? containerName = null, int? hostPort = null) where T: IRedisResource
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
                                  .WithEndpoint(containerPort: 8081, hostPort: hostPort, scheme: "http", name: containerName)
                                  .ExcludeFromManifest();

        return builder;
    }

    private static void WriteRedisResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "redis.v0");
    }

    private static void WriteRedisContainerResourceToManifest(ManifestPublishingContext context, RedisContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"{{{resource.Name}.bindings.tcp.host}}:{{{resource.Name}.bindings.tcp.port}}");
    }
}
