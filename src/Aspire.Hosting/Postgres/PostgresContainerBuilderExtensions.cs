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

    private static async Task WritePostgresComponentToManifest(Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "postgres.v1");
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a connection string for this service. The connection string will be available in the service's environment.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<PostgresContainerComponent> postgresBuilder, string? databaseName = null, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        connectionName = connectionName ?? postgresBuilder.Component.Name;

        return builder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, () =>
        {
            if (!postgresBuilder.Component.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                // HACK: When their are no allocated endpoints it could mean that there is a problem with
                //       DCP, however we want to try and use the same callback for now for generating the
                //       connection string expressions in the manifest. So rather than throwing where
                //       there are no allocated endpoints we will instead emit the appropriate expression.
                return $"{{{postgresBuilder.Component.Name}.connectionString}}";
            }

            if (!postgresBuilder.Component.TryGetLastAnnotation<PostgresPasswordAnnotation>(out var passwordAnnotation))
            {
                throw new InvalidOperationException($"Postgres does not have a password set!");
            }

            var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Postgres.

            var baseConnectionString = $"Host={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};Username=postgres;Password={passwordAnnotation.Password};";
            var connectionString = databaseName == null ? baseConnectionString : $"{baseConnectionString}Database={databaseName};";
            return connectionString;
        });
    }

    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, string connectionName, string connectionString)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, connectionString);
    }
}
