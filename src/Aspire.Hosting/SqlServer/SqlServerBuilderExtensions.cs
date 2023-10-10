// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.SqlServer;

public static class SqlServerBuilderExtensions
{
    public static IDistributedApplicationComponentBuilder<SqlServerContainerComponent> AddSqlServerContainer(this IDistributedApplicationBuilder builder, string name, string? password = null, int? port = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var sqlServer = new SqlServerContainerComponent(name, password);

        var componentBuilder = builder.AddComponent(sqlServer);
        componentBuilder.WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSqlServerContainerToManifest));
        componentBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433));
        componentBuilder.WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" });
        componentBuilder.WithEnvironment("ACCEPT_EULA", "Y");
        componentBuilder.WithEnvironment("MSSQL_SA_PASSWORD", sqlServer.GeneratedPassword);
        return componentBuilder;
    }

    public static IDistributedApplicationComponentBuilder<SqlServerConnectionComponent> AddSqlServerConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        connectionString = connectionString
                           ?? builder.Configuration.GetConnectionString(name)
                           ?? throw new DistributedApplicationException($"A connection string for SQL Server resource '{name}' could not be retrieved.");

        var sqlServerConnectionComponent = new SqlServerConnectionComponent(name, connectionString);

        return builder.AddComponent(sqlServerConnectionComponent)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                WriteSqlServerConnectionToManifest(jsonWriter, sqlServerConnectionComponent)));
    }

    private static void WriteSqlServerConnectionToManifest(Utf8JsonWriter jsonWriter, SqlServerConnectionComponent sqlServerConnectionComponent)
    {
        jsonWriter.WriteString("type", "sqlserver.connection.v1");
        jsonWriter.WriteString("connectionString", sqlServerConnectionComponent.GetConnectionString());
    }

    private static void WriteSqlServerContainerToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "sqlserver.server.v1");
    }

    private static void WriteSqlServerDatabaseComponentToManifest(Utf8JsonWriter json, SqlServerDatabaseComponent sqlServerDatabaseComponent)
    {
        json.WriteString("type", "sqlserver.database.v1");
        json.WriteString("parent", sqlServerDatabaseComponent.Parent.Name);
    }

    public static IDistributedApplicationComponentBuilder<T> WithSqlServer<T, TSqlServer>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<TSqlServer> sqlServerBuilder, string? connectionName = null) where TSqlServer : ISqlServerComponent
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithReference(sqlServerBuilder, connectionName);
    }

    public static IDistributedApplicationComponentBuilder<SqlServerDatabaseComponent> AddDatabase(this IDistributedApplicationComponentBuilder<SqlServerContainerComponent> builder, string name)
    {
        var sqlServerDatabase = new SqlServerDatabaseComponent(name, builder.Component);
        return builder.ApplicationBuilder.AddComponent(sqlServerDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WriteSqlServerDatabaseComponentToManifest(json, sqlServerDatabase)));
    }
}
