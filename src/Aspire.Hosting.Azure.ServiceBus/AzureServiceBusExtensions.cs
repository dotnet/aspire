// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
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
            AzureProvisioning.ServiceBusNamespace serviceBusNamespace = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = AzureProvisioning.ServiceBusNamespace.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    var skuParameter = new ProvisioningParameter("sku", typeof(string))
                    {
                        Value = "Standard"
                    };
                    infrastructure.Add(skuParameter);
                    var resource = new AzureProvisioning.ServiceBusNamespace(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        Sku = new AzureProvisioning.ServiceBusSku()
                        {
                            Name = skuParameter
                        },
                        DisableLocalAuth = true,
                        Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                    };
                    return resource;
                });

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
        if (builder.Resource.IsEmulator)
        {
            throw new InvalidOperationException("The Azure Service Bus resource is already configured to run as an emulator.");
        }

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // Add emulator container

        // The password must be at least 8 characters long and contain characters from three of the following four sets: Uppercase letters, Lowercase letters, Base 10 digits, and Symbols
        var passwordParameter = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder.ApplicationBuilder, $"{builder.Resource.Name}-sql-pwd", minLower: 1, minUpper: 1, minNumeric: 1);

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
                .WithEnvironment(context =>
                {
                    context.EnvironmentVariables["MSSQL_SA_PASSWORD"] = passwordParameter;
                })
                .WithParentRelationship(builder.Resource);

        builder.WithAnnotation(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
        {
            var sqlEndpoint = sqlEdgeResource.Resource.GetEndpoint("tcp");

            context.EnvironmentVariables.Add("ACCEPT_EULA", "Y");
            context.EnvironmentVariables.Add("SQL_SERVER", $"{sqlEndpoint.Resource.Name}:{sqlEndpoint.TargetPort}");
            context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", passwordParameter);
        }));

        var lifetime = ContainerLifetime.Session;

        if (configureContainer != null)
        {
            var surrogate = new AzureServiceBusEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);

            if (surrogate.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var lifetimeAnnotation))
            {
                lifetime = lifetimeAnnotation.Lifetime;
            }
        }

        sqlEdgeResource = sqlEdgeResource.WithLifetime(lifetime);

        // RunAsEmulator() can be followed by custom model configuration so we need to delay the creation of the Config.json file
        // until all resources are about to be prepared and annotations can't be updated anymore.

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
        {
            // Create JSON configuration file

            var hasCustomConfigJson = builder.Resource.Annotations.OfType<ContainerMountAnnotation>().Any(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);

            if (hasCustomConfigJson)
            {
                return Task.CompletedTask;
            }

            // Create Config.json file content and its alterations in a temporary file
            var tempConfigFile = WriteEmulatorConfigJson(builder.Resource);

            try
            {
                // Apply ConfigJsonAnnotation modifications
                var configJsonAnnotations = builder.Resource.Annotations.OfType<ConfigJsonAnnotation>();

                if (configJsonAnnotations.Any())
                {
                    using var readStream = new FileStream(tempConfigFile, FileMode.Open, FileAccess.Read);
                    var jsonObject = JsonNode.Parse(readStream);
                    readStream.Close();

                    if (jsonObject == null)
                    {
                        throw new InvalidOperationException("The configuration file mount could not be parsed.");
                    }

                    foreach (var annotation in configJsonAnnotations)
                    {

                        annotation.Configure(jsonObject);
                    }

                    using var writeStream = new FileStream(tempConfigFile, FileMode.Open, FileAccess.Write);
                    using var writer = new Utf8JsonWriter(writeStream, new JsonWriterOptions { Indented = true });
                    jsonObject.WriteTo(writer);
                }

                var aspireStore = builder.ApplicationBuilder.CreateStore();

                // Deterministic file path for the configuration file based on its content
                var configJsonPath = aspireStore.GetFileNameWithContent($"{builder.Resource.Name}-Config.json", tempConfigFile);

                builder.WithAnnotation(new ContainerMountAnnotation(
                    configJsonPath,
                    AzureServiceBusEmulatorResource.EmulatorConfigJsonPath,
                    ContainerMountType.BindMount,
                    isReadOnly: true));
            }
            finally
            {
                try
                {
                    File.Delete(tempConfigFile);
                }
                catch
                {
                }
            }

            return Task.CompletedTask;
        });

        ServiceBusClient? serviceBusClient = null;
        string? queueOrTopicName = null;

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, async (@event, ct) =>
        {
            var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");
            }

            // Retrieve a queue/topic name to configure the health check

            var noRetryOptions = new ServiceBusClientOptions { RetryOptions = new ServiceBusRetryOptions { MaxRetries = 0 } };
            serviceBusClient = new ServiceBusClient(connectionString, noRetryOptions);

            queueOrTopicName =
                builder.Resource.Queues.Select(x => x.Name).FirstOrDefault()
                ?? builder.Resource.Topics.Select(x => x.Name).FirstOrDefault();
        });

        var healthCheckKey = $"{builder.Resource.Name}_check";

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
    /// <example>
    /// Here is an example of how to configure the emulator to use a different logging mechanism:
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddAzureServiceBus("servicebusns")
    ///        .RunAsEmulator(configure => configure
    ///            .WithConfiguration(document =>
    ///            {
    ///                document["UserConfig"]!["Logging"] = new JsonObject { ["Type"] = "Console" };
    ///            });
    ///        );
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithConfiguration(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, Action<JsonNode> configJson)
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

    private static string WriteEmulatorConfigJson(AzureServiceBusResource emulatorResource)
    {
        var filePath = Path.GetTempFileName();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write);
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

        return filePath;
    }
}
