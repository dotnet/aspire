// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AzureProvisioning = Azure.Provisioning.ServiceBus;

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
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = static (AzureResourceInfrastructure infrastructure) =>
        {
            var skuParameter = new ProvisioningParameter("sku", typeof(string))
            {
                Value = "Standard"
            };
            infrastructure.Add(skuParameter);

            var serviceBusNamespace = new AzureProvisioning.ServiceBusNamespace(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Sku = new AzureProvisioning.ServiceBusSku()
                {
                    Name = skuParameter
                },
                DisableLocalAuth = true,
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
            infrastructure.Add(serviceBusNamespace);

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            infrastructure.Add(principalTypeParameter);
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(principalIdParameter);

            infrastructure.Add(serviceBusNamespace.CreateRoleAssignment(AzureProvisioning.ServiceBusBuiltInRole.AzureServiceBusDataOwner, principalTypeParameter, principalIdParameter));

            infrastructure.Add(new ProvisioningOutput("serviceBusEndpoint", typeof(string)) { Value = serviceBusNamespace.ServiceBusEndpoint });

            var azureResource = (AzureServiceBusResource)infrastructure.AspireResource;

            foreach (var queue in azureResource.Queues)
            {
                var cdkQueue = queue.ToProvisioningEntity();
                cdkQueue.Parent = serviceBusNamespace;
                infrastructure.Add(cdkQueue);
            }

            foreach (var topic in azureResource.Topics)
            {
                var cdkTopic = topic.ToProvisioningEntity();
                cdkTopic.Parent = serviceBusNamespace;
                infrastructure.Add(cdkTopic);

                foreach (var subscription in topic.Subscriptions)
                {
                    var cdkSubscription = subscription.ToProvisioningEntity();
                    cdkSubscription.Parent = cdkTopic;
                    infrastructure.Add(cdkSubscription);

                    foreach (var rule in subscription.Rules)
                    {
                        var cdkRule = rule.ToProvisioningEntity();
                        cdkRule.Parent = cdkSubscription;
                        infrastructure.Add(cdkRule);
                    }
                }
            }
        };

        var resource = new AzureServiceBusResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(WithQueue)} instead to add an Azure Service Bus Queue.")]
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name)
    {
        return builder.WithQueue(name);
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusQueue"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> WithQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusQueue>? configure = null)
    {
        var queue = builder.Resource.Queues.FirstOrDefault(x => x.Name == name);

        if (queue == null)
        {
            queue = new ServiceBusQueue(name);
            builder.Resource.Queues.Add(queue);
        }

        configure?.Invoke(queue);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(WithTopic)} instead to add an Azure Service Bus Topic.")]
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name)
    {
        return builder.WithTopic(name);
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(WithTopic)} instead to add an Azure Service Bus Topic and Subscriptions.")]
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, string[] subscriptions)
    {
        return builder.WithTopic(name, topic =>
        {
            foreach (var subscription in subscriptions)
            {
                if (!topic.Subscriptions.Any(x => x.Name == subscription))
                {
                    topic.Subscriptions.Add(new ServiceBusSubscription(subscription));
                }
            }
        });
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusTopic"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> WithTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusTopic>? configure = null)
    {
        var topic = builder.Resource.Topics.FirstOrDefault(x => x.Name == name);

        if (topic == null)
        {
            topic = new ServiceBusTopic(name);
            builder.Resource.Topics.Add(topic);
        }

        configure?.Invoke(topic);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Subscription resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(WithTopic)} instead to add an Azure Service Bus Subscription to a Topic.")]
    public static IResourceBuilder<AzureServiceBusResource> AddSubscription(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName)
    {
        builder.WithTopic(topicName, topic =>
        {
            if (!topic.Subscriptions.Any(x => x.Name == subscriptionName))
            {
                topic.Subscriptions.Add(new ServiceBusSubscription(subscriptionName));
            }
        });

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

        // Create a default file mount. This could be replaced by a user-provided file mount.
        var configHostFile = Path.Combine(Directory.CreateTempSubdirectory("AspireServiceBusEmulator").FullName, "Config.json");

        var defaultConfigFileMount = new ContainerMountAnnotation(
                configHostFile,
                AzureServiceBusEmulatorResource.EmulatorConfigJsonPath,
                ContainerMountType.BindMount,
                isReadOnly: true);

        builder.WithAnnotation(defaultConfigFileMount);

        // Add emulator container

        var password = PasswordGenerator.Generate(16, true, true, true, true, 0, 0, 0, 0);

        builder
            .WithEndpoint(name: "emulator", targetPort: 5672)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = ServiceBusEmulatorContainerImageTags.Registry,
                Image = ServiceBusEmulatorContainerImageTags.Image,
                Tag = ServiceBusEmulatorContainerImageTags.Tag
            });

        var sqlEdgeResource = builder.ApplicationBuilder
                .AddContainer($"{builder.Resource.Name}-sqledge",
                    image: ServiceBusEmulatorContainerImageTags.AzureSqlEdgeImage,
                    tag: ServiceBusEmulatorContainerImageTags.AzureSqlEdgeTag)
                .WithImageRegistry(ServiceBusEmulatorContainerImageTags.AzureSqlEdgeRegistry)
                .WithEndpoint(targetPort: 1433, name: "tcp")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_SA_PASSWORD", password);

        builder.WithAnnotation(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
        {
            var sqlEndpoint = sqlEdgeResource.Resource.GetEndpoint("tcp");

            context.EnvironmentVariables.Add("ACCEPT_EULA", "Y");
            context.EnvironmentVariables.Add("SQL_SERVER", $"{sqlEndpoint.Resource.Name}:{sqlEndpoint.TargetPort}");
            context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", password);
        }));

        ServiceBusClient? serviceBusClient = null;
        string? queueOrTopicName = null;

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, async (@event, ct) =>
        {
            var serviceBusEmulatorResources = builder.ApplicationBuilder.Resources.OfType<AzureServiceBusResource>().Where(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);

            if (!serviceBusEmulatorResources.Any())
            {
                // No-op if there is no Azure Service Bus emulator resource.
                return;
            }

            var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");
            }

            // Retrieve a queue/topic name to configure the health check

            var noRetryOptions = new ServiceBusClientOptions { RetryOptions = new ServiceBusRetryOptions { MaxRetries = 0 } };
            serviceBusClient = new ServiceBusClient(connectionString, noRetryOptions);

            queueOrTopicName =
                serviceBusEmulatorResources.SelectMany(x => x.Queues).Select(x => x.Name).FirstOrDefault()
                ?? serviceBusEmulatorResources.SelectMany(x => x.Topics).Select(x => x.Name).FirstOrDefault();

            // Create JSON configuration file

            foreach (var emulatorResource in serviceBusEmulatorResources)
            {
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);

                // If there is a custom mount for EmulatorConfigJsonPath we don't need to create the Config.json file.
                if (configFileMount != defaultConfigFileMount)
                {
                    continue;
                }

                var fileStreamOptions = new FileStreamOptions() { Mode = FileMode.Create, Access = FileAccess.Write };

                if (!OperatingSystem.IsWindows())
                {
                    fileStreamOptions.UnixCreateMode =
                        UnixFileMode.UserRead | UnixFileMode.UserWrite
                        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;
                }

                using (var stream = new FileStream(configFileMount.Source!, fileStreamOptions))
                {
                    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                    writer.WriteStartObject();                      // {
                    writer.WriteStartObject("UserConfig");          //   "UserConfig": {
                    writer.WriteStartArray("Namespaces");           //     "Namespaces": [
                    writer.WriteStartObject();                      //       {
                    writer.WriteString("Name", emulatorResource.Name);
                    writer.WriteStartArray("Queues");               //         "Queues": [

                    foreach (var queue in emulatorResource.Queues)
                    {
                        writer.WriteStartObject();
                        queue.WriteJsonObjectProperties(writer);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();                         //         ] (/Queues)

                    writer.WriteStartArray("Topics");               //         "Topics": [
                    foreach (var topic in emulatorResource.Topics)
                    {
                        writer.WriteStartObject();                  //           "{ (Topic)"
                        topic.WriteJsonObjectProperties(writer);

                        writer.WriteStartArray("Subscriptions");    //             "Subscriptions": [
                        foreach (var subscription in topic.Subscriptions)
                        {
                            writer.WriteStartObject();              //               "{ (Subscription)"
                            subscription.WriteJsonObjectProperties(writer);

                            writer.WriteStartArray("Rules");        //                 "Rules": [
                            foreach (var rule in subscription.Rules)
                            {
                                writer.WriteStartObject();
                                rule.WriteJsonObjectProperties(writer);
                                writer.WriteEndObject();
                            }

                            writer.WriteEndArray();                 //                  ] (/Rules)

                            writer.WriteEndObject();                //               } (/Subscription)
                        }

                        writer.WriteEndArray();                     //             ] (/Subscriptions)

                        writer.WriteEndObject();                    //           } (/Topic)
                    }
                    writer.WriteEndArray();                         //         ] (/Topics)

                    writer.WriteEndObject();                        //       } (/Namespace)
                    writer.WriteEndArray();                         //     ], (/Namespaces)
                    writer.WriteStartObject("Logging");             //     "Logging": {
                    writer.WriteString("Type", "File");             //       "Type": "File"
                    writer.WriteEndObject();                        //     } (/LoggingConfig)

                    writer.WriteEndObject();                        //   } (/UserConfig)
                    writer.WriteEndObject();                        // } (/Root)
                }

                // Apply ConfigJsonAnnotation modifications
                var configJsonAnnotations = emulatorResource.Annotations.OfType<ConfigJsonAnnotation>();

                foreach (var annotation in configJsonAnnotations)
                {
                    using var readStream = new FileStream(configFileMount.Source!, FileMode.Open, FileAccess.Read);
                    var jsonObject = JsonNode.Parse(readStream);
                    readStream.Close();

                    using var writeStream = new FileStream(configFileMount.Source!, FileMode.Open, FileAccess.Write);
                    using var writer = new Utf8JsonWriter(writeStream, new JsonWriterOptions { Indented = true });

                    if (jsonObject == null)
                    {
                        throw new InvalidOperationException("The configuration file mount could not be parsed.");
                    }
                    annotation.Configure(jsonObject);
                    jsonObject.WriteTo(writer);
                }
            }
        });

        var healthCheckKey = $"{builder.Resource.Name}_check";

        if (configureContainer != null)
        {
            var surrogate = new AzureServiceBusEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        // To use the existing ServiceBus health check we would need to know if there is any queue or topic defined.
        // We can register a health check for a queue and then no-op if there are no queues. Same for topics.
        // If no queues or no topics are defined then the health check will be successful.

        builder.ApplicationBuilder.Services.AddHealthChecks()
          .Add(new HealthCheckRegistration(
              healthCheckKey,
              sp => new ServiceBusHealthCheck(
                  () => serviceBusClient ?? throw new DistributedApplicationException($"{nameof(serviceBusClient)} was not initialized."),
                  () => queueOrTopicName),
              failureStatus: default,
              tags: default,
              timeout: default));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the configuration file of an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="path">Path to the file on the AppHost where the emulator configuration is located.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithConfigurationFile(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string path)
    {
        // Update the existing mount
        var configFileMount = builder.Resource.Annotations.OfType<ContainerMountAnnotation>().LastOrDefault(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);
        if (configFileMount != null)
        {
            builder.Resource.Annotations.Remove(configFileMount);
        }

        return builder.WithBindMount(path, AzureServiceBusEmulatorResource.EmulatorConfigJsonPath, isReadOnly: true);
    }

    /// <summary>
    /// Alters the JSON configuration document used by the emulator.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="configJson">A callback to update the JSON object representation of the configuration.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> ConfigureEmulator(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, Action<JsonNode> configJson)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configJson);

        builder.WithAnnotation(new ConfigJsonAnnotation(configJson));

        return builder;
    }

    /// <summary>
    /// Configures the host port for the Azure Service Bus emulator is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">Builder for the Azure Service Bus emulator container</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used, a random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithHostPort(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}
