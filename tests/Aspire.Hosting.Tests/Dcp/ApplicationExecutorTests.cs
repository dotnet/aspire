// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Xunit;

namespace Aspire.Hosting.Tests.Dcp;

public class ApplicationExecutorTests
{
    [Fact]
    public async Task CancelledSubscriptionIsCleanedUp()
    {
        ResourcePublisher publisher = new(CancellationToken.None);
        CancellationTokenSource cts = new();
        var called = false;

        var (_, subscription) = publisher.Subscribe();

        var task = Task.Run(async () =>
        {
            await foreach (var item in subscription.WithCancellation(cts.Token).ConfigureAwait(false))
            {
                // We should only loop one time.
                Assert.False(called);
                called = true;

                // Now we've received something, cancel.
                await cts.CancelAsync();
            }
        });

        // Push through an update.
        await publisher.IntegrateAsync(CreateResourceSnapshot("A"), ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        // Let the subscriber exit.
        await task;
    }
}
