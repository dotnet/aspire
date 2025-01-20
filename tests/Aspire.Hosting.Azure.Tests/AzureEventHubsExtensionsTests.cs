// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json.Nodes;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.EventHubs;
using Aspire.Hosting.Utils;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureEventHubsExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7175")]
    public async Task VerifyWaitForOnEventHubsEmulatorBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddAzureEventHubs("resource")
                              .WithHub("hubx")
                              .RunAsEmulator()
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/6751")]
    public async Task VerifyAzureEventHubsEmulatorResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var eventHub = builder.AddAzureEventHubs("eventhubns")
            .RunAsEmulator()
            .WithHub("hub");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration["ConnectionStrings:eventhubns"] = await eventHub.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureEventHubProducerClient("eventhubns", settings => settings.EventHubName = "hub");
        hb.AddAzureEventHubConsumerClient("eventhubns", settings => settings.EventHubName = "hub");

        using var host = hb.Build();
        await host.StartAsync();

        var producerClient = host.Services.GetRequiredService<EventHubProducerClient>();
        var consumerClient = host.Services.GetRequiredService<EventHubConsumerClient>();

        // If no exception is thrown when awaited, the Event Hubs service has acknowledged
        // receipt and assumed responsibility for delivery of the set of events to its partition.
        await producerClient.SendAsync([new EventData(Encoding.UTF8.GetBytes("hello worlds"))]);

        await foreach (var partitionEvent in consumerClient.ReadEventsAsync(new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(5) }))
        {
            Assert.Equal("hello worlds", Encoding.UTF8.GetString(partitionEvent.Data.EventBody.ToArray()));
            break;
        }
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotationWithDefaultPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataBindMount();
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal(Path.Combine(builder.AppHostDirectory, ".eventhubs", "eh"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataBindMount("mydata");
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotationWithDefaultName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataVolume();
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal($"{builder.GetVolumePrefix()}-eh-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataVolume("mydata");
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal("mydata", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AzureEventHubsWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithHostPort(port);
        });

        Assert.Collection(
            eventHubs.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(port, e.Port)
            );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AzureEventHubsWithEmulatorGetsExpectedImageTag(string? imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eventhubs");

        eventHubs.RunAsEmulator(container =>
        {
            if (!string.IsNullOrEmpty(imageTag))
            {
                container.WithImageTag(imageTag);
            }
        });

        var containerImageAnnotation = eventHubs.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        Assert.Equal(imageTag ?? EventHubsEmulatorContainerImageTags.Tag, containerImageAnnotation.Tag);
        Assert.Equal(EventHubsEmulatorContainerImageTags.Registry, containerImageAnnotation.Registry);
        Assert.Equal(EventHubsEmulatorContainerImageTags.Image, containerImageAnnotation.Image);
    }

    [Fact]
    public async Task NamedResourcesAreReused()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh");

        eventHubs.WithHub("hub1");
        eventHubs.WithHub("hub1");
        eventHubs.WithHub("hub1", hub => hub.PartitionCount = 3);
        eventHubs.WithHub("hub1", hub => hub.ConsumerGroups.Add(new("cg1")));

        var manifest = await ManifestUtils.GetManifestWithBicep(eventHubs.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

            resource eh 'Microsoft.EventHub/namespaces@2024-01-01' = {
              name: take('eh-${uniqueString(resourceGroup().id)}', 256)
              location: location
              sku: {
                name: sku
              }
              tags: {
                'aspire-resource-name': 'eh'
              }
            }

            resource eh_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(eh.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
                principalType: principalType
              }
              scope: eh
            }

            resource hub1 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
              name: 'hub1'
              properties: {
                partitionCount: 3
              }
              parent: eh
            }

            resource cg1 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2024-01-01' = {
              name: 'cg1'
              parent: hub1
            }

            output eventHubsEndpoint string = eh.properties.serviceBusEndpoint
            """;

        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AzureEventHubsEmulatorResourceInitializesProvisioningModel()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        global::Azure.Provisioning.EventHubs.EventHub? hub = null;
        global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup? cg = null;

        var eventHubs = builder.AddAzureEventHubs("eh")
            .WithHub("hub1", hub =>
            {
                hub.PartitionCount = 4;
                hub.ConsumerGroups.Add(new EventHubConsumerGroup("cg1"));
            })
            .ConfigureInfrastructure(infrastructure =>
            {
                hub = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.EventHubs.EventHub>().Single();
                cg = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup>().Single();
            });

        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestWithBicep(eventHubs.Resource);

        Assert.NotNull(hub);
        Assert.Equal("hub1", hub.Name.Value);
        Assert.Equal(4, hub.PartitionCount.Value);

        Assert.NotNull(cg);
        Assert.Equal("cg1", cg.Name.Value);
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureEventHubsEmulatorResourceGeneratesConfigJson()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var eventHubs = builder.AddAzureEventHubs("eh")
            .RunAsEmulator()
            .WithHub("hub1", hub =>
            {
                hub.PartitionCount = 4;
                hub.ConsumerGroups.Add(new EventHubConsumerGroup("cg1"));
            });

        using var app = builder.Build();
        await app.StartAsync();

        var eventHubsEmulatorResource = builder.Resources.OfType<AzureEventHubsResource>().Single(x => x is { } eventHubsResource && eventHubsResource.IsEmulator);
        var volumeAnnotation = eventHubsEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal(/*json*/"""
        {
          "UserConfig": {
            "NamespaceConfig": [
              {
                "Type": "EventHub",
                "Name": "emulatorNs1",
                "Entities": [
                  {
                    "Name": "hub1",
                    "PartitionCount": 4,
                    "ConsumerGroups": [
                      {
                        "Name": "cg1"
                      }
                    ]
                  }
                ]
              }
            ],
            "LoggingConfig": {
              "Type": "File"
            }
          }
        }
        """, configJsonContent);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureEventHubsEmulatorResourceGeneratesConfigJsonWithCustomizations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var eventHubs = builder
            .AddAzureEventHubs("eh")
            .WithHub("hub1")
            .RunAsEmulator(configure => configure.ConfigureEmulator(document =>
            {
                document["UserConfig"]!["LoggingConfig"] = new JsonObject { ["Type"] = "Console" };
            }));

        using var app = builder.Build();
        await app.StartAsync();

        var eventHubsEmulatorResource = builder.Resources.OfType<AzureEventHubsResource>().Single(x => x is { } eventHubsResource && eventHubsResource.IsEmulator);
        var volumeAnnotation = eventHubsEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal(/*json*/"""
        {
          "UserConfig": {
            "NamespaceConfig": [
              {
                "Type": "EventHub",
                "Name": "emulatorNs1",
                "Entities": [
                  {
                    "Name": "hub1",
                    "PartitionCount": 1,
                    "ConsumerGroups": []
                  }
                ]
              }
            ],
            "LoggingConfig": {
              "Type": "Console"
            }
          }
        }
        """, configJsonContent);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureEventHubsEmulator_WithConfigurationFile()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var configJsonPath = Path.GetTempFileName();

        var source = /*json*/"""
        {
          "UserConfig": {
            "NamespaceConfig": [
              {
                "Type": "EventHub",
                "Name": "emulatorNs1",
                "Entities": [
                  {
                    "Name": "hub1",
                    "PartitionCount": 2,
                    "ConsumerGroups": []
                  }
                ]
              }
            ],
            "LoggingConfig": {
              "Type": "Console"
            }
          }
        }
        """;

        File.WriteAllText(configJsonPath, source);

        var eventHubs = builder.AddAzureEventHubs("eh")
            .RunAsEmulator(configure => configure.WithConfigurationFile(configJsonPath));

        using var app = builder.Build();
        await app.StartAsync();

        var eventHubsEmulatorResource = builder.Resources.OfType<AzureEventHubsResource>().Single(x => x is { } eventHubsResource && eventHubsResource.IsEmulator);
        var volumeAnnotation = eventHubsEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal("/Eventhubs_Emulator/ConfigFiles/Config.json", volumeAnnotation.Target);

        Assert.Equal(source, configJsonContent);

        await app.StopAsync();

        try
        {
            File.Delete(configJsonPath);
        }
        catch
        {
        }
    }
}
