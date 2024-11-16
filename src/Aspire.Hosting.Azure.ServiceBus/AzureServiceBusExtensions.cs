// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Provisioning;
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
                Value = "Standard"
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
                topicDictionary.Add(topic.BicepIdentifier, topic);
            }
            var subscriptionDictionary = new Dictionary<(string, string), ServiceBusSubscription>();
            foreach (var (topicName, subscription) in azureResource.Subscriptions)
            {
                var topic = topicDictionary[topicName];
                subscription.Parent = topic;
                infrastructure.Add(subscription);

                // Subscriptions are added in the dictionary with their normalized names.
                subscriptionDictionary.Add((topicName, subscription.BicepIdentifier), subscription);
            }
            foreach (var (topicName, subscriptionName, rule) in azureResource.Rules)
            {
                var subscription = subscriptionDictionary[(topicName, subscriptionName)];
                rule.Parent = subscription;
                infrastructure.Add(rule);
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
        var normalizedTopicName = Infrastructure.NormalizeBicepIdentifier(name);
        var topic = new ServiceBusTopic(normalizedTopicName) { Name = name };

        configure?.Invoke(topic);

        builder.Resource.Topics.Add(topic);
        foreach (var subscriptionName in subscriptions)
        {
            var subscription = new ServiceBusSubscription(Infrastructure.NormalizeBicepIdentifier(subscriptionName)) { Name = subscriptionName };
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
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, [ResourceName] string name, Action<ServiceBusQueue>? configure = null)
    {
        var queue = new ServiceBusQueue(Infrastructure.NormalizeBicepIdentifier(name)) { Name = name };

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
        var topic = new ServiceBusTopic(Infrastructure.NormalizeBicepIdentifier(name)) { Name = name };

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
        var topic = new ServiceBusTopic(Infrastructure.NormalizeBicepIdentifier(name)) { Name = name };
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
        var normalizedTopicName = Infrastructure.NormalizeBicepIdentifier(topicName);
        var normalizedSubscriptionName = Infrastructure.NormalizeBicepIdentifier(subscriptionName);

        var subscription = new ServiceBusSubscription(normalizedSubscriptionName) { Name = subscriptionName };
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
    public static IResourceBuilder<AzureServiceBusResource> AddRule(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName, string ruleName, Action<ServiceBusRule>? configure = null)
    {
        var normalizedTopicName = Infrastructure.NormalizeBicepIdentifier(topicName);
        var normalizedSubscriptionName = Infrastructure.NormalizeBicepIdentifier(subscriptionName);
        var normalizedRuleName = Infrastructure.NormalizeBicepIdentifier(ruleName);

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
            var queueName = builder.Resource.Queues[0].Name.Value?.ToString();
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
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().FirstOrDefault(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath && v.IsReadOnly)
                    ?? emulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == AzureServiceBusEmulatorResource.EmulatorConfigJsonPath);

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
                    writer.WriteStartObject();                  //           {
                    if (((IBicepValue)queue.Name).IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.Name), queue.Name.Value);
                    }
                    writer.WriteStartObject("Properties");      //             "Properties": {
                    if (queue.AutoDeleteOnIdle.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.AutoDeleteOnIdle), XmlConvert.ToString(queue.AutoDeleteOnIdle.Value));
                    }
                    if (queue.DeadLetteringOnMessageExpiration.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.DeadLetteringOnMessageExpiration), queue.DeadLetteringOnMessageExpiration.Value);
                    }
                    if (queue.DefaultMessageTimeToLive.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.DefaultMessageTimeToLive), XmlConvert.ToString(queue.DefaultMessageTimeToLive.Value));
                    }
                    if (queue.DuplicateDetectionHistoryTimeWindow.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(queue.DuplicateDetectionHistoryTimeWindow.Value));
                    }
                    if (queue.EnableBatchedOperations.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.EnableBatchedOperations), queue.EnableBatchedOperations.Value);
                    }
                    if (queue.EnableExpress.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.EnableExpress), queue.EnableExpress.Value);
                    }
                    if (queue.EnablePartitioning.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.EnablePartitioning), queue.EnablePartitioning.Value);
                    }
                    if (queue.ForwardDeadLetteredMessagesTo.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.ForwardDeadLetteredMessagesTo), queue.ForwardDeadLetteredMessagesTo.Value);
                    }
                    if (queue.ForwardTo.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.ForwardTo), queue.ForwardTo.Value);
                    }
                    if (queue.LockDuration.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusQueue.LockDuration), XmlConvert.ToString(queue.LockDuration.Value));
                    }
                    if (queue.MaxDeliveryCount.IsSet())
                    {
                        writer.WriteNumber(nameof(ServiceBusQueue.MaxDeliveryCount), queue.MaxDeliveryCount.Value);
                    }
                    if (queue.MaxMessageSizeInKilobytes.IsSet())
                    {
                        writer.WriteNumber(nameof(ServiceBusQueue.MaxMessageSizeInKilobytes), queue.MaxMessageSizeInKilobytes.Value);
                    }
                    if (queue.MaxSizeInMegabytes.IsSet())
                    {
                        writer.WriteNumber(nameof(ServiceBusQueue.MaxSizeInMegabytes), queue.MaxSizeInMegabytes.Value);
                    }
                    if (queue.RequiresDuplicateDetection.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.RequiresDuplicateDetection), queue.RequiresDuplicateDetection.Value);
                    }
                    if (queue.RequiresSession.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusQueue.RequiresSession), queue.RequiresSession.Value);
                    }
                    if (queue.Status.IsSet())
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
                    if (topic.Name.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusTopic.Name), topic.Name.Value);
                    }
                    writer.WriteStartObject("Properties");      //             "Properties": {
                    if (topic.AutoDeleteOnIdle.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusTopic.AutoDeleteOnIdle), XmlConvert.ToString(topic.AutoDeleteOnIdle.Value));
                    }
                    if (topic.DefaultMessageTimeToLive.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusTopic.DefaultMessageTimeToLive), XmlConvert.ToString(topic.DefaultMessageTimeToLive.Value));
                    }
                    if (topic.DuplicateDetectionHistoryTimeWindow.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusTopic.DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(topic.DuplicateDetectionHistoryTimeWindow.Value));
                    }
                    if (topic.EnableBatchedOperations.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.EnableBatchedOperations), topic.EnableBatchedOperations.Value);
                    }
                    if (topic.EnableExpress.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.EnableExpress), topic.EnableExpress.Value);
                    }
                    if (topic.EnablePartitioning.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.EnablePartitioning), topic.EnablePartitioning.Value);
                    }
                    if (topic.MaxMessageSizeInKilobytes.IsSet())
                    {
                        writer.WriteNumber(nameof(ServiceBusTopic.MaxMessageSizeInKilobytes), topic.MaxMessageSizeInKilobytes.Value);
                    }
                    if (topic.MaxSizeInMegabytes.IsSet())
                    {
                        writer.WriteNumber(nameof(ServiceBusTopic.MaxSizeInMegabytes), topic.MaxSizeInMegabytes.Value);
                    }
                    if (topic.RequiresDuplicateDetection.IsSet())
                    {
                        writer.WriteBoolean(nameof(ServiceBusTopic.RequiresDuplicateDetection), topic.RequiresDuplicateDetection.Value);
                    }
                    if (topic.Status.IsSet())
                    {
                        writer.WriteString(nameof(ServiceBusTopic.Status), topic.Status.Value.ToString());
                    }
                    writer.WriteEndObject();                    //             } (/Properties)

                    writer.WriteStartArray("Subscriptions");      //             "Subscriptions": [
                    foreach (var (topicName, subscription) in emulatorResource.Subscriptions)
                    {
                        if (topicName != topic.BicepIdentifier)
                        {
                            continue;
                        }

                        writer.WriteStartObject();                  //           {
                        if (topic.Name.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusQueue.Name), subscription.Name.Value);
                        }

                        writer.WriteStartObject("Properties");      //             "Properties": {
                        if (subscription.Status.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.AutoDeleteOnIdle), XmlConvert.ToString(subscription.AutoDeleteOnIdle.Value));
                        }
                        if (subscription.ClientAffineProperties.IsSet() && subscription.ClientAffineProperties != null)
                        {
                            writer.WriteStartObject("ClientAffineProperties");      //             "ClientAffineProperties": {

                            if (subscription.ClientAffineProperties.ClientId.IsSet())
                            {
                                writer.WriteString(nameof(ServiceBusClientAffineProperties.ClientId), subscription.ClientAffineProperties.ClientId.Value);
                            }
                            if (subscription.ClientAffineProperties.IsDurable.IsSet())
                            {
                                writer.WriteBoolean(nameof(ServiceBusClientAffineProperties.IsDurable), subscription.ClientAffineProperties.IsDurable.Value);
                            }
                            if (subscription.ClientAffineProperties.IsShared.IsSet())
                            {
                                writer.WriteBoolean(nameof(ServiceBusClientAffineProperties.IsShared), subscription.ClientAffineProperties.IsShared.Value);
                            }

                            writer.WriteEndObject();                    //            } (/ClientAffineProperties)
                        }
                        if (subscription.DeadLetteringOnFilterEvaluationExceptions.IsSet())
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.DeadLetteringOnFilterEvaluationExceptions), subscription.DeadLetteringOnFilterEvaluationExceptions.Value);
                        }
                        if (subscription.DeadLetteringOnMessageExpiration.IsSet())
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.DeadLetteringOnMessageExpiration), subscription.DeadLetteringOnMessageExpiration.Value);
                        }
                        if (subscription.DefaultMessageTimeToLive.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.DefaultMessageTimeToLive), XmlConvert.ToString(subscription.DefaultMessageTimeToLive.Value));
                        }
                        if (subscription.DuplicateDetectionHistoryTimeWindow.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(subscription.DuplicateDetectionHistoryTimeWindow.Value));
                        }
                        if (subscription.EnableBatchedOperations.IsSet())
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.EnableBatchedOperations), subscription.EnableBatchedOperations.Value);
                        }
                        if (subscription.ForwardDeadLetteredMessagesTo.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.ForwardDeadLetteredMessagesTo), subscription.ForwardDeadLetteredMessagesTo.Value);
                        }
                        if (subscription.ForwardTo.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusQueue.ForwardTo), subscription.ForwardTo.Value);
                        }
                        if (subscription.IsClientAffine.IsSet())
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.IsClientAffine), subscription.IsClientAffine.Value);
                        }
                        if (subscription.LockDuration.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.LockDuration), XmlConvert.ToString(subscription.LockDuration.Value));
                        }
                        if (subscription.MaxDeliveryCount.IsSet())
                        {
                            writer.WriteNumber(nameof(ServiceBusSubscription.MaxDeliveryCount), subscription.MaxDeliveryCount.Value);
                        }
                        if (subscription.RequiresSession.IsSet())
                        {
                            writer.WriteBoolean(nameof(ServiceBusSubscription.RequiresSession), subscription.RequiresSession.Value);
                        }
                        if (subscription.Status.IsSet())
                        {
                            writer.WriteString(nameof(ServiceBusSubscription.Status), subscription.Status.Value.ToString());
                        }
                        writer.WriteEndObject();                    //             } (/Properties)

                        #region Rules
                        writer.WriteStartArray("Rules");      //             "Rules": [
                        foreach (var (ruleTopicName, ruleSubscriptionName, rule) in emulatorResource.Rules)
                        {
                            if (ruleTopicName != topic.BicepIdentifier && ruleSubscriptionName != subscription.BicepIdentifier)
                            {
                                continue;
                            }

                            writer.WriteStartObject();                  //           {
                            if (rule.Name.IsSet())
                            {
                                writer.WriteString(nameof(ServiceBusQueue.Name), rule.Name.Value);
                            }
                            writer.WriteStartObject("Properties");      //             "Properties": {

                            if (rule.Action.IsSet() && rule.Action != null)
                            {
                                writer.WriteStartObject(nameof(ServiceBusRule.Action));
                                if (rule.Action.SqlExpression.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusFilterAction.SqlExpression), rule.Action.SqlExpression.Value);
                                }
                                if (rule.Action.CompatibilityLevel.IsSet())
                                {
                                    writer.WriteNumber(nameof(ServiceBusFilterAction.CompatibilityLevel), rule.Action.CompatibilityLevel.Value);
                                }
                                if (rule.Action.RequiresPreprocessing.IsSet())
                                {
                                    writer.WriteBoolean(nameof(ServiceBusFilterAction.RequiresPreprocessing), rule.Action.RequiresPreprocessing.Value);
                                }
                                writer.WriteEndObject();
                            }

                            if (rule.CorrelationFilter.IsSet() && rule.CorrelationFilter != null)
                            {
                                writer.WriteStartObject(nameof(ServiceBusRule.CorrelationFilter));

                                if (rule.CorrelationFilter.ApplicationProperties.IsSet() && rule.CorrelationFilter.ApplicationProperties != null)
                                {
                                    var dic = new Dictionary<string, object>();
                                    
                                    foreach (var applicationProperty in rule.CorrelationFilter.ApplicationProperties)
                                    {
                                        dic.Add(applicationProperty.Key, applicationProperty.Value);
                                    }

                                    JsonSerializer.Serialize(writer, dic);
                                }
                                if (rule.CorrelationFilter.CorrelationId.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.CorrelationId), rule.CorrelationFilter.CorrelationId.Value);
                                }
                                if (rule.CorrelationFilter.MessageId.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.MessageId), rule.CorrelationFilter.MessageId.Value);
                                }
                                if (rule.CorrelationFilter.SendTo.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.SendTo), rule.CorrelationFilter.SendTo.Value);
                                }
                                if (rule.CorrelationFilter.ReplyTo.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyTo), rule.CorrelationFilter.ReplyTo.Value);
                                }
                                if (rule.CorrelationFilter.Subject.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.Subject), rule.CorrelationFilter.Subject.Value);
                                }
                                if (rule.CorrelationFilter.SessionId.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.SessionId), rule.CorrelationFilter.SessionId.Value);
                                }
                                if (rule.CorrelationFilter.ReplyToSessionId.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyToSessionId), rule.CorrelationFilter.ReplyToSessionId.Value);
                                }
                                if (rule.CorrelationFilter.ContentType.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusCorrelationFilter.ContentType), rule.CorrelationFilter.ContentType.Value);
                                }
                                if (rule.CorrelationFilter.RequiresPreprocessing.IsSet())
                                {
                                    writer.WriteBoolean(nameof(ServiceBusCorrelationFilter.RequiresPreprocessing), rule.CorrelationFilter.RequiresPreprocessing.Value);
                                }

                                writer.WriteEndObject();
                            }

                            if (rule.FilterType.IsSet())
                            {
                                writer.WriteString(nameof(ServiceBusRule.FilterType), rule.FilterType.Value.ToString());
                            }

                            if (rule.SqlFilter.IsSet() && rule.SqlFilter != null)
                            {
                                writer.WriteStartObject(nameof(ServiceBusRule.SqlFilter));
                                if (rule.SqlFilter.SqlExpression.IsSet())
                                {
                                    writer.WriteString(nameof(ServiceBusSqlFilter.SqlExpression), rule.SqlFilter.SqlExpression.Value);
                                }
                                if (rule.SqlFilter.CompatibilityLevel.IsSet())
                                {
                                    writer.WriteNumber(nameof(ServiceBusSqlFilter.CompatibilityLevel), rule.SqlFilter.CompatibilityLevel.Value);
                                }
                                if (rule.SqlFilter.RequiresPreprocessing.IsSet())
                                {
                                    writer.WriteBoolean(nameof(ServiceBusSqlFilter.RequiresPreprocessing), rule.SqlFilter.RequiresPreprocessing.Value);
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
        => builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data", isReadOnly: false);

    /// <summary>
    /// Adds a bind mount for the configuration file of an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureServiceBusEmulatorResource"/>.</param>
    /// <param name="path">Path to the file on the AppHost where the emulator configuration is located.</param>
    /// <returns>A builder for the <see cref="AzureServiceBusEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureServiceBusEmulatorResource> WithConfigJson(this IResourceBuilder<AzureServiceBusEmulatorResource> builder, string path)
    => builder.WithBindMount(path, AzureServiceBusEmulatorResource.EmulatorConfigJsonPath, isReadOnly: true);

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

    private static bool IsSet(this IBicepValue value) => value.Kind != BicepValueKind.Unset;
}
