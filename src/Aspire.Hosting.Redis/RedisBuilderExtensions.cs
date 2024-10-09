// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Redis resources to the application model.
/// </summary>
public static class RedisBuilderExtensions
{
    /// <summary>
    /// Adds a Redis container to the application model. This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.Tag"/> tag of the <inheritdoc cref="RedisContainerImageTags.Image"/> container image.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the Redis resource is able to service
    /// requests.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, [ResourceName] string name, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var redis = new RedisResource(name);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(redis, async (@event, ct) =>
        {
            connectionString = await redis.GetConnectionStringAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{redis.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddRedis(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(redis)
                      .WithEndpoint(port: port, targetPort: 6379, name: RedisResource.PrimaryEndpointName)
                      .WithImage(RedisContainerImageTags.Image, RedisContainerImageTags.Tag)
                      .WithImageRegistry(RedisContainerImageTags.Registry)
                      .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Configures a container resource for Redis Commander which is pre-configured to connect to the <see cref="RedisResource"/> that this method is used on.
    /// This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.RedisCommanderTag"/> tag of the <inheritdoc cref="RedisContainerImageTags.RedisCommanderImage"/> container image.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="RedisResource"/>.</param>
    /// <param name="configureContainer">Configuration callback for Redis Commander container resource.</param>
    /// <param name="containerName">Override the container name used for Redis Commander.</param>
    /// <returns></returns>
    public static IResourceBuilder<RedisResource> WithRedisCommander(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<RedisCommanderResource>>? configureContainer = null, string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<RedisCommanderResource>().SingleOrDefault() is { } existingRedisCommanderResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingRedisCommanderResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            containerName ??= $"{builder.Resource.Name}-commander";

            var resource = new RedisCommanderResource(containerName);
            var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
                                      .WithImage(RedisContainerImageTags.RedisCommanderImage, RedisContainerImageTags.RedisCommanderTag)
                                      .WithImageRegistry(RedisContainerImageTags.RedisCommanderRegistry)
                                      .WithHttpEndpoint(targetPort: 8081, name: "http")
                                      .ExcludeFromManifest();

            builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
            {
                var redisInstances = builder.ApplicationBuilder.Resources.OfType<RedisResource>();

                if (!redisInstances.Any())
                {
                    // No-op if there are no Redis resources present.
                    return Task.CompletedTask;
                }

                var hostsVariableBuilder = new StringBuilder();

                foreach (var redisInstance in redisInstances)
                {
                    if (redisInstance.PrimaryEndpoint.IsAllocated)
                    {
                        // Redis Commander assumes Redis is being accessed over a default Aspire container network and hardcodes the resource address
                        // This will need to be refactored once updated service discovery APIs are available
                        var hostString = $"{(hostsVariableBuilder.Length > 0 ? "," : string.Empty)}{redisInstance.Name}:{redisInstance.Name}:{redisInstance.PrimaryEndpoint.TargetPort}:0";
                        hostsVariableBuilder.Append(hostString);
                    }
                }

                resourceBuilder.WithEnvironment("REDIS_HOSTS", hostsVariableBuilder.ToString());

                return Task.CompletedTask;
            });

            configureContainer?.Invoke(resourceBuilder);

            resourceBuilder.WithRelationship(builder.Resource, "Manager");

            return builder;
        }
    }

    /// <summary>
    /// Configures a container resource for Redis Insight which is pre-configured to connect to the <see cref="RedisResource"/> that this method is used on.
    /// This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.RedisInsightTag"/> tag of the <inheritdoc cref="RedisContainerImageTags.RedisInsightImage"/> container image.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="RedisResource"/>.</param>
    /// <param name="configureContainer">Configuration callback for Redis Insight container resource.</param>
    /// <param name="containerName">Override the container name used for Redis Insight.</param>
    /// <returns></returns>
    public static IResourceBuilder<RedisResource> WithRedisInsight(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<RedisInsightResource>>? configureContainer = null, string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<RedisInsightResource>().SingleOrDefault() is { } existingRedisCommanderResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingRedisCommanderResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            containerName ??= $"{builder.Resource.Name}-insight";

            var resource = new RedisInsightResource(containerName);
            var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
                                      .WithImage(RedisContainerImageTags.RedisInsightImage, RedisContainerImageTags.RedisInsightTag)
                                      .WithImageRegistry(RedisContainerImageTags.RedisInsightRegistry)
                                      .WithHttpEndpoint(targetPort: 5540, name: "http")
                                      .ExcludeFromManifest();

            builder.ApplicationBuilder.Eventing.Subscribe<AfterResourcesCreatedEvent>(async (e, ct) =>
            {
                var redisInstances = builder.ApplicationBuilder.Resources.OfType<RedisResource>();

                if (!redisInstances.Any())
                {
                    // No-op if there are no Redis resources present.
                    return;
                }

                var redisInsightResource = builder.ApplicationBuilder.Resources.OfType<RedisInsightResource>().Single();
                var insightEndpoint = redisInsightResource.PrimaryEndpoint;

                using var client = new HttpClient();
                client.BaseAddress = new Uri($"{insightEndpoint.Scheme}://{insightEndpoint.Host}:{insightEndpoint.Port}");

                var rls = e.Services.GetRequiredService<ResourceLoggerService>();
                var resourceLogger = rls.GetLogger(resource);

                await ImportRedisDatabases(resourceLogger, redisInstances, client, ct).ConfigureAwait(false);
            });

            configureContainer?.Invoke(resourceBuilder);

            return builder;
        }

        static async Task ImportRedisDatabases(ILogger resourceLogger, IEnumerable<RedisResource> redisInstances, HttpClient client, CancellationToken ct)
        {
            using (var stream = new MemoryStream())
            {
                using var writer = new Utf8JsonWriter(stream);

                writer.WriteStartArray();

                foreach (var redisResource in redisInstances)
                {
                    if (redisResource.PrimaryEndpoint.IsAllocated)
                    {
                        var endpoint = redisResource.PrimaryEndpoint;
                        writer.WriteStartObject();
                        writer.WriteString("host", redisResource.Name);
                        writer.WriteNumber("port", endpoint.TargetPort!.Value);
                        writer.WriteString("name", redisResource.Name);
                        writer.WriteNumber("db", 0);
                        //todo: provide username and password when https://github.com/dotnet/aspire/pull/4642 merged.
                        writer.WriteNull("username");
                        writer.WriteNull("password");
                        writer.WriteString("connectionType", "STANDALONE");
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
                await writer.FlushAsync(ct).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);

                var content = new MultipartFormDataContent();

                var fileContent = new StreamContent(stream);

                content.Add(fileContent, "file", "RedisInsight_connections.json");

                var apiUrl = $"/api/databases/import";

                var pipeline = new ResiliencePipelineBuilder().AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxRetryAttempts = 5,
                }).Build();

                try
                {
                    await pipeline.ExecuteAsync(async (ctx) =>
                    {
                        var response = await client.PostAsync(apiUrl, content, ctx)
                        .ConfigureAwait(false);

                        response.EnsureSuccessStatusCode();
                    }, ct).ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    resourceLogger.LogError("Could not import Redis databases into RedisInsight. Reason: {reason}", ex.Message);
                }
            };
        }
    }

    /// <summary>
    /// Configures the host port that the Redis Commander resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Redis Commander.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for RedisCommander.</returns>
    public static IResourceBuilder<RedisCommanderResource> WithHostPort(this IResourceBuilder<RedisCommanderResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Configures the host port that the Redis Insight resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Redis Insight.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for RedisInsight.</returns>
    public static IResourceBuilder<RedisInsightResource> WithHostPort(this IResourceBuilder<RedisInsightResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
    public static IResourceBuilder<RedisResource> WithDataVolume(this IResourceBuilder<RedisResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }
        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Redis container resource and enables Redis persistence.
    /// </summary>
    /// <remarks>
    /// Use <see cref="WithPersistence(IResourceBuilder{RedisResource}, TimeSpan?, long)"/> to adjust Redis persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddRedis("cache")
    ///                    .WithDataBindMount("myredisdata")
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
    public static IResourceBuilder<RedisResource> WithDataBindMount(this IResourceBuilder<RedisResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        builder.WithBindMount(source, "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }
        return builder;
    }

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
    public static IResourceBuilder<RedisResource> WithPersistence(this IResourceBuilder<RedisResource> builder, TimeSpan? interval = null, long keysChangedThreshold = 1)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(context =>
        {
            context.Args.Add("--save");
            context.Args.Add(
                (interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            context.Args.Add(keysChangedThreshold.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }), ResourceAnnotationMutationBehavior.Replace);
    }
}
