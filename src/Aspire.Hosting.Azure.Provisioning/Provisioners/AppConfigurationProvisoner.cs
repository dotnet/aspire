// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class AppConfigurationProvisioner(ILogger<AppConfigurationProvisioner> logger) : AzureResourceProvisioner<AzureAppConfigurationResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureAppConfigurationResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string endpoint)
        {
            resource.Endpoint = endpoint;
            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        AzureAppConfigurationResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
    {
        context.ResourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not AppConfigurationStoreResource)
        {
            logger.LogWarning("Resource {resourceName} is not a app configuration resource. Deleting it.", resource.Name);

            await context.ArmClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var appConfigurationResource = azureResource as AppConfigurationStoreResource;

        if (appConfigurationResource is null)
        {
            var appConfigurationName = Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20];

            logger.LogInformation("Creating app configuration {appConfigurationName} in {location}...", appConfigurationName, context.Location);

            var appConfigurationData = new AppConfigurationStoreData(context.Location, new AppConfigurationSku("free"));
            appConfigurationData.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

            var sw = ValueStopwatch.StartNew();
            var operation = await context.ResourceGroup.GetAppConfigurationStores().CreateOrUpdateAsync(WaitUntil.Completed, appConfigurationName, appConfigurationData, cancellationToken).ConfigureAwait(false);
            appConfigurationResource = operation.Value;

            logger.LogInformation("App Configuration {appConfigurationName} created in {elapsed}", appConfigurationResource.Data.Name, sw.GetElapsedTime());
        }
        resource.Endpoint = appConfigurationResource.Data.Endpoint;

        var connectionStrings = context.UserSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.Endpoint;

        // App Configuration Data Owner
        // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#app-configuration-data-owner
        var roleDefinitionId = CreateRoleDefinitionId(context.Subscription, "5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b");

        await DoRoleAssignmentAsync(context.ArmClient, appConfigurationResource.Id, context.Principal.Id, roleDefinitionId, cancellationToken).ConfigureAwait(false);
    }
}
