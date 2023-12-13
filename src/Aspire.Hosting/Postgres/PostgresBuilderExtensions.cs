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
                      .WithManifestPublishingCallback(context => WritePostgresContainerResourceToManifest(context, postgresContainer))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "latest" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256")
                      .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=scram-sha-256 --auth-local=scram-sha-256")
                      .WithEnvironment(context =>
                      {
                          if (context.PublisherName == "manifest")
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{postgresContainer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, postgresContainer.Password);
                          }
                      });
    }

    /// <summary>
    /// Adds a PostgreSQL resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresContainerResource}"/>.</returns>
    public static IResourceBuilder<PostgresServerResource> AddPostgres(this IDistributedApplicationBuilder builder, string name)
    {
        var password = Guid.NewGuid().ToString("N");
        var postgresServer = new PostgresServerResource(name, password);
        return builder.AddResource(postgresServer)
                      .WithManifestPublishingCallback(WritePostgresContainerToManifest)
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "latest" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256")
                      .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=scram-sha-256 --auth-local=scram-sha-256")
                      .WithEnvironment(PasswordEnvVarName, () => postgresServer.Password);
    }

    /// <summary>
    /// Adds a PostgreSQL database to the application model.
    /// </summary>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresDatabaseResource}"/>.</returns>
    public static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IResourceBuilder<IPostgresParentResource> builder, string name)
    {
        var postgresDatabase = new PostgresDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(postgresDatabase)
                                         .WithManifestPublishingCallback(context => WritePostgresDatabaseToManifest(context, postgresDatabase));
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

    private static void WritePostgresContainerResourceToManifest(ManifestPublishingContext context, PostgresContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"Host={{{resource.Name}.bindings.tcp.host}};Port={{{resource.Name}.bindings.tcp.port}};Username=postgres;Password={{{resource.Name}.inputs.password}};");
        context.Writer.WriteStartObject("inputs");      // "inputs": {
        context.Writer.WriteStartObject("password");    //   "password": {
        context.Writer.WriteString("type", "string");   //     "type": "string",
        context.Writer.WriteBoolean("secret", true);    //     "secret": true,
        context.Writer.WriteStartObject("default");     //     "default": {
        context.Writer.WriteStartObject("generate");    //       "generate": {
        context.Writer.WriteNumber("minLength", 10);    //         "minLength": 10,
        context.Writer.WriteEndObject();                //       }
        context.Writer.WriteEndObject();                //     }
        context.Writer.WriteEndObject();                //   }
        context.Writer.WriteEndObject();                // }
    }
}
