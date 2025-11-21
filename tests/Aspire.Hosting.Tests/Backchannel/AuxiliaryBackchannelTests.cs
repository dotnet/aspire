// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using System.Net.Sockets;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamJsonRpc;

namespace Aspire.Hosting.Backchannel;

public class AuxiliaryBackchannelTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task CanStartAuxiliaryBackchannelService()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        var connectedEventReceived = new TaskCompletionSource<AuxiliaryBackchannelConnectedEvent>();
        builder.Eventing.Subscribe<AuxiliaryBackchannelConnectedEvent>((e, ct) =>
        {
            connectedEventReceived.TrySetResult(e);
            return Task.CompletedTask;
        });

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        // Get the service and verify it started
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);
        Assert.True(File.Exists(service.SocketPath));

        // Connect a client
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));

        // Verify the connected event was published
        var connectedEvent = await connectedEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(60));
        Assert.NotNull(connectedEvent);
        Assert.Equal(service.SocketPath, connectedEvent.SocketPath);

        socket.Dispose();
        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task CanConnectMultipleClientsToAuxiliaryBackchannel()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        var connectedEventCount = 0;
        var connectedEventLock = new object();
        builder.Eventing.Subscribe<AuxiliaryBackchannelConnectedEvent>((e, ct) =>
        {
            lock (connectedEventLock)
            {
                connectedEventCount++;
            }
            return Task.CompletedTask;
        });

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Connect multiple clients concurrently
        var client1Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var client2Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var client3Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        
        var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
        
        await client1Socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));
        await client2Socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));
        await client3Socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));

        // Give some time for events to be published
        await Task.Delay(1000);

        // Verify that all three connections triggered events
        lock (connectedEventLock)
        {
            Assert.Equal(3, connectedEventCount);
        }

        client1Socket.Dispose();
        client2Socket.Dispose();
        client3Socket.Dispose();
        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task CanInvokeRpcMethodOnAuxiliaryBackchannel()
    {
        // This test verifies that RPC methods can be invoked, but skips actual Dashboard dependency
        // In real scenarios, the Dashboard would provide these values
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        // Configure Dashboard options manually for testing
        builder.Services.Configure<Dashboard.DashboardOptions>(options =>
        {
            options.McpEndpointUrl = "http://localhost:5000/mcp";
            options.McpApiKey = "test-api-key-12345";
        });

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Connect a client
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromSeconds(60));

        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var rpc = JsonRpc.Attach(stream);

        // Invoke the GetMcpConnectionInfoAsync RPC method
        var connectionInfo = await rpc.InvokeAsync<McpConnectionInfo>(
            "GetMcpConnectionInfoAsync",
            Array.Empty<object>()
        ).WaitAsync(TimeSpan.FromSeconds(60));

        Assert.NotNull(connectionInfo);
        Assert.Equal("http://localhost:5000/mcp", connectionInfo.EndpointUrl);
        Assert.Equal("test-api-key-12345", connectionInfo.ApiToken);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task MultipleClientsCanInvokeRpcMethodsConcurrently()
    {
        // This test verifies that multiple clients can invoke RPC methods concurrently
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        // Configure Dashboard options manually for testing
        builder.Services.Configure<Dashboard.DashboardOptions>(options =>
        {
            options.McpEndpointUrl = "http://localhost:5000/mcp";
            options.McpApiKey = "test-api-key-concurrent";
        });

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Create multiple clients and invoke RPC methods concurrently
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
            await socket.ConnectAsync(endpoint);

            using var stream = new NetworkStream(socket, ownsSocket: true);
            using var rpc = JsonRpc.Attach(stream);

            var connectionInfo = await rpc.InvokeAsync<McpConnectionInfo>(
                "GetMcpConnectionInfoAsync",
                Array.Empty<object>()
            );

            Assert.NotNull(connectionInfo);
            Assert.Equal("http://localhost:5000/mcp", connectionInfo.EndpointUrl);
            Assert.Equal("test-api-key-concurrent", connectionInfo.ApiToken);

            return connectionInfo;
        });

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(60));
        Assert.Equal(5, results.Length);
        Assert.All(results, Assert.NotNull);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }
}
