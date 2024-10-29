// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Elastic.Clients.Elasticsearch;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elasticsearch;
using Aspire.Hosting.Utils;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Elasticsearch resources to the application model.
/// </summary>
public static class ElasticsearchBuilderExtensions
{
    private const int ElasticsearchPort = 9200;
    private const int ElasticsearchInternalPort = 9300;

    /// <summary>
    /// Adds an Elasticsearch container resource to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="ElasticsearchContainerImageTags.Tag"/> tag of the <inheritdoc cref="ElasticsearchContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <param name="password">The parameter used to provide the superuser password for the elasticsearch. If <see langword="null"/> a random password will be generated.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an Elasticsearch container to the application model and reference it in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var elasticsearch = builder.AddElasticsearch("elasticsearch");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(elasticsearch);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ElasticsearchResource> AddElasticsearch(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var elasticsearch = new ElasticsearchResource(name, passwordParameter);

        string? connectionString = null;
        ElasticsearchClient? elasticsearchClient = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(elasticsearch, async (@event, ct) =>
        {
            connectionString = await elasticsearch.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString is null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{elasticsearch.Name}' resource but the connection string was null.");
            }
            elasticsearchClient = new ElasticsearchClient(new Uri(connectionString));
        });

        var healthCheckKey = $"{name}_check";
        // todo: Use health check from AspNetCore.Diagnostics.HealthChecks once following PR released:
        // https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/pull/2244
        builder.Services.AddHealthChecks()
          .Add(new HealthCheckRegistration(
              healthCheckKey,
              sp => new ElasticsearchHealthCheck(elasticsearchClient!),
              failureStatus: default,
              tags: default,
              timeout: default));

        return builder.AddResource(elasticsearch)
             .WithImage(ElasticsearchContainerImageTags.Image, ElasticsearchContainerImageTags.Tag)
             .WithImageRegistry(ElasticsearchContainerImageTags.Registry)
             .WithHttpEndpoint(targetPort: ElasticsearchPort, port: port, name: ElasticsearchResource.PrimaryEndpointName)
             .WithEndpoint(targetPort: ElasticsearchInternalPort, port: port, name: ElasticsearchResource.InternalEndpointName)
             .WithEnvironment("discovery.type", "single-node")
             .WithEnvironment("xpack.security.enabled", "true")
             .WithEnvironment(context =>
             {
                 context.EnvironmentVariables["ELASTIC_PASSWORD"] = elasticsearch.PasswordParameter;
             })
             .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Elasticsearch container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an Elasticsearch container to the application model and reference it in a .NET project. Additionally, in this
    /// example a data volume is added to the container to allow data to be persisted across container restarts.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var elasticsearch = builder.AddElasticsearch("elasticsearch")
    /// .WithDataVolume();
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(elasticsearch);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ElasticsearchResource> WithDataVolume(this IResourceBuilder<ElasticsearchResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/usr/share/elasticsearch/data");
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Elasticsearch container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an Elasticsearch container to the application model and reference it in a .NET project. Additionally, in this
    /// example a bind mount is added to the container to allow data to be persisted across container restarts.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var elasticsearch = builder.AddElasticsearch("elasticsearch")
    /// .WithDataBindMount("./data/elasticsearch/data");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(elasticsearch);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ElasticsearchResource> WithDataBindMount(this IResourceBuilder<ElasticsearchResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/usr/share/elasticsearch/data");
    }
}
