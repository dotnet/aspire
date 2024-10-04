// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSqlExtensionsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureSqlServer(bool publishMode)
    {
        using var builder = TestDistributedApplicationBuilder.Create(publishMode ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run);

        var sql = builder.AddAzureSqlServer("sql");

        sql.AddDatabase("db1");
        sql.AddDatabase("db2", "db2Name");

        var manifest = await ManifestUtils.GetManifestWithBicep(sql.Resource);

        var principalTypeParam = "";
        if (!publishMode)
        {
            principalTypeParam = """
                ,
                    "principalType": ""
                """;
        }
        var expectedManifest = $$"""
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sql.module.bicep",
              "params": {
                "principalId": "",
                "principalName": ""{{principalTypeParam}}
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var allowAllIpsFirewall = "";
        var bicepPrincipalTypeParam = "";
        var bicepPrincipalTypeSetter = "";
        if (!publishMode)
        {
            allowAllIpsFirewall = """

                resource sqlFirewallRule_AllowAllIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
                  name: 'AllowAllIps'
                  properties: {
                    endIpAddress: '255.255.255.255'
                    startIpAddress: '0.0.0.0'
                  }
                  parent: sql
                }
                
                """;

            bicepPrincipalTypeParam = """
                
                param principalType string
                
                """;

            bicepPrincipalTypeSetter = """

                      principalType: principalType
                """;
        }

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string
            {{bicepPrincipalTypeParam}}
            resource sql 'Microsoft.Sql/servers@2021-11-01' = {
              name: take('sql-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administrators: {
                  administratorType: 'ActiveDirectory'{{bicepPrincipalTypeSetter}}
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
                minimalTlsVersion: '1.2'
                publicNetworkAccess: 'Enabled'
                version: '12.0'
              }
              tags: {
                'aspire-resource-name': 'sql'
              }
            }

            resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: sql
            }
            {{allowAllIpsFirewall}}
            resource db1 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db1'
              location: location
              parent: sql
            }

            resource db2 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db2Name'
              location: location
              parent: sql
            }

            output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureSqlServerRunAsContainerProducesCorrectConnectionString(bool addDbBeforeRunAsContainer)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var sql = builder.AddAzureSqlServer("sql");

        IResourceBuilder<AzureSqlDatabaseResource> db1 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db2 = null!;
        if (addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");

        }
        sql.RunAsContainer(c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
        });

        if (!addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
        }

        Assert.False(sql.Resource.IsContainer(), "The resource should still be the Azure Resource.");
        var serverConnectionString = await sql.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", serverConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true", serverConnectionString);

        var db1ConnectionString = await db1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db1ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db1", db1ConnectionString);

        var db2ConnectionString = await db2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db2ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db2Name", db2ConnectionString);
    }
}
