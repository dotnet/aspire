// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding PostgreSQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PostgresBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";

    /// <summary>
    /// Adds a PostgreSQL container to the application model. The default image is "postgres" and the tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for PostgreSQL.</param>
    /// <param name="password">The password for the PostgreSQL container. Defaults to a random password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresContainerResource}"/>.</returns>
    public static IResourceBuilder<PostgresContainerResource> AddPostgresContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var postgresContainer = new PostgresContainerResource(name, password);
        return builder.AddResource(postgresContainer)
                      .WithManifestPublishingCallback(WritePostgresContainerToManifest)
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "latest" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
                      .WithEnvironment(PasswordEnvVarName, () => postgresContainer.Password);
    }

    /// <summary>
    /// Adds a PostgreSQL connection to the application model. Connection strings can also be read from the connection string section of the configuration using the name of the resource.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The PostgreSQL connection string (optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresConnectionResource}"/>.</returns>
    public static IResourceBuilder<PostgresConnectionResource> AddPostgresConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var postgresConnection = new PostgresConnectionResource(name, connectionString);

        return builder.AddResource(postgresConnection)
            .WithManifestPublishingCallback(context => WritePostgresConnectionToManifest(context, postgresConnection));
    }

    /// <summary>
    /// Adds a PostgreSQL database to the application model.
    /// </summary>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresDatabaseResource}"/>.</returns>
    public static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IResourceBuilder<PostgresContainerResource> builder, string name)
    {
        var postgresDatabase = new PostgresDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(postgresDatabase)
                                         .WithManifestPublishingCallback(context => WritePostgresDatabaseToManifest(context, postgresDatabase));
    }

    private static void WritePostgresConnectionToManifest(ManifestPublishingContext context, PostgresConnectionResource postgresConnection)
    {
        context.Writer.WriteString("type", "postgres.connection.v0");
        context.Writer.WriteString("connectionString", postgresConnection.GetConnectionString());
    }

    private static void WritePostgresContainerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "postgres.server.v0");
    }

    private static void WritePostgresDatabaseToManifest(ManifestPublishingContext context, PostgresDatabaseResource postgresDatabase)
    {
        context.Writer.WriteString("type", "postgres.database.v0");
        context.Writer.WriteString("parent", postgresDatabase.Parent.Name);
    }
}
