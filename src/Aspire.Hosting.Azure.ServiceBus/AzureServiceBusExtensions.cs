// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.ServiceBus;

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
                queue.Parent = serviceBusNamespace;
                infrastructure.Add(queue);
            }
            var topicDictionary = new Dictionary<string, ServiceBusTopic>();
            foreach (var topic in azureResource.Topics)
            {
                topic.Parent = serviceBusNamespace;
                infrastructure.Add(topic);

                // Topics are added in the dictionary with their normalized names.
                topicDictionary.Add(topic.IdentifierName, topic);
            }
            foreach (var subscription in azureResource.Subscriptions)
            {
                var topic = topicDictionary[subscription.TopicName];
                subscription.Subscription.Parent = topic;
                infrastructure.Add(subscription.Subscription);
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
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusTopic"/>.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, string[] subscriptions, Action<ServiceBusTopic>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeIdentifierName(name);
        var topic = new ServiceBusTopic(normalizedTopicName) { Name = name };

        configure?.Invoke(topic);

        builder.Resource.Topics.Add(topic);
        foreach (var subscription in subscriptions)
        {
            builder.Resource.Subscriptions.Add((normalizedTopicName, new ServiceBusSubscription(Infrastructure.NormalizeIdentifierName(subscription)) { Name = subscription }));
        }
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusQueue"/>.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusQueue>? configure = null)
    {
        var queue = new ServiceBusQueue(Infrastructure.NormalizeIdentifierName(name)) { Name = name };

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
        var topic = new ServiceBusTopic(Infrastructure.NormalizeIdentifierName(name)) { Name = name };

        builder.Resource.Topics.Add(topic);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="ServiceBusTopic"/>.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusTopic> configure)
    {
        var topic = new ServiceBusTopic(Infrastructure.NormalizeIdentifierName(name)) { Name = name };
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
    public static IResourceBuilder<AzureServiceBusResource> AddSubscription(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName, Action<ServiceBusSubscription>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeIdentifierName(topicName);
        var sub = new ServiceBusSubscription(normalizedTopicName) { Name = subscriptionName };
        configure?.Invoke(sub);
        builder.Resource.Subscriptions.Add((normalizedTopicName, sub));
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
    public static IResourceBuilder<AzureServiceBusResource> AddRule(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName, string ruleName, Action<ServiceBusRule>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeIdentifierName(topicName);
        var normalizedSubscriptionName = Infrastructure.NormalizeIdentifierName(subscriptionName);
        var normalizedRuleName = Infrastructure.NormalizeIdentifierName(ruleName);

        var rule = new ServiceBusRule(normalizedRuleName) { Name = ruleName };
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

        //ServiceBusClient? client = null;

        //builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(builder.Resource, async (@event, ct) =>
        //{
        //    var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false)
        //                ?? throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");

        //    // For the purposes of the health check we only need to know a queue name. If we don't have a queue
        //    // name we can't configure a valid producer client connection so we should throw. What good is
        //    // an queue namespace without a queue? :)
        //    if (builder.Resource.Queues is { Count: > 0 } && builder.Resource.Queues[0] is string hub)
        //    {
        //        var healthCheckConnectionString = $"{connectionString};EntityPath={hub};";
        //        client = new ServiceBusClient(healthCheckConnectionString);
        //    }
        //    else
        //    {
        //        throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource does not have any Event Hubs.");
        //    }
        //});

        //var healthCheckKey = $"{builder.Resource.Name}_check";
        //builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureServiceBusQueue(
        //    sp => connectionString ?? throw new DistributedApplicationException("EventHubProducerClient is not initialized"),
        //    healthCheckKey
        //    );

        //builder.WithHealthCheck(healthCheckKey);

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
                writer.WriteStartArray("Namespaces");           //     "Namespaces": [
                writer.WriteStartObject();                      //       {
                // Is this name is currently required by the emulator like it is for EventHub?
                writer.WriteString("Name", "sbemulatorns");     //         "Name": "sbemulatorns"
                writer.WriteStartArray("Queues");               //         "Queues": [

                foreach (var queue in emulatorResource.Queues)
                {
                    writer.WriteStartObject();                  //           {
                    if (queue.Name.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.Name), queue.Name.Value);
                    }
                    writer.WriteStartObject("Properties");      //             "Properties": {
                    if (queue.AutoDeleteOnIdle.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.AutoDeleteOnIdle), XmlConvert.ToString(queue.AutoDeleteOnIdle.Value));
                    }
                    if (queue.DeadLetteringOnMessageExpiration.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.DeadLetteringOnMessageExpiration), queue.DeadLetteringOnMessageExpiration.Value);
                    }
                    if (queue.DefaultMessageTimeToLive.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.DefaultMessageTimeToLive), XmlConvert.ToString(queue.DefaultMessageTimeToLive.Value));
                    }
                    if (queue.DuplicateDetectionHistoryTimeWindow.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(queue.DuplicateDetectionHistoryTimeWindow.Value));
                    }
                    if (queue.EnableBatchedOperations.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.EnableBatchedOperations), queue.EnableBatchedOperations.Value);
                    }
                    if (queue.EnableExpress.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.EnableExpress), queue.EnableExpress.Value);
                    }
                    if (queue.EnablePartitioning.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.EnablePartitioning), queue.EnablePartitioning.Value);
                    }
                    if (queue.ForwardDeadLetteredMessagesTo.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.ForwardDeadLetteredMessagesTo), queue.ForwardDeadLetteredMessagesTo.Value);
                    }
                    if (queue.ForwardTo.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.ForwardTo), queue.ForwardTo.Value);
                    }
                    if (queue.LockDuration.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusQueue.LockDuration), XmlConvert.ToString(queue.LockDuration.Value));
                    }
                    if (queue.MaxDeliveryCount.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteNumber(nameof(ServiceBusQueue.MaxDeliveryCount), queue.MaxDeliveryCount.Value);
                    }
                    if (queue.MaxMessageSizeInKilobytes.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteNumber(nameof(ServiceBusQueue.MaxMessageSizeInKilobytes), queue.MaxMessageSizeInKilobytes.Value);
                    }
                    if (queue.MaxSizeInMegabytes.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteNumber(nameof(ServiceBusQueue.MaxSizeInMegabytes), queue.MaxSizeInMegabytes.Value);
                    }
                    if (queue.RequiresDuplicateDetection.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.RequiresDuplicateDetection), queue.RequiresDuplicateDetection.Value);
                    }
                    if (queue.RequiresSession.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.RequiresSession), queue.RequiresSession.Value);
                    }
                    if (queue.Status.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusTopic.Status), queue.Status.Value.ToString());
                    }
                    writer.WriteEndObject();                    //             } (/Properties)

                    writer.WriteEndObject();                    //           } (/Queue)
                }
                writer.WriteEndArray();                         //         ] (/Queues)

                writer.WriteStartArray("Topics");               //         "Topics": [
                foreach (var topic in emulatorResource.Topics)
                {
                    writer.WriteStartObject();                  //           {
                    if (topic.Name.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusTopic.Name), topic.Name.Value);
                    }
                    writer.WriteStartObject("Properties");      //             "Properties": {
                    if (topic.AutoDeleteOnIdle.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusTopic.AutoDeleteOnIdle), XmlConvert.ToString(topic.AutoDeleteOnIdle.Value));
                    }
                    if (topic.DefaultMessageTimeToLive.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusTopic.DefaultMessageTimeToLive), XmlConvert.ToString(topic.DefaultMessageTimeToLive.Value));
                    }
                    if (topic.DuplicateDetectionHistoryTimeWindow.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusTopic.DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(topic.DuplicateDetectionHistoryTimeWindow.Value));
                    }
                    if (topic.EnableBatchedOperations.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.EnableBatchedOperations), topic.EnableBatchedOperations.Value);
                    }
                    if (topic.EnableExpress.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.EnableExpress), topic.EnableExpress.Value);
                    }
                    if (topic.EnablePartitioning.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.EnablePartitioning), topic.EnablePartitioning.Value);
                    }
                    if (topic.MaxMessageSizeInKilobytes.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteNumber(nameof(ServiceBusTopic.MaxMessageSizeInKilobytes), topic.MaxMessageSizeInKilobytes.Value);
                    }
                    if (topic.MaxSizeInMegabytes.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteNumber(nameof(ServiceBusTopic.MaxSizeInMegabytes), topic.MaxSizeInMegabytes.Value);
                    }
                    if (topic.RequiresDuplicateDetection.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.RequiresDuplicateDetection), topic.RequiresDuplicateDetection.Value);
                    }
                    if (topic.Status.Kind != BicepValueKind.Unset)
                    {
                        writer.WriteString(nameof(ServiceBusTopic.Status), topic.Status.Value.ToString());
                    }
                    writer.WriteEndObject();                    //             } (/Properties)

                    writer.WriteStartArray("Subscriptions");      //             "Subscriptions": [
                    foreach (var entry in emulatorResource.Subscriptions)
                    {
                        if (entry.TopicName != topic.IdentifierName)
                        {
                            continue;
                        }

                        var sub = entry.Subscription;

                        writer.WriteStartObject();                  //           {
                        if (topic.Name.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusQueue.Name), sub.Name.Value);
                        }

                        writer.WriteStartObject("Properties");      //             "Properties": {
                        if (sub.Status.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.AutoDeleteOnIdle), XmlConvert.ToString(sub.AutoDeleteOnIdle.Value));
                        }

                        if (sub.ClientAffineProperties.Kind != BicepValueKind.Unset && sub.ClientAffineProperties.Value != null)
                        {
                            writer.WriteStartObject("ClientAffineProperties");      //             "ClientAffineProperties": {

                            if (sub.ClientAffineProperties.Value.ClientId.Kind != BicepValueKind.Unset)
                            {
                                writer.WriteString(nameof(ServiceBusClientAffineProperties.ClientId), sub.ClientAffineProperties.Value.ClientId.Value);
                            }
                            if (sub.ClientAffineProperties.Value.IsDurable.Kind != BicepValueKind.Unset)
                            {
                                writer.WriteBoolean(nameof(ServiceBusClientAffineProperties.IsDurable), sub.ClientAffineProperties.Value.IsDurable.Value);
                            }
                            if (sub.ClientAffineProperties.Value.IsShared.Kind != BicepValueKind.Unset)
                            {
                                writer.WriteBoolean(nameof(ServiceBusClientAffineProperties.IsShared), sub.ClientAffineProperties.Value.IsShared.Value);
                            }

                            writer.WriteEndObject();                    //            } (/ClientAffineProperties)
                        }

                        if (sub.DeadLetteringOnFilterEvaluationExceptions.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.DeadLetteringOnFilterEvaluationExceptions), sub.DeadLetteringOnFilterEvaluationExceptions.Value);
                        }
                        if (sub.DeadLetteringOnMessageExpiration.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.DeadLetteringOnMessageExpiration), sub.DeadLetteringOnMessageExpiration.Value);
                        }
                        if (sub.DefaultMessageTimeToLive.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.DefaultMessageTimeToLive), XmlConvert.ToString(sub.DefaultMessageTimeToLive.Value));
                        }
                        if (sub.DuplicateDetectionHistoryTimeWindow.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(sub.DuplicateDetectionHistoryTimeWindow.Value));
                        }
                        if (sub.EnableBatchedOperations.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.EnableBatchedOperations), sub.EnableBatchedOperations.Value);
                        }
                        if (sub.ForwardDeadLetteredMessagesTo.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.ForwardDeadLetteredMessagesTo), sub.ForwardDeadLetteredMessagesTo.Value);
                        }
                        if (sub.ForwardTo.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusQueue.ForwardTo), sub.ForwardTo.Value);
                        }
                        if (sub.IsClientAffine.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.IsClientAffine), sub.IsClientAffine.Value);
                        }
                        if (sub.LockDuration.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.LockDuration), XmlConvert.ToString(sub.LockDuration.Value));
                        }
                        if (sub.MaxDeliveryCount.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteNumber(nameof(ServiceBusSubscription.MaxDeliveryCount), sub.MaxDeliveryCount.Value);
                        }
                        if (sub.RequiresSession.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.RequiresSession), sub.RequiresSession.Value);
                        }
                        if (sub.Status.Kind != BicepValueKind.Unset)
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.Status), sub.Status.Value.ToString());
                        }
                        writer.WriteEndObject();                    //             } (/Properties)

                        #region Rules
                        writer.WriteStartArray("Rules");      //             "Rules": [
                        foreach (var ruleEntry in emulatorResource.Rules)
                        {
                            if (ruleEntry.TopicName != topic.IdentifierName && ruleEntry.SubscriptionName != sub.IdentifierName)
                            {
                                continue;
                            }

                            var rule = ruleEntry.Rule;

                            writer.WriteStartObject();                  //           {
                            if (rule.Name.Kind != BicepValueKind.Unset)
                            {
                                writer.WriteString(nameof(ServiceBusQueue.Name), rule.Name.Value);
                            }
                            writer.WriteStartObject("Properties");      //             "Properties": {

                            if (rule.Action.Kind != BicepValueKind.Unset && rule.Action.Value != null)
                            {
                                writer.WriteStartObject(nameof(ServiceBusRule.Action));
                                if (rule.Action.Value.SqlExpression.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusFilterAction.SqlExpression), rule.Action.Value.SqlExpression.Value);
                                }
                                if (rule.Action.Value.CompatibilityLevel.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteNumber(nameof(ServiceBusFilterAction.CompatibilityLevel), rule.Action.Value.CompatibilityLevel.Value);
                                }
                                if (rule.Action.Value.RequiresPreprocessing.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteBoolean(nameof(ServiceBusFilterAction.RequiresPreprocessing), rule.Action.Value.RequiresPreprocessing.Value);
                                }
                                writer.WriteEndObject();
                            }

                            if (rule.CorrelationFilter.Kind != BicepValueKind.Unset && rule.CorrelationFilter.Value != null)
                            {
                                writer.WriteStartObject(nameof(ServiceBusRule.CorrelationFilter));

                                if (rule.CorrelationFilter.Value.ApplicationProperties.Kind != BicepValueKind.Unset && rule.CorrelationFilter.Value.ApplicationProperties != null)
                                {
                                    var dic = new Dictionary<string, object>();
                                    
                                    foreach (var applicationProperty in rule.CorrelationFilter.Value.ApplicationProperties)
                                    {
                                        dic.Add(applicationProperty.Key, applicationProperty.Value);
                                    }

                                    JsonSerializer.Serialize(writer, dic);
                                }
                                if (rule.CorrelationFilter.Value.CorrelationId.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.CorrelationId), rule.CorrelationFilter.Value.CorrelationId.Value);
                                }
                                if (rule.CorrelationFilter.Value.MessageId.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.MessageId), rule.CorrelationFilter.Value.MessageId.Value);
                                }
                                if (rule.CorrelationFilter.Value.SendTo.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.SendTo), rule.CorrelationFilter.Value.SendTo.Value);
                                }
                                if (rule.CorrelationFilter.Value.ReplyTo.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyTo), rule.CorrelationFilter.Value.ReplyTo.Value);
                                }
                                if (rule.CorrelationFilter.Value.Subject.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.Subject), rule.CorrelationFilter.Value.Subject.Value);
                                }
                                if (rule.CorrelationFilter.Value.SessionId.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.SessionId), rule.CorrelationFilter.Value.SessionId.Value);
                                }
                                if (rule.CorrelationFilter.Value.ReplyToSessionId.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyToSessionId), rule.CorrelationFilter.Value.ReplyToSessionId.Value);
                                }
                                if (rule.CorrelationFilter.Value.ContentType.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.ContentType), rule.CorrelationFilter.Value.ContentType.Value);
                                }
                                if (rule.CorrelationFilter.Value.RequiresPreprocessing.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteBoolean(nameof(ServiceBusCorrelationFilter.RequiresPreprocessing), rule.CorrelationFilter.Value.RequiresPreprocessing.Value);
                                }

                                writer.WriteEndObject();
                            }

                            if (rule.FilterType.Kind != BicepValueKind.Unset)
                            {
                                writer.WriteString(nameof(ServiceBusRule.FilterType), rule.FilterType.Value.ToString());
                            }

                            if (rule.SqlFilter.Kind != BicepValueKind.Unset && rule.SqlFilter.Value != null)
                            {
                                writer.WriteStartObject(nameof(ServiceBusRule.SqlFilter));
                                if (rule.SqlFilter.Value.SqlExpression.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteString(nameof(ServiceBusSqlFilter.SqlExpression), rule.SqlFilter.Value.SqlExpression.Value);
                                }
                                if (rule.SqlFilter.Value.CompatibilityLevel.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteNumber(nameof(ServiceBusSqlFilter.CompatibilityLevel), rule.SqlFilter.Value.CompatibilityLevel.Value);
                                }
                                if (rule.SqlFilter.Value.RequiresPreprocessing.Kind != BicepValueKind.Unset)
                                {
                                    writer.WriteBoolean(nameof(ServiceBusSqlFilter.RequiresPreprocessing), rule.SqlFilter.Value.RequiresPreprocessing.Value);
                                }
                                writer.WriteEndObject();
                            }

                            writer.WriteEndObject();                    //           } (/Rule)
                        }

                        writer.WriteEndArray();
                        #endregion Rules

                        writer.WriteEndObject();                    //           } (/Subscription)
                    }

                    writer.WriteEndArray();                    //             ] (/Subscriptions)

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
