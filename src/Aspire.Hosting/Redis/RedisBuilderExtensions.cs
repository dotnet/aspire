// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

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
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteRedisResourceToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 6379))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "latest" });
    }

    /// <summary>
    /// Adds a Redis connection to the application model. Connection strings can also be read from the connection string section of the configuration using the name of the resource.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/>.</returns>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var redis = new RedisResource(name, connectionString);
        return builder.AddResource(redis)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                        WriteRedisResourceToManifest(jsonWriter, redis.GetConnectionString())));
    }

    private static void WriteRedisResourceToManifest(Utf8JsonWriter jsonWriter) =>
        WriteRedisResourceToManifest(jsonWriter, null);

    private static void WriteRedisResourceToManifest(Utf8JsonWriter jsonWriter, string? connectionString)
    {
        jsonWriter.WriteString("type", "redis.v0");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }
}
