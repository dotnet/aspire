// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.MySql;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MySQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MySqlBuilderExtensions
{
    private const string PasswordEnvVarName = "MYSQL_ROOT_PASSWORD";

    /// <summary>
    /// Adds a MySQL server resource to the application model. For local development a container is used. This version the package defaults to the 8.3.0 tag of the mysql container image
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
                      .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: 3306)) // Internal port is always 3306.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mysql", Tag = "8.3.0" })
                      .WithEnvironment(context =>
                      {
                          if (context.ExecutionContext.IsPublishMode)
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, $"{{{resource.Name}.inputs.password}}");
                          }
                          else
                          {
                              context.EnvironmentVariables.Add(PasswordEnvVarName, resource.Password);
                          }
                      })
                      .PublishAsContainer();
    }

    /// <summary>
    /// Adds a MySQL database to the application model.
    /// </summary>
    /// <param name="builder">The MySQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<MySqlServerResource> builder, string name, string? databaseName = null)
    {
        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var mySqlDatabase = new MySqlDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(mySqlDatabase)
                                         .WithManifestPublishingCallback(mySqlDatabase.WriteToManifest);
    }

    /// <summary>
    /// Adds a phpMyAdmin administration and development platform for MySql to the application model. This version the package defaults to the 5.2 tag of the phpmyadmin container image
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
                                  .WithAnnotation(new ContainerImageAnnotation { Image = "phpmyadmin", Tag = "5.2" })
                                  .WithHttpEndpoint(containerPort: 80, hostPort: hostPort, name: containerName)
                                  .WithBindMount(Path.GetTempFileName(), "/etc/phpmyadmin/config.user.inc.php")
                                  .ExcludeFromManifest();
        
        return builder;
    }

    /// <summary>
    /// Changes resource to be published as a container.
    /// </summary>
    /// <param name="builder">The <see cref="MySqlServerResource"/> builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> PublishAsContainer(this IResourceBuilder<MySqlServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(builder.Resource.WriteToManifest);
    }
}
