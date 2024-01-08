// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class DashboardClientTests
{
    [Fact]
    public async Task SubscribeResources_Cancellation_ChannelRemoved()
    {
        // Arrange
        var instance = new DashboardClient(NullLoggerFactory.Instance);
        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();
        var (snapshot, subscription) = client.SubscribeResources();

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription.WithCancellation(cts.Token))
            {
            }
        });

        Assert.Collection(instance._outgoingChannels,
            item => Assert.False(item.Reader.Completion.IsCompleted));

        // Act
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask).ConfigureAwait(false);
        Assert.Empty(instance._outgoingChannels);
    }

    [Fact]
    public async Task SubscribeResources_Dispose_ChannelRemoved()
    {
        // Arrange
        var instance = new DashboardClient(NullLoggerFactory.Instance);
        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();
        var (snapshot, subscription) = client.SubscribeResources();

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription.WithCancellation(cts.Token))
            {
            }
        });

        Assert.Collection(instance._outgoingChannels,
            item => Assert.False(item.Reader.Completion.IsCompleted));

        // Act
        await instance.DisposeAsync();

        // Assert
        await readTask.ConfigureAwait(false);
        Assert.Empty(instance._outgoingChannels);
    }

    [Fact]
    public async Task SubscribeResources_AfterDispose_DisposedException()
    {
        // Arrange
        var instance = new DashboardClient(NullLoggerFactory.Instance);
        IDashboardClient client = instance;

        await instance.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(client.SubscribeResources);
    }
}
