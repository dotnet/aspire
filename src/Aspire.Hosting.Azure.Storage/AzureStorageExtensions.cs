// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Storage.Models;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Storage resources to an application model.
/// </summary>
public static class AzureStorageExtensions
{
    /// <summary>
    /// Adds an Azure Storage resource to the application model.This resource can be used to create Azure blob, table, and queue resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureStorage(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure Storage resource to the application model.This resource can be used to create Azure blob, table, and queue resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.Storage.StorageAccount"/> resource.</param>
    /// <returns></returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureStorageResource>, ResourceModuleConstruct, StorageAccount>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var storageAccount = construct.AddStorageAccount(
                name: name,
                kind: StorageKind.StorageV2,
                sku: StorageSkuName.StandardGrs
                );

            // Unfortunately Azure Storage does not list ACA as one of the resource types in which
            // the AzureServices firewall policy works. This means that we need this Azure Storage
            // account to have its default action set to Allow.
            storageAccount.AssignProperty(p => p.NetworkRuleSet.DefaultAction, "'Allow'");

            storageAccount.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            var blobService = new BlobService(construct);

            var blobRole = storageAccount.AssignRole(RoleDefinition.StorageBlobDataContributor);
            blobRole.AssignProperty(p => p.PrincipalId, construct.PrincipalIdParameter);
            blobRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            var tableRole = storageAccount.AssignRole(RoleDefinition.StorageTableDataContributor);
            tableRole.AssignProperty(p => p.PrincipalId, construct.PrincipalIdParameter);
            tableRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            var queueRole = storageAccount.AssignRole(RoleDefinition.StorageQueueDataContributor);
            queueRole.AssignProperty(p => p.PrincipalId, construct.PrincipalIdParameter);
            queueRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            storageAccount.AddOutput("blobEndpoint", sa => sa.PrimaryEndpoints.BlobUri);
            storageAccount.AddOutput("queueEndpoint", sa => sa.PrimaryEndpoints.QueueUri);
            storageAccount.AddOutput("tableEndpoint", sa => sa.PrimaryEndpoints.TableUri);

            var resource = (AzureStorageResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, storageAccount);
        };
        var resource = new AzureStorageResource(name, configureConstruct);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures an Azure Storage resource to be emulated using Azurite. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model. This version the package defaults to version 3.29.0 of the mcr.microsoft.com/azure-storage/azurite container image.
    /// </summary>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> RunAsEmulator(this IResourceBuilder<AzureStorageResource> builder, Action<IResourceBuilder<AzureStorageEmulatorResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithEndpoint(name: "blob", targetPort: 10000)
               .WithEndpoint(name: "queue", targetPort: 10001)
               .WithEndpoint(name: "table", targetPort: 10002)
               .WithAnnotation(new ContainerImageAnnotation
               {
                   Registry = "mcr.microsoft.com",
                   Image = "azure-storage/azurite",
                   Tag = "3.29.0"
               });

        if (configureContainer != null)
        {
            var surrogate = new AzureStorageEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to an Azure Storage emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureStorageEmulatorResource"/>.</param>
    /// <param name="path">Relative path to the AppHost where emulator storage is persisted between runs. Defaults to the path '.azurite/{builder.Resource.Name}'</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>A builder for the <see cref="AzureStorageEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> WithDataBindMount(this IResourceBuilder<AzureStorageEmulatorResource> builder, string? path = null, bool isReadOnly = false)
        => builder.WithBindMount(path ?? $".azurite/{builder.Resource.Name}", "/data", isReadOnly);

    /// <summary>
    /// Adds a named volume for the data folder to an Azure Storage emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureStorageEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>A builder for the <see cref="AzureStorageEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> WithDataVolume(this IResourceBuilder<AzureStorageEmulatorResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/data", isReadOnly);

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for blob requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UseBlobPort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        return builder.WithEndpoint("blob", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for queue requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UseQueuePort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        return builder.WithEndpoint("queue", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for table requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UseTablePort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        return builder.WithEndpoint("table", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureBlobStorageResource"/> which can be referenced to get the Azure Storage blob endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureBlobStorageResource"/>.</returns>
    public static IResourceBuilder<AzureBlobStorageResource> AddBlobs(this IResourceBuilder<AzureStorageResource> builder, string name)
    {
        var resource = new AzureBlobStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureTableStorageResource"/> which can be referenced to get the Azure Storage tables endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureTableStorageResource"/>.</returns>
    public static IResourceBuilder<AzureTableStorageResource> AddTables(this IResourceBuilder<AzureStorageResource> builder, string name)
    {
        var resource = new AzureTableStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureQueueStorageResource"/> which can be referenced to get the Azure Storage queues endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureQueueStorageResource"/>.</returns>
    public static IResourceBuilder<AzureQueueStorageResource> AddQueues(this IResourceBuilder<AzureStorageResource> builder, string name)
    {
        var resource = new AzureQueueStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource);
    }
}
