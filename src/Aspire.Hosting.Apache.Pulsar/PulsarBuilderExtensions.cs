// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Apache.Pulsar;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Pulsar resource to a <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PulsarBuilderExtensions
{
    /// <summary>
    /// Adds Pulsar container to the application module
    /// Runs in standalone mode by default.
    /// </summary>
    /// <remarks>
    /// The default image and tag are "apachepulsar/pulsar" and "3.2.0".
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the endpoint name when referenced in dependency.</param>
    /// <param name="servicePort">The service port that the underlying container is bound to when running locally.</param>
    /// <param name="brokerPort">The broker port that the underlying container is bound to when running locally.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarResource> AddPulsar(
        this IDistributedApplicationBuilder builder,
        string name,
        int? servicePort = null,
        int? brokerPort = null
    )
    {
        var pulsar = new PulsarResource(name);
        return builder.AddResource(pulsar)
            .WithImage(PulsarContainerImageTags.Image, PulsarContainerImageTags.Tag)
            .WithImageRegistry(PulsarContainerImageTags.Registry)
            .WithEndpoint(port: servicePort, targetPort: 8080, name: PulsarResource.ServiceEndpointName, scheme: "http")
            .WithEndpoint(port: brokerPort, targetPort: 6650, name: PulsarResource.BrokerEndpointName, scheme: "pulsar")
            .WithEntrypoint("/bin/bash")
            .AsStandalone();
    }

    /// <summary>
    /// Configures Pulsar to run in standalone mode
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarResource> AsStandalone(
        this IResourceBuilder<PulsarResource> builder
    ) => builder
        .WithAnnotation(new StandalonePulsarCommandLineArgsAnnotation(), ResourceAnnotationMutationBehavior.Replace);

    /// <summary>
    /// Adds a named volume for the data folder to Pulsar container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarResource> WithDataVolume(
        this IResourceBuilder<PulsarResource> builder,
        string? name = null,
        bool isReadOnly = false
    ) => builder
        .WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "pulsardata"), "/pulsar/data", isReadOnly);

    /// <summary>
    /// Adds a named volume for the config folder to Pulsar container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarResource> WithConfigVolume(
        this IResourceBuilder<PulsarResource> builder,
        string? name = null,
        bool isReadOnly = false
    ) => builder
        .WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "pulsarconf"), "/pulsar/conf", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to Pulsar container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarResource> WithDataBindMount(
        this IResourceBuilder<PulsarResource> builder,
        string source,
        bool isReadOnly = false
    ) => builder
        .WithBindMount(source, "/pulsar/data", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the config folder to Pulsar container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarResource> WithConfigBindMount(
        this IResourceBuilder<PulsarResource> builder,
        string source,
        bool isReadOnly = false
    ) => builder
        .WithBindMount(source, "/pulsar/conf", isReadOnly);
}
