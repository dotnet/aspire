// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Native.Rpc;
using Aspire.Hosting.Native.Server;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Native.Core.Tests;

public class NativeFramingTests
{
    [Fact]
    public async Task PeerKeepsBorrowedEnvelopeAliveUntilAsyncDispatchCompletes()
    {
        using var stream = new MemoryStream();
        await NativeRpcFraming.WriteAsync(stream,
            """{"jsonrpc":"2.0","id":1,"method":"pending","params":{"value":"retained"}}"""u8.ToArray(), CancellationToken.None);
        await NativeRpcFraming.WriteAsync(stream,
            """{"jsonrpc":"2.0","id":2,"method":"next"}"""u8.ToArray(), CancellationToken.None);
        stream.Position = 0;
        using var peer = new NativeRpcPeer(stream, new NativeServerOptions(), NullLogger.Instance);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatched = 0;
        var run = peer.RunAsync(async envelope =>
        {
            Interlocked.Increment(ref dispatched);
            if (envelope.GetProperty("method").GetString() == "pending")
            {
                await resume.Task.WaitAsync(TestContext.Current.CancellationToken);
                Assert.Equal("retained", envelope.GetProperty("params").GetProperty("value").GetString());
                Assert.Equal(1, envelope.GetProperty("id").GetInt32());
            }
            return null;
        }, TestContext.Current.CancellationToken);
        try
        {
            Assert.Equal(2, dispatched);
            Assert.True(peer.Closed.IsCancellationRequested);
            Assert.False(run.IsCompleted);
        }
        finally
        {
            resume.TrySetResult();
            await run.WaitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData("content-length:\t2 \r\nContent-Type: application/vscode-jsonrpc; charset=utf-8\r\n\r\n{}")]
    [InlineData("Content-Type: application/json\r\nContent-Length: 2\r\n\r\n{}")]
    public async Task ReusedHeaderBufferPreservesCaseWhitespaceAndOptionalContentType(string frame)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(frame + "Content-Length: 4\r\n\r\nnull"));
        var reader = new NativeRpcFraming(stream);
        Assert.Equal("{}", Encoding.UTF8.GetString((await reader.ReadAsync(CancellationToken.None))!));
        Assert.Equal("null", Encoding.UTF8.GetString((await reader.ReadAsync(CancellationToken.None))!));
        Assert.Null(await reader.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CoalescedFramesPreserveUtf8ByteLengthsAndTheNextMessage()
    {
        using var stream = new MemoryStream();
        await NativeRpcFraming.WriteAsync(stream, Encoding.UTF8.GetBytes("{\"message\":\"caf\u00e9\"}"), CancellationToken.None);
        await NativeRpcFraming.WriteAsync(stream, "{}"u8.ToArray(), CancellationToken.None);
        stream.Position = 0;
        var reader = new NativeRpcFraming(stream);
        Assert.Equal("{\"message\":\"caf\u00e9\"}", Encoding.UTF8.GetString((await reader.ReadAsync(CancellationToken.None))!));
        Assert.Equal("{}", Encoding.UTF8.GetString((await reader.ReadAsync(CancellationToken.None))!));
        Assert.Null(await reader.ReadAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("Content-Length: 262145\r\n\r\n")]
    [InlineData("Content-Length: 1\r\nContent-Length: 1\r\n\r\n")]
    [InlineData("Content-Length: -1\r\n\r\n")]
    [InlineData("Content-Length: invalid\r\n\r\n")]
    [InlineData("Content-Type: application/json\r\n\r\n")]
    [InlineData("Content-Length: 0\nContent-Type: application/json\r\n\r\n")]
    [InlineData("Content-Length: 0\r\nUnknown: value\r\n\r\n")]
    [InlineData("Content-Length: 0\r\ncontent-length: 0\r\n\r\n")]
    public async Task InvalidLengthsAreRejectedBeforeReadingThePayload(string header)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(header));
        var reader = new NativeRpcFraming(stream);
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("Content-Length: 10\r\n")]
    [InlineData("Content-Length: 10\r\n\r\n{}")]
    public async Task TruncatedFramesAreNotTreatedAsCompletedRequests(string frame)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(frame));
        var reader = new NativeRpcFraming(stream);
        await Assert.ThrowsAsync<EndOfStreamException>(() => reader.ReadAsync(CancellationToken.None));
    }
}
