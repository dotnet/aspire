// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding PostgreSQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PostgresBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";

    /// <summary>
    /// Adds a PostgreSQL resource to the application model. A container is used for local development. This version the package defaults to the 16.2 tag of the postgres container image
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port used when launching the container. If null a random port will be assigned.</param>
    /// <param name="password">The administrator password used for the container during local development. If null a random password will be generated.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PostgresServerResource> AddPostgres(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password ??= PasswordGenerator.GeneratePassword(6, 6, 2, 2);

        var postgresServer = new PostgresServerResource(name, password);
        return builder.AddResource(postgresServer)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "16.2" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256")
                      .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=scram-sha-256 --auth-local=scram-sha-256")
                      .WithEnvironment(context =>
                      {
                          if (context.ExecutionContext.IsPublishMode)
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{postgresServer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, postgresServer.Password);
                          }
                      })
                      .PublishAsContainer();
    }

    /// <summary>
    /// Adds a PostgreSQL database to the application model.
    /// </summary>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IResourceBuilder<PostgresServerResource> builder, string name, string? databaseName = null)
    {
        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var postgresDatabase = new PostgresDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(postgresDatabase)
                                         .WithManifestPublishingCallback(postgresDatabase.WriteToManifest);
    }

    /// <summary>
    /// Adds a pgAdmin 4 administration and development platform for PostgreSQL to the application model. This version the package defaults to the 8.3 tag of the dpage/pgadmin4 container image
    /// </summary>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="hostPort">The host port for the application ui.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPgAdmin<T>(this IResourceBuilder<T> builder, int? hostPort = null, string? containerName = null) where T: PostgresServerResource
    {
        if (builder.ApplicationBuilder.Resources.OfType<PgAdminContainerResource>().Any())
        {
            return builder;
        }

        builder.ApplicationBuilder.Services.TryAddLifecycleHook<PgAdminConfigWriterHook>();

        containerName ??= $"{builder.Resource.Name}-pgadmin";

        var pgAdminContainer = new PgAdminContainerResource(containerName);
        builder.ApplicationBuilder.AddResource(pgAdminContainer)
                                  .WithAnnotation(new ContainerImageAnnotation { Image = "dpage/pgadmin4", Tag = "8.3" })
                                  .WithHttpEndpoint(containerPort: 80, hostPort: hostPort, name: containerName)
                                  .WithEnvironment(SetPgAdminEnvironmentVariables)
                                  .WithBindMount(Path.GetTempFileName(), "/pgadmin4/servers.json")
                                  .ExcludeFromManifest();

        return builder;
    }

    private static void SetPgAdminEnvironmentVariables(EnvironmentCallbackContext context)
    {
        // Disables pgAdmin authentication.
        context.EnvironmentVariables.Add("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False");
        context.EnvironmentVariables.Add("PGADMIN_CONFIG_SERVER_MODE", "False");

        // You need to define the PGADMIN_DEFAULT_EMAIL and PGADMIN_DEFAULT_PASSWORD or PGADMIN_DEFAULT_PASSWORD_FILE environment variables.
        context.EnvironmentVariables.Add("PGADMIN_DEFAULT_EMAIL", "admin@domain.com");
        context.EnvironmentVariables.Add("PGADMIN_DEFAULT_PASSWORD", "admin");
    }

    /// <summary>
    /// Changes the PostgreSQL resource to be published as a container in the manifest.
    /// </summary>
    /// <param name="builder">The Postgres server resource builder.</param>
    /// <returns></returns>
    public static IResourceBuilder<PostgresServerResource> PublishAsContainer(this IResourceBuilder<PostgresServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(builder.Resource.WriteToManifest);
    }
}
