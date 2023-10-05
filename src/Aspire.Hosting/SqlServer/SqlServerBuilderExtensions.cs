// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

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

    public static IDistributedApplicationComponentBuilder<SqlServerComponent> AddSqlServer(this IDistributedApplicationBuilder builder, string name, string connectionString)
    {
        return builder.AddComponent(new SqlServerComponent(name, connectionString))
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSqlServerComponentToManifest));
    }

    private static async Task WriteSqlServerComponentToManifest(Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "sqlserver.v1");
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static IDistributedApplicationComponentBuilder<T> WithSqlServer<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<SqlServerContainerComponent> sqlBuilder, string? databaseName, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        connectionName = connectionName ?? sqlBuilder.Component.Name;

        return builder.WithEnvironment((context) =>
        {
            var connectionStringName = $"{ConnectionStringEnvironmentName}{connectionName}";

            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{sqlBuilder.Component.Name}.connectionString}}";
                return;
            }

            var connectionString = sqlBuilder.Component.GetConnectionString(databaseName);
            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }
}
