// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Service Bus resources to the application model.
/// </summary>
public static class AzureServiceBusExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus Namespace resource to the application model. This resource can be used to create queue, topic, and subscription resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = static (AzureResourceInfrastructure infrastructure) =>
        {
            var skuParameter = new ProvisioningParameter("sku", typeof(string))
            {
                Value = new StringLiteral("Standard")
            };
            infrastructure.Add(skuParameter);

            var serviceBusNamespace = new ServiceBusNamespace(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Sku = new ServiceBusSku()
                {
                    Name = skuParameter
                },
                DisableLocalAuth = true,
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
            infrastructure.Add(serviceBusNamespace);

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(serviceBusNamespace.CreateRoleAssignment(ServiceBusBuiltInRole.AzureServiceBusDataOwner, principalTypeParameter, principalIdParameter));

            infrastructure.Add(new ProvisioningOutput("serviceBusEndpoint", typeof(string)) { Value = serviceBusNamespace.ServiceBusEndpoint });

            var azureResource = (AzureServiceBusResource)infrastructure.AspireResource;

            foreach (var queue in azureResource.Queues)
            {
                var queueResource = new ServiceBusQueue(Infrastructure.NormalizeIdentifierName(queue))
                {
                    Parent = serviceBusNamespace,
                    Name = queue
                };
                infrastructure.Add(queueResource);
            }
            var topicDictionary = new Dictionary<string, ServiceBusTopic>();
            foreach (var topic in azureResource.Topics)
            {
                var topicResource = new ServiceBusTopic(Infrastructure.NormalizeIdentifierName(topic))
                {
                    Parent = serviceBusNamespace,
                    Name = topic
                };
                infrastructure.Add(topicResource);
                topicDictionary.Add(topic, topicResource);
            }
            foreach (var subscription in azureResource.Subscriptions)
            {
                var topic = topicDictionary[subscription.TopicName];
                var subscriptionResource = new ServiceBusSubscription(Infrastructure.NormalizeIdentifierName(subscription.Name))
                {
                    Parent = topic,
                    Name = subscription.Name
                };
                infrastructure.Add(subscriptionResource);
            }
        };

        var resource = new AzureServiceBusResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, string[] subscriptions)
    {
        builder.Resource.Topics.Add(name);
        foreach (var subscription in subscriptions)
        {
            builder.Resource.Subscriptions.Add((name, subscription));
        }
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name)
    {
        builder.Resource.Queues.Add(name);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name)
    {
        builder.Resource.Topics.Add(name);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddSubscription(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName)
    {
        builder.Resource.Subscriptions.Add((topicName, subscriptionName));
        return builder;
    }

    /// <summary>
    /// Configures an Azure Service Bus resource to be emulated. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="ServiceBusEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="ServiceBusEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="ServiceBusEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// The following example creates an Azure Service Bus resource that runs locally is an emulator and referencing that
    /// resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var serviceBus = builder.AddAzureServiceBus("myservicebus")
    ///    .RunAsEmulator()
    ///    .AddQueue("queue");
    ///
    /// builder.AddProject&lt;Projects.InventoryService&gt;()
    ///        .WithReference(serviceBus);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureServiceBusResource> RunAsEmulator(this IResourceBuilder<AzureServiceBusResource> builder, Action<IResourceBuilder<AzureServiceBusEmulatorResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // Add emulator container
        var configHostFile = Path.GetTempFileName();
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(configHostFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
        }

        builder
            .WithEndpoint(name: "emulator", targetPort: 5672)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = ServiceBusEmulatorContainerImageTags.Registry,
                Image = ServiceBusEmulatorContainerImageTags.Image,
                Tag = ServiceBusEmulatorContainerImageTags.Tag
            })
            .WithAnnotation(new ContainerMountAnnotation(
                configHostFile,
                AzureServiceBusEmulatorResource.EmulatorConfigJsonPath,
                ContainerMountType.BindMount,
                isReadOnly: false));

        // Create a separate storage emulator for the Event Hub one
        var storageResource = builder.ApplicationBuilder
                .AddAzureStorage($"{builder.Resource.Name}-storage")
                .RunAsEmulator();

        var storage = storageResource.Resource;

        builder.WithAnnotation(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
        {
            var blobEndpoint = storage.GetEndpoint("blob");
            var tableEndpoint = storage.GetEndpoint("table");

            context.EnvironmentVariables.Add("ACCEPT_EULA", "Y");
            context.EnvironmentVariables.Add("BLOB_SERVER", $"{blobEndpoint.Resource.Name}:{blobEndpoint.TargetPort}");
            context.EnvironmentVariables.Add("METADATA_SERVER", $"{tableEndpoint.Resource.Name}:{tableEndpoint.TargetPort}");
        }));

        ServiceBusClient? client = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(builder.Resource, async (@event, ct) =>
        {
            var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false)
                        ?? throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");

            // For the purposes of the health check we only need to know a queue name. If we don't have a queue
            // name we can't configure a valid producer client connection so we should throw. What good is
            // an queue namespace without a queue? :)
            if (builder.Resource.Queues is { Count: > 0 } && builder.Resource.Queues[0] is string hub)
            {
                var healthCheckConnectionString = $"{connectionString};EntityPath={hub};";
                client = new ServiceBusClient(healthCheckConnectionString);
            }
            else
            {
                throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource does not have any Event Hubs.");
            }
        });

        var healthCheckKey = $"{builder.Resource.Name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureServiceBusQueue(
            sp => connectionString ?? throw new DistributedApplicationException("EventHubProducerClient is not initialized"),
            healthCheckKey
            );

        builder.WithHealthCheck(healthCheckKey);

        if (configureContainer != null)
        {
            var surrogate = new AzureServiceBusEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            var serviceBusEmulatorResources = builder.ApplicationBuilder.Resources.OfType<AzureServiceBusResource>().Where(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);

            if (!serviceBusEmulatorResources.Any())
            {
                // No-op if there is no Azure Service Bus emulator resource.
                return Task.CompletedTask;
            }

            foreach (var emulatorResource in serviceBusEmulatorResources)
            {
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);

                using var stream = new FileStream(configFileMount.Source!, FileMode.Create);
                using var writer = new Utf8JsonWriter(stream);

                writer.WriteStartObject();                      // {
                writer.WriteStartObject("UserConfig");          //   "UserConfig": {
                writer.WriteStartArray("NamespaceConfig");      //     "NamespaceConfig": [
                writer.WriteStartObject();                      //       {
                writer.WriteString("Type", "EventHub");         //         "Type": "EventHub",

                // This name is currently required by the emulator
                writer.WriteString("Name", "emulatorNs1");      //         "Name": "emulatorNs1"
                writer.WriteStartArray("Entities");             //         "Entities": [

                foreach (var hub in emulatorResource.Hubs)
                {
                    // The default consumer group ('$default') is automatically created

                    writer.WriteStartObject();                  //           {
                    writer.WriteString("Name", hub);            //             "Name": "hub",
                    writer.WriteString("PartitionCount", "2");  //             "PartitionCount": "2",
                    writer.WriteStartArray("ConsumerGroups");   //             "ConsumerGroups": [
                    writer.WriteEndArray();                     //             ]
                    writer.WriteEndObject();                    //           }
                }

                writer.WriteEndArray();                         //         ] (/Entities)
                writer.WriteEndObject();                        //       }
                writer.WriteEndArray();                         //     ], (/NamespaceConfig)
                writer.WriteStartObject("LoggingConfig");       //     "LoggingConfig": {
                writer.WriteString("Type", "File");             //       "Type": "File"
                writer.WriteEndObject();                        //     } (/LoggingConfig)

                writer.WriteEndObject();                        //   } (/UserConfig)
                writer.WriteEndObject();                        // } (/Root)

            }

            return Task.CompletedTask;

        });

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="path">Relative path to the AppHost where emulator storage is persisted between runs. Defaults to the path '.servicebus/{builder.Resource.Name}'</param>
    /// <returns>A builder for the <see cref="AzureServiceBusEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithDataBindMount(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string? path = null)
        => builder.WithBindMount(path ?? $".servicebus/{builder.Resource.Name}", "/data", isReadOnly: false);

    /// <summary>
    /// Adds a named volume for the data folder to an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>A builder for the <see cref="AzureServiceBusEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithDataVolume(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string? name = null)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/data", isReadOnly: false);

    /// <summary>
    /// Configures the gateway port for the Azure Service Bus emulator.
    /// </summary>
    /// <param name="builder">Builder for the Azure Service Bus emulator container</param>
    /// <param name="port">Host port to bind to the emulator gateway port.</param>
    /// <returns>Azure Service Bus emulator resource builder.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithGatewayPort(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}
