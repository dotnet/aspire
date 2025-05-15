// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Storage;
using Azure.Identity;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Storage resources to an application model.
/// </summary>
public static class AzureStorageExtensions
{
    private const string SkipApiVersionCheckArgument = "--skipApiVersionCheck";

    /// <summary>
    /// Adds an Azure Storage resource to the application model. This resource can be used to create Azure blob, table, and queue resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    /// <remarks>
    /// By default references to the Azure Storage resource will be assigned the following roles:
    /// 
    /// - <see cref="StorageBuiltInRole.StorageBlobDataContributor"/>
    /// - <see cref="StorageBuiltInRole.StorageTableDataContributor"/>
    /// - <see cref="StorageBuiltInRole.StorageQueueDataContributor"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureStorageResource}, StorageBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var storageAccount = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = StorageAccount.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new StorageAccount(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    Kind = StorageKind.StorageV2,
                    AccessTier = StorageAccountAccessTier.Hot,
                    Sku = new StorageSku() { Name = StorageSkuName.StandardGrs },
                    NetworkRuleSet = new StorageAccountNetworkRuleSet()
                    {
                        // Unfortunately Azure Storage does not list ACA as one of the resource types in which
                        // the AzureServices firewall policy works. This means that we need this Azure Storage
                        // account to have its default action set to Allow.
                        DefaultAction = StorageNetworkDefaultAction.Allow
                    },
                    // Set the minimum TLS version to 1.2 to ensure resources provisioned are compliant
                    // with the pending deprecation of TLS 1.0 and 1.1.
                    MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2,
                    // Disable shared key access to the storage account as managed identity is configured
                    // to access the storage account by default.
                    AllowSharedKeyAccess = false,
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

            var blobs = new BlobService("blobs")
            {
                Parent = storageAccount
            };
            infrastructure.Add(blobs);

            infrastructure.Add(new ProvisioningOutput("blobEndpoint", typeof(string)) { Value = storageAccount.PrimaryEndpoints.BlobUri });
            infrastructure.Add(new ProvisioningOutput("queueEndpoint", typeof(string)) { Value = storageAccount.PrimaryEndpoints.QueueUri });
            infrastructure.Add(new ProvisioningOutput("tableEndpoint", typeof(string)) { Value = storageAccount.PrimaryEndpoints.TableUri });

            var azureResource = (AzureStorageResource)infrastructure.AspireResource;

            foreach (var blobContainer in azureResource.BlobContainers)
            {
                var cdkBlobContainer = blobContainer.ToProvisioningEntity();
                cdkBlobContainer.Parent = blobs;
                infrastructure.Add(cdkBlobContainer);
            }

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = storageAccount.Name });
        };

        var resource = new AzureStorageResource(name, configureInfrastructure);

        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(StorageBuiltInRole.GetBuiltInRoleName,
                StorageBuiltInRole.StorageBlobDataContributor,
                StorageBuiltInRole.StorageTableDataContributor,
                StorageBuiltInRole.StorageQueueDataContributor);
    }

    /// <summary>
    /// Configures an Azure Storage resource to be emulated using Azurite. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="StorageEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="StorageEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="StorageEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> RunAsEmulator(this IResourceBuilder<AzureStorageResource> builder, Action<IResourceBuilder<AzureStorageEmulatorResource>>? configureContainer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithEndpoint(name: "blob", targetPort: 10000)
               .WithEndpoint(name: "queue", targetPort: 10001)
               .WithEndpoint(name: "table", targetPort: 10002)
               .WithAnnotation(new ContainerImageAnnotation
               {
                   Registry = StorageEmulatorContainerImageTags.Registry,
                   Image = StorageEmulatorContainerImageTags.Image,
                   Tag = StorageEmulatorContainerImageTags.Tag
               });

        BlobServiceClient? blobServiceClient = null;
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, async (@event, ct) =>
        {
            // The BlobServiceClient is created before the health check is run.
            // We can't use ConnectionStringAvailableEvent here because the resource doesn't have a connection string, so
            // we use BeforeResourceStartedEvent

            var connectionString = await builder.Resource.GetBlobConnectionString().GetValueAsync(ct).ConfigureAwait(false) ?? throw new DistributedApplicationException($"{nameof(ConnectionStringAvailableEvent)} was published for the '{builder.Resource.Name}' resource but the connection string was null.");
            blobServiceClient = CreateBlobServiceClient(connectionString);
        });

        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(builder.Resource, async (@event, ct) =>
        {
            // This event is triggered when the health check is healthy.

            if (blobServiceClient is null)
            {
                throw new InvalidOperationException("BlobServiceClient is not initialized.");
            }

            foreach (var container in builder.Resource.BlobContainers)
            {
                await blobServiceClient.GetBlobContainerClient(container.BlobContainerName).CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);
            }
        });

        var healthCheckKey = $"{builder.Resource.Name}_check";

        builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureBlobStorage(sp =>
        {
            return blobServiceClient ?? throw new InvalidOperationException("BlobServiceClient is not initialized.");
        }, name: healthCheckKey);

        builder.WithHealthCheck(healthCheckKey);

        // The default arguments list is coming from https://github.com/Azure/Azurite/blob/c3f93445fbd8fd54d380eb265a5665166c460d2b/Dockerfile#L47C6-L47C106
        // They need to be repeated in order to be able to add --skipApiVersionCheck

        var surrogate = new AzureStorageEmulatorResource(builder.Resource);
        var surrogateBuilder = builder.ApplicationBuilder
            .CreateResourceBuilder(surrogate)
            .WithArgs("azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0", SkipApiVersionCheckArgument);

        configureContainer?.Invoke(surrogateBuilder);

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
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithBindMount(path ?? $".azurite/{builder.Resource.Name}", "/data", isReadOnly);
    }

    /// <summary>
    /// Adds a named volume for the data folder to an Azure Storage emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureStorageEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>A builder for the <see cref="AzureStorageEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> WithDataVolume(this IResourceBuilder<AzureStorageEmulatorResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data", isReadOnly);
    }

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for blob requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> WithBlobPort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
    public static IResourceBuilder<AzureStorageEmulatorResource> WithQueuePort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureStorageEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> WithTablePort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("table", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Ensures the emulator checks that the requested API version is valid.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="enable">Whether to enable API version check or not. Default is <lang>true</lang>.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureStorageEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> WithApiVersionCheck(this IResourceBuilder<AzureStorageEmulatorResource> builder, bool enable = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithArgs(context =>
        {
            context.Args.Remove(SkipApiVersionCheckArgument);

            if (enable)
            {
                context.Args.Add(SkipApiVersionCheckArgument);
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureBlobStorageResource"/> which can be referenced to get the Azure Storage blob endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureBlobStorageResource"/>.</returns>
    public static IResourceBuilder<AzureBlobStorageResource> AddBlobs(this IResourceBuilder<AzureStorageResource> builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new AzureBlobStorageResource(name, builder.Resource);

        string? connectionString = null;
        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(resource, async (@event, ct) =>
        {
            connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
        });

        var healthCheckKey = $"{resource.Name}_check";

        BlobServiceClient? blobServiceClient = null;
        builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureBlobStorage(sp =>
        {
            return blobServiceClient ??= CreateBlobServiceClient(connectionString ?? throw new InvalidOperationException("Connection string is not initialized."));
        }, name: healthCheckKey);

        return builder.ApplicationBuilder.AddResource(resource).WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureBlobStorageContainerResource"/> which can be referenced to get the Azure Storage blob container endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureBlobStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="blobContainerName">The name of the blob container.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureBlobStorageContainerResource"/>.</returns>
    public static IResourceBuilder<AzureBlobStorageContainerResource> AddBlobContainer(this IResourceBuilder<AzureBlobStorageResource> builder, [ResourceName] string name, string? blobContainerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        blobContainerName ??= name;

        AzureBlobStorageContainerResource resource = new(name, blobContainerName, builder.Resource);
        builder.Resource.Parent.BlobContainers.Add(resource);

        string? connectionString = null;
        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(resource, async (@event, ct) =>
        {
            connectionString = await resource.Parent.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
        });

        var healthCheckKey = $"{resource.Name}_check";

        BlobServiceClient? blobServiceClient = null;
        builder.ApplicationBuilder.Services.AddHealthChecks().AddAzureBlobStorage(
            sp => blobServiceClient ??= CreateBlobServiceClient(connectionString ?? throw new InvalidOperationException("Connection string is not initialized.")),
            optionsFactory: sp => new HealthChecks.Azure.Storage.Blobs.AzureBlobStorageHealthCheckOptions { ContainerName = blobContainerName },
            name: healthCheckKey);

        return builder.ApplicationBuilder
            .AddResource(resource).WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureTableStorageResource"/> which can be referenced to get the Azure Storage tables endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureTableStorageResource"/>.</returns>
    public static IResourceBuilder<AzureTableStorageResource> AddTables(this IResourceBuilder<AzureStorageResource> builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new AzureTableStorageResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// Creates a builder for the <see cref="AzureQueueStorageResource"/> which can be referenced to get the Azure Storage queues endpoint for the storage account.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for <see cref="AzureStorageResource"/>/</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="AzureQueueStorageResource"/>.</returns>
    public static IResourceBuilder<AzureQueueStorageResource> AddQueues(this IResourceBuilder<AzureStorageResource> builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new AzureQueueStorageResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource);
    }

    private static BlobServiceClient CreateBlobServiceClient(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            return new BlobServiceClient(uri, new DefaultAzureCredential());
        }
        else
        {
            return new BlobServiceClient(connectionString);
        }
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure Storage account. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure Storage account.</param>
    /// <param name="roles">The built-in storage roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the StorageBlobDataContributor role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var storage = builder.AddAzureStorage("storage");
    /// var blobs = storage.AddBlobs("blobs");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataContributor)
    ///   .WithReference(blobs);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureStorageResource> target,
        params StorageBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, StorageBuiltInRole.GetBuiltInRoleName, roles);
    }
}
