// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public static class PostgresBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<PostgresContainerComponent> AddPostgresContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        var postgresContainer = new PostgresContainerComponent(name);
        return builder.AddComponent(postgresContainer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WritePostgresComponentToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 5432)) // Internal port is always 5432.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "postgres", Tag = "latest" })
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
                      .WithAnnotation(new PostgresPasswordAnnotation(password ?? ""))
                      .WithEnvironment(PasswordEnvVarName, () =>
                      {
                          if (!postgresContainer.TryGetLastAnnotation<PostgresPasswordAnnotation>(out var passwordAnnotation))
                          {
                              throw new DistributedApplicationException("Password annotation not found!");
                          }

                          return passwordAnnotation.Password;
                      });
    }

    public static IDistributedApplicationComponentBuilder<PostgresComponent> AddPostgres(this IDistributedApplicationBuilder builder, string name, string? connectionString)
    {
        var postgres = new PostgresComponent(name, connectionString);

        return builder.AddComponent(postgres)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                WritePostgresComponentToManifest(jsonWriter, postgres.GetConnectionString())));
    }

    private static void WritePostgresComponentToManifest(Utf8JsonWriter jsonWriter) =>
        WritePostgresComponentToManifest(jsonWriter, null);

    private static void WritePostgresComponentToManifest(Utf8JsonWriter jsonWriter, string? connectionString)
    {
        jsonWriter.WriteString("type", "postgres.v1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }

    /// <summary>
    /// Sets a connection string for this service. The connection string will be available in the service's environment.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<IPostgresComponent> postgresBuilder, string? databaseName = null, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        var postgres = postgresBuilder.Component;
        connectionName = connectionName ?? postgresBuilder.Component.Name;

        return builder.WithEnvironment((context) =>
        {
            var connectionStringName = $"{ConnectionStringEnvironmentName}{connectionName}";

            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{postgres.Name}.connectionString}}";
                return;
            }

            var connectionString = postgres.GetConnectionString(databaseName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new DistributedApplicationException($"A connection string for Postgres '{postgres.Name}' could not be retrieved.");
            }
            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }
}
