// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding SQL Server resources to the application model.
/// </summary>
public static class SqlServerBuilderExtensions
{
    /// <summary>
    /// Adds a SQL Server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="password">The password of the SQL Server. By default, this will be randomly generated.</param>
    /// <param name="port">The host port for the SQL Server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> AddSqlServer(this IDistributedApplicationBuilder builder, string name, string? password = null, int? port = null)
    {
        var sqlServer = new SqlServerServerResource(name, password);

        return builder.AddResource(sqlServer)
                      .WithEndpoint(hostPort: port, containerPort: 1433, name: SqlServerServerResource.PrimaryEndpointName)
                      .WithImage("mssql/server", "2022-latest")
                      .WithImageRegistry("mcr.microsoft.com")
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables["MSSQL_SA_PASSWORD"] = sqlServer.PasswordInput;
                      });
    }

    /// <summary>
    /// Adds a SQL Server database to the application model. This is a child resource of a <see cref="SqlServerServerResource"/>.
    /// </summary>
    /// <param name="builder">The SQL Server resource builders.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IResourceBuilder<SqlServerServerResource> builder, string name, string? databaseName = null)
    {
        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var sqlServerDatabase = new SqlServerDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(sqlServerDatabase)
                                         .WithManifestPublishingCallback(sqlServerDatabase.WriteToManifest);
    }

    /// <summary>
    /// Adds a named volume for the log folder to a SqlServer resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the resource name. </param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithDataVolume(this IResourceBuilder<SqlServerServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? $"{builder.Resource.Name}-data", "/var/opt/mssql/data", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the log folder to a SqlServer resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithDataBindMount(this IResourceBuilder<SqlServerServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/opt/mssql/data", isReadOnly);

    /// <summary>
    /// Adds a named volume for the log folder to a SqlServer container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the resource name. </param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithLogsVolume(this IResourceBuilder<SqlServerServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? $"{builder.Resource.Name}-logs", "/var/opt/mssql/log", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the log folder to a SqlServer container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithLogsBindMount(this IResourceBuilder<SqlServerServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/opt/mssql/log", isReadOnly);
}
