// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding SQL Server resources to the application model.
/// </summary>
public static class SqlServerBuilderExtensions
{
    /// <summary>
    /// Adds a SQL Server container to the application model. The default image in "mcr.microsoft.com/mssql/server" an the tag is "2022-latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="password">The password of the SQL Server. By default, this will be randomly generated.</param>
    /// <param name="port">The host port for the SQL Server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{SqlServerContainerResource}"/>.</returns>
    public static IResourceBuilder<SqlServerContainerResource> AddSqlServerContainer(this IDistributedApplicationBuilder builder, string name, string? password = null, int? port = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var sqlServer = new SqlServerContainerResource(name, password);

        return builder.AddResource(sqlServer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSqlServerContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433))
                      .WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" })
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment("MSSQL_SA_PASSWORD", sqlServer.Password);
    }

    /// <summary>
    /// Adds a SQL Server connection to the application model. Connection strings can also be read from the connection string section of the configuration using the name of the resource.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{SqlServerConnectionResource}"/>.</returns>
    public static IResourceBuilder<SqlServerConnectionResource> AddSqlServerConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var sqlServerConnection = new SqlServerConnectionResource(name, connectionString);

        return builder.AddResource(sqlServerConnection)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter => WriteSqlServerConnectionToManifest(jsonWriter, sqlServerConnection)));
    }

    private static void WriteSqlServerConnectionToManifest(Utf8JsonWriter jsonWriter, SqlServerConnectionResource sqlServerConnection)
    {
        jsonWriter.WriteString("type", "sqlserver.connection.v1");
        jsonWriter.WriteString("connectionString", sqlServerConnection.GetConnectionString());
    }

    private static void WriteSqlServerContainerToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "sqlserver.server.v1");
    }

    private static void WriteSqlServerDatabaseToManifest(Utf8JsonWriter json, SqlServerDatabaseResource sqlServerDatabase)
    {
        json.WriteString("type", "sqlserver.database.v1");
        json.WriteString("parent", sqlServerDatabase.Parent.Name);
    }

    /// <summary>
    /// Adds a SQL Server database to the application model. This is a child resource of a <see cref="SqlServerContainerResource"/>.
    /// </summary>
    /// <param name="builder">The SQL Server resource builders.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{SqlServerDatabaseResource}"/>.</returns>
    public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IResourceBuilder<SqlServerContainerResource> builder, string name)
    {
        var sqlServerDatabase = new SqlServerDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(sqlServerDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WriteSqlServerDatabaseToManifest(json, sqlServerDatabase)));
    }
}
