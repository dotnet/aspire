// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.Utils;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureServiceBusExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task NameResourcesAreReused()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

#pragma warning disable CS0618 // Type or member is obsolete
        serviceBus.AddQueue("queue1");
        serviceBus.AddQueue("queue1");
        serviceBus.WithQueue("queue1", queue => queue.DefaultMessageTimeToLive = TimeSpan.FromSeconds(1));
        serviceBus.AddTopic("topic1");
        serviceBus.AddTopic("topic1");
        serviceBus.WithTopic("topic1", topic =>
        {
            topic.DefaultMessageTimeToLive = TimeSpan.FromSeconds(1);
            var subscription = new ServiceBusSubscription("subscription1");
            subscription.Rules.Add(new ServiceBusRule("rule1"));
            topic.Subscriptions.Add(subscription);
        });
        serviceBus.AddSubscription("topic1", "subscription1");
        serviceBus.AddSubscription("topic1", "subscription2");
#pragma warning restore CS0618 // Type or member is obsolete

        var manifest = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

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

            resource queue1 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue1'
              properties: {
                defaultMessageTimeToLive: 'PT1S'
              }
              parent: sb
            }

            resource topic1 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 'topic1'
              properties: {
                defaultMessageTimeToLive: 'PT1S'
              }
              parent: sb
            }

            resource subscription1 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
              name: 'subscription1'
              parent: topic1
            }

            resource rule1 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = {
              name: 'rule1'
              properties: {
                filterType: 'CorrelationFilter'
              }
              parent: subscription1
            }
            
            resource subscription2 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
              name: 'subscription2'
              parent: topic1
            }

            output serviceBusEndpoint string = sb.properties.serviceBusEndpoint
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TopicNamesCanBeLongerThan24(bool useObsoleteMethods)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        if (useObsoleteMethods)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            serviceBus.AddTopic("device-connection-state-events1234567890-even-longer");
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else
        {
            serviceBus.WithTopic("device-connection-state-events1234567890-even-longer");
        }

        var manifest = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

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

    [Fact(Skip = "Azure ServiceBus emulator is not reliable in CI - https://github.com/dotnet/aspire/issues/7066")]
    [RequiresDocker]
    public async Task VerifyWaitForOnServiceBusEmulatorBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create(output);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddAzureServiceBus("resource")
                              .WithQueue("queue1")
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

    [Fact(Skip = "Azure ServiceBus emulator is not reliable in CI - https://github.com/dotnet/aspire/issues/7066")]
    [RequiresDocker]
    public async Task VerifyAzureServiceBusEmulatorResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(output);
        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator()
            .WithQueue("queue123");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration["ConnectionStrings:servicebusns"] = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureServiceBusClient("servicebusns");

        using var host = hb.Build();
        await host.StartAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(serviceBus.Resource.Name, KnownResourceStates.Running, cts.Token);
        await rns.WaitForResourceHealthyAsync(serviceBus.Resource.Name, cts.Token);

        var serviceBusClient = host.Services.GetRequiredService<ServiceBusClient>();

        await using var sender = serviceBusClient.CreateSender("queue123");
        await sender.SendMessageAsync(new ServiceBusMessage("Hello, World!"), cts.Token);

        await using var receiver = serviceBusClient.CreateReceiver("queue123");
        var message = await receiver.ReceiveMessageAsync(cancellationToken: cts.Token);

        Assert.Equal("Hello, World!", message.Body.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureServiceBusWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithHostPort(port);
        });

        Assert.Collection(
            serviceBus.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(port, e.Port)
            );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AddAzureServiceBusWithEmulatorGetsExpectedImageTag(string? imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        serviceBus.RunAsEmulator(container =>
        {
            if (!string.IsNullOrEmpty(imageTag))
            {
                container.WithImageTag(imageTag);
            }
        });

        var containerImageAnnotation = serviceBus.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        Assert.Equal(imageTag ?? ServiceBusEmulatorContainerImageTags.Tag, containerImageAnnotation.Tag);
        Assert.Equal(ServiceBusEmulatorContainerImageTags.Registry, containerImageAnnotation.Registry);
        Assert.Equal(ServiceBusEmulatorContainerImageTags.Image, containerImageAnnotation.Image);
    }

    [Fact]
    public async Task AzureServiceBusEmulatorResourceInitializesProvisioningModel()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        global::Azure.Provisioning.ServiceBus.ServiceBusQueue? queue = null;
        global::Azure.Provisioning.ServiceBus.ServiceBusTopic? topic = null;
        global::Azure.Provisioning.ServiceBus.ServiceBusSubscription? subscription = null;
        global::Azure.Provisioning.ServiceBus.ServiceBusRule? rule = null;

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .WithQueue("queue1", queue =>
            {
                queue.DeadLetteringOnMessageExpiration = true;
                queue.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                queue.ForwardDeadLetteredMessagesTo = "someQueue";
                queue.LockDuration = TimeSpan.FromMinutes(5);
                queue.MaxDeliveryCount = 10;
                queue.RequiresDuplicateDetection = true;
                queue.RequiresSession = true;
            })
            .WithTopic("topic1", topic =>
            {
                topic.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                topic.RequiresDuplicateDetection = true;

                var subscription = new ServiceBusSubscription("subscription1")
                {
                    DeadLetteringOnMessageExpiration = true,
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(1),
                    LockDuration = TimeSpan.FromMinutes(5),
                    MaxDeliveryCount = 10,
                    ForwardDeadLetteredMessagesTo = "",
                    RequiresSession = true,
                };
                topic.Subscriptions.Add(subscription);

                var rule = new ServiceBusRule("rule1")
                {
                    FilterType = ServiceBusFilterType.SqlFilter,
                    CorrelationFilter = new()
                    {
                        ContentType = "application/text",
                        CorrelationId = "id1",
                        Subject = "subject1",
                        MessageId = "msgid1",
                        ReplyTo = "someQueue",
                        ReplyToSessionId = "sessionId",
                        SessionId = "session1",
                        SendTo = "xyz"
                    }
                };
                subscription.Rules.Add(rule);
            })
            .ConfigureInfrastructure(infrastructure =>
            {
                queue = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusQueue>().Single();
                topic = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusTopic>().Single();
                subscription = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusSubscription>().Single();
                rule = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusRule>().Single();
            });

        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        Assert.NotNull(queue);
        Assert.Equal("queue1", queue.Name.Value);
        Assert.True(queue.DeadLetteringOnMessageExpiration.Value);
        Assert.Equal(TimeSpan.FromMinutes(1), queue.DefaultMessageTimeToLive.Value);
        Assert.Equal(TimeSpan.FromSeconds(20), queue.DuplicateDetectionHistoryTimeWindow.Value);
        Assert.Equal("someQueue", queue.ForwardDeadLetteredMessagesTo.Value);
        Assert.Equal(TimeSpan.FromMinutes(5), queue.LockDuration.Value);
        Assert.Equal(10, queue.MaxDeliveryCount.Value);
        Assert.True(queue.RequiresDuplicateDetection.Value);
        Assert.True(queue.RequiresSession.Value);

        Assert.NotNull(topic);
        Assert.Equal("topic1", topic.Name.Value);
        Assert.Equal(TimeSpan.FromMinutes(1), topic.DefaultMessageTimeToLive.Value);
        Assert.Equal(TimeSpan.FromSeconds(20), topic.DuplicateDetectionHistoryTimeWindow.Value);
        Assert.True(topic.RequiresDuplicateDetection.Value);

        Assert.NotNull(subscription);
        Assert.Equal("subscription1", subscription.Name.Value);
        Assert.True(subscription.DeadLetteringOnMessageExpiration.Value);
        Assert.Equal(TimeSpan.FromMinutes(1), subscription.DefaultMessageTimeToLive.Value);
        Assert.Equal(TimeSpan.FromMinutes(5), subscription.LockDuration.Value);
        Assert.Equal(10, subscription.MaxDeliveryCount.Value);
        Assert.Equal("", subscription.ForwardDeadLetteredMessagesTo.Value);
        Assert.True(subscription.RequiresSession.Value);

        Assert.NotNull(rule);
        Assert.Equal("rule1", rule.Name.Value);
        Assert.Equal(global::Azure.Provisioning.ServiceBus.ServiceBusFilterType.SqlFilter, rule.FilterType.Value);
        Assert.Equal("application/text", rule.CorrelationFilter.ContentType.Value);
        Assert.Equal("id1", rule.CorrelationFilter.CorrelationId.Value);
        Assert.Equal("subject1", rule.CorrelationFilter.Subject.Value);
        Assert.Equal("msgid1", rule.CorrelationFilter.MessageId.Value);
        Assert.Equal("someQueue", rule.CorrelationFilter.ReplyTo.Value);
        Assert.Equal("sessionId", rule.CorrelationFilter.ReplyToSessionId.Value);
        Assert.Equal("session1", rule.CorrelationFilter.SessionId.Value);
        Assert.Equal("xyz", rule.CorrelationFilter.SendTo.Value);
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureServiceBusEmulatorResourceGeneratesConfigJson()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator()
            .WithQueue("queue1", queue =>
            {
                queue.DeadLetteringOnMessageExpiration = true;
                queue.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                queue.ForwardDeadLetteredMessagesTo = "someQueue";
                queue.LockDuration = TimeSpan.FromMinutes(5);
                queue.MaxDeliveryCount = 10;
                queue.RequiresDuplicateDetection = true;
                queue.RequiresSession = true;
            })
            .WithTopic("topic1", topic =>
            {
                topic.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                topic.RequiresDuplicateDetection = true;

                var subscription = new ServiceBusSubscription("subscription1")
                {
                    DeadLetteringOnMessageExpiration = true,
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(1),
                    LockDuration = TimeSpan.FromMinutes(5),
                    MaxDeliveryCount = 10,
                    ForwardDeadLetteredMessagesTo = "",
                    RequiresSession = true,
                };
                topic.Subscriptions.Add(subscription);

                var rule = new ServiceBusRule("rule1")
                {
                    FilterType = ServiceBusFilterType.SqlFilter,
                    CorrelationFilter = new()
                    {
                        ContentType = "application/text",
                        CorrelationId = "id1",
                        Subject = "subject1",
                        MessageId = "msgid1",
                        ReplyTo = "someQueue",
                        ReplyToSessionId = "sessionId",
                        SessionId = "session1",
                        SendTo = "xyz"
                    }
                };
                subscription.Rules.Add(rule);
            });

        using var app = builder.Build();
        await app.StartAsync();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal(/*json*/"""
        {
          "UserConfig": {
            "Namespaces": [
              {
                "Name": "servicebusns",
                "Queues": [
                  {
                    "Name": "queue1",
                    "Properties": {
                      "DeadLetteringOnMessageExpiration": true,
                      "DefaultMessageTimeToLive": "PT1M",
                      "DuplicateDetectionHistoryTimeWindow": "PT20S",
                      "ForwardDeadLetteredMessagesTo": "someQueue",
                      "LockDuration": "PT5M",
                      "MaxDeliveryCount": 10,
                      "RequiresDuplicateDetection": true,
                      "RequiresSession": true
                    }
                  }
                ],
                "Topics": [
                  {
                    "Name": "topic1",
                    "Properties": {
                      "DefaultMessageTimeToLive": "PT1M",
                      "DuplicateDetectionHistoryTimeWindow": "PT20S",
                      "RequiresDuplicateDetection": true
                    },
                    "Subscriptions": [
                      {
                        "Name": "subscription1",
                        "Properties": {
                          "DeadLetteringOnMessageExpiration": true,
                          "DefaultMessageTimeToLive": "PT1M",
                          "ForwardDeadLetteredMessagesTo": "",
                          "LockDuration": "PT5M",
                          "MaxDeliveryCount": 10,
                          "RequiresSession": true
                        },
                        "Rules": [
                          {
                            "Name": "rule1",
                            "Properties": {
                              "FilterType": "Sql",
                              "CorrelationFilter": {
                                "CorrelationId": "id1",
                                "MessageId": "msgid1",
                                "To": "xyz",
                                "ReplyTo": "someQueue",
                                "Label": "subject1",
                                "SessionId": "session1",
                                "ReplyToSessionId": "sessionId",
                                "ContentType": "application/text"
                              }
                            }
                          }
                        ]
                      }
                    ]
                  }
                ]
              }
            ],
            "Logging": {
              "Type": "File"
            }
          }
        }
        """, configJsonContent);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureServiceBusEmulatorResourceGeneratesConfigJsonOnlyChangedProperties()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator()
            .WithQueue("queue1", queue =>
            {
                queue.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
            });

        using var app = builder.Build();
        await app.StartAsync();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal("""
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [
                      {
                        "Name": "queue1",
                        "Properties": {
                          "DefaultMessageTimeToLive": "PT1M"
                        }
                      }
                    ],
                    "Topics": []
                  }
                ],
                "Logging": {
                  "Type": "File"
                }
              }
            }
            """, configJsonContent);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureServiceBusEmulatorResourceGeneratesConfigJsonWithCustomizations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator(configure => configure.ConfigureEmulator(document =>
            {
                document["UserConfig"]!["Logging"] = new JsonObject { ["Type"] = "Console" };
            }));

        using var app = builder.Build();
        await app.StartAsync();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal("""
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [],
                    "Topics": []
                  }
                ],
                "Logging": {
                  "Type": "Console"
                }
              }
            }
            """, configJsonContent);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AzureServiceBusEmulator_WithConfigurationFile()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var configJsonPath = Path.GetTempFileName();

        File.WriteAllText(configJsonPath, """
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [ "queue456" ],
                    "Topics": []
                  }
                ]
              }
            }
            """);

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator(configure => configure.WithConfigurationFile(configJsonPath));

        using var app = builder.Build();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.Equal("/ServiceBus_Emulator/ConfigFiles/Config.json", volumeAnnotation.Target);

        Assert.Equal("""
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [ "queue456" ],
                    "Topics": []
                  }
                ]
              }
            }
            """, configJsonContent);

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
