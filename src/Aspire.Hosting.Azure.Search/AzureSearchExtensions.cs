// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Search;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AI Search resources to the application model.
/// </summary>
public static class AzureSearchExtensions
{
    /// <summary>
    /// Adds an Azure AI Search service resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure AI Search resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSearchConstructResource}"/>.</returns>
    public static IResourceBuilder<AzureSearchResource> AddAzureSearch(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        AzureSearchResource resource = new(name, ConfigureSearch);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);

        void ConfigureSearch(AzureResourceInfrastructure construct)
        {
            var search = new SearchService(construct.Resource.GetBicepIdentifier())
            {
                SearchSkuName = SearchServiceSkuName.Basic,
                ReplicaCount = 1,
                PartitionCount = 1,
                HostingMode = SearchServiceHostingMode.Default,
                IsLocalAuthDisabled = true,
                Tags = { { "aspire-resource-name", name } }
            };
            construct.Add(search);

            construct.Add(search.CreateRoleAssignment(SearchBuiltInRole.SearchIndexDataContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
            construct.Add(search.CreateRoleAssignment(SearchBuiltInRole.SearchServiceContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));

            // TODO: The endpoint format should move into the CDK so we can maintain this
            // logic in a single location and have a better chance at supporting more than
            // just public Azure in the future.  https://github.com/Azure/azure-sdk-for-net/issues/42640
            construct.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = BicepFunction.Interpolate($"Endpoint=https://{search.Name}.search.windows.net")
            });
        }
    }
}
