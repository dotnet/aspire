// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Dashboard;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class ResourcePublisherTests
{
    [Fact]
    public async Task ProducesExpectedSnapshotAndUpdates()
    {
        CancellationTokenSource cts = new();
        ResourcePublisher publisher = new(cts.Token);

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");
        var c = CreateResourceSnapshot("C");

        await publisher.IntegrateAsync(new TestResource("A"), a, ResourceSnapshotChangeType.Upsert);
        await publisher.IntegrateAsync(new TestResource("B"), b, ResourceSnapshotChangeType.Upsert);

        Assert.Equal(0, publisher.OutgoingSubscriberCount);

        var (snapshot, subscription) = publisher.Subscribe();

        Assert.Equal(1, publisher.OutgoingSubscriberCount);

        Assert.Equal(2, snapshot.Length);
        Assert.Single(snapshot.Where(s => s.Name == "A"));
        Assert.Single(snapshot.Where(s => s.Name == "B"));

        var tcs = new TaskCompletionSource<IReadOnlyList<ResourceSnapshotChange>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task = Task.Run(async () =>
        {
            await foreach (var change in subscription)
            {
                tcs.TrySetResult(change);
            }
        });

        await publisher.IntegrateAsync(new TestResource("C"), c, ResourceSnapshotChangeType.Upsert);

        var change = Assert.Single(await tcs.Task);
        Assert.Equal(ResourceSnapshotChangeType.Upsert, change.ChangeType);
        Assert.Equal("C", change.Resource.Name);

        await cts.CancelAsync();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Ignore possible cancellation error.
        }

        Assert.Equal(0, publisher.OutgoingSubscriberCount);
    }

    [Fact]
    public async Task SupportsMultipleSubscribers()
    {
        CancellationTokenSource cts = new();
        ResourcePublisher publisher = new(cts.Token);

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");
        var c = CreateResourceSnapshot("C");

        await publisher.IntegrateAsync(new TestResource("A"), a, ResourceSnapshotChangeType.Upsert);
        await publisher.IntegrateAsync(new TestResource("B"), b, ResourceSnapshotChangeType.Upsert);

        Assert.Equal(0, publisher.OutgoingSubscriberCount);

        var (snapshot1, subscription1) = publisher.Subscribe();
        var (snapshot2, subscription2) = publisher.Subscribe();

        Assert.Equal(2, publisher.OutgoingSubscriberCount);

        Assert.Equal(2, snapshot1.Length);
        Assert.Equal(2, snapshot2.Length);

        await publisher.IntegrateAsync(new TestResource("C"), c, ResourceSnapshotChangeType.Upsert);

        var enumerator1 = subscription1.GetAsyncEnumerator(cts.Token);
        var enumerator2 = subscription2.GetAsyncEnumerator(cts.Token);

        await enumerator1.MoveNextAsync();
        await enumerator2.MoveNextAsync();

        var v1 = Assert.Single(enumerator1.Current);
        var v2 = Assert.Single(enumerator2.Current);

        Assert.Equal(ResourceSnapshotChangeType.Upsert, v1.ChangeType);
        Assert.Equal(ResourceSnapshotChangeType.Upsert, v2.ChangeType);
        Assert.Equal("C", v1.Resource.Name);
        Assert.Equal("C", v2.Resource.Name);

        await cts.CancelAsync();

        Assert.False(await enumerator1.MoveNextAsync());
        Assert.False(await enumerator2.MoveNextAsync());

        Assert.Equal(0, publisher.OutgoingSubscriberCount);
    }

    [Fact]
    public async Task MergesResourcesInSnapshot()
    {
        CancellationTokenSource cts = new();
        ResourcePublisher publisher = new(cts.Token);

        var a1 = CreateResourceSnapshot("A");
        var a2 = CreateResourceSnapshot("A");
        var a3 = CreateResourceSnapshot("A");

        await publisher.IntegrateAsync(new TestResource("A"), a1, ResourceSnapshotChangeType.Upsert);
        await publisher.IntegrateAsync(new TestResource("A"), a2, ResourceSnapshotChangeType.Upsert);
        await publisher.IntegrateAsync(new TestResource("A"), a3, ResourceSnapshotChangeType.Upsert);

        var (snapshot, _) = publisher.Subscribe();

        Assert.Equal("A", Assert.Single(snapshot).Name);

        await cts.CancelAsync();
    }

    [Fact]
    public async Task DeletesRemoveFromSnapshot()
    {
        CancellationTokenSource cts = new();
        ResourcePublisher publisher = new(cts.Token);

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");

        await publisher.IntegrateAsync(new TestResource("A"), a, ResourceSnapshotChangeType.Upsert);
        await publisher.IntegrateAsync(new TestResource("B"), b, ResourceSnapshotChangeType.Upsert);
        await publisher.IntegrateAsync(new TestResource("A"), a, ResourceSnapshotChangeType.Delete);

        var (snapshot, _) = publisher.Subscribe();

        Assert.Equal("B", Assert.Single(snapshot).Name);

        await cts.CancelAsync();
    }

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
        await publisher.IntegrateAsync(new TestResource("A"), CreateResourceSnapshot("A"), ResourceSnapshotChangeType.Upsert);

        // Let the subscriber exit.
        await task;
    }

    private static GenericResourceSnapshot CreateResourceSnapshot(string name)
    {
        return new GenericResourceSnapshot(new()
        {
            Properties = [],
            ResourceType = KnownResourceTypes.Container
        })
        {
            Name = name,
            Uid = "",
            State = null,
            StateStyle = null,
            ExitCode = null,
            CreationTimeStamp = null,
            StartTimeStamp = null,
            StopTimeStamp = null,
            DisplayName = "",
            Urls = [],
            Volumes = [],
            Environment = [],
            HealthState = null,
            Commands = []
        };
    }

    private sealed class TestResource(string name) : Resource(name)
    {
    }
}
