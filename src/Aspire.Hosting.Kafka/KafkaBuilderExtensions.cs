// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Kafka resources to the application model.
/// </summary>
public static class KafkaBuilderExtensions
{
    private const int KafkaBrokerPort = 9092;

    /// <summary>
    /// Adds a Kafka resource to the application. A container is used for local development.  This version the package defaults to the 7.6.0 tag of the confluentinc/confluent-local container image.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency</param>
    /// <param name="port">The host port of Kafka broker.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{KafkaServerResource}"/>.</returns>
    public static IResourceBuilder<KafkaServerResource> AddKafka(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var kafka = new KafkaServerResource(name);
        return builder.AddResource(kafka)
            .WithEndpoint(targetPort: KafkaBrokerPort, port: port, name: KafkaServerResource.PrimaryEndpointName)
            .WithImage(KafkaContainerImageTags.Image, KafkaContainerImageTags.Tag)
            .WithImageRegistry(KafkaContainerImageTags.Registry)
            .WithEnvironment(context => ConfigureKafkaContainer(context, kafka));
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Kafka container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KafkaServerResource> WithDataVolume(this IResourceBuilder<KafkaServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/kafka/data", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Kafka container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KafkaServerResource> WithDataBindMount(this IResourceBuilder<KafkaServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/lib/kafka/data", isReadOnly);

    private static void ConfigureKafkaContainer(EnvironmentCallbackContext context, KafkaServerResource resource)
    {
        // confluentinc/confluent-local is a docker image that contains a Kafka broker started with KRaft to avoid pulling a separate image for ZooKeeper.
        // See https://github.com/confluentinc/kafka-images/blob/master/local/README.md.
        // When not explicitly set default configuration is applied.
        // See https://github.com/confluentinc/kafka-images/blob/master/local/include/etc/confluent/docker/configureDefaults for more details.

        var hostPort = context.ExecutionContext.IsPublishMode
            ? KafkaBrokerPort
            : resource.PrimaryEndpoint.Port;
        context.EnvironmentVariables.Add("KAFKA_ADVERTISED_LISTENERS",
            $"PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:{hostPort}");
    }
}
