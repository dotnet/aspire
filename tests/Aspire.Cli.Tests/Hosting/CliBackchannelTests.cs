// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Channels;
using Aspire.Cli;
using Aspire.Hosting.Utils;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests.Cli;

public class CliBackchannelTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task AppHostRespondsToPingWithMatchingTimestamp()
    {
        var socketPath = DotNetCliRunner.GetBackchannelSocketPath();
        using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(socketPath);
        serverSocket.Bind(endpoint);
        serverSocket.Listen(1);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.Configuration["ASPIRE_CLI_BACKCHANNEL_PATH"] = socketPath;
        using var app = builder.Build();
        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(10));

        var clientSocket = await serverSocket.AcceptAsync();
        using var stream = new NetworkStream(clientSocket, true);

        var cliRpcTarget = new DummyCliRpcTarget();
        var rpc = JsonRpc.Attach(stream, cliRpcTarget);

        // When we send a ping, it should return the same timestamp.
        var sendTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var responseTimestamp = await rpc.InvokeAsync<long>("PingAsync", sendTimestamp);

        Assert.Equal(sendTimestamp, responseTimestamp);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AppHostConnectsBackToCliWithPingRequest()
    {
        var testStartedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var socketPath = DotNetCliRunner.GetBackchannelSocketPath();
        using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(socketPath);
        serverSocket.Bind(endpoint);
        serverSocket.Listen(1);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.Configuration["ASPIRE_CLI_BACKCHANNEL_PATH"] = socketPath;
        using var app = builder.Build();
        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(10));

        var clientSocket = await serverSocket.AcceptAsync();
        using var stream = new NetworkStream(clientSocket, true);

        var cliRpcTarget = new DummyCliRpcTarget();
        var rpc = JsonRpc.Attach(stream, cliRpcTarget);

        // This assertion is a little absurd, but the apphsot pinging the CLI
        // after the test starts is an invaraint I can assert on :)
        var pingTimestamp = await cliRpcTarget.PingAsyncChannel.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(pingTimestamp > testStartedTimestamp);
    
        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }
}

public class DummyCliRpcTarget
{
    public Channel<long> PingAsyncChannel { get; set; } = Channel.CreateUnbounded<long>();

    public async Task<long> PingAsync(long timestamp)
    {
        await PingAsyncChannel.Writer.WriteAsync(timestamp);
        return timestamp;
    }
}