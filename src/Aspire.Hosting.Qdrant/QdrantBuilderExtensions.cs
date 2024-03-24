// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Qdrant;

/// <summary>
/// Provides extension methods for adding Qdrant resources to the application model.
/// </summary>
public static class QdrantBuilderExtensions
{
    private const int QdrantPortHttp = 6334;
    private const int QdrantPortDashboard = 6333;
    private const string ApiKeyEnvVarName = "QDRANT__SERVICE__API_KEY";

    /// <summary>
    /// Adds a Qdrant resource to the application. A container is used for local development.  This version the package defaults to the v1.8.3 tag of the qdrant/qdrant container image.
    /// The .NET client library uses the gRPC port by default to communicate and this resource exposes that endpoint.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency</param>
    /// <param name="apiKey">The parameter used to provide the API Key for the Qdrant resource. If <see langword="null"/> a random key will be generated.</param>
    /// <param name="port">The host port of Qdrant database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{QdrantServerResource}"/>.</returns>
    public static IResourceBuilder<QdrantServerResource> AddQdrant(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? apiKey = null,
        int? port = null)
    {
        var apiKeyParameter = apiKey?.Resource ??
            ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-apiKey", special: false);
        var qdrant = new QdrantServerResource(name, apiKeyParameter);
        return builder.AddResource(qdrant)
            .WithImage(QdrantContainerImageTags.Image, QdrantContainerImageTags.Tag)
            .WithHttpEndpoint(hostPort: port, containerPort: QdrantPortHttp)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[ApiKeyEnvVarName] = qdrant.ApiKeyParameter;
            });
    }

    /// <summary>
    /// Enables the Qdrant web UI dashboard with will be at /dashboard on the endpoint and require the API key to authenticate.
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithDashboard<T>(this IResourceBuilder<T> builder) where T : QdrantServerResource
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithHttpEndpoint(containerPort: QdrantPortDashboard, name: "dashboard");
        }

        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Qdrant container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the resource name.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<QdrantServerResource> WithDataVolume(this IResourceBuilder<QdrantServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/qdrant/storage", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Qdrant container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<QdrantServerResource> WithDataBindMount(this IResourceBuilder<QdrantServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/lib/qdrant/storage", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the init folder to a Qdrant container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<QdrantServerResource> WithInitBindMount(this IResourceBuilder<QdrantServerResource> builder, string source, bool isReadOnly = true)
        => builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
}
