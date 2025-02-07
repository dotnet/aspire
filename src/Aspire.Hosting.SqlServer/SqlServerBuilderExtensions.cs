// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding SQL Server resources to the application model.
/// </summary>
public static class SqlServerBuilderExtensions
{
    /// <summary>
    /// Adds a SQL Server resource to the application model. A container is used for local development.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="SqlServerContainerImageTags.Tag"/> tag of the <inheritdoc cref="SqlServerContainerImageTags.Registry"/>/<inheritdoc cref="SqlServerContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="password">The parameter used to provide the administrator password for the SQL Server resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port for the SQL Server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> AddSqlServer(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<ParameterResource>? password = null, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // The password must be at least 8 characters long and contain characters from three of the following four sets: Uppercase letters, Lowercase letters, Base 10 digits, and Symbols
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", minLower: 1, minUpper: 1, minNumeric: 1);

        var sqlServer = new SqlServerServerResource(name, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(sqlServer, async (@event, ct) =>
        {
            connectionString = await sqlServer.GetConnectionStringAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{sqlServer.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddSqlServer(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(sqlServer)
                      .WithEndpoint(port: port, targetPort: 1433, name: SqlServerServerResource.PrimaryEndpointName)
                      .WithImage(SqlServerContainerImageTags.Image, SqlServerContainerImageTags.Tag)
                      .WithImageRegistry(SqlServerContainerImageTags.Registry)
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables["MSSQL_SA_PASSWORD"] = sqlServer.PasswordParameter;
                      })
                      .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a SQL Server database to the application model. This is a child resource of a <see cref="SqlServerServerResource"/>.
    /// </summary>
    /// <param name="builder">The SQL Server resource builders.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IResourceBuilder<SqlServerServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var sqlServerDatabase = new SqlServerDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(sqlServerDatabase);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a SQL Server resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithDataVolume(this IResourceBuilder<SqlServerServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/var/opt/mssql", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a SQL Server resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithDataBindMount(this IResourceBuilder<SqlServerServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        // c.f. https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-configure?view=sql-server-ver15&pivots=cs1-bash#mount-a-host-directory-as-data-volume

        foreach (var dir in new string[] { "data", "log", "secrets" })
        {
            var path = Path.Combine(source, dir);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            builder.WithBindMount(path, $"/var/opt/mssql/{dir}", isReadOnly);
        }

        return builder;
    }
}
