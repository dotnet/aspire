// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public static class SqlServerCloudApplicationBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IDistributedApplicationComponentBuilder<SqlServerContainerComponent> AddSqlServerContainer(this IDistributedApplicationBuilder builder, string name, string? password = null, int? port = null)
    {
        var sqlServer = new SqlServerContainerComponent();

        var componentBuilder = builder.AddComponent(name, sqlServer);
        componentBuilder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433));
        componentBuilder.WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" });
        componentBuilder.WithEnvironment("ACCEPT_EULA", "Y");
        componentBuilder.WithEnvironment("MSSQL_SA_PASSWORD", sqlServer.GeneratedPassword);
        return componentBuilder;
    }

    public static IDistributedApplicationComponentBuilder<T> WithSqlServer<T>(this IDistributedApplicationComponentBuilder<T> projectBuilder, IDistributedApplicationComponentBuilder<SqlServerContainerComponent> sqlBuilder, string? databaseName, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        if (string.IsNullOrEmpty(connectionName))
        {
            DistributedApplicationComponentExtensions.TryGetName(sqlBuilder.Component, out connectionName);

            if (connectionName is null)
            {
                throw new DistributedApplicationException("SqlServer connection name could not be determined. Please provide one.");
            }
        }

        return projectBuilder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, () =>
        {
            if (!sqlBuilder.Component.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
            {
                throw new DistributedApplicationException("Sql component does not have endpoint annotation.");
            }

            var endpoint = allocatedEndpoints.Single();

            // HACK: Use  the 127.0.0.1 address because localhost is resolving to [::1] following
            //       up with DCP on this issue.
            return $"Server=127.0.0.1,{endpoint.Port};Database={databaseName ?? "master"};User ID=sa;Password={sqlBuilder.Component.GeneratedPassword};TrustServerCertificate=true;";
        });
    }

    public static IDistributedApplicationComponentBuilder<T> WithSqlServer<T>(this IDistributedApplicationComponentBuilder<T> projectBuilder, string connectionName, string connectionString)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return projectBuilder.WithEnvironment(ConnectionStringEnvironmentName + connectionName, connectionString);
    }
}
