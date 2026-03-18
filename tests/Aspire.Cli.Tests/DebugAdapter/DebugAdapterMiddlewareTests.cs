// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.DebugAdapter.Protocol;
using Aspire.DebugAdapter.Types;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.DebugAdapter;

public class DebugAdapterMiddlewareTests
{
    [Fact]
    public async Task UpstreamRequestWithResponseIsForwarded()
    {
        await using var fixture = new MiddlewareTestFixture();

        // Client sends an evaluate request
        var evaluateReceivedTcs = new TaskCompletionSource<EvaluateRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Adapter.RequestReceived += msg =>
        {
            if (msg is EvaluateRequest evalReq)
            {
                evaluateReceivedTcs.TrySetResult(evalReq);
            }
        };

        var responseTcs = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Client.ResponseReceived += resp =>
        {
            responseTcs.TrySetResult(resp);
        };

        var request = new EvaluateRequest { Arguments = new EvaluateArguments { Expression = "1 + 1" } };
        await fixture.Client.SendRequestAsync(request);

        // Adapter receives the forwarded request
        var receivedRequest = await evaluateReceivedTcs.Task.DefaultTimeout();
        Assert.Equal("1 + 1", receivedRequest.Arguments.Expression);

        // Adapter sends back a response
        var response = new EvaluateResponse
        {
            Success = true,
            Body = new EvaluateResponseBody { Result = "test-result", VariablesReference = 0 }
        };
        await fixture.Adapter.SendResponseAsync(response, receivedRequest.Seq);

        // Client receives the forwarded response
        var clientResponse = await responseTcs.Task.DefaultTimeout();
        Assert.True(clientResponse.Success);
    }

    [Fact]
    public async Task UpstreamRequestWithoutResponseBodyIsForwarded()
    {
        await using var fixture = new MiddlewareTestFixture();

        var pauseReceivedTcs = new TaskCompletionSource<PauseRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Adapter.RequestReceived += msg =>
        {
            if (msg is PauseRequest pauseReq)
            {
                pauseReceivedTcs.TrySetResult(pauseReq);
            }
        };

        var responseTcs = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Client.ResponseReceived += resp =>
        {
            responseTcs.TrySetResult(resp);
        };

        var request = new PauseRequest { Arguments = new PauseArguments { ThreadId = 42 } };
        await fixture.Client.SendRequestAsync(request);

        var receivedRequest = await pauseReceivedTcs.Task.DefaultTimeout();
        Assert.Equal(42, receivedRequest.Arguments.ThreadId);

        // Adapter sends back an acknowledgement response (no body)
        var response = new PauseResponse { Success = true };
        await fixture.Adapter.SendResponseAsync(response, receivedRequest.Seq);

        var clientResponse = await responseTcs.Task.DefaultTimeout();
        Assert.True(clientResponse.Success);
    }

    [Fact]
    public async Task DownstreamClientRequestIsForwardedToUpstream()
    {
        await using var fixture = new MiddlewareTestFixture();

        // Adapter sends a reverse request (RunInTerminal) to the client
        var clientRequestTcs = new TaskCompletionSource<RunInTerminalRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Client.RequestReceived += msg =>
        {
            if (msg is RunInTerminalRequest ritReq)
            {
                clientRequestTcs.TrySetResult(ritReq);
            }
        };

        var adapterResponseTcs = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Adapter.ResponseReceived += resp =>
        {
            adapterResponseTcs.TrySetResult(resp);
        };

        var reverseRequest = new RunInTerminalRequest
        {
            Arguments = new RunInTerminalRequestArguments
            {
                Kind = "integrated",
                Title = "test-run",
                Cwd = "/tmp",
                Args = ["echo", "hello"]
            }
        };
        await fixture.Adapter.SendRequestAsync(reverseRequest);

        // Client receives the forwarded reverse request
        var received = await clientRequestTcs.Task.DefaultTimeout();
        Assert.Equal("integrated", received.Arguments.Kind);
        Assert.Equal("test-run", received.Arguments.Title);
        Assert.Equal("/tmp", received.Arguments.Cwd);
        Assert.Equal(["echo", "hello"], received.Arguments.Args);

        // Client sends back a response
        var clientResp = new RunInTerminalResponse
        {
            Success = true,
            Body = new RunInTerminalResponseBody { ProcessId = 1234 }
        };
        await fixture.Client.SendResponseAsync(clientResp, received.Seq);

        // Adapter receives the forwarded response
        var adapterResponse = await adapterResponseTcs.Task.DefaultTimeout();
        Assert.True(adapterResponse.Success);
    }

    [Fact]
    public async Task DownstreamEventIsForwardedToUpstream()
    {
        await using var fixture = new MiddlewareTestFixture();

        var eventTcs = new TaskCompletionSource<OutputEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Client.EventReceived += msg =>
        {
            if (msg is OutputEvent outputEvt)
            {
                eventTcs.TrySetResult(outputEvt);
            }
        };

        var outputEvent = new OutputEvent { Body = new OutputEventBody { Output = "hello" } };
        await fixture.Adapter.SendEventAsync(outputEvent);

        var evt = await eventTcs.Task.DefaultTimeout();
        Assert.Equal("hello", evt.Body.Output);
    }

    private sealed class MiddlewareTestFixture : IAsyncDisposable
    {
        // Streams: client <-> middleware <-> adapter
        private readonly BlockingStream _clientToMiddleware = new();
        private readonly BlockingStream _middlewareToClient = new();
        private readonly BlockingStream _middlewareToAdapter = new();
        private readonly BlockingStream _adapterToMiddleware = new();

        public TestDebugAdapterClient Client { get; }
        public TestDebugAdapter Adapter { get; }
        public DebugAdapterMiddleware Middleware { get; }
        public List<string> MiddlewareLogs { get; } = [];

        private readonly CancellationTokenSource _cts = new();
        private readonly Task _middlewareTask;

        public MiddlewareTestFixture()
        {
            Client = new TestDebugAdapterClient(_middlewareToClient, _clientToMiddleware);
            Adapter = new TestDebugAdapter(_middlewareToAdapter, _adapterToMiddleware);

            var clientTransport = new StreamMessageTransport(_clientToMiddleware, _middlewareToClient);
            var hostTransport = new StreamMessageTransport(_adapterToMiddleware, _middlewareToAdapter);

            Middleware = new DebugAdapterMiddleware();
            Middleware.SetLogCallback(MiddlewareLogs.Add);
            _middlewareTask = Middleware.RunAsync(clientTransport, hostTransport, _cts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();

            _clientToMiddleware.CompleteWriting();
            _middlewareToClient.CompleteWriting();
            _middlewareToAdapter.CompleteWriting();
            _adapterToMiddleware.CompleteWriting();

            try
            {
                await _middlewareTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                // Middleware may throw during shutdown
            }

            await Client.DisposeAsync();
            await Adapter.DisposeAsync();
            _cts.Dispose();
        }
    }
}
