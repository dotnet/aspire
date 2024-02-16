// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.MySql;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MySQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MySqlBuilderExtensions
{
    private const string PasswordEnvVarName = "MYSQL_ROOT_PASSWORD";

    /// <summary>
    /// Adds a MySQL server resource to the application model. For local development a container is used.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MySQL.</param>
    /// <param name="password">The password for the MySQL root user. Defaults to a random password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> AddMySql(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password ??= PasswordGenerator.GeneratePassword(6, 6, 2, 2);

        var resource = new MySqlServerResource(name, password);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(WriteMySqlContainerToManifest)
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 3306)) // Internal port is always 3306.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mysql", Tag = "latest" })
                      .WithEnvironment(context =>
                      {
                          if (context.ExecutionContext.Operation == DistributedApplicationOperation.Publish)
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{resource.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, resource.Password);
                          }
                      });
    }

    /// <summary>
    /// Adds a MySQL database to the application model.
    /// </summary>
    /// <param name="builder">The MySQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<MySqlServerResource> builder, string name)
    {
        var mySqlDatabase = new MySqlDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(mySqlDatabase)
                                         .WithManifestPublishingCallback(context => WriteMySqlDatabaseToManifest(context, mySqlDatabase));
    }

    /// <summary>
    /// Adds a phpMyAdmin administration and development platform for MySql to the application model.
    /// </summary>
    /// <param name="builder">The MySql server resource builder.</param>
    /// <param name="hostPort">The host port for the application ui.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPhpMyAdmin<T>(this IResourceBuilder<T> builder, int? hostPort = null, string? containerName = null) where T : MySqlServerResource
    {
        if (builder.ApplicationBuilder.Resources.OfType<PhpMyAdminContainerResource>().Any())
        {
            return builder;
        }

        builder.ApplicationBuilder.Services.TryAddLifecycleHook<PhpMyAdminConfigWriterHook>();

        containerName ??= $"{builder.Resource.Name}-phpmyadmin";

        var phpMyAdminContainer = new PhpMyAdminContainerResource(containerName);
        builder.ApplicationBuilder.AddResource(phpMyAdminContainer)
                                  .WithAnnotation(new ContainerImageAnnotation { Image = "phpmyadmin", Tag = "latest" })
                                  .WithHttpEndpoint(containerPort: 80, hostPort: hostPort, name: containerName)
                                  .WithVolumeMount(Path.GetTempFileName(), "/etc/phpmyadmin/config.user.inc.php")
                                  .ExcludeFromManifest();
        
        return builder;
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

    /// <summary>
    /// Changes resource to be published as a container.
    /// </summary>
    /// <param name="builder">The <see cref="MySqlServerResource"/> builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> PublishAsContainer(this IResourceBuilder<MySqlServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(context => WriteMySqlContainerResourceToManifest(context, builder.Resource));
    }

    private static void WriteMySqlContainerResourceToManifest(ManifestPublishingContext context, MySqlServerResource resource)
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
