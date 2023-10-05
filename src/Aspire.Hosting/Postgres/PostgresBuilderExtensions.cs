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

    public static IDistributedApplicationComponentBuilder<PostgresComponent> AddPostgres(this IDistributedApplicationBuilder builder, string name, string connectionString)
    {
        return builder.AddComponent(new PostgresComponent(name, connectionString))
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(WritePostgresComponentToManifest));
    }

    private static async Task WritePostgresComponentToManifest(Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "postgres.v1");
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a connection string for this service. The connection string will be available in the service's environment.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<IPostgresComponent> postgresBuilder, string? databaseName = null, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        connectionName = connectionName ?? postgresBuilder.Component.Name;

        return builder.WithEnvironment((context) =>
        {
            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[$"{ConnectionStringEnvironmentName}{connectionName}"] = $"{{{postgresBuilder.Component.Name}.connectionString}}";
                return;
            }

            var connectionString = postgresBuilder.Component.GetConnectionString(databaseName);
            context.EnvironmentVariables[$"{ConnectionStringEnvironmentName}{connectionName}"] = connectionString;
        });
    }
}
