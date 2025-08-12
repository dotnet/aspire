// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;

namespace Aspire.Hosting.Kusto;

/// <summary>
/// Extension methods for adding Kusto resources to the application model.
/// </summary>
public static class KustoResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Kusto resource to the application model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When adding a <see cref="KustoResource"/> to your application model the resource can then
    /// be referenced by other resources using the resource name. When the dependent resource is using
    /// the extension method <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// then the dependent resource will wait until the Kusto database is available.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KustoResource> AddKusto(this IDistributedApplicationBuilder builder, [ResourceName] string name = "kusto")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        KustoResource resource = new(name);
        var resourceBuilder = builder.AddResource(resource);

        // Register a health check that will be used to verify Kusto is available
        ICslQueryProvider? queryProvider = null;
        resourceBuilder.OnConnectionStringAvailable(async (resource, evt, ct) =>
        {
            var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
            throw new DistributedApplicationException($"ConnectionStringAvailableEvent  published for resource '{resource.Name}', but the connection string was null.");

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
        resourceBuilder.OnResourceReady(async (resource, evt, ct) =>
        {
            var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false) ??
                throw new DistributedApplicationException($"Connection string for Kusto resource '{resourceBuilder.Resource.Name}' is null.");

            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new()
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    ShouldHandle = new PredicateBuilder().Handle<KustoRequestThrottledException>(),
                })
                .Build();

            var kcsb = new KustoConnectionStringBuilder(connectionString);
            using var adminProvider = KustoClientFactory.CreateCslAdminProvider(kcsb);

            foreach (var annotation in resource.Annotations.OfType<KustoCreationScriptAnnotation>())
            {
                var crp = new ClientRequestProperties()
                {
                    ClientRequestId = Guid.NewGuid().ToString(),
                };
                crp.SetParameter(ClientRequestProperties.OptionQueryConsistency, ClientRequestProperties.OptionQueryConsistency_Strong);

                await pipeline.ExecuteAsync(async cancellationToken => await adminProvider.ExecuteControlCommandAsync(annotation.Database ?? adminProvider.DefaultDatabaseName, annotation.Script, crp).ConfigureAwait(false), ct).ConfigureAwait(false);
            }
        });

        return resourceBuilder
            .WithHealthCheck(healthCheckKey)
            .ExcludeFromManifest();
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
    public static IResourceBuilder<KustoResource> RunAsEmulator(
        this IResourceBuilder<KustoResource> builder,
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
            .WithEnvironment("-m", "4G");

        configureContainer?.Invoke(surrogateBuilder);

        return builder;
    }

    /// <summary>
    /// Configures the Kusto resource with any control script (e.g. to create tables or ingest data with the .ingest command).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method allows you to specify a KQL script that will be executed against the Kusto database when the resource is ready.
    /// </para>
    /// <para>
    /// When creating and populating databases, the creation operations must be split across multiple scripts, as the database must first be
    /// created, and then a subsequent script must run using a connection to the newly created database.
    /// </para>
    /// </remarks>
    /// <param name="builder">The resource builder to configure.</param>
    /// <param name="script">KQL script to create databases, tables, or data.</param>
    /// <param name="database">The database to use when executing the script. If <see langword="null"/> use the default database.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<KustoResource> WithCreationScript(this IResourceBuilder<KustoResource> builder, string script, string? database = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(script);

        // Store script as an annotation on the resource
        builder.WithAnnotation(new KustoCreationScriptAnnotation(script, database));

        return builder;
    }
}
