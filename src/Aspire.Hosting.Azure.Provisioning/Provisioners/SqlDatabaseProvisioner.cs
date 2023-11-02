// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class SqlDatabaseProvisioner(ILogger<SqlDatabaseProvisioner> logger) : AzureResourceProvisioner<AzureSqlDatabaseResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureSqlDatabaseResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string connectionString)
        {
            resource.ConnectionString = connectionString;
            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        AzureSqlDatabaseResource resource,
        UserPrincipal principal,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {
        resourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not SqlDatabaseResource)
        {
            logger.LogWarning("Resource {resourceName} is not a SQL database resource. Deleting it.", resource.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var sqlServerResource = GetServer(resource, resourceMap);
        var sqlDatabaseResource = azureResource as SqlDatabaseResource;

        if (sqlDatabaseResource is null)
        {
            var sqlDatabaseName = resource.Name;

            logger.LogInformation("Creating SQL database {sqlServerName}/{sqlDatabaseName} in {location}...", sqlServerResource.Data.Name, sqlDatabaseName, location);

            var sqlDatabaseData = new SqlDatabaseData(location)
            {
                Sku = new SqlSku("S0")
            };
            sqlDatabaseData.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

            var sw = Stopwatch.StartNew();
            var operation = await sqlServerResource.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, sqlDatabaseName, sqlDatabaseData, cancellationToken).ConfigureAwait(false);
            sqlDatabaseResource = operation.Value;
            sw.Stop();

            logger.LogInformation("SQL database {sqlServerName} created in {elapsed}", sqlDatabaseResource.Data.Name, sw.Elapsed);
        }
        resource.ConnectionString = $"Server=tcp:{sqlServerResource.Data}.database.windows.net,1433;Initial Catalog={sqlDatabaseResource.Data.Name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.ConnectionString;

        static SqlServerResource GetServer(AzureSqlDatabaseResource resource, Dictionary<string, ArmResource> resourceMap)
        {
            resourceMap.TryGetValue(resource.Parent.Name, out var sqlServerResource);
            return (sqlServerResource as SqlServerResource)!;
        }
    }
}
