// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Redis;

namespace Aspire.Hosting;

public static class RedisBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<RedisContainerResource> AddRedisContainer(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var redis = new RedisContainerResource(name);
        return builder.AddResource(redis)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteRedisResourceToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 6379))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "latest" });
    }

    public static IDistributedApplicationResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
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
        jsonWriter.WriteString("type", "redis.v1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }
}
