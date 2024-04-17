// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Nats;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding NATS resources to the application model.
/// </summary>
public static class NatsBuilderExtensions
{
    /// <summary>
    /// Adds a NATS server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for NATS server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> AddNats(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var nats = new NatsServerResource(name);
        return builder.AddResource(nats)
                      .WithEndpoint(targetPort: 4222, port: port, name: NatsServerResource.PrimaryEndpointName)
                      .WithImage(NatsContainerImageTags.Image, NatsContainerImageTags.Tag)
                      .WithImageRegistry(NatsContainerImageTags.Registry);
    }

    /// <summary>
    /// Adds JetStream support to the NATS server resource.
    /// </summary>
    /// <param name="builder">NATS resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> WithJetStream(this IResourceBuilder<NatsServerResource> builder)
    {
        return builder.WithArgs(["-js"]);
    }

    private const string DataPath = "/data";

    /// <summary>
    /// Adds a named volume for the data folder to a NATS resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> WithDataVolume(this IResourceBuilder<NatsServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        return builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), DataPath, isReadOnly)
                      .WithArgs(["-sd", DataPath]);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a NATS resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> WithDataBindMount(this IResourceBuilder<NatsServerResource> builder, string source, bool isReadOnly = false)
    {
        return builder.WithBindMount(source, "/data", isReadOnly)
                      .WithArgs(["-sd", DataPath]);
    }
}
