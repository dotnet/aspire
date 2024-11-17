// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureServiceBusExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task TopicNamesCanBeLongerThan24()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        serviceBus.AddTopic("device-connection-state-events1234567890-even-longer");

        var manifest = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalId string

            param principalType string

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
              name: take('sb-${uniqueString(resourceGroup().id)}', 50)
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

            resource device_connection_state_events1234567890_even_longer 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 'device-connection-state-events1234567890-even-longer'
              parent: sb
            }

            output serviceBusEndpoint string = sb.properties.serviceBusEndpoint
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }
}
