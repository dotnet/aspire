// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services connection resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesAzureConnectionsBuilderExtensions
{
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
    /// Adds an Application Insights connection to the Azure Cognitive Services project.
    ///
    /// This is used for agent evals, telemetry, and other observability for hosted agents.
    /// </summary>
    /// <returns></returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        AzureApplicationInsightsResource appInsights)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(appInsights);
        if (appInsights.IsEmulator())
        {
            throw new InvalidOperationException("Cannot create a AI Foundry project connection to an emulator Application Insights resource.");
        }
        return builder.AddConnection($"connection-{Guid.NewGuid():N}", (infra) => new ApiKeyAuthConnectionProperties()
        {
            Category = CognitiveServicesConnectionCategory.ApiKey, // TODO: Should be "AppInsights"
            Target = appInsights.ConnectionString.AsProvisioningParameter(infra),
            IsSharedToAll = false,
            CredentialsKey = "", // TODO: get credentials somehow
            Metadata =
            {
                { "ApiType", "Azure" },
                { "ResourceId", appInsights.NameOutputReference.AsProvisioningParameter(infra) }
            }
        });
    }
}
