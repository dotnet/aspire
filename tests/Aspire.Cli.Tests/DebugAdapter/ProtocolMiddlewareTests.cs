// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.DebugAdapter.Protocol;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.DebugAdapter;

public class ProtocolMiddlewareTests
{
    [Fact]
    public async Task MiddlewareForwardsErrorResponseWithoutCrashing()
    {
        // Simulate the scenario where vsdbg returns success=false for an evaluate request.
        // The middleware should forward the error response to the client without crashing.
        await using var fixture = new ProtocolMiddlewareTestFixture();

        // Send an evaluate request from client to host
        var requestJson = """{"seq":1,"type":"request","command":"evaluate","arguments":{"expression":"@page"}}""";
        await fixture.WriteToClientInputAsync(requestJson);

        // Read the forwarded request from the host output
        var forwardedRequest = await fixture.ReadFromHostOutputAsync().DefaultTimeout();
        Assert.Contains("\"command\":\"evaluate\"", forwardedRequest);

        // Send an error response back from the host (like vsdbg would send for @page)
        var errorResponseJson = """{"seq":1,"type":"response","request_seq":1,"success":false,"command":"evaluate","message":"not available","body":{}}""";
        await fixture.WriteToHostInputAsync(errorResponseJson);

        // Read the forwarded error response from the client output
        var forwardedResponse = await fixture.ReadFromClientOutputAsync().DefaultTimeout();
        Assert.Contains("\"success\":false", forwardedResponse);
        Assert.Contains("\"command\":\"evaluate\"", forwardedResponse);

        // Verify the middleware is still running by sending another request
        var request2Json = """{"seq":2,"type":"request","command":"threads"}""";
        await fixture.WriteToClientInputAsync(request2Json);
        var forwarded2 = await fixture.ReadFromHostOutputAsync().DefaultTimeout();
        Assert.Contains("\"command\":\"threads\"", forwarded2);
    }

    [Fact]
    public async Task MiddlewareForwardsSuccessResponseCorrectly()
    {
        await using var fixture = new ProtocolMiddlewareTestFixture();

        // Send a threads request from client
        var requestJson = """{"seq":1,"type":"request","command":"threads"}""";
        await fixture.WriteToClientInputAsync(requestJson);

        // Read the forwarded request
        var forwarded = await fixture.ReadFromHostOutputAsync().DefaultTimeout();
        Assert.Contains("\"command\":\"threads\"", forwarded);

        // Send a success response from host
        var responseJson = """{"seq":1,"type":"response","request_seq":1,"success":true,"command":"threads","body":{"threads":[{"id":1,"name":"main"}]}}""";
        await fixture.WriteToHostInputAsync(responseJson);

        // Read the forwarded response
        var forwardedResponse = await fixture.ReadFromClientOutputAsync().DefaultTimeout();
        Assert.Contains("\"success\":true", forwardedResponse);
        Assert.Contains("\"threads\"", forwardedResponse);
    }

    [Fact]
    public async Task MiddlewareForwardsHostEvents()
    {
        await using var fixture = new ProtocolMiddlewareTestFixture();

        // Send an event from the host
        var eventJson = """{"seq":1,"type":"event","event":"output","body":{"category":"console","output":"hello world"}}""";
        await fixture.WriteToHostInputAsync(eventJson);

        // Read the forwarded event from the client output
        var forwardedEvent = await fixture.ReadFromClientOutputAsync().DefaultTimeout();
        Assert.Contains("\"event\":\"output\"", forwardedEvent);
        Assert.Contains("hello world", forwardedEvent);
    }

    [Fact]
    public async Task MiddlewarePropagatesExceptionWhenHostDisconnects()
    {
        await using var fixture = new ProtocolMiddlewareTestFixture();

        // Close the host input to simulate host disconnection
        await fixture.CloseHostInputAsync();

        // The middleware should complete (not hang)
        await fixture.WaitForCompletionAsync().DefaultTimeout();
    }

    /// <summary>
    /// Test fixture that sets up a protocol-level middleware with in-memory streams.
    /// Uses raw <see cref="StreamMessageTransport"/> instances, not the VS SDK.
    /// </summary>
    private sealed class ProtocolMiddlewareTestFixture : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _middlewareTask;
        private readonly DebugAdapterMiddleware _middleware;

        // Streams that the test writes into (simulating the external side)
        private readonly BlockingStream _clientToMiddleware;
        private readonly BlockingStream _middlewareToClient;
        private readonly BlockingStream _hostToMiddleware;
        private readonly BlockingStream _middlewareToHost;

        public ProtocolMiddlewareTestFixture()
        {
            _clientToMiddleware = new BlockingStream();
            _middlewareToClient = new BlockingStream();
            _hostToMiddleware = new BlockingStream();
            _middlewareToHost = new BlockingStream();

            var clientTransport = new StreamMessageTransport(_clientToMiddleware, _middlewareToClient);
            var hostTransport = new StreamMessageTransport(_hostToMiddleware, _middlewareToHost);

            var logs = new List<string>();
            _middleware = new DebugAdapterMiddleware();
            _middleware.SetLogCallback(logs.Add);

            _middlewareTask = _middleware.RunAsync(clientTransport, hostTransport, _cts.Token);
        }

        public async Task WriteToClientInputAsync(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var wire = Encoding.UTF8.GetBytes($"Content-Length: {bytes.Length}\r\n\r\n");
            await _clientToMiddleware.WriteAsync(wire);
            await _clientToMiddleware.WriteAsync(bytes);
            await _clientToMiddleware.FlushAsync();
        }

        public async Task WriteToHostInputAsync(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var wire = Encoding.UTF8.GetBytes($"Content-Length: {bytes.Length}\r\n\r\n");
            await _hostToMiddleware.WriteAsync(wire);
            await _hostToMiddleware.WriteAsync(bytes);
            await _hostToMiddleware.FlushAsync();
        }

        public async Task<string> ReadFromClientOutputAsync()
        {
            return await ReadDapMessageAsync(_middlewareToClient);
        }

        public async Task<string> ReadFromHostOutputAsync()
        {
            return await ReadDapMessageAsync(_middlewareToHost);
        }

        public Task CloseHostInputAsync()
        {
            _hostToMiddleware.CompleteWriting();
            return Task.CompletedTask;
        }

        public Task WaitForCompletionAsync() => _middlewareTask;

        private static async Task<string> ReadDapMessageAsync(BlockingStream stream)
        {
            // Read the Content-Length header
            var headerBuilder = new StringBuilder();
            var buffer = new byte[1];
            while (true)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                {
                    throw new EndOfStreamException("Stream closed while reading header");
                }
                headerBuilder.Append((char)buffer[0]);
                if (headerBuilder.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                {
                    break;
                }
            }

            var header = headerBuilder.ToString();
            var contentLengthPrefix = "Content-Length: ";
            var startIdx = header.IndexOf(contentLengthPrefix, StringComparison.OrdinalIgnoreCase);
            var lengthStr = header[(startIdx + contentLengthPrefix.Length)..header.IndexOf("\r\n", startIdx, StringComparison.Ordinal)];
            var contentLength = int.Parse(lengthStr, System.Globalization.CultureInfo.InvariantCulture);

            // Read the JSON content
            var content = new byte[contentLength];
            var totalRead = 0;
            while (totalRead < contentLength)
            {
                var read = await stream.ReadAsync(content.AsMemory(totalRead, contentLength - totalRead));
                if (read == 0)
                {
                    throw new EndOfStreamException("Stream closed while reading content");
                }
                totalRead += read;
            }

            return Encoding.UTF8.GetString(content);
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _clientToMiddleware.CompleteWriting();
            _hostToMiddleware.CompleteWriting();

            try
            {
                await _middlewareTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (Exception)
            {
                // Middleware may throw during disposal
            }

            _cts.Dispose();
            _clientToMiddleware.Dispose();
            _middlewareToClient.Dispose();
            _hostToMiddleware.Dispose();
            _middlewareToHost.Dispose();
        }
    }
}
