// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.EventHubs;
using Aspire.Hosting.Utils;
using Azure.Messaging.EventHubs.Producer;
using Azure.Provisioning;
using AzureProvisioning = Azure.Provisioning.EventHubs;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Event Hubs resources to the application model.
/// </summary>
public static class AzureEventHubsExtensions
{
    /// <summary>
    /// Adds an Azure Event Hubs Namespace resource to the application model. This resource can be used to create Event Hub resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(
        this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = static (AzureResourceInfrastructure infrastructure) =>
        {
            var skuParameter = new ProvisioningParameter("sku", typeof(string))
            {
                Value = "Standard"
            };
            infrastructure.Add(skuParameter);

            var eventHubsNamespace = new AzureProvisioning.EventHubsNamespace(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Sku = new AzureProvisioning.EventHubsSku()
                {
                    Name = skuParameter
                },
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
            infrastructure.Add(eventHubsNamespace);

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            infrastructure.Add(principalTypeParameter);
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(principalIdParameter);

            infrastructure.Add(eventHubsNamespace.CreateRoleAssignment(AzureProvisioning.EventHubsBuiltInRole.AzureEventHubsDataOwner, principalTypeParameter, principalIdParameter));

            infrastructure.Add(new ProvisioningOutput("eventHubsEndpoint", typeof(string)) { Value = eventHubsNamespace.ServiceBusEndpoint });

            var azureResource = (AzureEventHubsResource)infrastructure.AspireResource;

            foreach (var hub in azureResource.Hubs)
            {
                var cdkHub = hub.ToProvisioningEntity();
                cdkHub.Parent = eventHubsNamespace;
                infrastructure.Add(cdkHub);

                foreach (var consumerGroup in hub.ConsumerGroups)
                {
                    var cdkConsumerGroup = consumerGroup.ToProvisioningEntity();
                    cdkConsumerGroup.Parent = cdkHub;
                    infrastructure.Add(cdkConsumerGroup);
                }
            }
        };

        var resource = new AzureEventHubsResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Event Hubs hub resource to the application model. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="name">The name of the Event Hub.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(WithHub)} instead to add an Azure Event Hub.")]
    public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder, [ResourceName] string name)
    {
        return builder.WithHub(name);
    }

    /// <summary>
    /// Adds an Azure Event Hubs hub resource to the application model. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="EventHubsEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="EventHubsEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="EventHubsEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="name">The name of the Event Hub.</param>
    /// <param name="configure">An optional method that can be used for customizing the <see cref="EventHub"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureEventHubsResource> WithHub(this IResourceBuilder<AzureEventHubsResource> builder, [ResourceName] string name, Action<EventHub>? configure = null)
    {
        var hub = builder.Resource.Hubs.FirstOrDefault(x => x.Name == name);

        if (hub == null)
        {
            hub = new EventHub(name);
            builder.Resource.Hubs.Add(hub);
        }

        configure?.Invoke(hub);

        return builder;
    }

    /// <summary>
    /// Configures an Azure Event Hubs resource to be emulated. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="EventHubsEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="EventHubsEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="EventHubsEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <example>
    /// The following example creates an Azure Event Hubs resource that runs locally is an emulator and referencing that
    /// resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var eventHub = builder.AddAzureEventHubs("eventhubns")
    ///    .RunAsEmulator()
    ///    .AddEventHub("hub");
    ///
    /// builder.AddProject&lt;Projects.InventoryService&gt;()
    ///        .WithReference(eventHub);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureEventHubsResource> RunAsEmulator(this IResourceBuilder<AzureEventHubsResource> builder, Action<IResourceBuilder<AzureEventHubsEmulatorResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        var configHostFile = Path.Combine(Directory.CreateTempSubdirectory("AspireEventHubsEmulator").FullName, "Config.json");

        var customMountAnnotation = new ContainerMountAnnotation(
                configHostFile,
                AzureEventHubsEmulatorResource.EmulatorConfigJsonPath,
                ContainerMountType.BindMount,
                isReadOnly: false);

        builder
            .WithEndpoint(name: "emulator", targetPort: 5672)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = EventHubsEmulatorContainerImageTags.Registry,
                Image = EventHubsEmulatorContainerImageTags.Image,
                Tag = EventHubsEmulatorContainerImageTags.Tag
            })
            .WithAnnotation(customMountAnnotation);

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

        EventHubProducerClient? client = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(builder.Resource, async (@event, ct) =>
        {
            var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false)
                        ?? throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");

            // For the purposes of the health check we only need to know a hub name. If we don't have a hub
            // name we can't configure a valid producer client connection so we should throw. What good is
            // an event hub namespace without an event hub? :)
            if (builder.Resource.Hubs is { Count: > 0 } && builder.Resource.Hubs[0] is { } hub)
            {
                var healthCheckConnectionString = $"{connectionString};EntityPath={hub.Name};";
                client = new EventHubProducerClient(healthCheckConnectionString);
            }
            else
            {
                throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource does not have any Event Hubs.");
            }
        });

        var healthCheckKey = $"{builder.Resource.Name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureEventHub(
            sp => client ?? throw new DistributedApplicationException("EventHubProducerClient is not initialized"),
            healthCheckKey
            );

        builder.WithHealthCheck(healthCheckKey);

        if (configureContainer != null)
        {
            var surrogate = new AzureEventHubsEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            var eventHubsEmulatorResources = builder.ApplicationBuilder.Resources.OfType<AzureEventHubsResource>().Where(x => x is { } eventHubsResource && eventHubsResource.IsEmulator);

            if (!eventHubsEmulatorResources.Any())
            {
                // No-op if there is no Azure Event Hubs emulator resource.
                return Task.CompletedTask;
            }

            foreach (var emulatorResource in eventHubsEmulatorResources)
            {
                // A custom file mount with read-only access is used to mount the emulator configuration file. If it's not found, the read-write mount we defined on the container is used.
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().LastOrDefault(v => v.Target == AzureEventHubsEmulatorResource.EmulatorConfigJsonPath);

                // If the latest mount for EmulatorConfigJsonPath is our custom one then we can generate it.
                if (configFileMount != customMountAnnotation)
                {
                    continue;
                }
                using var stream = new FileStream(configFileMount.Source!, FileMode.Create);
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(configHostFile,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite
                        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
                }

                writer.WriteStartObject();                      // {
                writer.WriteStartObject("UserConfig");          //   "UserConfig": {
                writer.WriteStartArray("NamespaceConfig");      //     "NamespaceConfig": [
                writer.WriteStartObject();                      //       {
                writer.WriteString("Type", "EventHub");

                // This name is currently required by the emulator
                writer.WriteString("Name", "emulatorNs1");
                writer.WriteStartArray("Entities");             //         "Entities": [

                foreach (var hub in emulatorResource.Hubs)
                {
                    writer.WriteStartObject();
                    hub.WriteJsonObjectProperties(writer);
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

            // Apply ConfigJsonAnnotation modifications
            foreach (var emulatorResource in eventHubsEmulatorResources)
            {
                var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().LastOrDefault(v => v.Target == AzureEventHubsEmulatorResource.EmulatorConfigJsonPath);

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
                    using var writer = new Utf8JsonWriter(writeStream, new JsonWriterOptions { Indented = true });

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
    /// Adds a bind mount for the data folder to an Azure Event Hubs emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureEventHubsEmulatorResource"/>.</param>
    /// <param name="path">Relative path to the AppHost where emulator storage is persisted between runs. Defaults to the path '.eventhubs/{builder.Resource.Name}'</param>
    /// <returns>A builder for the <see cref="AzureEventHubsEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithDataBindMount(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, string? path = null)
        => builder.WithBindMount(path ?? $".eventhubs/{builder.Resource.Name}", "/data", isReadOnly: false);

    /// <summary>
    /// Adds a named volume for the data folder to an Azure Event Hubs emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureEventHubsEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>A builder for the <see cref="AzureEventHubsEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithDataVolume(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, string? name = null)
        => builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data", isReadOnly: false);

    /// <summary>
    /// Configures the host port for the Azure Event Hubs emulator is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">Builder for the Azure Event Hubs emulator container</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used, a random port will be assigned.</param>
    /// <returns>Azure Event Hubs emulator resource builder.</returns>
    [Obsolete("Use WithHostPort instead.")]
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithGatewayPort(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, int? port)
    {
        return WithHostPort(builder, port);
    }

    /// <summary>
    /// Configures the host port for the Azure Event Hubs emulator is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">Builder for the Azure Event Hubs emulator container</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used, a random port will be assigned.</param>
    /// <returns>Azure Event Hubs emulator resource builder.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithHostPort(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a bind mount for the configuration file of an Azure Service Bus emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureEventHubsEmulatorResource"/>.</param>
    /// <param name="path">Path to the file on the AppHost where the emulator configuration is located.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithConfigurationFile(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, string path)
    => builder.WithBindMount(path, AzureEventHubsEmulatorResource.EmulatorConfigJsonPath, isReadOnly: false);

    /// <summary>
    /// Alters the JSON configuration document used by the emulator.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureEventHubsEmulatorResource"/>.</param>
    /// <param name="configJson">A callback to update the JSON object representation of the configuration.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> ConfigureEmulator(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, Action<JsonNode> configJson)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configJson);

        builder.WithAnnotation(new ConfigJsonAnnotation(configJson));

        return builder;
    }
}
