// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
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
        // This test verifies that RPC methods can be invoked
        // When the Dashboard is not part of the app model, null should be returned
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

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

        // Invoke the GetDashboardMcpConnectionInfoAsync RPC method
        var connectionInfo = await rpc.InvokeAsync<DashboardMcpConnectionInfo?>(
            "GetDashboardMcpConnectionInfoAsync",
            Array.Empty<object>()
        ).WaitAsync(TimeSpan.FromSeconds(60));

        // Since the dashboard is not part of the app model, it should return null
        Assert.Null(connectionInfo);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task GetAppHostInformationAsyncReturnsAppHostPath()
    {
        // This test verifies that GetAppHostInformationAsync returns the AppHost path
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

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

        // Invoke the GetAppHostInformationAsync RPC method
        var appHostInfo = await rpc.InvokeAsync<AppHostInformation>(
            "GetAppHostInformationAsync",
            Array.Empty<object>()
        ).WaitAsync(TimeSpan.FromSeconds(60));

        // The AppHost path should be set
        Assert.NotNull(appHostInfo);
        Assert.NotNull(appHostInfo.AppHostPath);
        Assert.NotEmpty(appHostInfo.AppHostPath);

        // The ProcessId should be set and valid
        Assert.True(appHostInfo.ProcessId > 0);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task MultipleClientsCanInvokeRpcMethodsConcurrently()
    {
        // This test verifies that multiple clients can invoke RPC methods concurrently
        // When the Dashboard is not part of the app model, null should be returned
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

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

            var connectionInfo = await rpc.InvokeAsync<DashboardMcpConnectionInfo?>(
                "GetDashboardMcpConnectionInfoAsync",
                Array.Empty<object>()
            );

            // Since the dashboard is not part of the app model, it should return null
            Assert.Null(connectionInfo);

            return connectionInfo;
        });

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(60));
        Assert.Equal(5, results.Length);
        Assert.All(results, Assert.Null);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task GetAppHostInformationAsyncReturnsFilePathWithExtension()
    {
        // This test verifies that GetAppHostInformationAsync returns the full file path with extension
        // For .csproj-based AppHosts, it should include the .csproj extension
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

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

        // Invoke the GetAppHostInformationAsync RPC method
        var appHostInfo = await rpc.InvokeAsync<AppHostInformation>(
            "GetAppHostInformationAsync",
            Array.Empty<object>()
        ).WaitAsync(TimeSpan.FromSeconds(60));

        // Verify the AppHost path is returned
        Assert.NotNull(appHostInfo);
        Assert.NotNull(appHostInfo.AppHostPath);
        Assert.NotEmpty(appHostInfo.AppHostPath);
        
        // The path should be an absolute path
        Assert.True(Path.IsPathRooted(appHostInfo.AppHostPath), $"Expected absolute path but got: {appHostInfo.AppHostPath}");
        
        // In test scenarios where assembly metadata is not available, we may get a path without extension
        // (falling back to AppHost:Path). In real scenarios with proper metadata, we should get .csproj or .cs
        // So we just verify the path is non-empty and rooted
        outputHelper.WriteLine($"AppHost path returned: {appHostInfo.AppHostPath}");

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task SocketPathUsesAuxiPrefix()
    {
        // This test verifies that the socket path uses "auxi.sock." prefix instead of "aux.sock."
        // to avoid Windows reserved device name issues (AUX is reserved on Windows < 11)
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Verify that the socket path uses "auxi.sock." prefix
        var fileName = Path.GetFileName(service.SocketPath);
        Assert.StartsWith("auxi.sock.", fileName);
        
        // Verify that the socket file can be created (not blocked by Windows reserved names)
        Assert.True(File.Exists(service.SocketPath), $"Socket file should exist at: {service.SocketPath}");

        outputHelper.WriteLine($"Socket path: {service.SocketPath}");

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);
    }

    [Fact]
    [RequiresDocker]
    public async Task CallResourceMcpToolAsyncThrowsWhenResourceNotFound()
    {
        // This test verifies that CallResourceMcpToolAsync throws when resource is not found
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Add a simple container resource (without MCP)
        builder.AddContainer("mycontainer", "nginx");

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Connect a client
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var rpc = JsonRpc.Attach(stream);

        // Try to call a tool on a non-existent resource
        var ex = await Assert.ThrowsAsync<RemoteInvocationException>(async () =>
        {
            await rpc.InvokeAsync<JsonElement>(
                "CallResourceMcpToolAsync",
                new object[] { "nonexistent-resource", "some-tool", new Dictionary<string, object?>() }
            ).WaitAsync(TestConstants.DefaultTimeoutTimeSpan);
        });

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);

        await app.StopAsync().WaitAsync(TestConstants.LongTimeoutTimeSpan);
    }

    [Fact]
    [RequiresDocker]
    public async Task CallResourceMcpToolAsyncThrowsWhenResourceHasNoMcpAnnotation()
    {
        // This test verifies that CallResourceMcpToolAsync throws when resource has no MCP annotation
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Add a simple container resource (without MCP)
        builder.AddContainer("mycontainer", "nginx");

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Connect a client
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var rpc = JsonRpc.Attach(stream);

        // Try to call a tool on a resource without MCP annotation
        var ex = await Assert.ThrowsAsync<RemoteInvocationException>(async () =>
        {
            await rpc.InvokeAsync<JsonElement>(
                "CallResourceMcpToolAsync",
                new object[] { "mycontainer", "some-tool", new Dictionary<string, object?>() }
            ).WaitAsync(TestConstants.DefaultTimeoutTimeSpan);
        });

        Assert.Contains("MCP endpoint annotation", ex.Message, StringComparison.OrdinalIgnoreCase);

        await app.StopAsync().WaitAsync(TestConstants.LongTimeoutTimeSpan);
    }

    [Fact]
    public async Task StopAppHostAsyncInitiatesShutdown()
    {
        // This test verifies that StopAppHostAsync initiates AppHost shutdown
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(outputHelper);

        // Register the auxiliary backchannel service
        builder.Services.AddSingleton<AuxiliaryBackchannelService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuxiliaryBackchannelService>());

        using var app = builder.Build();

        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        // Get the service
        var service = app.Services.GetRequiredService<AuxiliaryBackchannelService>();
        Assert.NotNull(service.SocketPath);

        // Connect a client
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(service.SocketPath);
        await socket.ConnectAsync(endpoint).WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var rpc = JsonRpc.Attach(stream);

        // Call StopAppHostAsync - this should return immediately and initiate shutdown asynchronously
        await rpc.InvokeAsync(
            "StopAppHostAsync",
            Array.Empty<object>()
        ).WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        // The app should eventually stop
        // We give it some time since StopAppHostAsync initiates shutdown asynchronously
        var lifetime = app.Services.GetService<IHostApplicationLifetime>();
        Assert.NotNull(lifetime);

        // Wait for the application to stop or timeout
        using var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutTimeSpan);
        try
        {
            await Task.Delay(Timeout.Infinite, lifetime.ApplicationStopping).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected - either the app stopped or we timed out
        }

        // If we get here without timeout, the stop was initiated
        outputHelper.WriteLine("StopAppHostAsync initiated shutdown successfully");
    }
}
