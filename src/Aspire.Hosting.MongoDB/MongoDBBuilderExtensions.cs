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
            connectionString = await mongoDBContainer.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

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

        return builder.WithContainerFiles(initPath, importFullPath);
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
}
