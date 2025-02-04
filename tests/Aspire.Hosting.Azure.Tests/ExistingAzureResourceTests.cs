// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class ExistingAzureResourceTests
{
    [Fact]
    public async Task AddExistingAzureServiceBusInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "messagingExisting": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param messagingExisting string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: messagingExisting
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public void ThrowsIfRunAsExistingCalledTwiceOnSameResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName);

        Assert.Throws<InvalidOperationException>(() => serviceBus.RunAsExisting(existingResourceName));
    }

    [Fact]
    public void ThrowsIfPublishAsExistingCalledTwiceOnSameResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName);

        Assert.Throws<InvalidOperationException>(() => serviceBus.PublishAsExisting(existingResourceName));
    }

    [Fact]
    public async Task RequiresPublishAsExistingInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
              name: take('messaging-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
                disableLocalAuth: true
              }
              sku: {
                name: sku
              }
              tags: {
                'aspire-resource-name': 'messaging'
              }
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task AddExistingAzureServiceBusInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "messagingExisting": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param messagingExisting string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: messagingExisting
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingServiceBusWithResourceGroupInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName, existingResourceGroupName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "messagingExisting": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param messagingExisting string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: messagingExisting
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;

        Assert.Equal(expectedBicep, BicepText);

    }
}
