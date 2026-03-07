// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using StreamJsonRpc;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class JsonRpcCallbackInvokerTests
{
    [Fact]
    public void SetConnection_MarksInvokerConnected()
    {
        var invoker = new JsonRpcCallbackInvoker();
        using var rpc = new JsonRpc(Stream.Null);

        Assert.False(invoker.IsConnected);

        invoker.SetConnection(rpc);

        Assert.True(invoker.IsConnected);
    }

    [Fact]
    public async Task InvokeAsync_WithoutConnection_ThrowsInvalidOperationException()
    {
        var invoker = new JsonRpcCallbackInvoker();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync<JsonNode?>("callback-1", null));

        Assert.Equal("No client connection available for callback invocation", exception.Message);
    }
}
