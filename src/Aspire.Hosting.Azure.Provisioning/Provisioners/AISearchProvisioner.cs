// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.ResourceManager.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning.Provisioners;

internal sealed class AISearchProvisioner(ILogger<AISearchProvisioner> logger) : AzureResourceProvisioner<AzureAISearchResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureAISearchResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string connectionString)
        {
            resource.ConnectionString = connectionString;
            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        AzureAISearchResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
    {
        context.ResourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not SearchServiceResource)
        {
            logger.LogWarning("Resource {resourceName} is not an AI Search resource. Deleting it.", resource.Name);

            await context.ArmClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var aiSearchResource = azureResource as SearchServiceResource;

        if (aiSearchResource is null)
        {
            var aiSearchName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            var parameters = new SearchServiceData(context.Location);

            logger.LogInformation("Creating AI Search {aiSearchName} in {location}...", aiSearchName, context.Location);

            var aiSearchCreateOperation = await context.ResourceGroup
                .GetSearchServices().CreateOrUpdateAsync(WaitUntil.Completed, aiSearchName, parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
            aiSearchResource = aiSearchCreateOperation.Value;

            logger.LogInformation("AI Search {aiSearchName} created.", aiSearchResource.Data.Name);
        }

        // SearchServiceResource doesn't have an "Endpoint" property
        resource.ConnectionString = $"https://{aiSearchResource.Data.Name.ToLowerInvariant()}.search.windows.net";

        // Search Service Contributor role
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#search-service-contributor
        var searchServiceContributorId = CreateRoleDefinitionId(context.Subscription, "7ca78c08-252a-4471-8644-bb5ff32d4ba0");

        // Search Index Data Contributor role
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#search-index-data-contributor
        var searchIndexDataContributorId = CreateRoleDefinitionId(context.Subscription, "8ebe5a00-799e-43f5-93ac-243d3dce84a7");

        var t0 = DoRoleAssignmentAsync(context.ArmClient, aiSearchResource.Id, context.Principal.Id, searchServiceContributorId, cancellationToken);
        var t1 = DoRoleAssignmentAsync(context.ArmClient, aiSearchResource.Id, context.Principal.Id, searchIndexDataContributorId, cancellationToken);

        await Task.WhenAll(t0, t1).ConfigureAwait(false);
    }
}
