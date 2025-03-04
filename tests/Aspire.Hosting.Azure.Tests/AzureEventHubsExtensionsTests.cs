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
                              .RunAsEmulator()
                              .WithHealthCheck("blocking_check");
        resource.AddHub("hubx");

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/6751")]
    public async Task VerifyAzureEventHubsEmulatorResource(bool referenceHub)
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var eventHubns = builder.AddAzureEventHubs("eventhubns")
            .RunAsEmulator();
        var eventHub = eventHubns.AddHub("hub");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        if (referenceHub)
        {
            hb.Configuration["ConnectionStrings:hub"] = await eventHub.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
            hb.AddAzureEventHubProducerClient("hub");
            hb.AddAzureEventHubConsumerClient("hub");
        }
        else
        {
            hb.Configuration["ConnectionStrings:eventhubns"] = await eventHubns.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
            hb.AddAzureEventHubProducerClient("eventhubns", settings => settings.EventHubName = "hub");
            hb.AddAzureEventHubConsumerClient("eventhubns", settings => settings.EventHubName = "hub");
        }

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
    [RequiresDocker]
    public async Task AzureEventHubsNs_ProducesAndConsumes()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var eventHubns = builder.AddAzureEventHubs("eventhubns")
            .RunAsEmulator();
        var eventHub = eventHubns.AddHub("hub");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration["ConnectionStrings:eventhubns"] = await eventHubns.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureEventHubProducerClient("eventhubns", settings => settings.EventHubName = "hub");
        hb.AddAzureEventHubConsumerClient("eventhubns", settings => settings.EventHubName = "hub");

        using var host = hb.Build();
        await host.StartAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceHealthyAsync(eventHubns.Resource.Name, cts.Token);

        var producerClient = host.Services.GetRequiredService<EventHubProducerClient>();
        var consumerClient = host.Services.GetRequiredService<EventHubConsumerClient>();

        // If no exception is thrown when awaited, the Event Hubs service has acknowledged
        // receipt and assumed responsibility for delivery of the set of events to its partition.
        await producerClient.SendAsync([new EventData(Encoding.UTF8.GetBytes("hello worlds"))], cts.Token);

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
    public async Task CanSetHubAndConsumerGroupName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh");

        eventHubs.AddHub("hub-resource", "hub-name")
            .WithProperties(hub => hub.PartitionCount = 3)
            .AddConsumerGroup("cg1", "group-name");

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

            resource hub_resource 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
              name: 'hub-name'
              properties: {
                partitionCount: 3
              }
              parent: eh
            }

            resource cg1 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2024-01-01' = {
              name: 'group-name'
              parent: hub_resource
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
            .ConfigureInfrastructure(infrastructure =>
            {
                hub = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.EventHubs.EventHub>().Single();
                cg = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup>().Single();
            });

        eventHubs.AddHub("hub1")
            .WithProperties(hub => hub.PartitionCount = 4)
            .AddConsumerGroup("cg1");

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
            .RunAsEmulator();

        eventHubs.AddHub("hub1")
            .WithProperties(hub => hub.PartitionCount = 4)
            .AddConsumerGroup("cg1");

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
            .RunAsEmulator(configure => configure
            .WithConfiguration(document =>
            {
                document["UserConfig"]!["LoggingConfig"] = new JsonObject { ["Type"] = "Console" };
            })
            .WithConfiguration(document =>
            {
                document["Custom"] = JsonValue.Create(42);
            }));

        eventHubs.AddHub("hub1");

        using var app = builder.Build();
        await app.StartAsync();

        var eventHubsEmulatorResource = builder.Resources.OfType<AzureEventHubsResource>().Single(x => x is { } eventHubsResource && eventHubsResource.IsEmulator);
        var volumeAnnotation = eventHubsEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        if (!OperatingSystem.IsWindows())
        {
            // Ensure the configuration file has correct attributes
            var fileInfo = new FileInfo(volumeAnnotation.Source!);

            var expectedUnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

            Assert.True(fileInfo.UnixFileMode.HasFlag(expectedUnixFileMode));
        }

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
          },
          "Custom": 42
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureEventHubsWithEmulator_SetsStorageLifetime(bool isPersistent)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var lifetime = isPersistent ? ContainerLifetime.Persistent : ContainerLifetime.Session;

        var serviceBus = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithLifetime(lifetime);
        });

        var azurite = builder.Resources.FirstOrDefault(x => x.Name == "eh-storage");

        Assert.NotNull(azurite);

        serviceBus.Resource.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var sbLifetimeAnnotation);
        azurite.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var sqlLifetimeAnnotation);

        Assert.Equal(lifetime, sbLifetimeAnnotation?.Lifetime);
        Assert.Equal(lifetime, sqlLifetimeAnnotation?.Lifetime);
    }

    [Fact]
    public void RunAsEmulator_CalledTwice_Throws()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureEventHubs("eh").RunAsEmulator();

        Assert.Throws<InvalidOperationException>(() => serviceBus.RunAsEmulator());
    }

    [Fact]
    public void AzureEventHubsHasCorrectConnectionStrings()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var eventHubs = builder.AddAzureEventHubs("eh");
        var eventHub = eventHubs.AddHub("hub1");
        var consumerGroup = eventHub.AddConsumerGroup("cg1");

        Assert.Equal("{eh.outputs.eventHubsEndpoint}", eventHubs.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("Endpoint={eh.outputs.eventHubsEndpoint};EntityPath=hub1", eventHub.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("Endpoint={eh.outputs.eventHubsEndpoint};EntityPath=hub1;ConsumerGroup=cg1", consumerGroup.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AzureEventHubsAppliesAzureFunctionsConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var eventHubs = builder.AddAzureEventHubs("eh");
        var eventHub = eventHubs.AddHub("hub1");
        var consumerGroup = eventHub.AddConsumerGroup("cg1");

        var target = new Dictionary<string, object>();
        ((IResourceWithAzureFunctionsConfig)eventHubs.Resource).ApplyAzureFunctionsConfiguration(target, "eh");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__eh__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__eh__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__eh__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__eh__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__eh__FullyQualifiedNamespace", k),
            k => Assert.Equal("eh__fullyQualifiedNamespace", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)eventHub.Resource).ApplyAzureFunctionsConfiguration(target, "hub1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__hub1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__hub1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__hub1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__hub1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__hub1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__hub1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__hub1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__hub1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__hub1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__hub1__FullyQualifiedNamespace", k),
            k => Assert.Equal("hub1__fullyQualifiedNamespace", k));
        Assert.Equal("hub1", target["Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__hub1__EventHubName"]);

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)consumerGroup.Resource).ApplyAzureFunctionsConfiguration(target, "cg1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__cg1__ConsumerGroup", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__cg1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__cg1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__cg1__ConsumerGroup", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__cg1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__cg1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__cg1__ConsumerGroup", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__cg1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__cg1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__cg1__ConsumerGroup", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__cg1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__cg1__FullyQualifiedNamespace", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__cg1__ConsumerGroup", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__cg1__EventHubName", k),
            k => Assert.Equal("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__cg1__FullyQualifiedNamespace", k),
            k => Assert.Equal("cg1__fullyQualifiedNamespace", k));
        Assert.Equal("cg1", target["Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__cg1__ConsumerGroup"]);
        Assert.Equal("hub1", target["Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__cg1__EventHubName"]);
    }
}
