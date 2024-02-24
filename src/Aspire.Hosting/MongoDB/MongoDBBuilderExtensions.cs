// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MongoDB;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MongoDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MongoDBBuilderExtensions
{
    private const int DefaultContainerPort = 27017;

    /// <summary>
    /// Adds a MongoDB resource to the application model. A container is used for local development.Adds a Kafka resource to the application. A container is used for local development.  This version the package defaults to the 7.0.5 tag of the mongo container image
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
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: DefaultContainerPort)) // Internal port is always 27017.
            .WithAnnotation(new ContainerImageAnnotation { Image = "mongo", Tag = "7.0.5" })
            .PublishAsContainer();
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
            .AddResource(mongoDBDatabase)
            .WithManifestPublishingCallback(mongoDBDatabase.WriteMongoDBDatabaseToManifest);
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
                                  .WithAnnotation(new ContainerImageAnnotation { Image = "mongo-express", Tag = "1.0.2-20" })
                                  .WithEnvironment(context => ConfigureMongoExpressContainer(context, builder.Resource))
                                  .WithHttpEndpoint(containerPort: 8081, hostPort: hostPort, name: containerName)
                                  .ExcludeFromManifest();

        return builder;
    }

    private static void ConfigureMongoExpressContainer(EnvironmentCallbackContext context, IResource resource)
    {
        var hostPort = GetResourcePort(resource);
        
        context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_URL", $"mongodb://host.docker.internal:{hostPort}/?directConnection=true");
        context.EnvironmentVariables.Add("ME_CONFIG_BASICAUTH", "false");

        static int GetResourcePort(IResource resource)
        {
            if (!resource.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                throw new DistributedApplicationException(
                    $"MongoDB resource \"{resource.Name}\" does not have endpoint annotation.");
            }

            return allocatedEndpoints.Single().Port;
        }
    }
}
