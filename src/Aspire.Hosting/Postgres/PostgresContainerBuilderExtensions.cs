// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public static class PostgresContainerBuilderExtensions
{
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<PostgresContainerComponent> AddPostgresContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        var postgresContainer = new PostgresContainerComponent();
        return builder.AddComponent(name, postgresContainer)
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

    /// <summary>
    /// Sets a connection string for this service. The connection string will be available in the service's environment.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<T> WithPostgresDatabase<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<PostgresContainerComponent> postgres, string? databaseName = null, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        if (string.IsNullOrEmpty(connectionName) && !postgres.Component.TryGetName(out connectionName))
        {
            throw new DistributedApplicationException("Postgres connection name could not be determined. Please provide one.");
        }

        return builder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, () =>
        {
            if (!postgres.Component.TryGetLastAnnotation<PostgresPasswordAnnotation>(out var passwordAnnotation))
            {
                throw new InvalidOperationException($"Postgres does not have a password set!");
            }

            if (!postgres.Component.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                throw new InvalidOperationException("Expected allocated endpoints!");
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
