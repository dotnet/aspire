// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MongoDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MongoDBBuilderExtensions
{
    private const int DefaultContainerPort = 27017;

    /// <summary>
    /// Adds a MongoDB container to the application model. The default image is "mongo" and the tag is "latest".
    /// </summary>
    /// <returns></returns>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MongoDB.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MongoDBContainerResource}"/>.</returns>
    public static IResourceBuilder<MongoDBContainerResource> AddMongoDBContainer(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var mongoDBContainer = new MongoDBContainerResource(name);

        return builder
            .AddResource(mongoDBContainer)
            .WithManifestPublishingCallback(context => WriteMongoDBContainerToManifest(context, mongoDBContainer))
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: DefaultContainerPort)) // Internal port is always 27017.
            .WithAnnotation(new ContainerImageAnnotation { Image = "mongo", Tag = "latest" });
    }

    /// <summary>
    /// Adds a MongoDB resource to the application model. A container is used for local development.
    /// </summary>
    /// <returns></returns>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MongoDBContainerResource}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> AddMongoDB(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        var mongoDBContainer = new MongoDBServerResource(name);

        return builder
            .AddResource(mongoDBContainer)
            .WithManifestPublishingCallback(WriteMongoDBServerToManifest)
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, containerPort: DefaultContainerPort)) // Internal port is always 27017.
            .WithAnnotation(new ContainerImageAnnotation { Image = "mongo", Tag = "latest" });
    }

    /// <summary>
    /// Adds a MongoDB database to the application model.
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MongoDBDatabaseResource}"/>.</returns>
    public static IResourceBuilder<MongoDBDatabaseResource> AddDatabase(this IResourceBuilder<MongoDBContainerResource> builder, string name)
    {
        var mongoDBDatabase = new MongoDBDatabaseResource(name, builder.Resource);

        return builder.ApplicationBuilder
            .AddResource(mongoDBDatabase)
            .WithManifestPublishingCallback(context => context.WriteMongoDBDatabaseToManifest(mongoDBDatabase));
    }

    private static void WriteMongoDBContainerToManifest(this ManifestPublishingContext context, MongoDBContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"{{{resource.Name}.bindings.tcp.host}}:{{{resource.Name}.bindings.tcp.port}}");
    }

    private static void WriteMongoDBServerToManifest(this ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "mongodb.server.v0");
    }

    private static void WriteMongoDBDatabaseToManifest(this ManifestPublishingContext context, MongoDBDatabaseResource mongoDbDatabase)
    {
        context.Writer.WriteString("type", "mongodb.database.v0");
        context.Writer.WriteString("parent", mongoDbDatabase.Parent.Name);
    }
}
