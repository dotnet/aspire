// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MongoDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MongoDBBuilderExtensions
{
    // Internal port is always 27017.
    private const int DefaultContainerPort = 27017;

    /// <summary>
    /// Adds a MongoDB resource to the application model. A container is used for local development. This version the package defaults to the 7.0.5 tag of the mongo container image.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MongoDB.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var mongoDBContainer = new MongoDBServerResource(name);

        return builder
            .AddResource(mongoDBContainer)
            .WithEndpoint(port: port, targetPort: DefaultContainerPort, name: MongoDBServerResource.PrimaryEndpointName)
            .WithImage(MongoDBContainerImageTags.Image, MongoDBContainerImageTags.Tag)
            .WithImageRegistry(MongoDBContainerImageTags.Registry);
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
    /// <param name="hostPort">The host port for the application ui.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithMongoExpress<T>(this IResourceBuilder<T> builder, int? hostPort = null, string? containerName = null) where T : MongoDBServerResource
    {
        containerName ??= $"{builder.Resource.Name}-mongoexpress";

        var mongoExpressContainer = new MongoExpressContainerResource(containerName);
        builder.ApplicationBuilder.AddResource(mongoExpressContainer)
                                  .WithImage("mongo-express", "1.0.2-20")
                                  .WithImageRegistry(MongoDBContainerImageTags.Registry)
                                  .WithEnvironment(context => ConfigureMongoExpressContainer(context, builder.Resource))
                                  .WithHttpEndpoint(targetPort: 8081, port: hostPort, name: containerName)
                                  .ExcludeFromManifest();

        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithDataVolume(this IResourceBuilder<MongoDBServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/data/db", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithDataBindMount(this IResourceBuilder<MongoDBServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/data/db", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the init folder to a MongoDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> WithInitBindMount(this IResourceBuilder<MongoDBServerResource> builder, string source, bool isReadOnly = true)
        => builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);

    private static void ConfigureMongoExpressContainer(EnvironmentCallbackContext context, MongoDBServerResource resource)
    {
        context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_URL", $"mongodb://{resource.PrimaryEndpoint.ContainerHost}:{resource.PrimaryEndpoint.Port}/?directConnection=true");
        context.EnvironmentVariables.Add("ME_CONFIG_BASICAUTH", "false");
    }
}
