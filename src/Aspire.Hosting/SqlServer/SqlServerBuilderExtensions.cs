// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

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
        password = password ?? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").ToUpper();
        var sqlServer = new SqlServerContainerResource(name, password);

        return builder.AddResource(sqlServer)
                      .WithManifestPublishingCallback(context => WriteSqlServerContainerToManifest(context, sqlServer))
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433))
                      .WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" })
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment(context =>
                      {
                          if (context.PublisherName == "manifest")
                          {
                              context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", $"{{{sqlServer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", sqlServer.Password);
                          }
                      });

    }

    /// <summary>
    /// Adds a SQL Server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{SqlServerContainerResource}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> AddSqlServer(this IDistributedApplicationBuilder builder, string name)
    {
        var password = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").ToUpper();
        var sqlServer = new SqlServerServerResource(name, password);

        return builder.AddResource(sqlServer)
                      .WithManifestPublishingCallback(WriteSqlServerToManifest)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, containerPort: 1433))
                      .WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" })
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment("MSSQL_SA_PASSWORD", sqlServer.Password);
    }

    private static void WriteSqlServerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "sqlserver.server.v1");
    }

    private static void WriteSqlServerContainerToManifest(ManifestPublishingContext context, SqlServerContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"Server={{{resource.Name}.bindings.tcp.host}},{{{resource.Name}.bindings.tcp.port}};User ID=sa;Password={{{resource.Name}.inputs.password}};TrustServerCertificate=true;");
        context.Writer.WriteStartObject("inputs");      // "inputs": {
        context.Writer.WriteStartObject("password");    //   "password": {
        context.Writer.WriteString("type", "string");   //     "type": "string",
        context.Writer.WriteBoolean("secret", true);    //     "secret": true,
        context.Writer.WriteStartObject("default");     //     "default": {
        context.Writer.WriteStartObject("generate");    //       "generate": {
        context.Writer.WriteNumber("minLength", 10);    //         "minLength": 10,
        context.Writer.WriteEndObject();                //       }
        context.Writer.WriteEndObject();                //     }
        context.Writer.WriteEndObject();                //   }
        context.Writer.WriteEndObject();                // }
    }

    private static void WriteSqlServerDatabaseToManifest(ManifestPublishingContext context, SqlServerDatabaseResource sqlServerDatabase)
    {
        context.Writer.WriteString("type", "sqlserver.database.v1");
        context.Writer.WriteString("parent", sqlServerDatabase.Parent.Name);
    }

    /// <summary>
    /// Adds a SQL Server database to the application model. This is a child resource of a <see cref="SqlServerContainerResource"/>.
    /// </summary>
    /// <param name="builder">The SQL Server resource builders.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{SqlServerDatabaseResource}"/>.</returns>
    public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IResourceBuilder<ISqlServerParentResource> builder, string name)
    {
        var sqlServerDatabase = new SqlServerDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(sqlServerDatabase)
                                         .WithManifestPublishingCallback(context => WriteSqlServerDatabaseToManifest(context, sqlServerDatabase));
    }
}
