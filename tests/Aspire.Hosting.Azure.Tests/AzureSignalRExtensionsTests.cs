// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSignalRExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr");

        var manifest = await ManifestUtils.GetManifestWithBicep(signalr.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Endpoint=https://{signalr.outputs.hostName};AuthType=azure",
              "path": "signalr.module.bicep",
              "params": {
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalType string

            param principalId string

            resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' = {
              name: take('signalr-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                cors: {
                  allowedOrigins: [
                    '*'
                  ]
                }
                features: [
                  {
                    flag: 'ServiceMode'
                    value: 'Default'
                  }
                ]
              }
              kind: 'SignalR'
              sku: {
                name: 'Free_F1'
                capacity: 1
              }
              tags: {
                'aspire-resource-name': 'signalr'
              }
            }

            resource signalr_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalType: principalType
              }
              scope: signalr
            }

            output hostName string = signalr.properties.hostName
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddServerlessAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless);

        var manifest = await ManifestUtils.GetManifestWithBicep(signalr.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Endpoint=https://{signalr.outputs.hostName};AuthType=azure",
              "path": "signalr.module.bicep",
              "params": {
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalType string

            param principalId string

            resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' = {
              name: take('signalr-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                cors: {
                  allowedOrigins: [
                    '*'
                  ]
                }
                features: [
                  {
                    flag: 'ServiceMode'
                    value: 'Serverless'
                  }
                ]
              }
              kind: 'SignalR'
              sku: {
                name: 'Free_F1'
                capacity: 1
              }
              tags: {
                'aspire-resource-name': 'signalr'
              }
            }

            resource signalr_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalType: principalType
              }
              scope: signalr
            }

            resource signalr_SignalRRestApiOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'fd53cd77-2268-407a-8f46-7e7863d0f521'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'fd53cd77-2268-407a-8f46-7e7863d0f521')
                principalType: principalType
              }
              scope: signalr
            }

            output hostName string = signalr.properties.hostName
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }
}
