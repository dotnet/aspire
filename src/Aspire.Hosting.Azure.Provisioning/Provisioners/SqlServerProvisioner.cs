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
    public override bool ConfigureResource(IConfiguration configuration, AzureSqlServerResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string hostname)
        {
            resource.Hostname = hostname;
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

        // We need to add server to the resource map if it doesn't exist as we'll need it when provisioning databases
        // TODO: Should this actually be done in the enumerator?
        resourceMap.TryAdd(resource.Name,sqlServerResource);
    }

    private async Task AddFirewallRule(SqlServerResource sqlServerResource)
    {
        var ipAddress = await GetPublicIp().ConfigureAwait(false);
        var firewallRules = sqlServerResource.GetSqlFirewallRules();
        if (!await firewallRules.ExistsAsync(ipAddress).ConfigureAwait(false))
        {
            logger.LogInformation("Creating firewall rule for SQL server {sqlServerName}...", sqlServerResource.Data.Name);
            var data = new SqlFirewallRuleData
            {
                StartIPAddress = ipAddress,
                EndIPAddress = ipAddress,
            };
            await firewallRules.CreateOrUpdateAsync(WaitUntil.Completed, ipAddress, data).ConfigureAwait(false);
            logger.LogInformation("Firewall rule for SQL server {sqlServerName} created", sqlServerResource.Data.Name);
        }

        static async Task<string> GetPublicIp()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("https://ifconfig.me/ip").ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
