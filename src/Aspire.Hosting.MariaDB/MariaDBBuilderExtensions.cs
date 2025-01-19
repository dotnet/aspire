// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MariaDB;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MariaDb resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MariaDBBuilderExtensions
{
    private const string PasswordEnvVarName = "MARIADB_ROOT_PASSWORD";

    /// <summary>
    /// Adds a MariaDB server resource to the application model. For local development a container is used.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="MariaDBContainerImageTags.Tag"/> tag of the <inheritdoc cref="MariaDBContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="password">The parameter used to provide the root password for the MariaDb resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port for MariaDb.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MariaDBServerResource> AddMariaDB(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<ParameterResource>? password = null, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new MariaDBServerResource(name, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(resource, async (@event, ct) =>
        {
            connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{resource.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddMySql(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(resource)
                      .WithEndpoint(port: port, targetPort: 3306, name: MariaDBServerResource.PrimaryEndpointName) // Internal port is always 3306.
                      .WithImage(MariaDBContainerImageTags.Image, MariaDBContainerImageTags.Tag)
                      .WithImageRegistry(MariaDBContainerImageTags.Registry)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[PasswordEnvVarName] = resource.PasswordParameter;
                      })
                      .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a MariaDB database to the application model.
    /// </summary>
    /// <param name="builder">The MariaDb server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MariaDBDatabaseResource> AddDatabase(this IResourceBuilder<MariaDBServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var mariaDbDatabase = new MariaDBDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(mariaDbDatabase);
    }

    /// <summary>
    /// Adds a phpMyAdmin administration and development platform for MariaDB to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="MariaDBContainerImageTags.PhpMyAdminTag"/> tag of the <inheritdoc cref="MariaDBContainerImageTags.PhpMyAdminImage"/> container image.
    /// </remarks>
    /// <param name="builder">The MariaDB server resource builder.</param>
    /// <param name="configureContainer">Callback to configure PhpMyAdmin container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPhpMyAdmin<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<PhpMyAdminContainerResource>>? configureContainer = null, string? containerName = null) where T : MariaDBServerResource
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
                                                .WithImage(MariaDBContainerImageTags.PhpMyAdminImage, MariaDBContainerImageTags.PhpMyAdminTag)
                                                .WithImageRegistry(MariaDBContainerImageTags.Registry)
                                                .WithHttpEndpoint(targetPort: 80, name: "http")
                                                .WithBindMount(configurationTempFileName, "/etc/phpmyadmin/config.user.inc.php")
                                                .ExcludeFromManifest();

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            var mariaDBInstances = builder.ApplicationBuilder.Resources.OfType<MariaDBServerResource>();

            if (!mariaDBInstances.Any())
            {
                // No-op if there are no MariaDb resources present.
                return Task.CompletedTask;
            }

            if (mariaDBInstances.Count() == 1)
            {
                var singleInstance = mariaDBInstances.Single();
                if (singleInstance.PrimaryEndpoint.IsAllocated)
                {
                    var endpoint = singleInstance.PrimaryEndpoint;
                    phpMyAdminContainerBuilder.WithEnvironment(context =>
                    {
                        // PhpMyAdmin assumes MariaDb is being accessed over a default Aspire container network and hardcodes the resource address
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
                foreach (var mariaDBInstance in mariaDBInstances)
                {
                    if (mariaDBInstance.PrimaryEndpoint.IsAllocated)
                    {
                        var endpoint = mariaDBInstance.PrimaryEndpoint;
                        writer.WriteLine("$i++;");
                        // PhpMyAdmin assumes MariaDb is being accessed over a default Aspire container network and hardcodes the resource address
                        // This will need to be refactored once updated service discovery APIs are available
                        writer.WriteLine($"$cfg['Servers'][$i]['host'] = '{endpoint.Resource.Name}:{endpoint.TargetPort}';");
                        writer.WriteLine($"$cfg['Servers'][$i]['verbose'] = '{mariaDBInstance.Name}';");
                        writer.WriteLine($"$cfg['Servers'][$i]['auth_type'] = 'cookie';");
                        writer.WriteLine($"$cfg['Servers'][$i]['user'] = 'root';");
                        writer.WriteLine($"$cfg['Servers'][$i]['password'] = '{mariaDBInstance.PasswordParameter.Value}';");
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
    /// Adds a named volume for the data folder to a MariaDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MariaDBServerResource> WithDataVolume(this IResourceBuilder<MariaDBServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/var/lib/mariadb", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a MariaDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MariaDBServerResource> WithDataBindMount(this IResourceBuilder<MariaDBServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/var/lib/mariadb", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to a MariaDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MariaDBServerResource> WithInitBindMount(this IResourceBuilder<MariaDBServerResource> builder, string source, bool isReadOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }
}
