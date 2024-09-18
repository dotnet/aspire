// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MySql;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new MySqlServerResource(name, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(resource, async (@event, ct) =>
        {
            connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{resource.Name}' resource but the connection string was null.");
            }

            var lookup = builder.Resources.OfType<MySqlDatabaseResource>().ToDictionary(d => d.Name);

            foreach (var databaseName in resource.Databases)
            {
                if (!lookup.TryGetValue(databaseName.Key, out var databaseResource))
                {
                    throw new DistributedApplicationException($"Database resource '{databaseName}' under MySql resource '{resource.Name}' was not found in the model.");
                }

                var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(databaseResource, @event.Services);
                await builder.Eventing.PublishAsync<ConnectionStringAvailableEvent>(connectionStringAvailableEvent, ct).ConfigureAwait(false);

                var beforeResourceStartedEvent = new BeforeResourceStartedEvent(databaseResource, @event.Services);
                await builder.Eventing.PublishAsync(beforeResourceStartedEvent, ct).ConfigureAwait(false);
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddMySql(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(resource)
                      .WithEndpoint(port: port, targetPort: 3306, name: MySqlServerResource.PrimaryEndpointName) // Internal port is always 3306.
                      .WithImage(MySqlContainerImageTags.Image, MySqlContainerImageTags.Tag)
                      .WithImageRegistry(MySqlContainerImageTags.Registry)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[PasswordEnvVarName] = resource.PasswordParameter;
                      })
                      .WithHealthCheck(healthCheckKey);
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var mySqlDatabase = new MySqlDatabaseResource(name, databaseName, builder.Resource);

        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(mySqlDatabase, async (@event, ct) =>
        {
            connectionString = await mySqlDatabase.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{mySqlDatabase.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddMySql(sp => connectionString!, name: healthCheckKey);

        return builder.ApplicationBuilder.AddResource(mySqlDatabase)
                                         .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a phpMyAdmin administration and development platform for MySql to the application model. This version the package defaults to the 5.2 tag of the phpmyadmin container image
    /// </summary>
    /// <param name="builder">The MySql server resource builder.</param>
    /// <param name="configureContainer">Callback to configure PhpMyAdmin container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPhpMyAdmin<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<PhpMyAdminContainerResource>>? configureContainer = null, string? containerName = null) where T : MySqlServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PhpMyAdminContainerResource>().Any())
        {
            return builder;
        }

        containerName ??= $"{builder.Resource.Name}-phpmyadmin";

        var configurationTempFileName = Path.GetTempFileName();

        var phpMyAdminContainer = new PhpMyAdminContainerResource(containerName);
        var phpMyAdminContainerBuilder = builder.ApplicationBuilder.AddResource(phpMyAdminContainer)
                                                .WithImage(MySqlContainerImageTags.PhpMyAdminImage, MySqlContainerImageTags.PhpMyAdminTag)
                                                .WithImageRegistry(MySqlContainerImageTags.Registry)
                                                .WithHttpEndpoint(targetPort: 80, name: "http")
                                                .WithBindMount(configurationTempFileName, "/etc/phpmyadmin/config.user.inc.php")
                                                .ExcludeFromManifest();

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            var mySqlInstances = builder.ApplicationBuilder.Resources.OfType<MySqlServerResource>();

            if (!mySqlInstances.Any())
            {
                // No-op if there are no MySql resources present.
                return Task.CompletedTask;
            }

            if (mySqlInstances.Count() == 1)
            {
                var singleInstance = mySqlInstances.Single();
                if (singleInstance.PrimaryEndpoint.IsAllocated)
                {
                    var endpoint = singleInstance.PrimaryEndpoint;
                    phpMyAdminContainerBuilder.WithEnvironment(context =>
                    {
                        // PhpMyAdmin assumes MySql is being accessed over a default Aspire container network and hardcodes the resource address
                        // This will need to be refactored once updated service discovery APIs are available
                        context.EnvironmentVariables.Add("PMA_HOST", $"{endpoint.Resource.Name}:{endpoint.TargetPort}");
                        context.EnvironmentVariables.Add("PMA_USER", "root");
                        context.EnvironmentVariables.Add("PMA_PASSWORD", singleInstance.PasswordParameter.Value);
                    });
                }
            }
            else
            {
                using var stream = new FileStream(configurationTempFileName, FileMode.Create);
                using var writer = new StreamWriter(stream);

                writer.WriteLine("<?php");
                writer.WriteLine();
                writer.WriteLine("$i = 0;");
                writer.WriteLine();
                foreach (var mySqlInstance in mySqlInstances)
                {
                    if (mySqlInstance.PrimaryEndpoint.IsAllocated)
                    {
                        var endpoint = mySqlInstance.PrimaryEndpoint;
                        writer.WriteLine("$i++;");
                        // PhpMyAdmin assumes MySql is being accessed over a default Aspire container network and hardcodes the resource address
                        // This will need to be refactored once updated service discovery APIs are available
                        writer.WriteLine($"$cfg['Servers'][$i]['host'] = '{endpoint.Resource.Name}:{endpoint.TargetPort}';");
                        writer.WriteLine($"$cfg['Servers'][$i]['verbose'] = '{mySqlInstance.Name}';");
                        writer.WriteLine($"$cfg['Servers'][$i]['auth_type'] = 'cookie';");
                        writer.WriteLine($"$cfg['Servers'][$i]['user'] = 'root';");
                        writer.WriteLine($"$cfg['Servers'][$i]['password'] = '{mySqlInstance.PasswordParameter.Value}';");
                        writer.WriteLine($"$cfg['Servers'][$i]['AllowNoPassword'] = true;");
                        writer.WriteLine();
                    }
                }
                writer.WriteLine("$cfg['DefaultServer'] = 1;");
                writer.WriteLine("?>");
            }

            return Task.CompletedTask;
        });

        configureContainer?.Invoke(phpMyAdminContainerBuilder);

        return builder;
    }

    /// <summary>
    /// Configures the host port that the PGAdmin resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for PGAdmin.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used, a random port will be assigned.</param>
    /// <returns>The resource builder for PGAdmin.</returns>
    public static IResourceBuilder<PhpMyAdminContainerResource> WithHostPort(this IResourceBuilder<PhpMyAdminContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a named volume for the data folder to a MySql container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> WithDataVolume(this IResourceBuilder<MySqlServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/mysql", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a MySql container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> WithDataBindMount(this IResourceBuilder<MySqlServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/var/lib/mysql", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to a MySql container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MySqlServerResource> WithInitBindMount(this IResourceBuilder<MySqlServerResource> builder, string source, bool isReadOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }
}
