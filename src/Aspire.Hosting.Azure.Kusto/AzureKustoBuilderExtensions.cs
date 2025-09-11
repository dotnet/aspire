// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Kusto;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Extension methods for adding Kusto resources to the application model.
/// </summary>
public static class AzureKustoBuilderExtensions
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
    /// Adds an Azure Data Explorer (Kusto) cluster resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// When adding an <see cref="AzureKustoClusterResource"/> to your application model the resource can then
    /// be referenced by other resources using the resource name. When the dependent resource is using
    /// the extension method <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// then the dependent resource will wait until the Kusto database is available.
    /// </para>
    /// <para>
    /// By default references to the Azure Data Explorer cluster resource will be assigned the following roles:
    /// 
    /// - <see cref="KustoBuiltInRole.Contributor"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureKustoClusterResource}, KustoBuiltInRole[])"/>.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<AzureKustoClusterResource> AddAzureKustoCluster(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (Aspire.Hosting.Azure.AzureResourceInfrastructure infrastructure) =>
        {
            var cluster = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = KustoCluster.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new KustoCluster(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    // Basic cluster configuration - can be enhanced in the future
                    Sku = new KustoSku()
                    {
                        Name = KustoSkuName.StandardD11V2,
                        Tier = KustoSkuTier.Standard,
                        Capacity = 2
                    },
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

            // Add cluster URI output for connection strings
            infrastructure.Add(new ProvisioningOutput("clusterUri", typeof(string)) { Value = cluster.Name });

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = cluster.Name });

            var azureResource = (AzureKustoClusterResource)infrastructure.AspireResource;

            foreach (var database in azureResource.Databases)
            {
                var cdkDatabase = database.ToProvisioningEntity();
                cdkDatabase.Parent = cluster;
                infrastructure.Add(cdkDatabase);
            }
        };

        var resource = new AzureKustoClusterResource(name, configureInfrastructure);
        var resourceBuilder = builder.AddResource(resource);

        AddKustoHealthChecksAndLifecycleManagement(resourceBuilder);

        return resourceBuilder
            .WithDefaultRoleAssignments(GetBuiltInRoleNameHack, KustoBuiltInRoleHack.Contributor);
    }

    // HACK: Until this is resolved: https://github.com/Azure/azure-sdk-for-net/issues/52499
    private static string GetBuiltInRoleNameHack(KustoBuiltInRoleHack value)
    {
        return value._value switch
        {
            KustoBuiltInRoleHack.OwnerValue => nameof(KustoBuiltInRoleHack.Owner),
            KustoBuiltInRoleHack.ContributorValue => nameof(KustoBuiltInRoleHack.Contributor),
            KustoBuiltInRoleHack.ReaderValue => nameof(KustoBuiltInRoleHack.Reader),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Adds a Kusto read-write database to the application model.
    /// </summary>
    /// <param name="builder">The Kusto server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureKustoReadWriteDatabaseResource> AddReadWriteDatabase(this IResourceBuilder<AzureKustoClusterResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        var kustoDatabase = new AzureKustoReadWriteDatabaseResource(name, databaseName, builder.Resource);
        builder.Resource.Databases.Add(kustoDatabase);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(kustoDatabase);

        // Register a health check that will be used to verify database is available
        KustoConnectionStringBuilder? kcsb = null;
        resourceBuilder.OnConnectionStringAvailable(async (db, evt, ct) =>
        {
            var connectionString = await db.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
            throw new DistributedApplicationException($"ConnectionStringAvailableEvent published for resource '{db.Name}', but the connection string was null.");

            kcsb = new KustoConnectionStringBuilder(connectionString);
        });

        var healthCheckKey = $"{kustoDatabase.Name}_check";
        resourceBuilder.ApplicationBuilder
            .Services
            .AddHealthChecks()
            .AddAzureKustoHealthCheck(healthCheckKey, _ => kcsb!);

        return resourceBuilder
            .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Configures the Kusto resource to run as an emulator using the Kustainer container.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="AzureKustoEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="AzureKustoEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="AzureKustoEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The resource builder to configure.</param>
    /// <param name="configureContainer">
    /// Optional action to configure the Kusto emulator container.
    /// </param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<AzureKustoClusterResource> RunAsEmulator(
        this IResourceBuilder<AzureKustoClusterResource> builder,
        Action<IResourceBuilder<AzureKustoEmulatorResource>>? configureContainer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // Mark this resource as an emulator for consistent resource identification and tooling support
        builder.WithAnnotation(new EmulatorResourceAnnotation());

        // Add HTTP endpoint to the original resource so the connection string logic can detect emulator mode
        builder.WithHttpEndpoint(targetPort: AzureKustoEmulatorContainerDefaults.DefaultTargetPort, name: "http");

        var surrogate = new AzureKustoEmulatorResource(builder.Resource);
        var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);

        surrogateBuilder
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = AzureKustoEmulatorContainerImageTags.Registry,
                Image = AzureKustoEmulatorContainerImageTags.Image,
                Tag = AzureKustoEmulatorContainerImageTags.Tag
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
    public static IResourceBuilder<AzureKustoReadWriteDatabaseResource> WithCreationScript(this IResourceBuilder<AzureKustoReadWriteDatabaseResource> builder, string script)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(script);

        // Store script as an annotation on the resource
        builder.WithAnnotation(new AzureKustoCreateDatabaseScriptAnnotation(script));

        return builder;
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure Data Explorer (Kusto) cluster resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure Data Explorer cluster resource.</param>
    /// <param name="roles">The built-in Kusto roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the Reader role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var kusto = builder.AddAzureKustoCluster("kusto");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(kusto, KustoBuiltInRole.Reader)
    ///   .WithReference(kusto);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureKustoClusterResource> target,
        params KustoBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, KustoBuiltInRoleExtensions.GetBuiltInRoleName, roles);
    }

    /// <summary>
    /// Configures the host port that the Kusto emulator listens on for HTTP query requests.
    /// </summary>
    /// <param name="builder">Kusto emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureKustoEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureKustoEmulatorResource> WithHttpPort(this IResourceBuilder<AzureKustoEmulatorResource> builder, int port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds Kusto-specific health checks and lifecycle management.
    /// </summary>
    private static void AddKustoHealthChecksAndLifecycleManagement(IResourceBuilder<AzureKustoClusterResource> resourceBuilder)
    {
        var resource = resourceBuilder.Resource;

        // Register a health check that will be used to verify Kusto is available
        KustoConnectionStringBuilder? kcsb = null;
        resourceBuilder.OnConnectionStringAvailable(async (resource, evt, ct) =>
        {
            var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
            throw new DistributedApplicationException($"ConnectionStringAvailableEvent published for resource '{resource.Name}', but the connection string was null.");

            kcsb = new KustoConnectionStringBuilder(connectionString);
        });

        var healthCheckKey = $"{resource.Name}_check";
        resourceBuilder.ApplicationBuilder
            .Services
            .AddHealthChecks()
            .AddAzureKustoHealthCheck(healthCheckKey, _ => kcsb!);

        // Execute any setup now that Kusto is ready
        resourceBuilder.OnResourceReady(async (server, evt, ct) =>
        {
            if (kcsb is null)
            {
                throw new DistributedApplicationException($"Connection string for Kusto resource '{server.Name}' is not set.");
            }

            using var adminProvider = KustoClientFactory.CreateCslAdminProvider(kcsb);
            foreach (var kustoDatabase in server.Databases)
            {
                await CreateDatabaseAsync(adminProvider, kustoDatabase, evt.Services, ct).ConfigureAwait(false);
            }
        });

        resourceBuilder.WithHealthCheck(healthCheckKey);
    }

    private static async Task CreateDatabaseAsync(ICslAdminProvider adminProvider, AzureKustoReadWriteDatabaseResource databaseResource, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var crp = new ClientRequestProperties()
        {
            ClientRequestId = Guid.NewGuid().ToString(),
        };
        crp.SetParameter(ClientRequestProperties.OptionQueryConsistency, ClientRequestProperties.OptionQueryConsistency_Strong);

        var scriptAnnotation = databaseResource.Annotations.OfType<AzureKustoCreateDatabaseScriptAnnotation>().LastOrDefault();
        var script = scriptAnnotation?.Script ?? $".create database {databaseResource.DatabaseName} volatile;";

        var logger = serviceProvider.GetRequiredService<ResourceLoggerService>().GetLogger(databaseResource);
        var rns = serviceProvider.GetRequiredService<ResourceNotificationService>();

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
            await rns.PublishUpdateAsync(databaseResource, state => state with
            {
                State = KnownResourceStates.FailedToStart
            }).ConfigureAwait(false);
        }
    }
}
