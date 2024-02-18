// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.ResourceManager.Search;
using Azure.ResourceManager.Search.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning.Provisioners;

internal sealed class AzureSearchProvisioner(ILogger<AzureSearchProvisioner> logger) : AzureResourceProvisioner<AzureSearchResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureSearchResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string connectionString)
        {
            resource.ConnectionString = connectionString;
            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        AzureSearchResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
    {
        context.ResourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not SearchServiceResource)
        {
            logger.LogWarning("Resource {resourceName} is not an Azure Search resource. Deleting it.", resource.Name);

            await context.ArmClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var searchResource = azureResource as SearchServiceResource;

        if (searchResource is null)
        {
            var searchName = Guid.NewGuid().ToString("N");

            var searchServiceData = new SearchServiceData(context.Location)
            {
                SkuName = SearchSkuName.Free,
                // AuthOptions.AadOrApiKey is internal, so cannot set it.
                // https://github.com/Azure/azure-sdk-for-net/issues/42051
                IsLocalAuthDisabled = true
            };

            logger.LogInformation("Creating Azure Search {searchName} in {location}...", searchName, context.Location);

            var searchCreateOperation = await context.ResourceGroup
                .GetSearchServices().CreateOrUpdateAsync(WaitUntil.Completed, searchName, searchServiceData, cancellationToken: cancellationToken).ConfigureAwait(false);
            searchResource = searchCreateOperation.Value;

            logger.LogInformation("Azure Search {searchName} created.", searchResource.Data.Name);
        }

        // SearchServiceResource doesn't have an "Endpoint" property
        resource.ConnectionString = $"https://{searchResource.Data.Name.ToLowerInvariant()}.search.windows.net";

        var connectionStrings = context.UserSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.ConnectionString;

        // Search Service Contributor role
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#search-service-contributor
        var searchServiceContributorId = CreateRoleDefinitionId(context.Subscription, "7ca78c08-252a-4471-8644-bb5ff32d4ba0");

        // Search Index Data Contributor role
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#search-index-data-contributor
        var searchIndexDataContributorId = CreateRoleDefinitionId(context.Subscription, "8ebe5a00-799e-43f5-93ac-243d3dce84a7");

        var t0 = DoRoleAssignmentAsync(context.ArmClient, searchResource.Id, context.Principal.Id, searchServiceContributorId, cancellationToken);
        var t1 = DoRoleAssignmentAsync(context.ArmClient, searchResource.Id, context.Principal.Id, searchIndexDataContributorId, cancellationToken);

        await Task.WhenAll(t0, t1).ConfigureAwait(false);
    }
}
