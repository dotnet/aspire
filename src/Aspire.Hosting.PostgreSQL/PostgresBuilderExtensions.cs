// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding PostgreSQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PostgresBuilderExtensions
{
    private const string UserEnvVarName = "POSTGRES_USER";
    private const string PasswordEnvVarName = "POSTGRES_PASSWORD";

    /// <summary>
    /// Adds a PostgreSQL resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="userName">The parameter used to provide the user name for the PostgreSQL resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the administrator password for the PostgreSQL resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port used when launching the container. If null a random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the Postgres resource is able to service
    /// requests.
    /// </para>
    /// This version of the package defaults to the <inheritdoc cref="PostgresContainerImageTags.Tag"/> tag of the <inheritdoc cref="PostgresContainerImageTags.Image"/> container image.
    /// </remarks>
    public static IResourceBuilder<PostgresServerResource> AddPostgres(this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var postgresServer = new PostgresServerResource(name, userName?.Resource, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(postgresServer, async (@event, ct) =>
        {
            connectionString = await postgresServer.GetConnectionStringAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{postgresServer.Name}' resource but the connection string was null.");
            }
        });

        builder.Eventing.Subscribe<ResourceReadyEvent>(postgresServer, async (@event, ct) =>
        {
            if (connectionString is null)
            {
                throw new DistributedApplicationException($"ResourceReadyEvent was published for the '{postgresServer.Name}' resource but the connection string was null.");
            }

            // Non-database scoped connection string
            using var npgsqlConnection = new NpgsqlConnection(connectionString + ";Database=postgres;");

            await npgsqlConnection.OpenAsync(ct).ConfigureAwait(false);

            if (npgsqlConnection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException($"Could not open connection to '{postgresServer.Name}'");
            }

            foreach (var name in postgresServer.Databases.Keys)
            {
                if (builder.Resources.FirstOrDefault(n => string.Equals(n.Name, name, StringComparisons.ResourceName)) is PostgresDatabaseResource postgreDatabase)
                {
                    await CreateDatabaseAsync(npgsqlConnection, postgreDatabase, @event.Services, ct).ConfigureAwait(false);
                }
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddNpgSql(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey, configure: (connection) =>
        {
            // HACK: The Npgsql client defaults to using the username in the connection string if the database is not specified. Here
            //       we override this default behavior because we are working with a non-database scoped connection string. The Aspirified
            //       package doesn't have to deal with this because it uses a datasource from DI which doesn't have this issue:
            //
            //       https://github.com/npgsql/npgsql/blob/c3b31c393de66a4b03fba0d45708d46a2acb06d2/src/Npgsql/NpgsqlConnection.cs#L445
            //
            connection.ConnectionString += ";Database=postgres;";
        });

        return builder.AddResource(postgresServer)
                      .WithEndpoint(port: port, targetPort: 5432, name: PostgresServerResource.PrimaryEndpointName) // Internal port is always 5432.
                      .WithImage(PostgresContainerImageTags.Image, PostgresContainerImageTags.Tag)
                      .WithImageRegistry(PostgresContainerImageTags.Registry)
                      .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256")
                      .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=scram-sha-256 --auth-local=scram-sha-256")
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[UserEnvVarName] = postgresServer.UserNameReference;
                          context.EnvironmentVariables[PasswordEnvVarName] = postgresServer.PasswordParameter;
                      })
                      .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a PostgreSQL database to the application model.
    /// </summary>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the Postgres database is available.
    /// </para>
    /// <para>
    /// Note that by default calling <see cref="AddDatabase(IResourceBuilder{PostgresServerResource}, string, string?)"/>
    /// does not result in the database being created on the Postgres server. It is expected that code within your solution
    /// will create the database. As a result if <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// is used with this resource it will wait indefinitely until the database exists.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IResourceBuilder<PostgresServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        var postgresDatabase = new PostgresDatabaseResource(name, databaseName, builder.Resource);

        builder.Resource.AddDatabase(postgresDatabase.Name, postgresDatabase.DatabaseName);

        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(postgresDatabase, async (@event, ct) =>
        {
            connectionString = await postgresDatabase.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddNpgSql(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.ApplicationBuilder
            .AddResource(postgresDatabase)
            .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a pgAdmin 4 administration and development platform for PostgreSQL to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="PostgresContainerImageTags.PgAdminTag"/> tag of the <inheritdoc cref="PostgresContainerImageTags.PgAdminImage"/> container image.
    /// </remarks>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="configureContainer">Callback to configure PgAdmin container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPgAdmin<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<PgAdminContainerResource>>? configureContainer = null, string? containerName = null)
        where T : PostgresServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PgAdminContainerResource>().SingleOrDefault() is { } existingPgAdminResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingPgAdminResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            containerName ??= $"{builder.Resource.Name}-pgadmin";

            var pgAdminContainer = new PgAdminContainerResource(containerName);
            var pgAdminContainerBuilder = builder.ApplicationBuilder.AddResource(pgAdminContainer)
                                                 .WithImage(PostgresContainerImageTags.PgAdminImage, PostgresContainerImageTags.PgAdminTag)
                                                 .WithImageRegistry(PostgresContainerImageTags.PgAdminRegistry)
                                                 .WithHttpEndpoint(targetPort: 80, name: "http")
                                                 .WithEnvironment(SetPgAdminEnvironmentVariables)
                                                 .WithHttpHealthCheck("/browser")
                                                 .ExcludeFromManifest();

            pgAdminContainerBuilder.WithContainerFiles(
                destinationPath: "/pgadmin4",
                callback: (context, _) =>
                {
                    var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                    var postgresInstances = builder.ApplicationBuilder.Resources.OfType<PostgresServerResource>();

                    return Task.FromResult<IEnumerable<ContainerFileSystemItem>>([
                        new ContainerFile
                        {
                            Name = "servers.json",
                            Contents = WritePgAdminServerJson(postgresInstances),
                        },
                    ]);
                });

            configureContainer?.Invoke(pgAdminContainerBuilder);

            pgAdminContainerBuilder.WithRelationship(builder.Resource, "PgAdmin");

            return builder;
        }
    }

    /// <summary>
    /// Configures the host port that the PGAdmin resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for PGAdmin.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for PGAdmin.</returns>
    public static IResourceBuilder<PgAdminContainerResource> WithHostPort(this IResourceBuilder<PgAdminContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Configures the host port that the pgweb resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for pgweb.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for pgweb.</returns>
    public static IResourceBuilder<PgWebContainerResource> WithHostPort(this IResourceBuilder<PgWebContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds an administration and development platform for PostgreSQL to the application model using pgweb.
    /// This version of the package defaults to the <inheritdoc cref="PostgresContainerImageTags.PgWebTag"/> tag of the <inheritdoc cref="PostgresContainerImageTags.PgWebImage"/> container image.
    /// </summary>
    /// <param name="builder">The Postgres server resource builder.</param>
    /// <param name="configureContainer">Configuration callback for pgweb container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <remarks>
    /// <example>
    /// Use in application host with a Postgres resource
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var postgres = builder.AddPostgres("postgres")
    ///    .WithPgWeb();
    /// var db = postgres.AddDatabase("db");
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(db);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PostgresServerResource> WithPgWeb(this IResourceBuilder<PostgresServerResource> builder, Action<IResourceBuilder<PgWebContainerResource>>? configureContainer = null, string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PgWebContainerResource>().SingleOrDefault() is { } existingPgWebResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingPgWebResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            containerName ??= $"{builder.Resource.Name}-pgweb";
            var pgwebContainer = new PgWebContainerResource(containerName);
            var pgwebContainerBuilder = builder.ApplicationBuilder.AddResource(pgwebContainer)
                                               .WithImage(PostgresContainerImageTags.PgWebImage, PostgresContainerImageTags.PgWebTag)
                                               .WithImageRegistry(PostgresContainerImageTags.PgWebRegistry)
                                               .WithHttpEndpoint(targetPort: 8081, name: "http")
                                               .WithArgs("--bookmarks-dir=/.pgweb/bookmarks")
                                               .WithArgs("--sessions")
                                               .ExcludeFromManifest();

            configureContainer?.Invoke(pgwebContainerBuilder);

            pgwebContainerBuilder.WithRelationship(builder.Resource, "PgWeb");

            pgwebContainerBuilder.WithHttpHealthCheck();

            pgwebContainerBuilder.WithContainerFiles(
                destinationPath: "/",
                callback: (context, _) =>
                {
                    var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                    var postgresInstances = builder.ApplicationBuilder.Resources.OfType<PostgresDatabaseResource>();

                    // Add the bookmarks to the pgweb container
                    return Task.FromResult<IEnumerable<ContainerFileSystemItem>>([
                        new ContainerDirectory
                        {
                            Name = ".pgweb",
                            Entries = [
                                new ContainerDirectory
                                {
                                    Name = "bookmarks",
                                    Entries = WritePgWebBookmarks(postgresInstances),
                                },
                            ],
                        },
                    ]);
                });

            return builder;
        }
    }

    private static void SetPgAdminEnvironmentVariables(EnvironmentCallbackContext context)
    {
        // Disables pgAdmin authentication.
        context.EnvironmentVariables.Add("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False");
        context.EnvironmentVariables.Add("PGADMIN_CONFIG_SERVER_MODE", "False");

        // You need to define the PGADMIN_DEFAULT_EMAIL and PGADMIN_DEFAULT_PASSWORD or PGADMIN_DEFAULT_PASSWORD_FILE environment variables.
        context.EnvironmentVariables.Add("PGADMIN_DEFAULT_EMAIL", "admin@domain.com");
        context.EnvironmentVariables.Add("PGADMIN_DEFAULT_PASSWORD", "admin");

        // When running in the context of Codespaces we need to set some additional environment
        // variables so that PGAdmin will trust the forwarded headers that Codespaces port
        // forwarding will send.
        var config = context.ExecutionContext.ServiceProvider.GetRequiredService<IConfiguration>();
        if (context.ExecutionContext.IsRunMode && config.GetValue<bool>("CODESPACES", false))
        {
            context.EnvironmentVariables["PGADMIN_CONFIG_PROXY_X_HOST_COUNT"] = "1";
            context.EnvironmentVariables["PGADMIN_CONFIG_PROXY_X_PREFIX_COUNT"] = "1";
        }
    }

    /// <summary>
    /// Adds a named volume for the data folder to a PostgreSQL container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PostgresServerResource> WithDataVolume(this IResourceBuilder<PostgresServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"),
            "/var/lib/postgresql/data", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a PostgreSQL container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PostgresServerResource> WithDataBindMount(this IResourceBuilder<PostgresServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/var/lib/postgresql/data", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to a PostgreSQL container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PostgresServerResource> WithInitBindMount(this IResourceBuilder<PostgresServerResource> builder, string source, bool isReadOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }

    /// <summary>
    /// Defines the SQL script used to create the database.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="PostgresDatabaseResource"/>.</param>
    /// <param name="script">The SQL script used to create the database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The script can only contain SQL statements applying to the default database like CREATE DATABASE. Custom statements like table creation
    /// and data insertion are not supported since they require a distinct connection to the newly created database.
    /// <value>Default script is <code>CREATE DATABASE "&lt;QUOTED_DATABASE_NAME&gt;"</code></value>
    /// </remarks>
    public static IResourceBuilder<PostgresDatabaseResource> WithCreationScript(this IResourceBuilder<PostgresDatabaseResource> builder, string script)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(script);

        builder.WithAnnotation(new CreationScriptAnnotation(script));

        return builder;
    }

    private static IEnumerable<ContainerFileSystemItem> WritePgWebBookmarks(IEnumerable<PostgresDatabaseResource> postgresInstances)
    {
        var bookmarkFiles = new List<ContainerFileSystemItem>();

        foreach (var postgresDatabase in postgresInstances)
        {
            var user = postgresDatabase.Parent.UserNameParameter?.Value ?? "postgres";

            // PgAdmin assumes Postgres is being accessed over a default Aspire container network and hardcodes the resource address
            // This will need to be refactored once updated service discovery APIs are available
            var fileContent = $"""
                    host = "{postgresDatabase.Parent.Name}"
                    port = {postgresDatabase.Parent.PrimaryEndpoint.TargetPort}
                    user = "{user}"
                    password = "{postgresDatabase.Parent.PasswordParameter.Value}"
                    database = "{postgresDatabase.DatabaseName}"
                    sslmode = "disable"
                    """;

            bookmarkFiles.Add(new ContainerFile
            {
                Name = $"{postgresDatabase.Name}.toml",
                Contents = fileContent,
            });
        }

        return bookmarkFiles;
    }

    private static string WritePgAdminServerJson(IEnumerable<PostgresServerResource> postgresInstances)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteStartObject("Servers");

        var serverIndex = 1;

        foreach (var postgresInstance in postgresInstances)
        {
            var endpoint = postgresInstance.PrimaryEndpoint;

            writer.WriteStartObject($"{serverIndex}");
            writer.WriteString("Name", postgresInstance.Name);
            writer.WriteString("Group", "Servers");
            // PgAdmin assumes Postgres is being accessed over a default Aspire container network and hardcodes the resource address
            // This will need to be refactored once updated service discovery APIs are available
            writer.WriteString("Host", endpoint.Resource.Name);
            writer.WriteNumber("Port", (int)endpoint.TargetPort!);
            writer.WriteString("Username", postgresInstance.UserNameParameter?.Value ?? "postgres");
            writer.WriteString("SSLMode", "prefer");
            writer.WriteString("MaintenanceDB", "postgres");
            writer.WriteString("PasswordExecCommand", $"echo '{postgresInstance.PasswordParameter.Value}'"); // HACK: Generating a pass file and playing around with chmod is too painful.
            writer.WriteEndObject();

            serverIndex++;
        }

        writer.WriteEndObject();
        writer.WriteEndObject();

        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task CreateDatabaseAsync(NpgsqlConnection npgsqlConnection, PostgresDatabaseResource npgsqlDatabase, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var scriptAnnotation = npgsqlDatabase.Annotations.OfType<CreationScriptAnnotation>().LastOrDefault();

        try
        {
            var quotedDatabaseIdentifier = new NpgsqlCommandBuilder().QuoteIdentifier(npgsqlDatabase.DatabaseName);
            using var command = npgsqlConnection.CreateCommand();
            command.CommandText = scriptAnnotation?.Script ?? $"CREATE DATABASE {quotedDatabaseIdentifier}";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException p) when (p.SqlState == "42P04")
        {
            // Ignore the error if the database already exists.
        }
        catch (Exception e)
        {
            var logger = serviceProvider.GetRequiredService<ResourceLoggerService>().GetLogger(npgsqlDatabase.Parent);
            logger.LogError(e, "Failed to create database '{DatabaseName}'", npgsqlDatabase.DatabaseName);
        }
    }
}
