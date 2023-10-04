// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Redis;

public static class RedisContainerBuilderExtensions
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

    private static async Task WriteRedisComponentToManifest(Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "redis.v1");
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static IDistributedApplicationComponentBuilder<T> WithRedis<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<RedisContainerComponent> redisBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        connectionName = connectionName ?? redisBuilder.Component.Name;

        return builder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, () =>
        {
            if (builder.GetPublisherName() == "manifest")
            {
                return $"{{{redisBuilder.Component.Name}.connectionString}}";
            }

            if (!redisBuilder.Component.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
            {
                throw new DistributedApplicationException("Redis component does not have endpoint annotation.");
            }

            // We should only have one endpoint for Redis for local scenarios.
            var endpoint = allocatedEndpoints.Single();
            return endpoint.EndPointString;
        });
    }

    public static IDistributedApplicationComponentBuilder<T> WithRedis<T>(this IDistributedApplicationComponentBuilder<T> projectBuilder, string connectionName, string connectionString)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return projectBuilder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, connectionString);
    }
}
