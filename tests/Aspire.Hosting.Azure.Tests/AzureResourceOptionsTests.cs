// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureResourceOptionsTests(ITestOutputHelper output)
{
    /// <summary>
    /// Ensures that an AzureProvisioningOptions can be configured to modify the ProvisioningContext
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
                options.ProvisioningContext.PropertyResolvers.Insert(0, new AspireV8ResourceNamePropertyResolver());
            });

            var serviceBus = builder.AddAzureServiceBus("sb");

            using var app = builder.Build();
            await app.StartAsync();

            var actualBicep = await File.ReadAllTextAsync(Path.Combine(tempDir.FullName, "sb.module.bicep"));

            var expectedBicep = """
                @description('The location for the resource(s) to be deployed.')
                param location string = resourceGroup().location

                param sku string = 'Standard'

                param principalId string

                param principalType string

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

                resource sb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                    principalType: principalType
                  }
                  scope: sb
                }

                output serviceBusEndpoint string = sb.properties.serviceBusEndpoint
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
