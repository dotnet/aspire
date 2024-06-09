// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elasticsearch;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Elasticsearch resources to the application model.
/// </summary>
public static class ElasticsearchBuilderExtensions
{
    private const int ElasticsearchPort = 9200;
    private const int ElasticsearchInternalPort = 9300;

    /// <summary>
    /// Adds a Elasticsearch container to the application model.
    /// </summary>
    /// <remarks>
    /// The default image is "elasticsearch" and the tag is "8.8.0".
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <param name="password">The parameter used to provide the superuser password for the elasticsearch. If <see langword="null"/> a random password will be generated.</param>
    /// <returns></returns>
    public static IResourceBuilder<ElasticsearchResource> AddElasticsearch(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var elasticsearch = new ElasticsearchResource(name, passwordParameter);

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
             });

    }

    /// <summary>
    /// Adds a named volume for the data folder to a Elasticseach container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ElasticsearchResource> WithDataVolume(this IResourceBuilder<ElasticsearchResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/usr/share/elasticsearch/data", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Elasticseach container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ElasticsearchResource> WithDataBindMount(this IResourceBuilder<ElasticsearchResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/usr/share/elasticsearch/data", isReadOnly);

}
