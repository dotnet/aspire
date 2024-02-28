// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

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
        // The password must be at least 8 characters long and contain characters from three of the following four sets: Uppercase letters, Lowercase letters, Base 10 digits, and Symbols
        password ??= PasswordGenerator.GeneratePassword(6, 6, 2, 2);

        var sqlServer = new SqlServerServerResource(name, password);

        return builder.AddResource(sqlServer)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 1433))
                      .WithAnnotation(new ContainerImageAnnotation { Registry = "mcr.microsoft.com", Image = "mssql/server", Tag = "2022-latest" })
                      .WithEnvironment("ACCEPT_EULA", "Y")
                      .WithEnvironment(context =>
                      {
                          if (context.ExecutionContext.IsPublishMode)
                          {
                              context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", $"{{{sqlServer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", sqlServer.Password);
                          }
                      })
                      .PublishAsContainer();
    }

    /// <summary>
    /// Changes the SQL Server resource to be published as a container.
    /// </summary>
    /// <param name="builder">Builder for the underlying <see cref="SqlServerServerResource"/>.</param>
    /// <returns></returns>
    public static IResourceBuilder<SqlServerServerResource> PublishAsContainer(this IResourceBuilder<SqlServerServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(builder.Resource.WriteToManifest);
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
}
