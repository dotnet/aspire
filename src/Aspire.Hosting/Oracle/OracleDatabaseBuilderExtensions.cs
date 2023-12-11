// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Oracle Database resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class OracleDatabaseBuilderExtensions
{
    private const string PasswordEnvVarName = "ORACLE_DATABASE_PASSWORD";

    /// <summary>
    /// Adds a Oracle Database container to the application model. The default image is "database/free" and the tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for Oracle Database.</param>
    /// <param name="password">The password for the Oracle Database container. Defaults to a random password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OracleDatabaseContainerResource}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseContainerResource> AddOracleDatabaseContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var oracleDatabaseContainer = new OracleDatabaseContainerResource(name, password);
        return builder.AddResource(oracleDatabaseContainer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteOracleDatabaseContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 1521))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "database/free", Tag = "latest", Registry = "container-registry.oracle.com" })
                      .WithEnvironment(PasswordEnvVarName, () => oracleDatabaseContainer.Password);
    }

    /// <summary>
    /// Adds a Oracle Database connection to the application model. Connection strings can also be read from the connection string section of the configuration using the name of the resource.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The Oracle Database connection string (optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OracleDatabaseConnectionResource}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseConnectionResource> AddOracleDatabaseConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var oracleDatabaseConnection = new OracleDatabaseConnectionResource(name, connectionString);

        return builder.AddResource(oracleDatabaseConnection)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation((context) => WriteOracleDatabaseConnectionToManifest(context, oracleDatabaseConnection)));
    }

    /// <summary>
    /// Adds a Oracle Database database to the application model.
    /// </summary>
    /// <param name="builder">The Oracle Database server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OracleDatabaseResource}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseResource> AddDatabase(this IResourceBuilder<OracleDatabaseContainerResource> builder, string name)
    {
        var oracleDatabase = new OracleDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(oracleDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WriteOracleDatabaseToManifest(json, oracleDatabase)));
    }

    private static void WriteOracleDatabaseConnectionToManifest(ManifestPublishingContext context, OracleDatabaseConnectionResource oracleDatabaseConnection)
    {
        context.Writer.WriteString("type", "oracle.connection.v0");
        context.Writer.WriteString("connectionString", oracleDatabaseConnection.GetConnectionString());
    }

    private static void WriteOracleDatabaseContainerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "oracle.server.v0");
    }

    private static void WriteOracleDatabaseToManifest(ManifestPublishingContext context, OracleDatabaseResource oracleDatabase)
    {
        context.Writer.WriteString("type", "oracle.database.v0");
        context.Writer.WriteString("parent", oracleDatabase.Parent.Name);
    }
}
