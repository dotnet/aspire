// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Milvus;
using Aspire.Hosting.Utils;
using Aspire.Milvus.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Milvus.Client;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Milvus resources to the application model.
/// </summary>
public static class MilvusBuilderExtensions
{
    private const int MilvusPortGrpc = 19530;

    /// <summary>
    /// Adds a Milvus resource to the application. A container is used for local development.
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var milvus = builder.AddMilvus("milvus");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(milvus);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// <remarks>
    /// This version the package defaults to the 2.3-latest tag of the milvusdb/milvus container image.
    /// The .NET client library uses the gRPC port by default to communicate and this resource exposes that endpoint.
    /// A web-based administration tool for Milvus can also be added using <see cref="WithAttu"/>.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency</param>
    /// <param name="apiKey">The parameter used to provide the auth key/token user for the Milvus resource.</param>
    /// <param name="grpcPort">The host port of gRPC endpoint of Milvus database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MilvusServerResource}"/>.</returns>
    public static IResourceBuilder<MilvusServerResource> AddMilvus(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? apiKey = null,
        int? grpcPort = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        var apiKeyParameter = apiKey?.Resource ??
            ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-key");

        var milvus = new MilvusServerResource(name, apiKeyParameter);

        MilvusClient? milvusClient = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(milvus, async (@event, ct) =>
        {
            var connectionString = await milvus.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false)
            ?? throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{milvus.Name}' resource but the connection string was null.");

            milvusClient = CreateMilvusClient(@event.Services, connectionString);
            var lookup = builder.Resources.OfType<MilvusDatabaseResource>().ToDictionary(d => d.Name);
            foreach (var databaseName in milvus.Databases)
            {
                if (!lookup.TryGetValue(databaseName.Key, out var databaseResource))
                {
                    throw new DistributedApplicationException($"Database resource '{databaseName}' under Milvus Server resource '{milvus.Name}' was not found in the model.");
                }
                var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(databaseResource, @event.Services);
                await builder.Eventing.PublishAsync<ConnectionStringAvailableEvent>(connectionStringAvailableEvent, ct).ConfigureAwait(false);

                var beforeResourceStartedEvent = new BeforeResourceStartedEvent(databaseResource, @event.Services);
                await builder.Eventing.PublishAsync(beforeResourceStartedEvent, ct).ConfigureAwait(false);
            }
        });

        var healthCheckKey = $"{name}_check";
        // TODO: Use health check from AspNetCore.Diagnostics.HealthChecks once it's implemented via this issue:
        // https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues/2214
        builder.Services.AddHealthChecks()
          .Add(new HealthCheckRegistration(
              healthCheckKey,
              sp => new MilvusHealthCheck(milvusClient!),
              failureStatus: default,
              tags: default,
              timeout: default));

        return builder.AddResource(milvus)
            .WithImage(MilvusContainerImageTags.Image, MilvusContainerImageTags.Tag)
            .WithImageRegistry(MilvusContainerImageTags.Registry)
            .WithHttpEndpoint(port: grpcPort, targetPort: MilvusPortGrpc, name: MilvusServerResource.PrimaryEndpointName)
            .WithEndpoint(MilvusServerResource.PrimaryEndpointName, endpoint =>
            {
                endpoint.Transport = "http2";
            })
            .WithEnvironment("COMMON_STORAGETYPE", "local")
            .WithEnvironment("ETCD_USE_EMBED", "true")
            .WithEnvironment("ETCD_DATA_DIR", "/var/lib/milvus/etcd")
            .WithEnvironment("COMMON_SECURITY_AUTHORIZATIONENABLED", "true")
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["COMMON_SECURITY_DEFAULTROOTPASSWORD"] = milvus.ApiKeyParameter;
            })
            .WithArgs("milvus", "run", "standalone")
            .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a Milvus database to the application model.
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var booksdb = builder.AddMilvus("milvus");
    ///   .AddDatabase("booksdb");
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(booksdb);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// <param name="builder">The Milvus server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <remarks>This method does not actually create the database in Milvus, rather helps complete a connection string that is used by the client component.</remarks>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MilvusDatabaseResource> AddDatabase(this IResourceBuilder<MilvusServerResource> builder, string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var milvusDatabaseResource = new MilvusDatabaseResource(name, databaseName, builder.Resource);

        string? connectionString = null;
        MilvusClient? milvusClient = null;
        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(milvusDatabaseResource, async (@event, ct) =>
        {
            connectionString = await milvusDatabaseResource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{milvusDatabaseResource.Name}' resource but the connection string was null.");
            }
            milvusClient = CreateMilvusClient(@event.Services, connectionString);
        });

        var healthCheckKey = $"{name}_check";
        // TODO: Use health check from AspNetCore.Diagnostics.HealthChecks once it's implemented via this issue:
        // https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues/2214
        builder.ApplicationBuilder.Services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                healthCheckKey,
                sp => new MilvusHealthCheck(milvusClient!),
                failureStatus: default,
                tags: default,
                timeout: default));

        return builder.ApplicationBuilder.AddResource(milvusDatabaseResource)
                                         .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds an administration and development platform for Milvus to the application model using Attu. This version the package defaults to the 2.3-latest tag of the attu container image
    /// </summary>
    /// <example>
    /// Use in application host with a Milvus resource
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var milvus = builder.AddMilvus("milvus")
    ///   .WithAttu();
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(milvus);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// <param name="builder">The Milvus server resource builder.</param>
    /// <param name="configureContainer">Configuration callback for Attu container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithAttu<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<AttuResource>>? configureContainer = null, string? containerName = null) where T : MilvusServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        containerName ??= $"{builder.Resource.Name}-attu";

        var attuContainer = new AttuResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(attuContainer)
                                                        .WithImage(MilvusContainerImageTags.AttuImage, MilvusContainerImageTags.AttuTag)
                                                        .WithImageRegistry(MilvusContainerImageTags.Registry)
                                                        .WithHttpEndpoint(targetPort: 3000, name: "http")
                                                        .WithEnvironment(context => ConfigureAttuContainer(context, builder.Resource))
                                                        .ExcludeFromManifest();

        configureContainer?.Invoke(resourceBuilder);

        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Milvus container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the resource name.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MilvusServerResource> WithDataVolume(this IResourceBuilder<MilvusServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/milvus", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Milvus container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MilvusServerResource> WithDataBindMount(this IResourceBuilder<MilvusServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);
        return builder.WithBindMount(source, "/var/lib/milvus", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the configuration of a Milvus container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configurationFilePath">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MilvusServerResource> WithConfigurationBindMount(this IResourceBuilder<MilvusServerResource> builder, string configurationFilePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationFilePath);
        return builder.WithBindMount(configurationFilePath, "/milvus/configs/milvus.yaml");
    }

    private static void ConfigureAttuContainer(EnvironmentCallbackContext context, MilvusServerResource resource)
    {
        // Attu assumes Milvus is being accessed over a default Aspire container network and hardcodes the resource address
        // This will need to be refactored once updated service discovery APIs are available
        context.EnvironmentVariables.Add("MILVUS_URL", $"{resource.PrimaryEndpoint.Scheme}://{resource.Name}:{resource.PrimaryEndpoint.TargetPort}");
    }
    internal static MilvusClient CreateMilvusClient(IServiceProvider sp, string? connectionString)
    {
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string is unavailable");
        }

        Uri? endpoint = null;
        string? key = null;
        string? database = null;

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            endpoint = uri;
        }
        else
        {
            var connectionBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (connectionBuilder.ContainsKey("Endpoint") && Uri.TryCreate(connectionBuilder["Endpoint"].ToString(), UriKind.Absolute, out var serviceUri))
            {
                endpoint = serviceUri;
            }

            if (connectionBuilder.ContainsKey("Key"))
            {
                key = connectionBuilder["Key"].ToString();
            }

            if (connectionBuilder.ContainsKey("Database"))
            {
                database = connectionBuilder["Database"].ToString();
            }
        }

        return new MilvusClient(endpoint!, apiKey: key!, database: database, loggerFactory: sp.GetRequiredService<ILoggerFactory>());
    }
}
