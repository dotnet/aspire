// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public static class PostgresContainerBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<PostgresContainerComponent> AddPostgresContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        var postgresContainer = new PostgresContainerComponent(name);
        return builder.AddComponent(postgresContainer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation((writer, ct) => WritePostgresComponentToManifestAsync(postgresContainer, writer, ct)))
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

    private static async Task WritePostgresComponentToManifestAsync(PostgresContainerComponent component, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "postgres.v1");

        var databases = component.Annotations.OfType<PostgresDatabaseAnnotation>();
        if (databases.Any())
        {
            jsonWriter.WriteStartObject("databases");

            foreach (var database in databases)
            {
                jsonWriter.WriteStartObject(database.DatabaseName);
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();

        }

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a connection string for this service. The connection string will be available in the service's environment.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<PostgresContainerComponent> postgresBuilder, string? databaseName = null, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        connectionName = connectionName ?? postgresBuilder.Component.Name;

        // We need to capture this here so that when we do manifest generation we know
        // how to enumerate the databases.
        if (databaseName != null && !postgresBuilder.Component.Annotations.OfType<PostgresDatabaseAnnotation>().Any(db => db.DatabaseName == databaseName))
        {
            postgresBuilder.WithAnnotation(new PostgresDatabaseAnnotation(databaseName));
        }

        return builder.WithEnvironment((context) =>
        {
            if (context.PublisherName == "manifest")
            {
                var manifestConnectionString = databaseName == null ? $"{postgresBuilder.Component.Name}.connectionString" : $"{postgresBuilder.Component.Name}.databases.{databaseName}.connectionString";
                context.EnvironmentVariables[$"{ConnectionStringEnvironmentName}{connectionName}"] = manifestConnectionString;
                return;
            }

            if (!postgresBuilder.Component.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                throw new InvalidOperationException("Expected allocated endpoints!");
            }

            if (!postgresBuilder.Component.TryGetLastAnnotation<PostgresPasswordAnnotation>(out var passwordAnnotation))
            {
                throw new InvalidOperationException($"Postgres does not have a password set!");
            }

            var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Postgres.

            var baseConnectionString = $"Host={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};Username=postgres;Password={passwordAnnotation.Password};";
            var connectionString = databaseName == null ? baseConnectionString : $"{baseConnectionString}Database={databaseName};";

            context.EnvironmentVariables[$"{ConnectionStringEnvironmentName}{connectionName}"] = connectionString;
        });
    }

    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, string connectionName, string connectionString)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, connectionString);
    }
}
