// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Kafka resources to the application model.
/// </summary>
public static class KafkaBuilderExtensions
{
    private const int KafkaBrokerPort = 9092;

    /// <summary>
    /// Changes the Kafka resource to be published as a container in the manifest.
    /// </summary>
    /// <param name="builder">Resource builder for <see cref="KafkaServerResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KafkaServerResource> PublishAsContainer(this IResourceBuilder<KafkaServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(context => WriteKafkaContainerToManifest(context, builder.Resource));
    }

    private static void WriteKafkaContainerToManifest(ManifestPublishingContext context, KafkaServerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString("connectionString", $"{{{resource.Name}.bindings.tcp.host}}:{{{resource.Name}.bindings.tcp.port}}");
    }

    /// <summary>
    /// Adds a Kafka resource to the application. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency</param>
    /// <param name="port">The host port of Kafka broker.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{KafkaServerResource}"/>.</returns>
    public static IResourceBuilder<KafkaServerResource> AddKafka(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var kafka = new KafkaServerResource(name);
        return builder.AddResource(kafka)
            .WithEndpoint(containerPort: KafkaBrokerPort, hostPort: port)
            .WithAnnotation(new ContainerImageAnnotation{ Image = "confluentinc/confluent-local", Tag = "latest" })
            .WithManifestPublishingCallback(WriteKafkaServerToManifest)
            .WithEnvironment(context => ConfigureKafkaContainer(context, kafka));

        static void WriteKafkaServerToManifest(ManifestPublishingContext context)
        {
            context.Writer.WriteString("type", "kafka.server.v0");
        }
    }

    private static void ConfigureKafkaContainer(EnvironmentCallbackContext context, IResource resource)
    {
        // confluentinc/confluent-local is a docker image that contains a Kafka broker started with KRaft to avoid pulling a separate image for ZooKeeper.
        // See https://github.com/confluentinc/kafka-images/blob/master/local/README.md.
        // When not explicitly set default configuration is applied.
        // See https://github.com/confluentinc/kafka-images/blob/master/local/include/etc/confluent/docker/configureDefaults for more details.

        var hostPort = context.ExecutionContext.Operation == DistributedApplicationOperation.Publish
            ? KafkaBrokerPort
            : GetResourcePort(resource);
        context.EnvironmentVariables.Add("KAFKA_ADVERTISED_LISTENERS",
            $"PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:{hostPort}");

        static int GetResourcePort(IResource resource)
        {
            if (!resource.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                throw new DistributedApplicationException(
                    $"Kafka resource \"{resource.Name}\" does not have endpoint annotation.");
            }

            return allocatedEndpoints.Single().Port;
        }
    }
}
