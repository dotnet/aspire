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
        using CancellationTokenSource cts = new();
        ResourcePublisher publisher = new();

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");
        var c = CreateResourceSnapshot("C");

        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        var (snapshot, subscription) = publisher.Subscribe();

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

        publisher.Dispose();

        await task;
    }

    [Fact]
    public async Task SupportsMultipleSubscribers()
    {
        using CancellationTokenSource cts = new();
        ResourcePublisher publisher = new();

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");
        var c = CreateResourceSnapshot("C");

        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        var (snapshot1, subscription1) = publisher.Subscribe();
        var (snapshot2, subscription2) = publisher.Subscribe();

        Assert.Equal(2, snapshot1.Length);
        Assert.Equal(2, snapshot2.Length);

        await publisher.IntegrateAsync(c, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        var enumerator1 = subscription1.GetAsyncEnumerator();
        var enumerator2 = subscription2.GetAsyncEnumerator();

        await enumerator1.MoveNextAsync();
        await enumerator2.MoveNextAsync();

        var v1 = Assert.Single(enumerator1.Current);
        var v2 = Assert.Single(enumerator2.Current);

        Assert.Equal(ResourceSnapshotChangeType.Upsert, v1.ChangeType);
        Assert.Equal(ResourceSnapshotChangeType.Upsert, v2.ChangeType);
        Assert.Equal("C", v1.Resource.Name);
        Assert.Equal("C", v2.Resource.Name);

        publisher.Dispose();

        await enumerator1.DisposeAsync();
        await enumerator2.DisposeAsync();
    }

    [Fact]
    public async Task MergesResourcesInSnapshot()
    {
        using CancellationTokenSource cts = new();
        using ResourcePublisher publisher = new();

        var a1 = CreateResourceSnapshot("A");
        var a2 = CreateResourceSnapshot("A");
        var a3 = CreateResourceSnapshot("A");

        await publisher.IntegrateAsync(a1, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(a2, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(a3, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

        var (snapshot, _) = publisher.Subscribe();

        Assert.Equal("A", Assert.Single(snapshot).Name);
    }

    [Fact]
    public async Task SubscriptionWithCancellation_Removed()
    {
        using CancellationTokenSource cts = new();
        ResourcePublisher publisher = new();

        var (snapshot, subscription) = publisher.Subscribe();
        Assert.Single(publisher._outgoingChannels);

        var task = Task.Run(async () =>
        {
            await foreach (var _ in subscription.WithCancellation(cts.Token).ConfigureAwait(false))
            {
            }
        });

        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        Assert.Empty(publisher._outgoingChannels);
    }

    [Fact]
    public async void SubscriptionWithDispose_Removed()
    {
        ResourcePublisher publisher = new();

        var (snapshot, subscription) = publisher.Subscribe();
        Assert.Single(publisher._outgoingChannels);

        var task = Task.Run(async () =>
        {
            await foreach (var _ in subscription.ConfigureAwait(false))
            {
            }
        });

        publisher.Dispose();
        await task;

        Assert.Empty(publisher._outgoingChannels);
    }

    [Fact]
    public async Task DeletesRemoveFromSnapshot()
    {
        using CancellationTokenSource cts = new();
        using ResourcePublisher publisher = new();

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");

        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        await publisher.IntegrateAsync(a, ResourceSnapshotChangeType.Delete).ConfigureAwait(false);

        var (snapshot, _) = publisher.Subscribe();

        Assert.Equal("B", Assert.Single(snapshot).Name);
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
