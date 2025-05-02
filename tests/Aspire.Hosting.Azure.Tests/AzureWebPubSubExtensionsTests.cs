// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

using Azure.Provisioning.WebPubSub;

using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureWebPubSubExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public void InvalidWebPubSubHubNameThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1");
        var ex = Assert.Throws<ArgumentException>(() => wps.AddHub("a_b_c"));
        Assert.StartsWith("Resource name 'a_b_c' is invalid.", ex.Message);
    }

    [Fact]
    public async Task AddWebPubSubHubNameWithSpecialChars()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        WebPubSubHub? realHub = null;
        var wps = builder.AddAzureWebPubSub("wps1").ConfigureInfrastructure(infrastructure =>
        {
            realHub = infrastructure.GetProvisionableResources().OfType<WebPubSubHub>().Single();
        });
        var hubName = "a-b-c";
        var hub = wps.AddHub(hubName);

        Assert.Equal(hubName, hub.Resource.Name);
        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        Assert.NotNull(realHub);
        Assert.Equal(hubName, realHub.Name.Value);
        Assert.Equal("a_b_c", realHub.BicepIdentifier);
    }

    [Fact]
    public async Task AddAzureWebPubSubHubWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1");
        wps.AddHub("abc");

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{wps1.outputs.endpoint}",
              "path": "wps1.module.bicep"
            }
            """;

        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        var manifestString = manifest.ManifestNode.ToString();
        output.WriteLine(manifestString);
        output.WriteLine(manifest.BicepText);

        Assert.Equal(expectedManifest, manifestString);
        Assert.Equal("wps1", wps.Resource.Name);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            resource abc 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
              name: 'abc'
              parent: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public void AddAzureWebPubSub_HasCorrectConnectionExpressions()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1");
        var hub = wps.AddHub("abc");
        var otherHub = wps.AddHub("def", "hij");

        Assert.Equal("{wps1.outputs.endpoint}", wps.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("Endpoint={wps1.outputs.endpoint};Hub=abc", hub.Resource.ConnectionStringExpression.ValueExpression);
        // Uses hub name instead of resource name since it was explicitly provided
        Assert.Equal("Endpoint={wps1.outputs.endpoint};Hub=hij", otherHub.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddWebPubSubWithHubConfigure()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var hubName = "abc";
        var wps = builder.AddAzureWebPubSub("wps1").ConfigureInfrastructure(infrastructure =>
        {
            var hub = infrastructure.GetProvisionableResources().OfType<WebPubSubHub>().First(i => i.BicepIdentifier == hubName);
            hub.Properties.AnonymousConnectPolicy = "allow";
        });
        wps.AddHub(hubName);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{wps1.outputs.endpoint}",
              "path": "wps1.module.bicep"
            }
            """;

        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        var manifestString = manifest.ManifestNode.ToString();
        output.WriteLine(manifestString);
        output.WriteLine(manifest.BicepText);

        Assert.Equal(expectedManifest, manifestString);
        Assert.Equal("wps1", wps.Resource.Name);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            resource abc 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
              name: 'abc'
              properties: {
                anonymousConnectPolicy: 'allow'
              }
              parent: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureWebPubSubHubWithEventHandlerExpressionWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceA = builder.AddProject<ProjectA>("serviceA", o => o.ExcludeLaunchProfile = true).WithHttpsEndpoint();
        var wps = builder.AddAzureWebPubSub("wps1");
        wps.AddHub("abc").AddEventHandler($"{serviceA.GetEndpoint("https")}/eventhandler/");

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{wps1.outputs.endpoint}",
              "path": "wps1.module.bicep",
              "params": {
                "abc_url_0": "{serviceA.bindings.https.url}/eventhandler/"
              }
            }
            """;

        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        var manifestString = manifest.ManifestNode.ToString();
        output.WriteLine(manifestString);
        output.WriteLine(manifest.BicepText);

        Assert.Equal(expectedManifest, manifestString);
        Assert.Equal("wps1", wps.Resource.Name);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            param abc_url_0 string

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            resource abc 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
              name: 'abc'
              properties: {
                eventHandlers: [
                  {
                    urlTemplate: abc_url_0
                    userEventPattern: '*'
                  }
                ]
              }
              parent: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task ConfigureConstructOverridesAddEventHandler()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceA = builder.AddProject<ProjectA>("serviceA", o => o.ExcludeLaunchProfile = true).WithHttpsEndpoint();
        var wps = builder.AddAzureWebPubSub("wps1").ConfigureInfrastructure(infrastructure =>
        {
            var hub = infrastructure.GetProvisionableResources().OfType<WebPubSubHub>().First(i => string.Equals(i.BicepIdentifier, "abc", StringComparison.OrdinalIgnoreCase));
            hub.Properties.EventHandlers.Add(new WebPubSubEventHandler() { UrlTemplate = "http://fake.com" });
        });
        wps.AddHub("ABC").AddEventHandler($"http://fake1.com");
        // Hub name is case insensitive
        wps.AddHub("abc");

        wps.Resource.Outputs["endpoint"] = "https://mywebpubsubendpoint";
        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        var manifestString = manifest.ManifestNode.ToString();
        output.WriteLine(manifestString);
        output.WriteLine(manifest.BicepText);

        Assert.Equal("wps1", wps.Resource.Name);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            resource ABC 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
              name: 'ABC'
              properties: {
                eventHandlers: [
                  {
                    urlTemplate: 'http://fake1.com'
                    userEventPattern: '*'
                  }
                  {
                    urlTemplate: 'http://fake.com'
                  }
                ]
              }
              parent: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureWebPubSubHubSettings()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceA = builder.AddProject<ProjectA>("serviceA", o => o.ExcludeLaunchProfile = true).WithHttpsEndpoint();
        var url1 = "fake3.com";
        var wps = builder.AddAzureWebPubSub("wps1").ConfigureInfrastructure(infrastructure =>
        {
            var hub = infrastructure.GetProvisionableResources().OfType<WebPubSubHub>().First(i => i.BicepIdentifier == "hub1");
            hub.Properties.AnonymousConnectPolicy = "allow";
            // allow directly event handler set
            hub.Properties.EventHandlers.Add(new WebPubSubEventHandler() { UrlTemplate = "http://fake1.com" });
        });
        // allow event handler set using a separate call
        // allow mulitple calls, and order matters
        wps.AddHub("hub1")
            .AddEventHandler($"http://fake2.com", "event1", authSettings: new UpstreamAuthSettings())
            .AddEventHandler($"http://{url1}", systemEvents: ["connect"], authSettings: new UpstreamAuthSettings { AuthType = UpstreamAuthType.ManagedIdentity, ManagedIdentityResource = "abc" });
        wps.AddHub("hub2")
            .AddEventHandler($"{serviceA.GetEndpoint("https")}/hub/eventhandler1")
            .AddEventHandler($"{serviceA.GetEndpoint("https")}/eventhandler2")
            .AddEventHandler($"{serviceA.GetEndpoint("https")}/eventhandler3", "event1", ["connect", "connected"]);

        wps.Resource.Outputs["endpoint"] = "https://mywebpubsubendpoint";

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{wps1.outputs.endpoint}",
              "path": "wps1.module.bicep",
              "params": {
                "hub2_url_0": "{serviceA.bindings.https.url}/hub/eventhandler1",
                "hub2_url_1": "{serviceA.bindings.https.url}/eventhandler2",
                "hub2_url_2": "{serviceA.bindings.https.url}/eventhandler3"
              }
            }
            """;

        var connectionStringResource = (IResourceWithConnectionString)wps.Resource;

        Assert.Equal("https://mywebpubsubendpoint", await connectionStringResource.GetConnectionStringAsync());
        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        var manifestString = manifest.ManifestNode.ToString();
        output.WriteLine(manifestString);
        output.WriteLine(manifest.BicepText);

        Assert.Equal(expectedManifest, manifestString);

        Assert.Equal("wps1", wps.Resource.Name);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            param hub2_url_0 string

            param hub2_url_1 string

            param hub2_url_2 string

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            resource hub1 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
              name: 'hub1'
              properties: {
                eventHandlers: [
                  {
                    urlTemplate: 'http://fake2.com'
                    userEventPattern: 'event1'
                  }
                  {
                    urlTemplate: 'http://fake3.com'
                    userEventPattern: '*'
                    systemEvents: [
                      'connect'
                    ]
                    auth: {
                      type: 'ManagedIdentity'
                      managedIdentity: {
                        resource: 'abc'
                      }
                    }
                  }
                  {
                    urlTemplate: 'http://fake1.com'
                  }
                ]
                anonymousConnectPolicy: 'allow'
              }
              parent: wps1
            }

            resource hub2 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
              name: 'hub2'
              properties: {
                eventHandlers: [
                  {
                    urlTemplate: hub2_url_0
                    userEventPattern: '*'
                  }
                  {
                    urlTemplate: hub2_url_1
                    userEventPattern: '*'
                  }
                  {
                    urlTemplate: hub2_url_2
                    userEventPattern: 'event1'
                    systemEvents: [
                      'connect'
                      'connected'
                    ]
                  }
                ]
              }
              parent: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public void AddHub_WithDifferentNameAndHubName_SetsPropertiesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1");

        var hub1 = wps.AddHub("hub1");
        var hub2 = wps.AddHub("resource2", "hub2");
        var hub3 = wps.AddHub("hub3", "hub3");
        var hub4 = wps.AddHub("hub4", null);

        Assert.Equal("hub1", hub1.Resource.Name);
        Assert.Equal("resource2", hub2.Resource.Name);
        Assert.Equal("hub3", hub3.Resource.Name);
        Assert.Equal("hub4", hub4.Resource.Name);

        Assert.Equal("hub1", hub1.Resource.HubName);
        Assert.Equal("hub2", hub2.Resource.HubName);
        Assert.Equal("hub3", hub3.Resource.HubName);
        Assert.Equal("hub4", hub4.Resource.HubName);

        Assert.Equal("Endpoint={wps1.outputs.endpoint};Hub=hub1", hub1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("Endpoint={wps1.outputs.endpoint};Hub=hub2", hub2.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("Endpoint={wps1.outputs.endpoint};Hub=hub3", hub3.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("Endpoint={wps1.outputs.endpoint};Hub=hub4", hub4.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AddHub_CalledTwiceWithSameHubName_ReturnsSameResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1");

        // Call AddHub twice with the same hub name but different resource names
        var hub1 = wps.AddHub("resource1", "same-hub");
        var hub2 = wps.AddHub("resource2", "same-hub");

        // Verify both calls return the same hub resource (only the first one is registered)
        Assert.Same(hub1.Resource, hub2.Resource);
        Assert.Equal("resource1", hub1.Resource.Name);
        Assert.Equal("same-hub", hub1.Resource.HubName);
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";
    }
}
