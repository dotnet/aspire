// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

public static class RedisBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<RedisContainerComponent> AddRedisContainer(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var redis = new RedisContainerComponent(name);

        var componentBuilder = builder.AddComponent(redis);
        componentBuilder.WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteRedisComponentToManifest));
        componentBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 6379)); // Internal port is always 6379.
        componentBuilder.WithAnnotation(new ContainerImageAnnotation { Image = "redis", Tag = "latest" });
        return componentBuilder;
    }

    public static IDistributedApplicationComponentBuilder<RedisComponent> AddRedis(this IDistributedApplicationBuilder builder, string name, string? connectionString)
    {
        var redis = new RedisComponent(name, connectionString);

        return builder.AddComponent(redis)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                WriteRedisComponentToManifest(jsonWriter, redis.GetConnectionString())));
    }

    private static void WriteRedisComponentToManifest(Utf8JsonWriter jsonWriter) =>
        WriteRedisComponentToManifest(jsonWriter, null);

    private static void WriteRedisComponentToManifest(Utf8JsonWriter jsonWriter, string? connectionString)
    {
        jsonWriter.WriteString("type", "redis.v1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }

    public static IDistributedApplicationComponentBuilder<T> WithRedis<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<IRedisComponent> redisBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        var redis = redisBuilder.Component;
        connectionName = connectionName ?? redis.Name;

        return builder.WithEnvironment((context) =>
        {
            var connectionStringName = $"{ConnectionStringEnvironmentName}{connectionName}";

            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{redis.Name}.connectionString}}";
                return;
            }

            var connectionString = redis.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new DistributedApplicationException($"A connection string for Redis '{redis.Name}' could not be retrieved.");
            }
            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }
}
