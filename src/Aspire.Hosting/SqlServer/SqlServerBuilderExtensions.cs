// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public static class SqlServerBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<SqlServerContainerResource> AddSqlServerContainer(this IDistributedApplicationBuilder builder, string name, string? password = null, int? port = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var sqlServer = new SqlServerContainerResource(name, password);

        return builder.AddResource(sqlServer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSqlServerContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433))
                      .WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" })
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment("MSSQL_SA_PASSWORD", sqlServer.GeneratedPassword);
    }

    public static IDistributedApplicationResourceBuilder<SqlServerConnectionResource> AddSqlServerConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
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

    public static IDistributedApplicationResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IDistributedApplicationResourceBuilder<SqlServerContainerResource> builder, string name)
    {
        var sqlServerDatabase = new SqlServerDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(sqlServerDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WriteSqlServerDatabaseToManifest(json, sqlServerDatabase)));
    }
}
