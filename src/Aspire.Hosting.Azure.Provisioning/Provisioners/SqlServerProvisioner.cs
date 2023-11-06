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

internal sealed class SqlServerProvisioner(ILogger<SqlServerProvisioner> logger) : AzureResourceProvisioner<AzureSqlServerResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureSqlServerResource resource, IEnumerable<IAzureChildResource> children)
    {
        if (configuration.GetConnectionString(resource.Name) is string hostname)
        {
            resource.Hostname = hostname;

            foreach (var database in children.OfType<AzureSqlDatabaseResource>())
            {
                if (configuration.GetConnectionString(database.Name) is string connectionString)
                {
                    database.ConnectionString = connectionString;
                }
                else
                {
                    return false;
                }
            }
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
        AzureSqlServerResource resource,
        IEnumerable<IAzureChildResource> children,
        UserPrincipal principal,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {
        resourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not SqlServerResource)
        {
            logger.LogWarning("Resource {resourceName} is not a SQL server resource. Deleting it.", resource.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var sqlServerResource = azureResource as SqlServerResource;

        if (sqlServerResource is null)
        {
            var sqlServerName = Guid.NewGuid().ToString().Replace("-", string.Empty)[0..20];

            logger.LogInformation("Creating SQL server {sqlServerName} in {location}...", sqlServerName, location);

            var sqlServerData = new SqlServerData(location)
            {
                PublicNetworkAccess = ServerNetworkAccessFlag.Enabled,
                Administrators = new ServerExternalAdministrator
                {
                    PrincipalType = SqlServerPrincipalType.User,
                    Login = principal.Name,
                    Sid = principal.Id,
                    TenantId = subscription.Data.TenantId,
                    IsAzureADOnlyAuthenticationEnabled = true,
                },
            };
            sqlServerData.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

            var sw = Stopwatch.StartNew();
            var operation = await resourceGroup.GetSqlServers().CreateOrUpdateAsync(WaitUntil.Completed, sqlServerName, sqlServerData, cancellationToken).ConfigureAwait(false);
            sqlServerResource = operation.Value;
            sw.Stop();

            logger.LogInformation("SQL server {sqlServerName} created in {elapsed}", sqlServerResource.Data.Name, sw.Elapsed);
        }
        resource.Hostname = sqlServerResource.Data.Name + ".database.windows.net";

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.Hostname;

        await AddFirewallRule(sqlServerResource).ConfigureAwait(false);

        foreach (var database in children.OfType<AzureSqlDatabaseResource>())
        {
            var connectionString = await CreateDatabaseIfNotExists(sqlServerResource, database, cancellationToken).ConfigureAwait(false);

            database.ConnectionString = connectionString;
            connectionStrings[database.Name] = connectionString;
        }
    }

    private async Task<string> CreateDatabaseIfNotExists(
        SqlServerResource sqlServerResource,
        AzureSqlDatabaseResource database,
        CancellationToken cancellationToken)
    {
        SqlDatabaseResource sqlDatabaseResource;
        var exists = await sqlServerResource.GetSqlDatabases().ExistsAsync(database.Name, cancellationToken).ConfigureAwait(false);

        if (exists)
        {
            sqlDatabaseResource = await sqlServerResource.GetSqlDatabases().GetAsync(database.Name, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation("Creating SQL database {sqlServerName}/{sqlDatabaseName} in {location}...", sqlServerResource.Data.Name, database.Name, sqlServerResource.Data.Location);

            var sqlDatabaseData = new SqlDatabaseData(sqlServerResource.Data.Location)
            {
                Sku = new SqlSku("S0")
            };
            sqlDatabaseData.Tags.Add(AzureProvisioner.AspireResourceNameTag, database.Name);

            var sw = Stopwatch.StartNew();
            var operation = await sqlServerResource.GetSqlDatabases().CreateOrUpdateAsync(WaitUntil.Completed, database.Name, sqlDatabaseData, cancellationToken).ConfigureAwait(false);
            sqlDatabaseResource = operation.Value;
            sw.Stop();

            logger.LogInformation("SQL database {sqlServerName} created in {elapsed}", sqlDatabaseResource.Data.Name, sw.Elapsed);
        }

        return $"Server=tcp:{sqlServerResource.Data.FullyQualifiedDomainName},1433;Initial Catalog={sqlDatabaseResource.Data.Name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";
    }

    private async Task AddFirewallRule(SqlServerResource sqlServerResource)
    {
        var ipAddress = await GetPublicIp().ConfigureAwait(false);
        var ruleName = $"Allow_{ipAddress}";
        var firewallRules = sqlServerResource.GetSqlFirewallRules();
        if (!await firewallRules.ExistsAsync(ruleName).ConfigureAwait(false))
        {
            logger.LogInformation("Creating firewall rule for SQL server {sqlServerName}...", sqlServerResource.Data.Name);
            var data = new SqlFirewallRuleData
            {
                StartIPAddress = ipAddress,
                EndIPAddress = ipAddress,
            };
            await firewallRules.CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data).ConfigureAwait(false);
            logger.LogInformation("Firewall rule for SQL server {sqlServerName} created", sqlServerResource.Data.Name);
        }

        static async Task<string> GetPublicIp()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("https://checkip.amazonaws.com").ConfigureAwait(false);
            return (await response.Content.ReadAsStringAsync().ConfigureAwait(false)).TrimEnd();
        }
    }
}
