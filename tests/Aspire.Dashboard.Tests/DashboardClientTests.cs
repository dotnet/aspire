// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class DashboardClientTests
{
    [Fact]
    public async void SubscribeResources_Cancellation_ChannelRemoved()
    {
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

        Assert.Collection(instance._outgoingChannels, item =>
        {
            Assert.False(item.Reader.Completion.IsCompleted);
        });

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask).ConfigureAwait(false);

        Assert.Empty(instance._outgoingChannels);
    }
}
