// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Aspire.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class DashboardClientTests
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<DashboardOptions> _dashboardOptions;

    public DashboardClientTests()
    {
        _configuration = new ConfigurationManager();

        var options = new DashboardOptions
        {
            ResourceServiceClient =
            {
                AuthMode = ResourceClientAuthMode.Unsecured,
                Url = "http://localhost:12345"
            }
        };
        options.ResourceServiceClient.TryParseOptions(out _);

        _dashboardOptions = Options.Create(options);
    }

    [Fact]
    public async Task SubscribeResources_OnCancel_ChannelRemoved()
    {
        await using var instance = CreateResourceServiceClient();
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

        await TaskHelpers.WaitIgnoreCancelAsync(readTask);

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);
    }

    [Fact]
    public async Task SubscribeResources_OnDispose_ChannelRemoved()
    {
        await using var instance = CreateResourceServiceClient();
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

        await TaskHelpers.WaitIgnoreCancelAsync(readTask);
    }

    [Fact]
    public async Task SubscribeResources_ThrowsIfDisposed()
    {
        await using IDashboardClient client = CreateResourceServiceClient();

        await client.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.SubscribeResourcesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeResources_IncreasesSubscriberCount()
    {
        await using var instance = CreateResourceServiceClient();
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
        await using var instance = CreateResourceServiceClient();

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

    private DashboardClient CreateResourceServiceClient()
    {
        return new DashboardClient(NullLoggerFactory.Instance, _configuration, _dashboardOptions);
    }
}
