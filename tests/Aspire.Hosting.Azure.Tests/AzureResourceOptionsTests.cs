// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureResourceOptionsTests(ITestOutputHelper output)
{
    /// <summary>
    /// Ensures that an AzureProvisioningOptions can be configured to modify the ProvisioningBuildOptions
    /// used when building the bicep for an Azure resource.
    ///
    /// This uses the .NET Aspire v8.x naming policy, which always calls toLower, appends a unique string with no separator,
    /// and uses a max of 24 characters.
    /// </summary>
    [Fact]
    public async Task AzureResourceOptionsCanBeConfigured()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        var outputPath = Path.Combine(tempDir.FullName, "aspire-manifest.json");

        using (var builder = TestDistributedApplicationBuilder.Create("Publishing:Publisher=manifest", "--output-path", outputPath))
        {
            builder.Services.Configure<AzureProvisioningOptions>(options =>
            {
                options.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new AspireV8ResourceNamePropertyResolver());
            });

            var serviceBus = builder.AddAzureServiceBus("sb");

            // ensure that resources with a hyphen still have a hyphen in the bicep name
            var sqlDatabase = builder.AddAzureSqlServer("sql-server")
                .RunAsContainer(x => x.WithLifetime(ContainerLifetime.Persistent))
                .AddDatabase("evadexdb").WithDefaultAzureSku();

            using var app = builder.Build();
            await app.StartAsync();

            var actualBicep = await File.ReadAllTextAsync(Path.Combine(tempDir.FullName, "sb.module.bicep"));

            var expectedBicep = """
                @description('The location for the resource(s) to be deployed.')
                param location string = resourceGroup().location

                param sku string = 'Standard'

                resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
                  name: toLower(take('sb${uniqueString(resourceGroup().id)}', 24))
                  location: location
                  properties: {
                    disableLocalAuth: true
                  }
                  sku: {
                    name: sku
                  }
                  tags: {
                    'aspire-resource-name': 'sb'
                  }
                }

                output serviceBusEndpoint string = sb.properties.serviceBusEndpoint

                output name string = sb.name
                """;
            output.WriteLine(actualBicep);
            Assert.Equal(expectedBicep, actualBicep);

            actualBicep = await File.ReadAllTextAsync(Path.Combine(tempDir.FullName, "sql-server.module.bicep"));

            expectedBicep = """
                @description('The location for the resource(s) to be deployed.')
                param location string = resourceGroup().location

                param principalId string

                param principalName string

                resource sql_server 'Microsoft.Sql/servers@2021-11-01' = {
                  name: toLower(take('sql-server${uniqueString(resourceGroup().id)}', 24))
                  location: location
                  properties: {
                    administrators: {
                      administratorType: 'ActiveDirectory'
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
                    'aspire-resource-name': 'sql-server'
                  }
                }

                resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
                  name: 'AllowAllAzureIps'
                  properties: {
                    endIpAddress: '0.0.0.0'
                    startIpAddress: '0.0.0.0'
                  }
                  parent: sql_server
                }

                resource evadexdb 'Microsoft.Sql/servers/databases@2021-11-01' = {
                  name: 'evadexdb'
                  location: location
                  parent: sql_server
                }

                output sqlServerFqdn string = sql_server.properties.fullyQualifiedDomainName

                output name string = sql_server.name
                """;
            output.WriteLine(actualBicep);
            Assert.Equal(expectedBicep, actualBicep);

            await app.StopAsync();
        }

        try
        {
            tempDir.Delete(recursive: true);
        }
        catch (IOException ex)
        {
            output.WriteLine($"Failed to delete {tempDir.FullName} : {ex.Message}. Ignoring.");
        }
    }
}
