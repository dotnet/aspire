// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.DebugAdapter.Protocol;
using Aspire.DebugAdapter.Types;

namespace Aspire.Cli.Tests.DebugAdapter;

public class StreamMessageTransportTests
{
    [Fact]
    public async Task ErrorResponseWithEmptyBodyIsDeserializedAsBaseResponseMessage()
    {
        // An error response (success=false) with an empty body should not throw,
        // even when the derived response type requires body properties.
        var json = """{"seq":1,"type":"response","request_seq":5,"success":false,"command":"evaluate","body":{}}""";

        var message = await ReceiveMessageAsync(json);

        var response = Assert.IsType<ResponseMessage>(message);
        Assert.Equal(1, response.Seq);
        Assert.Equal(5, response.RequestSeq);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ErrorResponseWithNoBodyIsDeserializedAsBaseResponseMessage()
    {
        // An error response with no body property at all should not throw.
        var json = """{"seq":2,"type":"response","request_seq":6,"success":false,"command":"evaluate","message":"not available"}""";

        var message = await ReceiveMessageAsync(json);

        var response = Assert.IsType<ResponseMessage>(message);
        Assert.Equal(2, response.Seq);
        Assert.Equal(6, response.RequestSeq);
        Assert.False(response.Success);
        Assert.Equal("not available", response.Message);
    }

    [Fact]
    public async Task ErrorResponseWithErrorBodyIsDeserializedAsBaseResponseMessage()
    {
        // An error response with a DAP error body (different shape from the typed response body).
        var json = """{"seq":3,"type":"response","request_seq":7,"success":false,"command":"evaluate","message":"Error","body":{"error":{"id":1,"format":"Cannot evaluate"}}}""";

        var message = await ReceiveMessageAsync(json);

        var response = Assert.IsType<ResponseMessage>(message);
        Assert.Equal(3, response.Seq);
        Assert.Equal(7, response.RequestSeq);
        Assert.False(response.Success);
        Assert.Equal("Error", response.Message);
    }

    [Fact]
    public async Task SuccessResponseIsDeserializedAsTypedResponse()
    {
        // A success response should still be deserialized as the typed derived class.
        var json = """{"seq":4,"type":"response","request_seq":8,"success":true,"command":"evaluate","body":{"result":"42","variablesReference":0}}""";

        var message = await ReceiveMessageAsync(json);

        var response = Assert.IsAssignableFrom<ResponseMessage>(message);
        Assert.Equal(4, response.Seq);
        Assert.Equal(8, response.RequestSeq);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ErrorResponsePreservesCommandInExtensionData()
    {
        // The "command" discriminator value should still be available for re-serialization.
        var json = """{"seq":5,"type":"response","request_seq":9,"success":false,"command":"evaluate","body":{}}""";

        var message = await ReceiveMessageAsync(json);

        var response = Assert.IsType<ResponseMessage>(message);
        Assert.NotNull(response.ExtensionData);
        Assert.True(response.ExtensionData.ContainsKey("command"));
        Assert.Equal("evaluate", response.ExtensionData["command"].GetString());
    }

    [Fact]
    public async Task ErrorResponseRoundTripsViaTransport()
    {
        // An error response should be readable and writable without losing data.
        var originalJson = """{"seq":6,"type":"response","request_seq":10,"success":false,"command":"setBreakpoints","message":"compile error"}""";

        var message = await ReceiveMessageAsync(originalJson);
        Assert.NotNull(message);

        // Write it back out
        using var outputStream = new MemoryStream();
        using var emptyInput = new MemoryStream();
        var outputTransport = new StreamMessageTransport(emptyInput, outputStream);

        await outputTransport.SendAsync(message);

        var written = Encoding.UTF8.GetString(outputStream.ToArray());
        Assert.Contains("\"success\":false", written);
        Assert.Contains("\"command\":\"setBreakpoints\"", written);
        Assert.Contains("\"request_seq\":10", written);
    }

    [Fact]
    public async Task RequestMessageIsDeserializedCorrectly()
    {
        var json = """{"seq":1,"type":"request","command":"initialize","arguments":{"adapterID":"test","clientID":"test"}}""";

        var message = await ReceiveMessageAsync(json);

        Assert.IsAssignableFrom<RequestMessage>(message);
    }

    [Fact]
    public async Task EventMessageIsDeserializedCorrectly()
    {
        var json = """{"seq":1,"type":"event","event":"output","body":{"category":"console","output":"hello"}}""";

        var message = await ReceiveMessageAsync(json);

        Assert.IsAssignableFrom<EventMessage>(message);
    }

    [Fact]
    public async Task DeserializedMessageSurvivesReserialization()
    {
        // A deserialized message should survive re-serialization without producing
        // duplicate discriminator properties that break the next deserialization.
        var stream1 = new BlockingStream();
        var stream2 = new BlockingStream();

        var sender = new StreamMessageTransport(Stream.Null, stream1);
        var middleReceiver = new StreamMessageTransport(stream1, Stream.Null);
        var middleSender = new StreamMessageTransport(Stream.Null, stream2);
        var finalReceiver = new StreamMessageTransport(stream2, Stream.Null);

        var request = new EvaluateRequest { Seq = 1, Arguments = new EvaluateArguments { Expression = "test" } };
        await sender.SendAsync(request);

        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var received = await middleReceiver.ReceiveAsync(cts1.Token);
        Assert.IsType<EvaluateRequest>(received);

        // Re-send the deserialized message (simulating middleware forwarding)
        received!.Seq = 100;
        await middleSender.SendAsync(received);

        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var final = await finalReceiver.ReceiveAsync(cts2.Token);

        var evalReq = Assert.IsType<EvaluateRequest>(final);
        Assert.Equal("test", evalReq.Arguments.Expression);

        stream1.CompleteWriting();
        stream2.CompleteWriting();
    }

    /// <summary>
    /// Helper that wraps a JSON string in DAP wire format and reads it via <see cref="StreamMessageTransport"/>.
    /// </summary>
    private static async Task<ProtocolMessage?> ReceiveMessageAsync(string json)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var wireMessage = $"Content-Length: {jsonBytes.Length}\r\n\r\n{json}";
        var wireBytes = Encoding.UTF8.GetBytes(wireMessage);

        using var inputStream = new MemoryStream(wireBytes);
        using var outputStream = new MemoryStream();
        var transport = new StreamMessageTransport(inputStream, outputStream);

        return await transport.ReceiveAsync(CancellationToken.None);
    }
}
