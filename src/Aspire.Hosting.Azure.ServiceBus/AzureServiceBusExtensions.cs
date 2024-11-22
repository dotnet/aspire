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
            var topicDictionary = new Dictionary<string, AzureProvisioning.ServiceBusTopic>();
            foreach (var topic in azureResource.Topics)
            {
                var cdkTopic = topic.ToProvisioningEntity();
                cdkTopic.Parent = serviceBusNamespace;
                infrastructure.Add(cdkTopic);

                // Topics are added in the dictionary with their normalized names.
                topicDictionary.Add(topic.Id, cdkTopic);
            }
            var subscriptionDictionary = new Dictionary<(string, string), AzureProvisioning.ServiceBusSubscription>();
            foreach (var (topicName, subscription) in azureResource.Subscriptions)
            {
                var cdkSubscription = subscription.ToProvisioningEntity();
                var topic = topicDictionary[topicName];
                cdkSubscription.Parent = topic;
                infrastructure.Add(cdkSubscription);

                // Subscriptions are added in the dictionary with their normalized names.
                subscriptionDictionary.Add((topicName, subscription.Id), cdkSubscription);
            }
            foreach (var (topicName, subscriptionName, rule) in azureResource.Rules)
            {
                var cdkRule = rule.ToProvisioningEntity();
                var subscription = subscriptionDictionary[(topicName, subscriptionName)];
                cdkRule.Parent = subscription;
                infrastructure.Add(cdkRule);
            }
        };

        var resource = new AzureServiceBusResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusTopic"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, string[] subscriptions, Action<ServiceBusTopic>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeBicepIdentifier(name);
        var topic = new ServiceBusTopic(normalizedTopicName, name);

        configure?.Invoke(topic);

        builder.Resource.Topics.Add(topic);
        foreach (var subscriptionName in subscriptions)
        {
            var subscription = new ServiceBusSubscription(Infrastructure.NormalizeBicepIdentifier(subscriptionName), subscriptionName);
            builder.Resource.Subscriptions.Add((normalizedTopicName, subscription));
        }
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusQueue"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusQueue>? configure = null)
    {
        var queue = new ServiceBusQueue(Infrastructure.NormalizeBicepIdentifier(name), name);

        configure?.Invoke(queue);

        builder.Resource.Queues.Add(queue);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name)
    {
        var topic = new ServiceBusTopic(Infrastructure.NormalizeBicepIdentifier(name), name);

        builder.Resource.Topics.Add(topic);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusTopic"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusTopic> configure)
    {
        var topic = new ServiceBusTopic(Infrastructure.NormalizeBicepIdentifier(name), name);
        configure?.Invoke(topic);

        builder.Resource.Topics.Add(topic);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Subscription resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusSubscription"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddSubscription(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName, Action<ServiceBusSubscription>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeBicepIdentifier(topicName);
        var normalizedSubscriptionName = Infrastructure.NormalizeBicepIdentifier(subscriptionName);

        var subscription = new ServiceBusSubscription(normalizedSubscriptionName, subscriptionName);
        configure?.Invoke(subscription);
        builder.Resource.Subscriptions.Add((normalizedTopicName, subscription));
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Rule resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="ruleName">The name of the rule</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusRule"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddRule(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName, string ruleName, Action<ServiceBusRule>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeBicepIdentifier(topicName);
        var normalizedSubscriptionName = Infrastructure.NormalizeBicepIdentifier(subscriptionName);
        var normalizedRuleName = Infrastructure.NormalizeBicepIdentifier(ruleName);

        var rule = new ServiceBusRule(normalizedRuleName, ruleName);
        configure?.Invoke(rule);

        builder.Resource.Rules.Add((normalizedTopicName, normalizedSubscriptionName, rule));
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
        var configHostFile = Path.Combine(Path.GetTempPath(), "AspireServiceBusEmulator", Path.GetRandomFileName() + ".json");

        Directory.CreateDirectory(Path.GetDirectoryName(configHostFile)!);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(configHostFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
        }

        var password = PasswordGenerator.Generate(16, true, true, true, true, 0, 0, 0, 0);

        var customMountAnnotation = new ContainerMountAnnotation(
                configHostFile,
                AzureServiceBusEmulatorResource.EmulatorConfigJsonPath,
                ContainerMountType.BindMount,
                isReadOnly: false);

        builder
            .WithEndpoint(name: "emulator", targetPort: 5672)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = ServiceBusEmulatorContainerImageTags.Registry,
                Image = ServiceBusEmulatorContainerImageTags.Image,
                Tag = ServiceBusEmulatorContainerImageTags.Tag
            })
            .WithAnnotation(customMountAnnotation);

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

        ServiceBusClient? client = null;
        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(builder.Resource, async (@event, ct) =>
        {
            connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");
            }

            client = new ServiceBusClient(connectionString);
        });

        var healthCheckKey = $"{builder.Resource.Name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureServiceBusQueue(connectionStringFactory: sp =>
        {
            return connectionString ?? throw new InvalidOperationException("ServiceBusClient is not initialized.");
        }, queueNameFactory: sp =>
        {
            var queueName = builder.Resource.Queues[0].Name;
            return queueName ?? throw new InvalidOperationException("Queue name is not initialized.");
        }, name: healthCheckKey);

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
                // A custom file mount with read-only access is used to mount the emulator configuration file. If it's not found, the read-write mount we defined on the container is used.
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().LastOrDefault(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);

                // If the latest mount for EmulatorConfigJsonPath is our custom one then we can generate it.
                if (configFileMount != customMountAnnotation)
                {
                    continue;
                }

                using var stream = new FileStream(configFileMount.Source!, FileMode.Create);
                using var writer = new Utf8JsonWriter(stream);

                writer.WriteStartObject();                      // {
                writer.WriteStartObject("UserConfig");          //   "UserConfig": {
                writer.WriteStartArray("Namespaces");           //     "Namespaces": [
                writer.WriteStartObject();                      //       {
                writer.WriteString("Name", "sbemulatorns");     //         "Name": "sbemulatorns"
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
                    foreach (var (topicName, subscription) in emulatorResource.Subscriptions)
                    {
                        if (topicName != topic.Id)
                        {
                            continue;
                        }
                        writer.WriteStartObject();              //               "{ (Subscription)"
                        subscription.WriteJsonObjectProperties(writer);

                        #region Rules
                        writer.WriteStartArray("Rules");        //                 "Rules": [
                        foreach (var (ruleTopicName, ruleSubscriptionName, rule) in emulatorResource.Rules)
                        {
                            if (ruleTopicName != topic.Id && ruleSubscriptionName != subscription.Id)
                            {
                                continue;
                            }

                            writer.WriteStartObject();
                            rule.WriteJsonObjectProperties(writer);
                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();                 //                  ] (/Rules)
                        #endregion Rules

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
            foreach (var emulatorResource in serviceBusEmulatorResources)
            {
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().LastOrDefault(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);

                // At this point there should be a mount for the Config.json file.
                if (configFileMount == null)
                {
                    throw new InvalidOperationException("The configuration file mount is not set.");
                }

                var configJsonAnnotations = emulatorResource.Annotations.OfType<ConfigJsonAnnotation>();

                foreach (var annotation in configJsonAnnotations)
                {
                    using var readStream = new FileStream(configFileMount.Source!, FileMode.Open, FileAccess.Read);
                    var jsonObject = JsonNode.Parse(readStream);
                    readStream.Close();

                    using var writeStream = new FileStream(configFileMount.Source!, FileMode.Open, FileAccess.Write);
                    using var writer = new Utf8JsonWriter(writeStream);

                    if (jsonObject == null)
                    {
                        throw new InvalidOperationException("The configuration file mount could not be parsed.");
                    }
                    annotation.Configure(jsonObject);
                    jsonObject.WriteTo(writer);
                }
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
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithDataBindMount(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string? path = null)
        => builder.WithBindMount(path ?? $".servicebus/{builder.Resource.Name}", "/data", isReadOnly: false);

    /// <summary>
    /// Adds a named volume for the data folder to an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithDataVolume(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string? name = null)
        => builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data", isReadOnly: false);

    /// <summary>
    /// Adds a bind mount for the configuration file of an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="path">Path to the file on the AppHost where the emulator configuration is located.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithConfigJson(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string path)
    => builder.WithBindMount(path, AzureServiceBusEmulatorResource.EmulatorConfigJsonPath, isReadOnly: false);

    /// <summary>
    /// Alters the JSON configuration document used by the emulator.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="configJson">A callback to update the JSON object representation of the configuration.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> ConfigureJson(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, Action<JsonNode> configJson)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configJson);

        builder.WithAnnotation(new ConfigJsonAnnotation(configJson));

        return builder;
    }

    /// <summary>
    /// Configures the gateway port for the Azure Service Bus emulator.
    /// </summary>
    /// <param name="builder">Builder for the Azure Service Bus emulator container</param>
    /// <param name="port">Host port to bind to the emulator gateway port.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithGatewayPort(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}
