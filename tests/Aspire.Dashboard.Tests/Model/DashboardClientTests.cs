// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class DashboardClientTests
{
    private readonly IConfiguration _configuration;

    public DashboardClientTests()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>() { { "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL", "http://localhost:12345" } });
        _configuration = configuration;
    }

    [Fact]
    public async Task SubscribeResources_OnCancel_ChannelRemoved()
    {
        await using var instance = new DashboardClient(NullLoggerFactory.Instance, _configuration);
        instance.SetInitialDataReceived();

        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        var (_, subscription) = await client.SubscribeResourcesAsync(CancellationToken.None);

        Assert.Equal(1, instance.OutgoingResourceSubscriberCount);

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription.WithCancellation(cts.Token))
            {
            }
        });

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask).ConfigureAwait(false);

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);
    }

    [Fact]
    public async Task SubscribeResources_OnDispose_ChannelRemoved()
    {
        await using var instance = new DashboardClient(NullLoggerFactory.Instance, _configuration);
        instance.SetInitialDataReceived();

        IDashboardClient client = instance;

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        var (_, subscription) = await client.SubscribeResourcesAsync(CancellationToken.None);

        Assert.Equal(1, instance.OutgoingResourceSubscriberCount);

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription)
            {
            }
        });

        await instance.DisposeAsync();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask).ConfigureAwait(false);
    }

    [Fact]
    public async Task SubscribeResources_ThrowsIfDisposed()
    {
        await using IDashboardClient client = new DashboardClient(NullLoggerFactory.Instance, _configuration);

        await client.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.SubscribeResourcesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeResources_IncreasesSubscriberCount()
    {
        await using var instance = new DashboardClient(NullLoggerFactory.Instance, _configuration);
        instance.SetInitialDataReceived();

        IDashboardClient client = instance;

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        _ = await client.SubscribeResourcesAsync(CancellationToken.None);

        Assert.Equal(1, instance.OutgoingResourceSubscriberCount);

        await instance.DisposeAsync();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);
    }

    [Fact]
    public async Task SubscribeResources_HasInitialData_InitialDataReturned()
    {
        await using var instance = new DashboardClient(NullLoggerFactory.Instance, _configuration);

        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();

        var subscribeTask = client.SubscribeResourcesAsync(CancellationToken.None);

        Assert.False(subscribeTask.IsCompleted);
        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        instance.SetInitialDataReceived([new Resource
        {
            Name = "test",
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
        }]);

        var (initialData, subscription) = await subscribeTask;

        Assert.Single(initialData);
    }
}
