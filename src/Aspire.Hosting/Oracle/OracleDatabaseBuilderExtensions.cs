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
    private const string PasswordEnvVarName = "ORACLE_PWD";

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
        password = password ?? Guid.NewGuid().ToString("N").Substring(0, 30);
        var oracleDatabaseContainer = new OracleDatabaseContainerResource(name, password);
        return builder.AddResource(oracleDatabaseContainer)
                      .WithManifestPublishingCallback(context => WriteOracleDatabaseContainerResourceToManifest(context, oracleDatabaseContainer))
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 1521))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "database/free", Tag = "latest", Registry = "container-registry.oracle.com" })
                      .WithEnvironment(context =>
                      {
                          if (context.PublisherName == "manifest")
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{oracleDatabaseContainer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, oracleDatabaseContainer.Password);
                          }
                      });
    }

    /// <summary>
    /// Adds a Oracle Database resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OracleDatabaseServerResource}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseServerResource> AddOracleDatabase(this IDistributedApplicationBuilder builder, string name)
    {
        var password = Guid.NewGuid().ToString("N").Substring(0, 30);
        var oracleDatabaseServer = new OracleDatabaseServerResource(name, password);
        return builder.AddResource(oracleDatabaseServer)
                      .WithManifestPublishingCallback(WriteOracleDatabaseContainerToManifest)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, containerPort: 1521))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "database/free", Tag = "latest", Registry = "container-registry.oracle.com" })
                      .WithEnvironment(PasswordEnvVarName, () => oracleDatabaseServer.Password);
    }

    /// <summary>
    /// Adds a Oracle Database database to the application model.
    /// </summary>
    /// <param name="builder">The Oracle Database server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OracleDatabaseResource}"/>.</returns>
    public static IResourceBuilder<OracleDatabaseResource> AddDatabase(this IResourceBuilder<IOracleDatabaseParentResource> builder, string name)
    {
        var oracleDatabase = new OracleDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(oracleDatabase)
                                         .WithManifestPublishingCallback(context => WriteOracleDatabaseToManifest(context, oracleDatabase));
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

    private static void WriteOracleDatabaseContainerResourceToManifest(ManifestPublishingContext context, OracleDatabaseContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"user id=system;password={{{resource.Name}.inputs.password}};data source={{{resource.Name}.bindings.tcp.host}}:{{{resource.Name}.bindings.tcp.port}};");
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
}
