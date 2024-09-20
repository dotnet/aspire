// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Oracle Database resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class OracleDatabaseBuilderExtensions
{
    private const string PasswordEnvVarName = "ORACLE_PWD";

    /// <summary>
    /// Adds a Oracle Server resource to the application model. A container is used for local development. This version the package defaults to the 23.3.0.0 tag of the container-registry.oracle.com/database/free container image
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="password">The parameter used to provide the administrator password for the Oracle Server resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port for Oracle Server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> AddOracle(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? password = null, int? port = null)
    {
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var oracleDatabaseServer = new OracleDatabaseServerResource(name, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(oracleDatabaseServer, async (@event, ct) =>
        {
            connectionString = await oracleDatabaseServer.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{oracleDatabaseServer.Name}' resource but the connection string was null.");
            }

            var lookup = builder.Resources.OfType<OracleDatabaseResource>().ToDictionary(d => d.Name);

            foreach (var databaseName in oracleDatabaseServer.Databases)
            {
                if (!lookup.TryGetValue(databaseName.Key, out var databaseResource))
                {
                    throw new DistributedApplicationException($"Database resource '{databaseName}' under Oracle resource '{oracleDatabaseServer.Name}' was not found in the model.");
                }

                var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(databaseResource, @event.Services);
                await builder.Eventing.PublishAsync<ConnectionStringAvailableEvent>(connectionStringAvailableEvent, ct).ConfigureAwait(false);

                var beforeResourceStartedEvent = new BeforeResourceStartedEvent(databaseResource, @event.Services);
                await builder.Eventing.PublishAsync(beforeResourceStartedEvent, ct).ConfigureAwait(false);
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks()
            .AddOracle(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(oracleDatabaseServer)
                      .WithEndpoint(port: port, targetPort: 1521, name: OracleDatabaseServerResource.PrimaryEndpointName)
                      .WithImage(OracleContainerImageTags.Image, OracleContainerImageTags.Tag)
                      .WithImageRegistry(OracleContainerImageTags.Registry)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[PasswordEnvVarName] = oracleDatabaseServer.PasswordParameter;
                      })
                      .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a Oracle Database database to the application model.
    /// </summary>
    /// <param name="builder">The Oracle Database server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseResource> AddDatabase(this IResourceBuilder<OracleDatabaseServerResource> builder, string name, string? databaseName = null)
    {
        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var oracleDatabase = new OracleDatabaseResource(name, databaseName, builder.Resource);

        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(oracleDatabase, async (@event, ct) =>
        {
            connectionString = await oracleDatabase.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{oracleDatabase.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks()
            .AddOracle(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.ApplicationBuilder.AddResource(oracleDatabase)
                                         .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Oracle Database server container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> WithDataVolume(this IResourceBuilder<OracleDatabaseServerResource> builder, string? name = null)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/opt/oracle/oradata", false);

    /// <summary>
    /// Adds a bind mount for the data folder to a Oracle Database server container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> WithDataBindMount(this IResourceBuilder<OracleDatabaseServerResource> builder, string source)
        => builder.WithBindMount(source, "/opt/oracle/oradata", false);

    /// <summary>
    /// Adds a bind mount for the init folder to a Oracle Database server container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> WithInitBindMount(this IResourceBuilder<OracleDatabaseServerResource> builder, string source)
        => builder.WithBindMount(source, "/opt/oracle/scripts/startup", false);

    /// <summary>
    /// Adds a bind mount for the database setup folder to a Oracle Database server container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> WithDbSetupBindMount(this IResourceBuilder<OracleDatabaseServerResource> builder, string source)
        => builder.WithBindMount(source, "/opt/oracle/scripts/setup", false);
}
