// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;

namespace Aspire.Hosting;

public static class PostgresBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";

    public static IDistributedApplicationResourceBuilder<PostgresContainerResource> AddPostgresContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var postgresContainer = new PostgresContainerResource(name, password);
        return builder.AddResource(postgresContainer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WritePostgresContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "latest" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
                      .WithEnvironment(PasswordEnvVarName, () => postgresContainer.Password);
    }

    public static IDistributedApplicationResourceBuilder<PostgresConnectionResource> AddPostgresConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var postgresConnection = new PostgresConnectionResource(name, connectionString);

        return builder.AddResource(postgresConnection)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation((json) => WritePostgresConnectionToManifest(json, postgresConnection)));
    }

    private static void WritePostgresConnectionToManifest(Utf8JsonWriter jsonWriter, PostgresConnectionResource postgresConnection)
    {
        jsonWriter.WriteString("type", "postgres.connection.v1");
        jsonWriter.WriteString("connectionString", postgresConnection.GetConnectionString());
    }

    private static void WritePostgresContainerToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "postgres.server.v1");
    }

    private static void WritePostgresDatabaseToManifest(Utf8JsonWriter json, PostgresDatabaseResource postgresDatabase)
    {
        json.WriteString("type", "postgres.database.v1");
        json.WriteString("parent", postgresDatabase.Parent.Name);
    }

    public static IDistributedApplicationResourceBuilder<PostgresDatabaseResource> AddDatabase(this IDistributedApplicationResourceBuilder<PostgresContainerResource> builder, string name)
    {
        var postgresDatabase = new PostgresDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(postgresDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WritePostgresDatabaseToManifest(json, postgresDatabase)));
    }
}
