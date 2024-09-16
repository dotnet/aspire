// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MongoDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MongoDBBuilderExtensions
{
    // Internal port is always 27017.
    private const int DefaultContainerPort = 27017;

    /// <summary>
    /// Adds a MongoDB resource to the application model. A container is used for local development. This version the package defaults to the 7.0.8 tag of the mongo container image.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MongoDB.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var mongoDBContainer = new MongoDBServerResource(name);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(mongoDBContainer, async (@event, ct) =>
        {
            connectionString = await mongoDBContainer.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{mongoDBContainer.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddMongoDb(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder
            .AddResource(mongoDBContainer)
            .WithEndpoint(port: port, targetPort: DefaultContainerPort, name: MongoDBServerResource.PrimaryEndpointName)
            .WithImage(MongoDBContainerImageTags.Image, MongoDBContainerImageTags.Tag)
            .WithImageRegistry(MongoDBContainerImageTags.Registry)
            .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a MongoDB database to the application model.
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBDatabaseResource> AddDatabase(this IResourceBuilder<MongoDBServerResource> builder, string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var mongoDBDatabase = new MongoDBDatabaseResource(name, databaseName, builder.Resource);

        return builder.ApplicationBuilder
            .AddResource(mongoDBDatabase);
    }

    /// <summary>
    /// Adds a MongoExpress administration and development platform for MongoDB to the application model. This version the package defaults to the 1.0.2-20 tag of the mongo-express container image
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="configureContainer">Configuration callback for Mongo Express container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithMongoExpress<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<MongoExpressContainerResource>>? configureContainer = null, string? containerName = null) where T : MongoDBServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        containerName ??= $"{builder.Resource.Name}-mongoexpress";

        var mongoExpressContainer = new MongoExpressContainerResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(mongoExpressContainer)
                                                        .WithImage(MongoDBContainerImageTags.MongoExpressImage, MongoDBContainerImageTags.MongoExpressTag)
                                                        .WithImageRegistry(MongoDBContainerImageTags.MongoExpressRegistry)
                                                        .WithEnvironment(context => ConfigureMongoExpressContainer(context, builder.Resource))
                                                        .WithHttpEndpoint(targetPort: 8081, name: "http")
                                                        .ExcludeFromManifest()
                                                        .WaitFor(builder);

        configureContainer?.Invoke(resourceBuilder);

        return builder;
    }

    /// <summary>
    /// Configures the host port that the Mongo Express resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Mongo Express.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for PGAdmin.</returns>
    public static IResourceBuilder<MongoExpressContainerResource> WithHostPort(this IResourceBuilder<MongoExpressContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a named volume for the data folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithDataVolume(this IResourceBuilder<MongoDBServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/data/db", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithDataBindMount(this IResourceBuilder<MongoDBServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/data/db", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithInitBindMount(this IResourceBuilder<MongoDBServerResource> builder, string source, bool isReadOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }

    /// <summary>
    /// Adds a replica set to the MongoDB server resource.
    /// </summary>
    /// <param name="builder">The MongoDB server resource.</param>
    /// <param name="replicaSetName">The name of the replica set. If not provided, defaults to <c>rs0</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithReplicaSet(this IResourceBuilder<MongoDBServerResource> builder, string? replicaSetName = null)
    {
        if (builder.Resource.TryGetLastAnnotation<MongoDbReplicaSetAnnotation>(out _))
        {
            throw new InvalidOperationException("A replica set has already been added to the MongoDB server resource.");
        }

        replicaSetName ??= "rs0";

        var port = SetPortAndTargetToBeSame(builder);

        // Add a container that initializes the replica set
        var init = builder.ApplicationBuilder
            .AddDockerfile("replicaset-init", GetReplicaSetInitDockerfileDir(replicaSetName, builder.Resource.Name, port))

            // We don't want to wait for the healthchecks to be successful since the initialization is required for that. However, we also don't want this to start
            // up until the database itself is ready
            .WaitFor(builder, includeHealthChecks: false);

        return builder
            .WithAnnotation(new MongoDbReplicaSetAnnotation(replicaSetName, init))
            .WithArgs("--replSet", replicaSetName, "--bind_ip_all", "--port", $"{port}");

        static int SetPortAndTargetToBeSame(IResourceBuilder<MongoDBServerResource> builder)
        {
            foreach (var endpoint in builder.Resource.Annotations.OfType<EndpointAnnotation>())
            {
                if (endpoint.Name == MongoDBServerResource.PrimaryEndpointName)
                {
                    if (endpoint.Port is { } port)
                    {
                        endpoint.TargetPort = port;
                    }

                    if (endpoint.TargetPort is not { } targetPort)
                    {
                        throw new InvalidOperationException("Target port is not set.");
                    }

                    // In the case of replica sets, the port and target port should be the same and is not proxied
                    endpoint.IsProxied = false;

                    return targetPort;
                }
            }

            throw new InvalidOperationException("No endpoint found for the MongoDB server resource.");
        }

        // See the conversation about setting up replica sets in Docker here: https://github.com/docker-library/mongo/issues/246
        static string GetReplicaSetInitDockerfileDir(string replicaSet, string host, int port)
        {
            var dir = Path.Combine(Path.GetTempPath(), "aspire.mongo", Path.GetRandomFileName());
            Directory.CreateDirectory(dir);

            var rsInitContents = $$"""rs.initiate({ _id:'{{replicaSet}}', members:[{_id:0,host:'localhost:{{port}}'}]})""";
            var init = Path.Combine(dir, "rs.js");
            File.WriteAllText(init, rsInitContents);

            var dockerfile = Path.Combine(dir, "Dockerfile");
            File.WriteAllText(dockerfile, $"""
                FROM {MongoDBContainerImageTags.Image}:{MongoDBContainerImageTags.Tag}
                WORKDIR /rsinit
                ADD rs.js rs.js
                ENTRYPOINT ["mongosh", "--port", "{port}", "--host", "{host}", "rs.js"]
                """);

            return dir;
        }
    }

    private static void ConfigureMongoExpressContainer(EnvironmentCallbackContext context, MongoDBServerResource resource)
    {
        var sb = new StringBuilder($"mongodb://{resource.Name}:{resource.PrimaryEndpoint.TargetPort}/?directConnection=true");

        if (resource.TryGetLastAnnotation<MongoDbReplicaSetAnnotation>(out var replica))
        {
            sb.Append('&');
            sb.Append(MongoDbReplicaSetAnnotation.QueryName);
            sb.Append('=');
            sb.Append(replica.ReplicaSetName);
        }

        // Mongo Exporess assumes Mongo is being accessed over a default Aspire container network and hardcodes the resource address
        // This will need to be refactored once updated service discovery APIs are available
        context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_URL", sb.ToString());
        context.EnvironmentVariables.Add("ME_CONFIG_BASICAUTH", "false");
    }

    /// <summary>
    /// Same as <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/> but with a few options we need.
    /// </summary>
    private static IResourceBuilder<T> WaitFor<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> dependency, bool includeHealthChecks = true) where T : IResource
    {
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, async (e, ct) =>
        {
            var rls = e.Services.GetRequiredService<ResourceLoggerService>();
            var resourceLogger = rls.GetLogger(builder.Resource);
            resourceLogger.LogInformation("Waiting for resource '{Name}' to enter the '{State}' state.", dependency.Resource.Name, KnownResourceStates.Running);

            var rns = e.Services.GetRequiredService<ResourceNotificationService>();
            await rns.PublishUpdateAsync(builder.Resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);
            var resourceEvent = await rns.WaitForResourceAsync(dependency.Resource.Name, re => IsContinuableState(re.Snapshot), cancellationToken: ct).ConfigureAwait(false);
            var snapshot = resourceEvent.Snapshot;

            if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
            {
                resourceLogger.LogError(
                    "Dependency resource '{ResourceName}' failed to start.",
                    dependency.Resource.Name
                    );

                throw new DistributedApplicationException($"Dependency resource '{dependency.Resource.Name}' failed to start.");
            }
            else if (snapshot.State!.Text == KnownResourceStates.Finished || snapshot.State!.Text == KnownResourceStates.Exited)
            {
                resourceLogger.LogError(
                    "Resource '{ResourceName}' has entered the '{State}' state prematurely.",
                    dependency.Resource.Name,
                    snapshot.State.Text
                    );

                throw new DistributedApplicationException(
                    $"Resource '{dependency.Resource.Name}' has entered the '{snapshot.State.Text}' state prematurely."
                    );
            }

            if (includeHealthChecks)
            {
                // If our dependency resource has health check annotations we want to wait until they turn healthy
                // otherwise we don't care about their health status.
                if (dependency.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var _))
                {
                    resourceLogger.LogInformation("Waiting for resource '{Name}' to become healthy.", dependency.Resource.Name);
                    await rns.WaitForResourceAsync(dependency.Resource.Name, re => re.Snapshot.HealthStatus == HealthStatus.Healthy, cancellationToken: ct).ConfigureAwait(false);
                }
            }
        });

        return builder;

        static bool IsContinuableState(CustomResourceSnapshot snapshot) =>
            snapshot.State?.Text == KnownResourceStates.Running ||
            snapshot.State?.Text == KnownResourceStates.Finished ||
            snapshot.State?.Text == KnownResourceStates.Exited ||
            snapshot.State?.Text == KnownResourceStates.FailedToStart;
    }
}
