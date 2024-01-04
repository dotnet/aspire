// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MySQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MySqlBuilderExtensions
{
    private const string PasswordEnvVarName = "MYSQL_ROOT_PASSWORD";

    /// <summary>
    /// Adds a MySQL container to the application model. The default image is "mysql" and the tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MySQL.</param>
    /// <param name="password">The password for the MySQL root user. Defaults to a random password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MySqlContainerResource}"/>.</returns>
    public static IResourceBuilder<MySqlContainerResource> AddMySqlContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password ??= Guid.NewGuid().ToString("N");
        var mySqlContainer = new MySqlContainerResource(name, password);
        return builder.AddResource(mySqlContainer)
                      .WithManifestPublishingCallback(context => WriteMySqlContainerResourceToManifest(context, mySqlContainer))
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 3306)) // Internal port is always 3306.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mysql", Tag = "latest" })
                      .WithEnvironment(context =>
                      {
                          if (context.PublisherName == "manifest")
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{mySqlContainer.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, mySqlContainer.Password);
                          }
                      });
    }

    /// <summary>
    /// Adds a MySQL server resource to the application model. For local development a container is used.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MySqlContainerResource}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> AddMySql(this IDistributedApplicationBuilder builder, string name)
    {
        var password = Guid.NewGuid().ToString("N");
        var mySqlContainer = new MySqlServerResource(name, password);
        return builder.AddResource(mySqlContainer)
                      .WithManifestPublishingCallback(WriteMySqlContainerToManifest)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, containerPort: 3306)) // Internal port is always 3306.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mysql", Tag = "latest" })
                      .WithEnvironment(PasswordEnvVarName, () => mySqlContainer.Password);
    }

    /// <summary>
    /// Adds a MySQL database to the application model.
    /// </summary>
    /// <param name="builder">The MySQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MySqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<IMySqlParentResource> builder, string name)
    {
        var mySqlDatabase = new MySqlDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(mySqlDatabase)
                                         .WithManifestPublishingCallback(context => WriteMySqlDatabaseToManifest(context, mySqlDatabase));
    }

    private static void WriteMySqlContainerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "mysql.server.v0");
    }

    private static void WriteMySqlDatabaseToManifest(ManifestPublishingContext context, MySqlDatabaseResource mySqlDatabase)
    {
        context.Writer.WriteString("type", "mysql.database.v0");
        context.Writer.WriteString("parent", mySqlDatabase.Parent.Name);
    }

    private static void WriteMySqlContainerResourceToManifest(ManifestPublishingContext context, MySqlContainerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"Server={{{resource.Name}.bindings.tcp.host}};Port={{{resource.Name}.bindings.tcp.port}};User ID=root;Password={{{resource.Name}.inputs.password}}");
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
