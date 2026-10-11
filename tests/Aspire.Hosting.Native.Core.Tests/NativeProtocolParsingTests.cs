// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Hosting.Native.Auxiliary;
using Aspire.Hosting.Native.Cli;
using Aspire.Hosting.Native.Dcp;
using Aspire.Hosting.Native.Runtime;
using Aspire.Hosting.Native.Rpc;

namespace Aspire.Hosting.Native.Core.Tests;

public class NativeProtocolParsingTests
{
    [Theory]
    [InlineData("Starting")]
    [InlineData("Running")]
    [InlineData("Failed")]
    [InlineData("Stopped")]
    public void LifecycleLabelsRoundTripExactly(string wire) =>
        Assert.Equal(wire, RuntimeStates.ParseObservation(wire).ToWire());

    [Theory]
    [InlineData("Unknown")]
    [InlineData("running")]
    [InlineData("1")]
    [InlineData("custom-integration-state")]
    public void ObservationsRetainTheExistingClosedNativeContract(string wire) =>
        Assert.Throws<ArgumentException>(() => RuntimeStates.ParseObservation(wire));

    [Fact]
    public void ClosedResultParsersRejectWrongDomainsAndNumericAliases()
    {
        Assert.Equal(ConfigurationStatus.Succeeded, RuntimeStates.ParseConfigurationResult("succeeded"));
        Assert.Equal(RequestOutcome.Cancelled, RuntimeStates.ParseCommandResult("cancelled"));
        Assert.Equal(LogStream.Stderr, RuntimeStates.ParseLogStream("stderr"));
        Assert.Equal(WorkloadProtocol.Udp, RuntimeStates.ParseProtocol("UDP"));
        Assert.Equal(WorkloadMountKind.Volume, RuntimeStates.ParseMountKind("volume"));
        Assert.Throws<ArgumentException>(() => RuntimeStates.ParseConfigurationResult("pending"));
        Assert.Throws<ArgumentException>(() => RuntimeStates.ParseCommandResult("accepted"));
        Assert.Throws<ArgumentException>(() => RuntimeStates.ParseProtocol("tcp"));
        Assert.Throws<ArgumentException>(() => RuntimeStates.ParseMountKind("0"));
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"status":{}}""")]
    [InlineData("""{"status":{"state":null}}""")]
    [InlineData("""{"status":{"state":"NewDcpState"}}""")]
    [InlineData("""{"status":{"state":"running"}}""")]
    public void UnrecognizedDcpStatesNeverBecomeSuccessfulWorkloads(string json)
    {
        var resource = JsonSerializer.Deserialize(json, DcpJsonContext.Default.DcpResource)!;
        Assert.Equal(DcpProcessState.Unknown, resource.Status?.State ?? DcpProcessState.Unknown);
        Assert.Throws<InvalidDataException>(() => DcpStates.Observe(resource.Status));
    }

    [Theory]
    [InlineData("""{"status":{"state":1}}""")]
    [InlineData("""{"status":{"state":true}}""")]
    [InlineData("""{"status":{"state":{}}}""")]
    [InlineData("""{"status":{"state":[]}}""")]
    public void MalformedDcpStatusTypesAreProtocolErrors(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, DcpJsonContext.Default.DcpResource));

    [Fact]
    public void RecognizedTransitionalAndTerminalDcpStatesHaveExplicitOutcomes()
    {
        Assert.Equal(WorkloadPhase.Running, DcpStates.Observe(new() { State = DcpProcessState.Running }).Phase);
        Assert.Equal(WorkloadPhase.Pending, DcpStates.Observe(new() { State = DcpProcessState.Paused }).Phase);
        Assert.Equal(WorkloadPhase.Exited, DcpStates.Observe(new() { State = DcpProcessState.Terminated }).Phase);
        Assert.Equal(WorkloadPhase.Failed, DcpStates.Observe(new() { State = DcpProcessState.FailedToStart, ExitCode = 0 }).Phase);
        Assert.True(DcpProcessState.Terminated.IsTerminal());
        Assert.False(DcpProcessState.Unknown.IsTerminal());
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"status":null}""")]
    [InlineData("""{"status":"FutureState"}""")]
    public void MissingAndUnknownAdministrativeStatusNeverCompleteCleanup(string json) =>
        Assert.Equal(DcpExecutionState.Unknown, JsonSerializer.Deserialize(json, DcpJsonContext.Default.DcpExecutionResponse)!.Status);

    [Theory]
    [InlineData("")]
    [InlineData("tail-without-newline")]
    [InlineData("ready\npartial")]
    [InlineData("ready\r\n\npartial")]
    public void LogParserRetainsOnlyCompleteLines(string text)
    {
        var expected = text switch
        {
            "ready\npartial" => new[] { "ready" },
            "ready\r\n\npartial" => ["ready\r", ""],
            _ => []
        };
        Assert.Equal(expected, DcpProtocol.ParseCompleteLogLines(Encoding.UTF8.GetBytes(text)));
    }

    [Theory]
    [InlineData("""[{"resourceName":"cache","status":"healthy"}]""")]
    [InlineData("""{"request":{"resourceName":"cache","status":"healthy"}}""")]
    [InlineData("""{"resourceName":"cache","status":"healthy"}""")]
    public void AuxiliaryArgumentShapesParseToTheSameTypedRequest(string parameters)
    {
        var request = NativeAuxiliaryProtocol.ReadRequest(Request(parameters), NativeAuxiliaryJsonContext.Default.NativeAuxiliaryWaitRequest, required: true)!;
        Assert.Equal("cache", request.ResourceName);
        Assert.True(NativeAuxiliaryProtocol.TryParseWaitTarget(request.Status, out var target));
        Assert.Equal(NativeWaitTarget.Healthy, target);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("null")]
    public void OptionalAbsentAuxiliaryRequestsDoNotBecomeRequiredRequests(string parameters)
    {
        var request = Request(parameters);
        Assert.Null(NativeAuxiliaryProtocol.ReadRequest(request, NativeAuxiliaryJsonContext.Default.NativeAuxiliaryWaitRequest, required: false));
        Assert.Throws<ArgumentException>(() => NativeAuxiliaryProtocol.ReadRequest(request,
            NativeAuxiliaryJsonContext.Default.NativeAuxiliaryWaitRequest, required: true));
    }

    [Theory]
    [InlineData("[{},{}]")]
    [InlineData("""{"request":{},"extra":1}""")]
    [InlineData("true")]
    public void ExtraOrMalformedAuxiliaryArgumentsAreRejected(string parameters) =>
        Assert.Throws<ArgumentException>(() => NativeAuxiliaryProtocol.ReadRequest(Request(parameters),
            NativeAuxiliaryJsonContext.Default.NativeAuxiliaryWaitRequest, required: true));

    [Theory]
    [InlineData("""{"jsonrpc":"2.0","method":"authenticate"}""", true)]
    [InlineData("""{"jsonrpc":"2.0","id":null,"method":"authenticate"}""", false)]
    [InlineData("""{"jsonrpc":"2.0","id":1,"method":"authenticate"}""", false)]
    [InlineData("""{"jsonrpc":"2.0","id":"one","method":"authenticate"}""", false)]
    public void RpcEnvelopeKeepsNotificationsDistinctFromNullRequestIds(string json, bool notification)
    {
        var request = NativeRpcProtocol.ParseRequest(Encoding.UTF8.GetBytes(json));
        Assert.Equal(notification, request.IsNotification);
        Assert.Equal("authenticate", request.Method);
    }

    [Theory]
    [InlineData("""{"jsonrpc":"2.0","method":"a","method":"b"}""")]
    [InlineData("""{"jsonrpc":"2.0","method":"a","params":[{"x":1,"x":2}]}""")]
    [InlineData("""{"jsonrpc":"2.0","method":"a","id":true}""")]
    [InlineData("""{"jsonrpc":"2.0","method":1}""")]
    [InlineData("""{"jsonrpc":"2.0","method":"a","unknown":true}""")]
    public void RpcEnvelopeRejectsDuplicateKeysAndMalformedClosedFields(string json) =>
        Assert.Throws<RpcFault>(() => NativeRpcProtocol.ParseRequest(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public void TraceParsingCannotAttachDuplicateMalformedOrOversizedCallerContext()
    {
        const string parent = "00-11111111111111111111111111111111-2222222222222222-01";
        var valid = Encoding.UTF8.GetBytes($$"""{"jsonrpc":"2.0","method":"a","traceparent":"{{parent}}"}""");
        Assert.NotNull(NativeRpcProtocol.ReadTraceContext(valid, 1024));
        Assert.Null(NativeRpcProtocol.ReadTraceContext(valid, 1));
        var duplicate = Encoding.UTF8.GetBytes($$"""{"traceparent":"invalid","traceparent":"{{parent}}"}""");
        Assert.Null(NativeRpcProtocol.ReadTraceContext(duplicate, 1024));
        Assert.Null(NativeRpcProtocol.ReadTraceContext("""{"traceparent":true}"""u8.ToArray(), 1024));
        Assert.Null(NativeRpcProtocol.ReadTraceContext("""{"traceparent":"invalid"}"""u8.ToArray(), 1024));
    }

    private static NativeCliRequest Request(string parameters) => new()
    {
        Jsonrpc = "2.0",
        Method = "WaitForResourceAsync",
        Params = JsonSerializer.Deserialize(parameters, NativeCliJsonContext.Default.JsonElement)
    };
}
