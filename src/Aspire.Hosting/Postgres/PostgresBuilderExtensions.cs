// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public static class PostgresBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";

    public static IDistributedApplicationComponentBuilder<PostgresContainerComponent> AddPostgresContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var postgresContainerComponent = new PostgresContainerComponent(name, password);
        return builder.AddComponent(postgresContainerComponent)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WritePostgresContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "latest" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
                      .WithEnvironment(PasswordEnvVarName, () => postgresContainerComponent.Password);
    }

    public static IDistributedApplicationComponentBuilder<PostgresConnectionComponent> AddPostgresConnection(this IDistributedApplicationBuilder builder, string name, string connectionString)
    {
        var postgresConnectionComponent = new PostgresConnectionComponent(name, connectionString);

        return builder.AddComponent(postgresConnectionComponent)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation((json) => WritePostgresComponentToManifest(json, postgresConnectionComponent)));
    }

    private static void WritePostgresComponentToManifest(Utf8JsonWriter jsonWriter, PostgresConnectionComponent postgresConnectionComponent)
    {
        jsonWriter.WriteString("type", "postgres.connection.v1");
        jsonWriter.WriteString("connectionString", postgresConnectionComponent.GetConnectionString());
    }

    private static void WritePostgresContainerToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "postgres.server.v1");
    }

    private static void WritePostgresDatabaseComponentToManifest(Utf8JsonWriter json, PostgresDatabaseComponent postgresDatabaseComponent)
    {
        json.WriteString("type", "postgres.database.v1");
        json.WriteString("parent", postgresDatabaseComponent.Parent.Name);
    }

    /// <summary>
    /// Sets a connection string for this service. The connection string will be available in the service's environment.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<T> WithPostgres<T, TPostgres>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<TPostgres> postgresBuilder, string? connectionName = null) where TPostgres : IPostgresComponent
        where T : IDistributedApplicationComponentWithEnvironment
    {
        connectionName ??= postgresBuilder.Component.Name;

        return builder.WithReference(postgresBuilder, connectionName);

        //return builder.WithEnvironment((context) =>
        //{
        //    var connectionStringName = $"{ConnectionStringEnvironmentName}{connectionName}";

        //    if (context.PublisherName == "manifest")
        //    {
        //        context.EnvironmentVariables[connectionStringName] = $"{{{postgres.Name}.connectionString}}";
        //        return;
        //    }

        //    var connectionString = postgres.GetConnectionString() ?? builder.ApplicationBuilder.Configuration.GetConnectionString(postgres.Name);

        //    if (string.IsNullOrEmpty(connectionString))
        //    {
        //        throw new DistributedApplicationException($"A connection string for Postgres '{postgres.Name}' could not be retrieved.");
        //    }

        //    context.EnvironmentVariables[connectionStringName] = connectionString;
        //});
    }

    public static IDistributedApplicationComponentBuilder<PostgresDatabaseComponent> AddDatabase(this IDistributedApplicationComponentBuilder<PostgresContainerComponent> builder, string name)
    {
        var postgresDatabase = new PostgresDatabaseComponent(name, builder.Component);
        return builder.ApplicationBuilder.AddComponent(postgresDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WritePostgresDatabaseComponentToManifest(json, postgresDatabase)));
    }
}
