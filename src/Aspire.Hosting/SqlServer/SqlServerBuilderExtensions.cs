// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.SqlServer;

public static class SqlServerBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<SqlServerContainerComponent> AddSqlServerContainer(this IDistributedApplicationBuilder builder, string name, string? password = null, int? port = null)
    {
        var sqlServer = new SqlServerContainerComponent(name);

        var componentBuilder = builder.AddComponent(sqlServer);
        componentBuilder.WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSqlServerComponentToManifest));
        componentBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433));
        componentBuilder.WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" });
        componentBuilder.WithEnvironment("ACCEPT_EULA", "Y");
        componentBuilder.WithEnvironment("MSSQL_SA_PASSWORD", sqlServer.GeneratedPassword);
        return componentBuilder;
    }

    public static IDistributedApplicationComponentBuilder<SqlServerComponent> AddSqlServer(this IDistributedApplicationBuilder builder, string name, string? connectionString)
    {
        var sqlServer = new SqlServerComponent(name, connectionString ?? builder.Configuration.GetConnectionString(name));

        return builder.AddComponent(sqlServer)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                WriteSqlServerComponentToManifest(jsonWriter, sqlServer.GetConnectionString())));
    }

    private static void WriteSqlServerComponentToManifest(Utf8JsonWriter jsonWriter) =>
        WriteSqlServerComponentToManifest(jsonWriter, null);

    private static void WriteSqlServerComponentToManifest(Utf8JsonWriter jsonWriter, string? connectionString)
    {
        jsonWriter.WriteString("type", "sqlserver.v1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }

    public static IDistributedApplicationComponentBuilder<T> WithSqlServer<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<ISqlServerComponent> sqlBuilder, string? databaseName, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        var sql = sqlBuilder.Component;
        connectionName = connectionName ?? sqlBuilder.Component.Name;

        return builder.WithEnvironment((context) =>
        {
            var connectionStringName = $"{ConnectionStringEnvironmentName}{connectionName}";

            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{sql.Name}.connectionString}}";
                return;
            }

            var connectionString = sql.GetConnectionString(databaseName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new DistributedApplicationException($"A connection string for SqlServer '{sql.Name}' could not be retrieved.");
            }
            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }
}
