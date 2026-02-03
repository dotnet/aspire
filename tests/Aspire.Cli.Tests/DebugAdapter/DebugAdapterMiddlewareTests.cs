// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Aspire.Hosting.Cli.DebugAdapter;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace Aspire.Cli.Tests.DebugAdapter;

public class DebugAdapterMiddlewareTests
{
    [Fact]
    public async Task UpstreamRequestWithResponseIsForwarded()
    {
        await using var fixture = new MiddlewareTestFixture();

        var responseTcs = new TaskCompletionSource<EvaluateResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new EvaluateRequest { Args = { Expression = "1 + 1" } };
        fixture.Client.Host.SendRequest(request, (_, r) => responseTcs.TrySetResult(r), (_, e) => responseTcs.TrySetException(e));

        var args = await fixture.Adapter.EvaluateReceived.Task.DefaultTimeout();
        Assert.Equal("1 + 1", args.Expression);

        var response = await responseTcs.Task.DefaultTimeout();
        Assert.Equal("test-result", response.Result);
    }

    [Fact]
    public async Task UpstreamRequestWithoutResponseIsForwarded()
    {
        await using var fixture = new MiddlewareTestFixture();

        var responseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new PauseRequest { Args = { ThreadId = 42 } };
        fixture.Client.Host.SendRequest(request, _ => responseTcs.TrySetResult(), (_, e) => responseTcs.TrySetException(e));

        await responseTcs.Task.DefaultTimeout();

        var args = await fixture.Adapter.PauseReceived.Task.DefaultTimeout();
        Assert.Equal(42, args.ThreadId);
    }

    [Fact]
    public async Task DownstreamClientRequestWithResponseIsForwardedToUpstream()
    {
        await using var fixture = new MiddlewareTestFixture();

        var responseTcs = new TaskCompletionSource<RunInTerminalResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new RunInTerminalRequest
        {
            Args =
            {
                Kind = RunInTerminalArguments.KindValue.Integrated,
                Title = "test-run",
                Cwd = "/tmp",
                Args = ["echo", "hello"]
            }
        };
        fixture.Adapter.Protocol.SendClientRequest(request, (_, r) => responseTcs.TrySetResult(r), (_, e) => responseTcs.TrySetException(e));

        var received = await fixture.Client.RunInTerminalReceived.Task.DefaultTimeout();
        Assert.Equal(RunInTerminalArguments.KindValue.Integrated, received.Kind);
        Assert.Equal("test-run", received.Title);
        Assert.Equal("/tmp", received.Cwd);
        Assert.Equal(["echo", "hello"], received.Args);

        var response = await responseTcs.Task.DefaultTimeout();
        Assert.Equal(1234, response.ProcessId);
    }

    [Fact]
    public async Task DownstreamEventIsForwardedToUpstream()
    {
        await using var fixture = new MiddlewareTestFixture();

        fixture.Adapter.Protocol.SendEvent(new OutputEvent("hello"));

        var evt = await fixture.Client.OutputEventReceived.Task.DefaultTimeout();
        Assert.Equal("hello", evt.Output);
    }

    private sealed class MiddlewareTestFixture : IAsyncDisposable
    {
        private readonly Pipe _clientToMiddleware = new();
        private readonly Pipe _middlewareToClient = new();
        private readonly Pipe _middlewareToAdapter = new();
        private readonly Pipe _adapterToMiddleware = new();

        public TestDebugAdapterClient Client { get; }
        public TestDebugAdapter Adapter { get; }
        public DebugAdapterMiddleware Middleware { get; }

        public MiddlewareTestFixture()
        {
            Client = new TestDebugAdapterClient(_middlewareToClient.Reader.AsStream(), _clientToMiddleware.Writer.AsStream());
            Adapter = new TestDebugAdapter(_middlewareToAdapter.Reader.AsStream(), _adapterToMiddleware.Writer.AsStream());
            Middleware = new DebugAdapterMiddleware(
                _clientToMiddleware.Reader.AsStream(),
                _middlewareToClient.Writer.AsStream(),
                _adapterToMiddleware.Reader.AsStream(),
                _middlewareToAdapter.Writer.AsStream());

            Adapter.Protocol.Run();
            Middleware.Run();
            Client.Host.Run();
        }

        public async ValueTask DisposeAsync()
        {
            // Stop the protocol dispatchers first
            Client.Host.Stop();
            Adapter.Protocol.Stop();
            Middleware.Protocol.Stop();

            // Complete all pipe writers to signal end-of-stream
            await _clientToMiddleware.Writer.CompleteAsync();
            await _middlewareToClient.Writer.CompleteAsync();
            await _middlewareToAdapter.Writer.CompleteAsync();
            await _adapterToMiddleware.Writer.CompleteAsync();

            // Complete readers as well
            await _clientToMiddleware.Reader.CompleteAsync();
            await _middlewareToClient.Reader.CompleteAsync();
            await _middlewareToAdapter.Reader.CompleteAsync();
            await _adapterToMiddleware.Reader.CompleteAsync();
        }
    }
}
