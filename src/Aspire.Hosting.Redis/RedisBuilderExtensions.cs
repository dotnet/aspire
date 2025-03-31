// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Redis;
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
    /// Adds a Redis container to the application model.
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
    /// This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.Tag"/> tag of the <inheritdoc cref="RedisContainerImageTags.Image"/> container image.
    /// </remarks>
    public static IResourceBuilder<RedisResource> AddRedis(this IDistributedApplicationBuilder builder, [ResourceName] string name, int? port)
    {
        return builder.AddRedis(name, port, null);
    }

    /// <summary>
    /// Adds a Redis container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <param name="password">The parameter used to provide the password for the Redis resource. If <see langword="null"/> a random password will be generated.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the Redis resource is able to service
    /// requests.
    /// </para>
    /// This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.Tag"/> tag of the <inheritdoc cref="RedisContainerImageTags.Image"/> container image.
    /// </remarks>
    public static IResourceBuilder<RedisResource> AddRedis(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // StackExchange.Redis doesn't support passwords with commas.
        // See https://github.com/StackExchange/StackExchange.Redis/issues/680 and
        // https://github.com/Azure/azure-dev/issues/4848 
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var redis = new RedisResource(name, passwordParameter);

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
                      .WithHealthCheck(healthCheckKey)
                      // see https://github.com/dotnet/aspire/issues/3838 for why the password is passed this way
                      .WithEntrypoint("/bin/sh")
                      .WithEnvironment(context =>
                      {
                          if (redis.PasswordParameter is { } password)
                          {
                              context.EnvironmentVariables["REDIS_PASSWORD"] = password;
                          }
                      })
                      .WithArgs(context =>
                      {
                          var redisCommand = new List<string>
                          {
                              "redis-server"
                          };

                          if (redis.PasswordParameter is not null)
                          {
                              redisCommand.Add("--requirepass");
                              redisCommand.Add("$REDIS_PASSWORD");
                          }

                          if (redis.TryGetLastAnnotation<PersistenceAnnotation>(out var persistenceAnnotation))
                          {
                              var interval = (persistenceAnnotation.Interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture);

                              redisCommand.Add("--save");
                              redisCommand.Add(interval);
                              redisCommand.Add(persistenceAnnotation.KeysChangedThreshold.ToString(CultureInfo.InvariantCulture));
                          }

                          context.Args.Add("-c");
                          context.Args.Add(string.Join(' ', redisCommand));

                          return Task.CompletedTask;
                      });
    }

    /// <summary>
    /// Configures a container resource for Redis Commander which is pre-configured to connect to the <see cref="RedisResource"/> that this method is used on.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.RedisCommanderTag"/> tag of the <inheritdoc cref="RedisContainerImageTags.RedisCommanderImage"/> container image.
    /// </remarks>
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
                        if (redisInstance.PasswordParameter is not null)
                        {
                            hostString += $":{redisInstance.PasswordParameter.Value}";
                        }
                        hostsVariableBuilder.Append(hostString);
                    }
                }

                resourceBuilder.WithEnvironment("REDIS_HOSTS", hostsVariableBuilder.ToString());

                return Task.CompletedTask;
            });

            configureContainer?.Invoke(resourceBuilder);

            resourceBuilder.WithRelationship(builder.Resource, "RedisCommander");

            return builder;
        }
    }

    /// <summary>
    /// Configures a container resource for Redis Insight which is pre-configured to connect to the <see cref="RedisResource"/> that this method is used on.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="RedisContainerImageTags.RedisInsightTag"/> tag of the <inheritdoc cref="RedisContainerImageTags.RedisInsightImage"/> container image.
    /// </remarks>
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

            // We need to wait for all endpoints to be allocated before attempting to import databases
            var endpointsAllocatedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
            {
                endpointsAllocatedTcs.TrySetResult();
                return Task.CompletedTask;
            });

            builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(resource, async (e, ct) =>
            {
                var redisInstances = builder.ApplicationBuilder.Resources.OfType<RedisResource>();

                if (!redisInstances.Any())
                {
                    // No-op if there are no Redis resources present.
                    return;
                }

                // Wait for all endpoints to be allocated before attempting to import databases
                await endpointsAllocatedTcs.Task.ConfigureAwait(false);

                var redisInsightResource = builder.ApplicationBuilder.Resources.OfType<RedisInsightResource>().Single();
                var insightEndpoint = redisInsightResource.PrimaryEndpoint;

                using var client = new HttpClient();
                client.BaseAddress = new Uri($"{insightEndpoint.Scheme}://{insightEndpoint.Host}:{insightEndpoint.Port}");

                var rls = e.Services.GetRequiredService<ResourceLoggerService>();
                var resourceLogger = rls.GetLogger(resource);

                await ImportRedisDatabases(resourceLogger, redisInstances, client, ct).ConfigureAwait(false);
            });

            resourceBuilder.WithRelationship(builder.Resource, "RedisInsight");

            configureContainer?.Invoke(resourceBuilder);

            return builder;
        }

        static async Task ImportRedisDatabases(ILogger resourceLogger, IEnumerable<RedisResource> redisInstances, HttpClient client, CancellationToken cancellationToken)
        {
            var databasesPath = "/api/databases";

            var pipeline = new ResiliencePipelineBuilder().AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxRetryAttempts = 5,
            }).Build();

            await pipeline.ExecuteAsync(async (ctx) =>
            {
                await InitializeRedisInsightSettings(client, resourceLogger, ctx).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            using (var stream = new MemoryStream())
            {
                // As part of configuring RedisInsight we need to factor in the possibility that the
                // container resource is being run with persistence turned on. In this case we need
                // to get the list of existing databases because we might need to delete some.
                var lookup = await pipeline.ExecuteAsync(async (ctx) =>
                {
                    var getDatabasesResponse = await client.GetFromJsonAsync<RedisDatabaseDto[]>(databasesPath, cancellationToken).ConfigureAwait(false);
                    return getDatabasesResponse?.ToLookup(
                        i => i.Name ?? throw new InvalidDataException("Database name is missing."),
                        i => i.Id ?? throw new InvalidDataException("Database ID is missing."));
                }, cancellationToken).ConfigureAwait(false);

                var databasesToDelete = new List<Guid>();

                using var writer = new Utf8JsonWriter(stream);

                writer.WriteStartArray();

                foreach (var redisResource in redisInstances)
                {
                    if (lookup is { } && lookup.Contains(redisResource.Name))
                    {
                        // It is possible that there are multiple databases with
                        // a conflicting name so we delete them all. This just keeps
                        // track of the specific ID that we need to delete.
                        databasesToDelete.AddRange(lookup[redisResource.Name]);
                    }

                    if (redisResource.PrimaryEndpoint.IsAllocated)
                    {
                        var endpoint = redisResource.PrimaryEndpoint;
                        writer.WriteStartObject();

                        writer.WriteString("host", redisResource.Name);
                        writer.WriteNumber("port", endpoint.TargetPort!.Value);
                        writer.WriteString("name", redisResource.Name);
                        writer.WriteNumber("db", 0);
                        writer.WriteNull("username");
                        if (redisResource.PasswordParameter is { } passwordParam)
                        {
                            writer.WriteString("password", passwordParam.Value);
                        }
                        else
                        {
                            writer.WriteNull("password");
                        }
                        writer.WriteString("connectionType", "STANDALONE");
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);

                var content = new MultipartFormDataContent();

                var fileContent = new StreamContent(stream);

                content.Add(fileContent, "file", "RedisInsight_connections.json");

                var apiUrl = $"{databasesPath}/import";

                try
                {
                    if (databasesToDelete.Any())
                    {
                        await pipeline.ExecuteAsync(async (ctx) =>
                        {
                            // Create a DELETE request to send to the existing instance of
                            // RedisInsight with the IDs of the database to delete.
                            var deleteContent = JsonContent.Create(new
                            {
                                ids = databasesToDelete
                            });

                            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, databasesPath)
                            {
                                Content = deleteContent
                            };

                            var deleteResponse = await client.SendAsync(deleteRequest, cancellationToken).ConfigureAwait(false);
                            deleteResponse.EnsureSuccessStatusCode();

                        }, cancellationToken).ConfigureAwait(false);
                    }

                    await pipeline.ExecuteAsync(async (ctx) =>
                    {
                        var response = await client.PostAsync(apiUrl, content, ctx)
                        .ConfigureAwait(false);

                        response.EnsureSuccessStatusCode();
                    }, cancellationToken).ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    resourceLogger.LogError("Could not import Redis databases into RedisInsight. Reason: {reason}", ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Initializes the Redis Insight settings to work around https://github.com/RedisInsight/RedisInsight/issues/3452.
    /// Redis Insight requires the encryption property to be set if the Redis database connection contains a password.
    /// </summary>
    private static async Task InitializeRedisInsightSettings(HttpClient client, ILogger resourceLogger, CancellationToken ct)
    {
        if (await AreSettingsInitialized(client, ct).ConfigureAwait(false))
        {
            return;
        }

        var jsonContent = JsonContent.Create(new
        {
            agreements = new
            {
                // all 4 are required to be set
                eula = false,
                analytics = false,
                notifications = false,
                encryption = false,
            }
        });

        var response = await client.PatchAsync("/api/settings", jsonContent, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            resourceLogger.LogDebug("Could not initialize RedisInsight settings. Reason: {reason}", await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task<bool> AreSettingsInitialized(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync("/api/settings", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var jsonResponse = JsonNode.Parse(content);
        var agreements = jsonResponse?["agreements"];

        return agreements is not null;
    }

    private class RedisDatabaseDto
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
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

        builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data", isReadOnly);
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
        ArgumentException.ThrowIfNullOrEmpty(source);

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

        return builder.WithAnnotation(
            new PersistenceAnnotation(interval, keysChangedThreshold), ResourceAnnotationMutationBehavior.Replace);
    }

    private sealed class PersistenceAnnotation(TimeSpan? interval, long keysChangedThreshold) : IResourceAnnotation
    {
        public TimeSpan? Interval => interval;
        public long KeysChangedThreshold => keysChangedThreshold;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Redis Insight container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Each overload targets a different resource builder type, allowing for tailored functionality. Optional volume names enhance usability, enabling users to easily provide custom names while maintaining clear and distinct method signatures.")]
    public static IResourceBuilder<RedisInsightResource> WithDataVolume(this IResourceBuilder<RedisInsightResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data");
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Redis Insight container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisInsightResource> WithDataBindMount(this IResourceBuilder<RedisInsightResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/data");
    }
}
