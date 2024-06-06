// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.EventHubs;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.EventHubs;

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
    /// <returns></returns>
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(
        this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable AZPROVISION001 // This API requires opting into experimental features
        return builder.AddAzureEventHubs(name, null);
#pragma warning restore AZPROVISION001 // This API requires opting into experimental features
    }

    /// <summary>
    /// Adds an Azure Event Hubs Namespace resource to the application model. This resource can be used to create Event Hub resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Optional callback to configure the Event Hubs namespace.</param>
    /// <returns></returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, string name,
        Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHubsNamespace>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var eventHubsNamespace = new EventHubsNamespace(construct, name: name);

            eventHubsNamespace.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            eventHubsNamespace.AssignProperty(p => p.Sku.Name, new Parameter("sku", defaultValue: "Standard"));

            var eventHubsDataOwnerRole = eventHubsNamespace.AssignRole(RoleDefinition.EventHubsDataOwner);
            eventHubsDataOwnerRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            eventHubsNamespace.AddOutput("eventHubsEndpoint", sa => sa.ServiceBusEndpoint);

            var azureResource = (AzureEventHubsResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            configureResource?.Invoke(azureResourceBuilder, construct, eventHubsNamespace);

            foreach (var hub in azureResource.Hubs)
            {
                var hubResource = new EventHub(construct, name: hub.Name, parent: eventHubsNamespace);
                hub.Configure?.Invoke(azureResourceBuilder, construct, hubResource);
            }

        };
        var resource = new AzureEventHubsResource(name, configureConstruct);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Event Hubs hub resource to the application model. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="name">The name of the Event Hub.</param>
    public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder, string name)
    {
#pragma warning disable AZPROVISION001 // This API requires opting into experimental features
        return builder.AddEventHub(name, null);
#pragma warning restore AZPROVISION001 // This API requires opting into experimental features
    }

    /// <summary>
    /// Adds an Azure Event Hubs hub resource to the application model. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="name">The name of the Event Hub.</param>
    /// <param name="configureHub">Optional callback to customize the Event Hub.</param>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder,
        string name, Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHub>? configureHub)
    {
        builder.Resource.Hubs.Add((name, configureHub));
        return builder;
    }

    /// <summary>
    /// Configures an Azure Event Hubs resource to be emulated. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
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

        builder.ApplicationBuilder.Services.TryAddLifecycleHook<AzureEventHubsEmulatorConfigWriterHook>();

        // Add emulator container

        builder
            .WithEndpoint(name: "emulator", targetPort: 5672)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = EventHubsEmulatorContainerImageTags.Registry,
                Image = EventHubsEmulatorContainerImageTags.Image,
                Tag = EventHubsEmulatorContainerImageTags.Tag
            })
            .WithAnnotation(new ContainerMountAnnotation(
                Path.GetTempFileName(),
                AzureEventHubsEmulatorResource.EmulatorConfigJsonPath,
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
            context.EnvironmentVariables.Add("BLOB_SERVER", $"{blobEndpoint.ContainerHost}:{blobEndpoint.Port}");
            context.EnvironmentVariables.Add("METADATA_SERVER", $"{tableEndpoint.ContainerHost}:{tableEndpoint.Port}");
        }));

        if (configureContainer != null)
        {
            var surrogate = new AzureEventHubsEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to an Azure Event Hubs emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureEventHubsEmulatorResource"/>.</param>
    /// <param name="path">Relative path to the AppHost where emulator storage is persisted between runs. Defaults to the path '.eventhubs/{builder.Resource.Name}'</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>A builder for the <see cref="AzureEventHubsEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithDataBindMount(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, string? path = null, bool isReadOnly = false)
        => builder.WithBindMount(path ?? $".eventhubs/{builder.Resource.Name}", "/data", isReadOnly);

    /// <summary>
    /// Configures the gateway port for the Azure Event Hubs emulator.
    /// </summary>
    /// <param name="builder">Builder for the Azure Event Hubs emulator container</param>
    /// <param name="port">Host port to bind to the emulator gateway port.</param>
    /// <returns>Azure Event Hubs emulator resource builder.</returns>
    public static IResourceBuilder<AzureEventHubsEmulatorResource> WithGatewayPort(this IResourceBuilder<AzureEventHubsEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}
