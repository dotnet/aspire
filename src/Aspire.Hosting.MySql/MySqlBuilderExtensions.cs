// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// <param name="password">The parameter used to provide the root password for the MySQL resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port for MySQL.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> AddMySql(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<ParameterResource>? password = null, int? port = null)
    {
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new MySqlServerResource(name, passwordParameter);
        return builder.AddResource(resource)
                      .WithEndpoint(port: port, targetPort: 3306, name: MySqlServerResource.PrimaryEndpointName) // Internal port is always 3306.
                      .WithImage(MySqlContainerImageTags.Image, MySqlContainerImageTags.Tag)
                      .WithImageRegistry(MySqlContainerImageTags.Registry)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[PasswordEnvVarName] = resource.PasswordParameter;
                      });
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
        return builder.ApplicationBuilder.AddResource(mySqlDatabase);
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
                                  .WithImage("phpmyadmin", "5.2")
                                  .WithImageRegistry(MySqlContainerImageTags.Registry)
                                  .WithHttpEndpoint(targetPort: 80, port: hostPort, name: containerName)
                                  .WithBindMount(Path.GetTempFileName(), "/etc/phpmyadmin/config.user.inc.php")
                                  .ExcludeFromManifest();

        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a MySql container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> WithDataVolume(this IResourceBuilder<MySqlServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/mysql", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a MySql container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> WithDataBindMount(this IResourceBuilder<MySqlServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/lib/mysql", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the init folder to a MySql container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> WithInitBindMount(this IResourceBuilder<MySqlServerResource> builder, string source, bool isReadOnly = true)
        => builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
}
