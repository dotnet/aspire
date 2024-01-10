// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class ResourcePublisherTests
{
    [Fact(Skip = "Passes locally but fails in CI. https://github.com/dotnet/aspire/issues/1410")]
    public async Task ProducesExpectedSnapshotAndUpdates()
    {
        CancellationTokenSource cts = new();
        ResourcePublisher publisher = new(cts.Token);

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");
        var c = CreateResourceSnapshot("C");

        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        Assert.Equal(0, publisher.OutgoingSubscriberCount);

        var (snapshot, subscription) = publisher.Subscribe();

        Assert.Equal(1, publisher.OutgoingSubscriberCount);

        Assert.Equal(2, snapshot.Length);
        Assert.Single(snapshot.Where(s => s.Name == "A"));
        Assert.Single(snapshot.Where(s => s.Name == "B"));

        using AutoResetEvent sync = new(initialState: false);
        List<IReadOnlyList<ResourceSnapshotChange>> changeBatches = [];

        var task = Task.Run(async () =>
        {
            await foreach (var change in subscription)
            {
                changeBatches.Add(change);
                sync.Set();
            }
        });

        await publisher.IntegrateAsync(c, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        Assert.True(sync.WaitOne(TimeSpan.FromSeconds(1)));

        var change = Assert.Single(changeBatches.SelectMany(o => o));
        Assert.Equal(ResourceSnapshotChangeType.Upsert, change.ChangeType);
        Assert.Equal("C", change.Resource.Name);

        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => task);

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

        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        Assert.Equal(0, publisher.OutgoingSubscriberCount);

        var (snapshot1, subscription1) = publisher.Subscribe();
        var (snapshot2, subscription2) = publisher.Subscribe();

        Assert.Equal(2, publisher.OutgoingSubscriberCount);

        Assert.Equal(2, snapshot1.Length);
        Assert.Equal(2, snapshot2.Length);

        await publisher.IntegrateAsync(c, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

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

        await publisher.IntegrateAsync(a1, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(a2, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(a3, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

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

        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Delete).ConfigureAwait(false);

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
        await publisher.IntegrateAsync(CreateResourceSnapshot("A"), ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        // Let the subscriber exit.
        await task;
    }

    private static ContainerSnapshot CreateResourceSnapshot(string name)
    {
        return new ContainerSnapshot()
        {
            Name = name,
            Uid = "",
            State = "",
            ExitCode = null,
            CreationTimeStamp = null,
            DisplayName = "",
            Endpoints = [],
            Environment = [],
            ExpectedEndpointsCount = null,
            Services = [],
            Args = [],
            Command = "",
            ContainerId = "",
            Image = "",
            Ports = []
        };
    }
}
