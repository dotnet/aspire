// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MongoDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MongoDBBuilderExtensions
{
    // Internal port is always 27017.
    private const int DefaultContainerPort = 27017;

    private const string UserEnvVarName = "MONGO_INITDB_ROOT_USERNAME";
    private const string PasswordEnvVarName = "MONGO_INITDB_ROOT_PASSWORD";

    /// <summary>
    /// Adds a MongoDB resource to the application model. A container is used for local development.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="MongoDBContainerImageTags.Tag"/> tag of the <inheritdoc cref="MongoDBContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MongoDB.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, [ResourceName] string name, int? port)
    {
        return AddMongoDB(builder, name, port, null, null);
    }

    /// <summary>
    /// <inheritdoc cref="AddMongoDB(IDistributedApplicationBuilder, string, int?)"/>
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MongoDB.</param>
    /// <param name="userName">A parameter that contains the MongoDb server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the MongoDb server password, or <see langword="null"/> to use a generated password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var mongoDBContainer = new MongoDBServerResource(name, userName?.Resource, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(mongoDBContainer, async (@event, ct) =>
        {
            connectionString = await mongoDBContainer.DirectConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{mongoDBContainer.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        // cache the client so it is reused on subsequent calls to the health check
        IMongoClient? client = null;
        builder.Services.AddHealthChecks()
            .AddMongoDb(
                sp => client ??= new MongoClient(connectionString ?? throw new InvalidOperationException("Connection string is unavailable")),
                name: healthCheckKey);

        return builder
            .AddResource(mongoDBContainer)
            .WithEndpoint(port: port, targetPort: DefaultContainerPort, name: MongoDBServerResource.PrimaryEndpointName)
            .WithImage(MongoDBContainerImageTags.Image, MongoDBContainerImageTags.Tag)
            .WithImageRegistry(MongoDBContainerImageTags.Registry)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[UserEnvVarName] = mongoDBContainer.UserNameReference;
                context.EnvironmentVariables[PasswordEnvVarName] = mongoDBContainer.PasswordParameter!;
            })
            .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a MongoDB database to the application model.
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBDatabaseResource> AddDatabase(this IResourceBuilder<MongoDBServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var mongoDBDatabase = new MongoDBDatabaseResource(name, databaseName, builder.Resource);

        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(mongoDBDatabase, async (@event, ct) =>
        {
            connectionString = await mongoDBDatabase.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{mongoDBDatabase.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        // cache the database client so it is reused on subsequent calls to the health check
        IMongoDatabase? database = null;
        builder.ApplicationBuilder.Services.AddHealthChecks()
            .AddMongoDb(
                sp => database ??=
                    new MongoClient(connectionString ?? throw new InvalidOperationException("Connection string is unavailable"))
                        .GetDatabase(databaseName),
                name: healthCheckKey);

        return builder.ApplicationBuilder
            .AddResource(mongoDBDatabase);
    }

    /// <summary>
    /// Adds a MongoExpress administration and development platform for MongoDB to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="MongoDBContainerImageTags.MongoExpressTag"/> tag of the <inheritdoc cref="MongoDBContainerImageTags.MongoExpressImage"/> container image.
    /// </remarks>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="configureContainer">Configuration callback for Mongo Express container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithMongoExpress<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<MongoExpressContainerResource>>? configureContainer = null, string? containerName = null)
        where T : MongoDBServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        containerName ??= $"{builder.Resource.Name}-mongoexpress";

        var mongoExpressContainer = new MongoExpressContainerResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(mongoExpressContainer)
                                                        .WithImage(MongoDBContainerImageTags.MongoExpressImage, MongoDBContainerImageTags.MongoExpressTag)
                                                        .WithImageRegistry(MongoDBContainerImageTags.MongoExpressRegistry)
                                                        .WithEnvironment(context => ConfigureMongoExpressContainer(context, builder.Resource))
                                                        .WithHttpEndpoint(targetPort: 8081, name: "http")
                                                        .WithParentRelationship(builder)
                                                        .ExcludeFromManifest();

        configureContainer?.Invoke(resourceBuilder);

        return builder;
    }

    /// <summary>
    /// Configures the host port that the Mongo Express resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Mongo Express.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
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

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data/db", isReadOnly);
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
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/data/db", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("Use WithInitFiles instead.")]
    public static IResourceBuilder<MongoDBServerResource> WithInitBindMount(this IResourceBuilder<MongoDBServerResource> builder, string source, bool isReadOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }

    /// <summary>
    /// Creates a connection string to the MongoDB server resource with the 'directConnection=true' parameter.
    /// </summary>
    /// <remarks>
    /// Direct connections are useful in environments with replica sets, ensuring the client connects directly to the specified server 
    /// rather than attempting to discover and connect to all members of the replica set. This is particularly important for applications
    /// running in containerized environments where MongoDB may only accept connections using the registered replica set name.
    /// </remarks>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the direct connection resource.</returns>
    public static IResourceBuilder<IResourceWithConnectionString> AsDirectConnection(this IResourceBuilder<MongoDBServerResource> builder)
        => builder.CreateDirectConnection();

    /// <summary>
    /// Creates a connection string to the MongoDB database resource with the 'directConnection=true' parameter.
    /// </summary>
    /// <remarks>
    /// Direct connections are useful in environments with replica sets, ensuring the client connects directly to the specified server 
    /// rather than attempting to discover and connect to all members of the replica set. This is particularly important for applications
    /// running in containerized environments where MongoDB may only accept connections using the registered replica set name.
    /// </remarks>
    /// <param name="builder">The MongoDB database resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the direct connection resource.</returns>
    public static IResourceBuilder<IResourceWithConnectionString> AsDirectConnection(this IResourceBuilder<MongoDBDatabaseResource> builder)
        => builder.CreateDirectConnection();

    private static IResourceBuilder<IResourceWithConnectionString> CreateDirectConnection(this IResourceBuilder<IResourceWithDirectConnectionString> builder)
        => builder.ApplicationBuilder.AddResource(new DirectConnectionString(builder.Resource.Name, builder.Resource.DirectConnectionStringExpression))
            .WithReferenceRelationship(builder.Resource.DirectConnectionStringExpression)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "ConnectionString",
                IsHidden = true,
                Properties = []
            });

    internal static IResourceBuilder<MongoDBServerResource> WithKeyFile(this IResourceBuilder<MongoDBServerResource> builder)
    {
        const string KeyFilePath = "/data/mongodb.key";

        // NOTE: This currently works because it's only called from WithReplicaSet that sets up the custom Dockerfile
        builder.WithArgs("--keyFile", KeyFilePath);
        builder.WithEnvironment("MONGO_KEYFILE_PATH", KeyFilePath);

        return builder;
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

        if (builder.Resource.PrimaryEndpoint.TargetPort is not { } port)
        {
            throw new InvalidOperationException("MongoDB server must have a target port");
        }

        var replicasetContextDir = Path.Combine(Path.GetDirectoryName(typeof(MongoDBBuilderExtensions).Assembly.Location)!, "Mongo.Replicaset");

        return builder
            .WithDockerfile(replicasetContextDir)
            .WithBuildArg("IMAGE_NAME", MongoDBContainerImageTags.Image)
            .WithBuildArg("IMAGE_TAG", MongoDBContainerImageTags.Tag)
            .WithAnnotation(new MongoDbReplicaSetAnnotation(replicaSetName))
            .WithArgs("--port", port)
            .WithArgs("--replSet", replicaSetName)
            .WithArgs("--bind_ip_all")
            .WithEnvironment(env =>
            {
                env.EnvironmentVariables["MONGO_HOSTANDPORT"] = builder.Resource.PrimaryEndpoint.Property(EndpointProperty.HostAndPort);
                env.EnvironmentVariables["MONGO_PORT"] = port;
                env.EnvironmentVariables["MONGO_AUTH_DB"] = MongoDBServerResource.DefaultAuthenticationDatabase;
                env.EnvironmentVariables["MONGO_REPLICASET_NAME"] = replicaSetName;
            })
            .WithKeyFile();
    }

    /// <summary>
    /// Copies init files into a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source file or directory on the host to copy into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithInitFiles(this IResourceBuilder<MongoDBServerResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        const string initPath = "/docker-entrypoint-initdb.d";

        var importFullPath = Path.GetFullPath(source, builder.ApplicationBuilder.AppHostDirectory);

        return builder.WithContainerFiles(
            initPath,
            ContainerDirectory.GetFileSystemItemsFromPath(importFullPath));
    }

    private static void ConfigureMongoExpressContainer(EnvironmentCallbackContext context, MongoDBServerResource resource)
    {
        // Mongo Express assumes Mongo is being accessed over a default Aspire container network and hardcodes the resource address
        // This will need to be refactored once updated service discovery APIs are available
        context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_SERVER", resource.Name);
        var targetPort = resource.PrimaryEndpoint.TargetPort;
        if (targetPort is int targetPortValue)
        {
            context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_PORT", targetPortValue.ToString(CultureInfo.InvariantCulture));
        }
        context.EnvironmentVariables.Add("ME_CONFIG_BASICAUTH", "false");
        if (resource.PasswordParameter is not null)
        {
            context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_ADMINUSERNAME", resource.UserNameReference);
            context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_ADMINPASSWORD", resource.PasswordParameter);
        }
    }

    /// <summary>
    /// Used to provide the direct connection string for <see cref="MongoDBServerResource.DirectConnectionStringExpression"/> or <see cref="MongoDBDatabaseResource.DirectConnectionStringExpression"/>.
    /// </summary>
    private sealed class DirectConnectionString(string name, ReferenceExpression other) : IResourceWithConnectionString
    {
        private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

        public string? ConnectionStringEnvironmentVariable { get; } = $"{ConnectionStringEnvironmentName}{name}";

        public ReferenceExpression ConnectionStringExpression => other;

        public string Name { get; } = $"{name}_direct";

        public ResourceAnnotationCollection Annotations { get; } = [];
    }
}
