// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Oracle Database resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class OracleDatabaseBuilderExtensions
{
    private const string PasswordEnvVarName = "ORACLE_PWD";

    /// <summary>
    /// Adds a Oracle Database resource to the application model. A container is used for local development. This version the package defaults to the 23.3.0.0 tag of the container-registry.oracle.com/database/free container image
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for Oracle Database.</param>
    /// <param name="password">The password for the Oracle Database container. Defaults to a random password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> AddOracleDatabase(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password ??= PasswordGenerator.GeneratePassword(6, 6, 2, 2);

        var oracleDatabaseServer = new OracleDatabaseServerResource(name, password);
        return builder.AddResource(oracleDatabaseServer)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 1521))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "database/free", Tag = "23.3.0.0", Registry = "container-registry.oracle.com" })
                      .WithEnvironment(context =>
                      {
                          if (context.ExecutionContext.IsPublishMode)
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{oracleDatabaseServer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, oracleDatabaseServer.Password);
                          }
                      })
                      .PublishAsContainer();
    }

    /// <summary>
    /// Adds a Oracle Database database to the application model.
    /// </summary>
    /// <param name="builder">The Oracle Database server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseResource> AddDatabase(this IResourceBuilder<OracleDatabaseServerResource> builder, string name, string? databaseName = null)
    {
        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var oracleDatabase = new OracleDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(oracleDatabase)
                                         .WithManifestPublishingCallback(oracleDatabase.WriteToManifest);
    }

    /// <summary>
    /// Changes the Oracle Database Server resource to be published as a container.
    /// </summary>
    /// <param name="builder">Builder for the underlying <see cref="OracleDatabaseServerResource"/>.</param>
    /// <returns></returns>
    public static IResourceBuilder<OracleDatabaseServerResource> PublishAsContainer(this IResourceBuilder<OracleDatabaseServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(builder.Resource.WriteToManifest);
    }
}
