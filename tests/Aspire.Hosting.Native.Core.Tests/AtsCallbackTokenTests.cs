// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias NativeLegacyServer;
extern alias NativeLegacy;

using System.IO.Pipelines;
using System.Text;
using System.Text.Json.Nodes;
using NativeLegacyServer::NativeHosting;
using RpcPeer = NativeLegacy::NativeHosting.RpcPeer;

namespace Aspire.Hosting.Native.Core.Tests;

public class AtsCallbackTokenTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedAndFailedCallbacksReleaseTheirTokens(bool fail)
    {
        await using var registry = new AtsRegistry();
        var input = new Pipe();
        var output = new Pipe();
        using var peer = new RpcPeer(input.Reader.AsStream(), output.Writer.AsStream(),
            (_, _, _) => Task.FromResult<JsonNode?>(null));
        var session = new AtsSession(registry, "test") { Peer = peer };
        var reading = peer.RunAsync();
        for (var i = 0; i < 50; i++)
        {
            var callback = session.CallbackAsync("callback", new JsonObject(), TestContext.Current.CancellationToken);
            var request = await ReadMessageAsync(output.Reader.AsStream());
            Assert.Equal(1, registry.TokenCount);
            var id = request["params"]![1]!["p0"]!.GetValue<string>();
            Assert.True(registry.CancelToken(id));
            var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = request["id"]!.DeepClone() };
            if (fail)
            {
                response["error"] = new JsonObject { ["code"] = -32603, ["message"] = "failure" };
            }
            else
            {
                response["result"] = true;
            }
            await WriteMessageAsync(input.Writer.AsStream(), response);
            // CancelToken cancels the callback request itself, including nested
            // generated calls that share its ID; either response outcome must release it.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callback);
            Assert.Equal(0, registry.TokenCount);
            Assert.False(registry.CancelToken(id));
            await ReadMessageAsync(output.Reader.AsStream());
        }
        await input.Writer.CompleteAsync();
        await reading;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResponseSuccessAndFailureReleaseTokensWithoutReset(bool fail)
    {
        await using var registry = new AtsRegistry();
        var input = new Pipe();
        var output = new Pipe();
        using var peer = new RpcPeer(input.Reader.AsStream(), output.Writer.AsStream(),
            (_, _, _) => Task.FromResult<JsonNode?>(null));
        var session = new AtsSession(registry, "test") { Peer = peer };
        var reading = peer.RunAsync();
        for (var i = 0; i < 50; i++)
        {
            var callback = session.CallbackAsync("callback", new JsonObject(), TestContext.Current.CancellationToken);
            var request = await ReadMessageAsync(output.Reader.AsStream());
            Assert.Equal(1, registry.TokenCount);
            var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = request["id"]!.DeepClone() };
            if (fail)
            {
                response["error"] = new JsonObject { ["code"] = -32603, ["message"] = "failure" };
            }
            else
            {
                response["result"] = true;
            }
            await WriteMessageAsync(input.Writer.AsStream(), response);
            if (fail)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => callback);
            }
            else
            {
                Assert.True(await callback);
            }
            Assert.Equal(0, registry.TokenCount);
        }
        await input.Writer.CompleteAsync();
        await reading;
    }

    [Fact]
    public async Task ResetCancelsInflightCallbackAndClearsFallbackTokens()
    {
        await using var registry = new AtsRegistry();
        var input = new Pipe();
        var output = new Pipe();
        using var peer = new RpcPeer(input.Reader.AsStream(), output.Writer.AsStream(),
            (_, _, _) => Task.FromResult<JsonNode?>(null));
        var session = new AtsSession(registry, "test") { Peer = peer };
        var reading = peer.RunAsync();
        var fallback = registry.Token(JsonValue.Create("guest-token"), CancellationToken.None);
        var callback = session.CallbackAsync("callback", new JsonObject(), TestContext.Current.CancellationToken);
        var request = await ReadMessageAsync(output.Reader.AsStream());
        var id = request["params"]![1]!["p0"]!.GetValue<string>();
        var shared = registry.Token(JsonValue.Create(id), CancellationToken.None);
        Assert.Equal(2, registry.TokenCount);
        await registry.ResetAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callback);
        Assert.True(fallback.IsCancellationRequested);
        Assert.True(shared.IsCancellationRequested);
        Assert.Equal(0, registry.TokenCount);
        Assert.False(registry.CancelToken(id));
        Assert.False(registry.Token(JsonValue.Create("new-generation"), CancellationToken.None).IsCancellationRequested);
        await input.Writer.CompleteAsync();
        await reading;
    }

    private static async Task<JsonObject> ReadMessageAsync(Stream stream)
    {
        // Match the ATS Content-Length: <bytes>\r\n\r\n frame, not character length.
        var header = new StringBuilder();
        var one = new byte[1];
        while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
        {
            await stream.ReadExactlyAsync(one, TestContext.Current.CancellationToken);
            header.Append((char)one[0]);
        }
        var length = int.Parse(header.ToString().Split(':')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, TestContext.Current.CancellationToken);

        return JsonNode.Parse(payload)!.AsObject();
    }

    private static async Task WriteMessageAsync(Stream stream, JsonObject message)
    {
        var payload = Encoding.UTF8.GetBytes(message.ToJsonString());
        await stream.WriteAsync(Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n"), TestContext.Current.CancellationToken);
        await stream.WriteAsync(payload, TestContext.Current.CancellationToken);
        await stream.FlushAsync(TestContext.Current.CancellationToken);
    }
}
