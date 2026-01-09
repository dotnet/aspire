// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services connection resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesProjectConnectionsBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Cognitive Services connection resource to a project. This is a low level
    /// interface that requires the caller to specify all connection properties.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services project resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services connection resource.</param>
    /// <param name="configureProperties">Action to customize the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the Azure Cognitive Services connection resource.</returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        string name,
        Func<AzureResourceInfrastructure, CognitiveServicesConnectionProperties> configureProperties)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var parent = builder.Resource;

        void configureInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var aspireResource = (AzureCognitiveServicesProjectConnectionResource)infrastructure.AspireResource;
            var projectBicepId = parent.GetBicepIdentifier();
            var project = AzureCognitiveServicesProjectResource.GetProvisionableResource(
                infrastructure,
                projectBicepId) ?? throw new InvalidOperationException($"Could not find parent Azure Cognitive Services project resource for project '{projectBicepId}'.");
            var connection = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
                (identifier, resourceName) =>
                {
                    var resource = aspireResource.FromExisting(identifier);
                    resource.Parent = project;
                    resource.Name = resourceName;
                    return resource;
                },
                infra =>
                {
                    var resource = new CognitiveServicesProjectConnection(aspireResource.GetBicepIdentifier())
                    {
                        Parent = project,
                        Name = name,
                        Properties = configureProperties(infra)
                    };
                    return resource;
                });

            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = connection.Name });
        }
        var connectionResource = new AzureCognitiveServicesProjectConnectionResource(name, configureInfrastructure, parent);
        return builder.ApplicationBuilder.AddResource(connectionResource);
    }

    /// <summary>
    /// Adds CosmosDB to a project as a connection
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        AzureCosmosDBResource db)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (db.IsEmulator())
        {
            throw new InvalidOperationException("Cannot create a AI Foundry project connection to an emulator Cosmos DB instance.");
        }
        return builder.AddConnection($"connection-{Guid.NewGuid():N}", (infra) => new AadAuthTypeConnectionProperties()
        {
            Category = CognitiveServicesConnectionCategory.CosmosDB,
            Target = db.ConnectionStringOutput.AsProvisioningParameter(infra),
            IsSharedToAll = false,
            Metadata =
            {
                { "ApiType", "Azure" },
                { "ResourceId", db.NameOutputReference.AsProvisioningParameter(infra) }
            }
        });
    }

    /// <summary>
    /// Adds CosmosDB to a project as a connection
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureCosmosDBResource> db)
    {
        return builder.AddConnection(db.Resource);
    }

    /// <summary>
    /// Adds an Azure Storage account to a project as a connection.
    /// </summary>
    /// <returns></returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        AzureStorageResource account)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(account);
        if (account.IsEmulator())
        {
            throw new InvalidOperationException("Cannot create a AI Foundry project connection to an emulator Storage account.");
        }
        return builder.AddConnection($"connection-{Guid.NewGuid():N}", (infra) => new AadAuthTypeConnectionProperties()
        {
            Category = CognitiveServicesConnectionCategory.AzureBlob,
            Target = account.BlobEndpoint.AsProvisioningParameter(infra),
            IsSharedToAll = false,
            // CredentialsKey = account.KeyOutputReference.AsProvisioningParameter(infra),
            Metadata =
            {
                { "ApiType", "Azure" },
                { "ResourceId", account.NameOutputReference.AsProvisioningParameter(infra) }
            }
        });
    }

    /// <summary>
    /// Adds an Azure Storage account to a project as a connection.
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureStorageResource> storage)
    {
        return builder.AddConnection(storage.Resource);
    }

    /// <summary>
    /// Adds a container registry connection to the Azure Cognitive Services project.
    /// </summary>
    /// <returns></returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        AzureContainerRegistryResource registry)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(registry);
        if (registry.IsEmulator())
        {
            throw new InvalidOperationException("Cannot create a AI Foundry project connection to an emulator Container Registry");
        }
        return builder.AddConnection($"connection-{Guid.NewGuid():N}", (infra) => new ManagedIdentityAuthTypeConnectionProperties()
        {
            Category = CognitiveServicesConnectionCategory.ContainerRegistry,
            Target = registry.RegistryEndpoint.AsProvisioningParameter(infra),
            IsSharedToAll = false,
            Credentials = new CognitiveServicesConnectionManagedIdentity(){
                ClientId = "aiprojectidentityprincipleaid",
                ResourceId = registry.NameOutputReference.AsProvisioningParameter(infra)
            },
            Metadata =
            {
                { "ApiType", "Azure" },
                { "ResourceId", registry.NameOutputReference.AsProvisioningParameter(infra) }
            }
        });
    }

    /// <summary>
    /// Adds a container registry connection to the Azure Cognitive Services project.
    /// </summary>
    /// <returns></returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureContainerRegistryResource> registry)
    {
        return builder.AddConnection(registry.Resource);
    }

    /// <summary>
    /// Adds a Key Vault connection to the Azure Cognitive Services project.
    /// </summary>
    /// <remarks>
    /// This connection allows the AI Foundry project to store secrets for various other connections.
    /// As such, we recommend adding this connection *before* any others, so that those connections
    /// can leverage the Key Vault connection for secret storage.
    /// </remarks>
    public static IResourceBuilder<AzureBicepResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        AzureKeyVaultResource keyVault)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(keyVault);
        if (keyVault.IsEmulator())
        {
            throw new InvalidOperationException("Cannot create a AI Foundry project connection to an emulator Key Vault.");
        }
        // Configuration based on https://github.com/azure-ai-foundry/foundry-samples/blob/9551912af4d4fdb8ea73e996145e940a7e369c84/infrastructure/infrastructure-setup-bicep/01-connections/connection-key-vault.bicep
        // We use a custom subclass because Azure.Provisioning.CognitiveServices does not support the "AzureKeyVault" connection category yet (as of 2026-01-06).
        // We also swap `ManagedIdentity` auth type for `AccountManagedIdentity`, because the latter seems to be an error in the Bicep template.
        return builder.AddConnection($"connection-{Guid.NewGuid():N}", (infra) => new AzureKeyVaultConnectionProperties()
        {
            Target = keyVault.Id.AsProvisioningParameter(infra),
            IsSharedToAll = true,
            Metadata =
            {
                { "ApiType", "Azure" },
                { "ResourceId", keyVault.Id.AsProvisioningParameter(infra) }
            }
        });
    }

    /// <summary>
    /// Adds a Key Vault connection to the Azure Cognitive Services project.
    /// </summary>
    public static IResourceBuilder<AzureBicepResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureKeyVaultResource> keyVault)
    {
        return builder.AddConnection(keyVault.Resource);
    }
}

