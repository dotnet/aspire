// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Extension methods for adding Kusto resources to the application model.
/// </summary>
public static class KustoResourceBuilderExtensions
{
    private static readonly ResiliencePipeline s_pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new()
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            ShouldHandle = new PredicateBuilder().Handle<KustoRequestThrottledException>(),
        })
        .Build();

    /// <summary>
    /// Adds a Kusto resource to the application model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When adding a <see cref="KustoServerResource"/> to your application model the resource can then
    /// be referenced by other resources using the resource name. When the dependent resource is using
    /// the extension method <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// then the dependent resource will wait until the Kusto database is available.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KustoServerResource> AddKusto(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var resource = new KustoServerResource(name);
        var resourceBuilder = builder.AddResource(resource);

        // Register a health check that will be used to verify Kusto is available
        ICslQueryProvider? queryProvider = null;
        resourceBuilder.OnConnectionStringAvailable(async (resource, evt, ct) =>
        {
            var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
            throw new DistributedApplicationException($"ConnectionStringAvailableEvent published for resource '{resource.Name}', but the connection string was null.");

            var kcsb = new KustoConnectionStringBuilder(connectionString);
            queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
        });

        var healthCheckKey = $"{resource.Name}_check";
        resourceBuilder.ApplicationBuilder.Services.AddHealthChecks()
         .Add(new HealthCheckRegistration(
             healthCheckKey,
             _ => new KustoHealthCheck(queryProvider!),
             failureStatus: default,
             tags: default,
             timeout: default));

        // Execute any setup now that Kusto is ready
        resourceBuilder.OnResourceReady(async (server, evt, ct) =>
        {
            var connectionString = await server.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
                throw new DistributedApplicationException($"Connection string for Kusto resource '{resourceBuilder.Resource.Name}' is null.");

            var kcsb = new KustoConnectionStringBuilder(connectionString);
            using var adminProvider = KustoClientFactory.CreateCslAdminProvider(kcsb);

            foreach (var name in server.Databases.Keys)
            {
                if (builder.Resources.FirstOrDefault(n => string.Equals(n.Name, name, StringComparisons.ResourceName)) is KustoDatabaseResource kustoDatabase)
                {
                    await CreateDatabaseAsync(adminProvider, kustoDatabase, evt.Services, ct).ConfigureAwait(false);
                }
            }
        });

        return resourceBuilder
            .WithHealthCheck(healthCheckKey)
            .ExcludeFromManifest();
    }

    /// <summary>
    /// Adds a Kusto database to the application model.
    /// </summary>
    /// <param name="builder">The Kusto server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KustoDatabaseResource> AddDatabase(this IResourceBuilder<KustoServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var kustoDatabase = new KustoDatabaseResource(name, databaseName, builder.Resource);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(kustoDatabase);

        // TODO: Refactor to shared heatlh check

        // Register a health check that will be used to verify database is available
        ICslQueryProvider? queryProvider = null;
        resourceBuilder.OnConnectionStringAvailable(async (db, evt, ct) =>
        {
            var connectionString = await db.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
            throw new DistributedApplicationException($"ConnectionStringAvailableEvent published for resource '{db.Name}', but the connection string was null.");

            var kcsb = new KustoConnectionStringBuilder(connectionString);
            queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
        });

        var healthCheckKey = $"{kustoDatabase.Name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks()
         .Add(new HealthCheckRegistration(
             healthCheckKey,
             _ => new KustoHealthCheck(queryProvider!),
             failureStatus: default,
             tags: default,
             timeout: default));

        return resourceBuilder;
    }

    /// <summary>
    /// Configures the Kusto resource to run as an emulator using the Kustainer container.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="KustoEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="KustoEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="KustoEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The resource builder to configure.</param>
    /// <param name="httpPort">The host port that the Kusto HTTP endpoint is bound to.</param>
    /// <param name="configureContainer">
    /// Optional action to configure the Kusto emulator container.
    /// </param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<KustoServerResource> RunAsEmulator(
        this IResourceBuilder<KustoServerResource> builder,
        int? httpPort = null,
        Action<IResourceBuilder<KustoEmulatorResource>>? configureContainer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var surrogate = new KustoEmulatorResource(builder.Resource);
        var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);

        surrogateBuilder
            .WithAnnotation(new EmulatorResourceAnnotation())
            .WithHttpEndpoint(targetPort: KustoEmulatorContainerDefaults.DefaultTargetPort, port: httpPort, name: "http")
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = KustoEmulatorContainerImageTags.Registry,
                Image = KustoEmulatorContainerImageTags.Image,
                Tag = KustoEmulatorContainerImageTags.Tag
            })
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithContainerRuntimeArgs("--memory", "4G");

        configureContainer?.Invoke(surrogateBuilder);

        return builder;
    }

    /// <summary>
    /// Defines the script used to create the database.
    /// </summary>
    /// <remarks>
    /// This script will only be executed when the Kusto resource is running in emulator mode. In production scenarios, the database creation should be handled as part of the provisioning process.
    /// <value>Default script is <code>.create database DATABASE_NAME volatile</code></value>
    /// </remarks>
    /// <param name="builder">The resource builder to configure.</param>
    /// <param name="script">KQL script to create databases, tables, or data.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<KustoDatabaseResource> WithCreationScript(this IResourceBuilder<KustoDatabaseResource> builder, string script)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(script);

        // Store script as an annotation on the resource
        builder.WithAnnotation(new KustoCreateDatabaseScriptAnnotation(script));

        return builder;
    }

    private static async Task CreateDatabaseAsync(ICslAdminProvider adminProvider, KustoDatabaseResource databaseResource, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var crp = new ClientRequestProperties()
        {
            ClientRequestId = Guid.NewGuid().ToString(),
        };
        crp.SetParameter(ClientRequestProperties.OptionQueryConsistency, ClientRequestProperties.OptionQueryConsistency_Strong);

        var scriptAnnotation = databaseResource.Annotations.OfType<KustoCreateDatabaseScriptAnnotation>().LastOrDefault();
        var script = scriptAnnotation?.Script ?? $".create database {databaseResource.DatabaseName} volatile;";

        var logger = serviceProvider.GetRequiredService<ResourceLoggerService>().GetLogger(databaseResource.Parent);
        logger.LogDebug("Creating database '{DatabaseName}'", databaseResource.DatabaseName);

        try
        {
            await s_pipeline.ExecuteAsync(async cancellationToken => await adminProvider.ExecuteControlCommandAsync(databaseResource.DatabaseName, script, crp).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Database '{DatabaseName}' created successfully", databaseResource.DatabaseName);
        }
        catch (KustoBadRequestException e) when (e.Message.Contains("EntityNameAlreadyExistsException"))
        {
            // Ignore the error if the database already exists.
            logger.LogDebug("Database '{DatabaseName}' already exists", databaseResource.DatabaseName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create database '{DatabaseName}'", databaseResource.DatabaseName);
        }
    }
}
