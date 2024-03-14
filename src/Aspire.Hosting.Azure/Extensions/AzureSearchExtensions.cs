// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Search;
using Azure.ResourceManager.Search.Models;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Search resources to the application model.
/// </summary>
public static class AzureSearchExtensions
{
    /// <summary>
    /// Adds an Azure AI Search service resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure AI Search resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSearchConstructResource}"/>.</returns>
    public static IResourceBuilder<AzureSearchResource> AddAzureSearch(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddAzureSearch(name, (_, _, _) => { });
#pragma warning restore CA2252 // This API requires opting into preview features
    }
    /// <summary>
    /// Adds an Azure AI Search service resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure AI Search resource.</param>
    /// <param name="configureResource">Callback to configure the Azure AI Search resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSearchConstructResource}"/>.</returns>
    [RequiresPreviewFeatures]
    public static IResourceBuilder<AzureSearchResource> AddAzureSearch(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<IResourceBuilder<AzureSearchResource>, ResourceModuleConstruct, SearchService>? configureResource = null)
    {
        AzureSearchResource resource = new(name, ConfigureSearch);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);

        void ConfigureSearch(ResourceModuleConstruct construct)
        {
            SearchService search =
                new(construct, name: name, sku: SearchSkuName.Basic)
                {
                    Properties =
                    {
                        ReplicaCount = 1,
                        PartitionCount = 1,
                        HostingMode = SearchServiceHostingMode.Default,
                        IsLocalAuthDisabled = true,
                        Tags = { { "aspire-resource-name", name } }
                    }
                };

            search.AssignRole(RoleDefinition.SearchIndexDataContributor)
                  .AssignProperty(role => role.PrincipalType, construct.PrincipalTypeParameter);
            search.AssignRole(RoleDefinition.SearchServiceContributor)
                  .AssignProperty(role => role.PrincipalType, construct.PrincipalTypeParameter);

            // TODO: The endpoint format should move into the CDK so we can maintain this
            // logic in a single location and have a better chance at supporting more than
            // just public Azure in the future.  https://github.com/Azure/azure-sdk-for-net/issues/42640
            search.AddOutput("connectionString", "'Endpoint=https://${{{0}}}.search.windows.net'", me => me.Name);

            if (configureResource is not null)
            {
                var resource = (AzureSearchResource)construct.Resource;
                var resourceBuilder = builder.CreateResourceBuilder(resource);
                configureResource(resourceBuilder, construct, search);
            }
        }
    }
}
