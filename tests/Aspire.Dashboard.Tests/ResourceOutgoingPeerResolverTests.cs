// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class ResourceOutgoingPeerResolverTests
{
    private static ResourceViewModel CreateResource(string name, string? serviceAddress = null, int? servicePort = null)
    {
        return new ResourceViewModel
        {
            Name = name,
            ResourceType = "Container",
            DisplayName = name,
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            Environment = [],
            Properties = FrozenDictionary<string, Value>.Empty,
            Urls = servicePort is null || servicePort is null ? [] : [new UrlViewModel(name, new($"http://{serviceAddress}:{servicePort}"), isInternal: false)],
            State = null,
            Commands = []
        };
    }

    [Fact]
    public void EmptyAttributes_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [], out _));
    }

    [Fact]
    public void EmptyUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "")], out _));
    }

    [Fact]
    public void NullUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create<string, string>("peer.service", null!)], out _));
    }

    [Fact]
    public void ExactValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void NumberAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1:5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void CommaAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1,5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void ServerAddressAndPort_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public async Task OnPeerChanges_DataUpdates_EventRaised()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ResourceViewModelSubscription>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceChannel = Channel.CreateUnbounded<ResourceViewModelChange>();
        var resultChannel = Channel.CreateUnbounded<int>();
        var dashboardClient = new MockDashboardClient(tcs.Task);
        var resolver = new ResourceOutgoingPeerResolver(dashboardClient);
        var changeCount = 1;
        resolver.OnPeerChanges(async () =>
        {
            await resultChannel.Writer.WriteAsync(changeCount++);
        });

        var readValue = 0;
        Assert.False(resultChannel.Reader.TryRead(out readValue));

        // Act 1
        tcs.SetResult(new ResourceViewModelSubscription(
            [CreateResource("test")],
            GetChanges()));

        // Assert 1
        readValue = await resultChannel.Reader.ReadAsync();
        Assert.Equal(1, readValue);

        // Act 2
        await sourceChannel.Writer.WriteAsync(new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, CreateResource("test2")));

        // Assert 2
        readValue = await resultChannel.Reader.ReadAsync();
        Assert.Equal(2, readValue);

        await resolver.DisposeAsync();

        async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> GetChanges([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in sourceChannel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return [item];
            }
        }
    }

    private static bool TryResolvePeerName(IDictionary<string, ResourceViewModel> resources, KeyValuePair<string, string>[] attributes, out string? peerName)
    {
        return ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, attributes, out peerName);
    }

    private sealed class MockDashboardClient(Task<ResourceViewModelSubscription> subscribeResult) : IDashboardClient
    {
        public bool IsEnabled => true;
        public Task WhenConnected => Task.CompletedTask;
        public string ApplicationName => "ApplicationName";
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken) => subscribeResult;
    }
}
