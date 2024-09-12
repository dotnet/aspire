// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Redis resources to the application model.
/// </summary>
public static class RedisBuilderExtensions
{
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
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the Redis resource is able to service
    /// requests.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var redis = new RedisResource(name);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(redis, async (@event, ct) =>
        {
            connectionString = await redis.GetConnectionStringAsync(ct).ConfigureAwait(false);
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
