// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

public class ConformanceTests_Queue : ConformanceTests
{
    // A pre-existing queue
    private const string HealthCheckQueueName = "test";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            GetMainConfigEntry(key),
            new(CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", key, nameof(AzureMessagingServiceBusSettings.HealthCheckQueueName)), HealthCheckQueueName),
            new(CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", key, "ClientOptions:RetryOptions:MaxRetries"), "0")
        });

    protected override void SetHealthCheck(AzureMessagingServiceBusSettings options, bool enabled)
        => options.HealthCheckQueueName = enabled ? HealthCheckQueueName : default;

    protected override void TriggerActivity(ServiceBusClient service)
        => _ = service.CreateReceiver(queueName: HealthCheckQueueName).PeekMessageAsync().GetAwaiter().GetResult();

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null), EnableTracingForAzureSdk()).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key"), EnableTracingForAzureSdk()).Dispose();

    private static bool GetCanConnect()
    {
        ServiceBusClientOptions clientOptions = new();
        clientOptions.RetryOptions.MaxRetries = 0; // don't enable retries (test runs few times faster)
        ServiceBusClient client = new(FullyQualifiedNamespace, new DefaultAzureCredential(), clientOptions);

        try
        {
            client.CreateReceiver(queueName: HealthCheckQueueName).PeekMessageAsync().GetAwaiter().GetResult();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
#pragma warning disable CA2012 // Use ValueTasks correctly
            client.DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore CA2012 // Use ValueTasks correctly
        }
    }
}
