// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Net.Sockets;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using Xunit;

namespace Aspire.Hosting.Backchannel;

public class AppHostBackchannelTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task CanConnectToBackchannel()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);
        builder.Configuration[KnownConfigNames.UnixSocketPath] = UnixSocketHelper.GetBackchannelSocketPath();

        var backchannelReadyTaskCompletionSource = new TaskCompletionSource<BackchannelReadyEvent>();
        builder.Eventing.Subscribe<BackchannelReadyEvent>((e, ct) => {
            backchannelReadyTaskCompletionSource.SetResult(e);
            return Task.CompletedTask;
        });

        var backchannelConnectedTaskCompletionSource = new TaskCompletionSource<BackchannelConnectedEvent>();
        builder.Eventing.Subscribe<BackchannelConnectedEvent>((e, ct) => {
            backchannelConnectedTaskCompletionSource.SetResult(e);
            return Task.CompletedTask;
        });

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var backchannelReadyEvent = await backchannelReadyTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(60));

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(backchannelReadyEvent.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));

        _ = await backchannelConnectedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(60));
        
        using var stream = new NetworkStream(socket, true);
        using var rpc = JsonRpc.Attach(stream);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task CanRespondToPingAsync()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);
        builder.Configuration[KnownConfigNames.UnixSocketPath] = UnixSocketHelper.GetBackchannelSocketPath();

        var backchannelReadyTaskCompletionSource = new TaskCompletionSource<BackchannelReadyEvent>();
        builder.Eventing.Subscribe<BackchannelReadyEvent>((e, ct) => {
            backchannelReadyTaskCompletionSource.SetResult(e);
            return Task.CompletedTask;
        });

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var backchannelReadyEvent = await backchannelReadyTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(60));

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(backchannelReadyEvent.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));

        using var stream = new NetworkStream(socket, true);
        using var rpc = JsonRpc.Attach(stream);

        var timestampOut = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var timestampIn = await rpc.InvokeWithCancellationAsync<long>(
            "PingAsync",
            [timestampOut]).WaitAsync(TimeSpan.FromSeconds(60));

        Assert.Equal(timestampOut, timestampIn);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task CanStreamResourceStates()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);
        builder.Configuration[KnownConfigNames.UnixSocketPath] = UnixSocketHelper.GetBackchannelSocketPath();

        builder.AddResource(new TestResource("test"))
               .WithInitialState(new () {
                    ResourceType = "TestResource",
                    State = new ("Running", null),
                    Properties = [new("A", "B"), new("c", "d")],
                    EnvironmentVariables = [new("e", "f", true), new("g", "h", false)]
               });

        var backchannelReadyTaskCompletionSource = new TaskCompletionSource<BackchannelReadyEvent>();
        builder.Eventing.Subscribe<BackchannelReadyEvent>((e, ct) => {
            backchannelReadyTaskCompletionSource.SetResult(e);
            return Task.CompletedTask;
        });

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var backchannelReadyEvent = await backchannelReadyTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(60));

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(backchannelReadyEvent.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));

        using var stream = new NetworkStream(socket, true);
        using var rpc = JsonRpc.Attach(stream);

        var resourceEvents = await rpc.InvokeAsync<IAsyncEnumerable<RpcResourceState>>(
            "GetResourceStatesAsync",
            Array.Empty<object>()
            ).WaitAsync(TimeSpan.FromSeconds(60));

        await foreach (var resourceEvent in resourceEvents)
        {
            Assert.Equal("test", resourceEvent.Resource);
            Assert.Equal("TestResource", resourceEvent.Type);
            Assert.Equal("Running", resourceEvent.State);
            Assert.Empty(resourceEvent.Endpoints);
            break;
        }

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }
}

file sealed class TestResource(string name) : Resource(name)
{

}

file sealed class DummyPublisher : IDistributedApplicationPublisher
{
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

file sealed class DummyPublisherOptions : PublishingOptions
{
}
