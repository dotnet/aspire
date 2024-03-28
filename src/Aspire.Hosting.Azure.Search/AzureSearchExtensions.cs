// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
#pragma warning disable ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureSearch(name, null);
#pragma warning restore ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
    /// <summary>
    /// Adds an Azure AI Search service resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure AI Search resource.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.Search.SearchService"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSearchConstructResource}"/>.</returns>
    [Experimental("ASPIRE0001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureSearchResource> AddAzureSearch(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<IResourceBuilder<AzureSearchResource>, ResourceModuleConstruct, SearchService>? configureResource)
    {
        builder.AddAzureProvisioning();

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

            var searchIndexContributorRole = search.AssignRole(RoleDefinition.SearchIndexDataContributor);
            searchIndexContributorRole.AssignProperty(role => role.PrincipalId, construct.PrincipalIdParameter);
            searchIndexContributorRole.AssignProperty(role => role.PrincipalType, construct.PrincipalTypeParameter);

            var searchServiceContributorRole = search.AssignRole(RoleDefinition.SearchServiceContributor);
            searchServiceContributorRole.AssignProperty(role => role.PrincipalId, construct.PrincipalIdParameter);
            searchServiceContributorRole.AssignProperty(role => role.PrincipalType, construct.PrincipalTypeParameter);

            // TODO: The endpoint format should move into the CDK so we can maintain this
            // logic in a single location and have a better chance at supporting more than
            // just public Azure in the future.  https://github.com/Azure/azure-sdk-for-net/issues/42640
            search.AddOutput("connectionString", "'Endpoint=https://${{{0}}}.search.windows.net'", me => me.Name);

            var resource = (AzureSearchResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, search);
        }
    }
}
