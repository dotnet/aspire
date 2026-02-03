// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Protocol;

namespace Aspire.Cli.Tests.DebugAdapter;

internal sealed class TestDebugAdapterClient
{
    public DebugProtocolHost Host { get; }

    public TaskCompletionSource<OutputEvent> OutputEventReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<RunInTerminalArguments> RunInTerminalReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Creates a test debug adapter client (IDE simulator).
    /// </summary>
    /// <param name="adapterStdOut">Stream to read responses/events from the adapter (adapter's stdout).</param>
    /// <param name="adapterStdIn">Stream to write requests to the adapter (adapter's stdin).</param>
    public TestDebugAdapterClient(Stream adapterStdOut, Stream adapterStdIn)
    {
        // DebugProtocolHost constructor: (debugAdapterStdIn, debugAdapterStdOut)
        // where StdIn = write requests TO adapter, StdOut = read responses FROM adapter
        Host = new DebugProtocolHost(adapterStdIn, adapterStdOut, registerStandardHandlers: false, DebugProtocolOptions.None);

        Host.RegisterEventType<OutputEvent>(evt => OutputEventReceived.TrySetResult(evt));

        Host.RegisterClientRequestType<RunInTerminalRequest, RunInTerminalArguments, RunInTerminalResponse>(
            responder =>
            {
                RunInTerminalReceived.TrySetResult(responder.Arguments);
                responder.SetResponse(new RunInTerminalResponse
                {
                    ProcessId = 1234
                });
            });
    }
}

internal sealed class TestDebugAdapter : DebugAdapterBase
{
    public TaskCompletionSource<EvaluateArguments> EvaluateReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<PauseArguments> PauseReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TestDebugAdapter(Stream input, Stream output)
    {
        InitializeProtocolClient(input, output, DebugProtocolOptions.None);
    }

    protected override void HandleEvaluateRequestAsync(IRequestResponder<EvaluateArguments, EvaluateResponse> responder)
    {
        EvaluateReceived.TrySetResult(responder.Arguments);
        responder.SetResponse(new EvaluateResponse
        {
            Result = "test-result",
            VariablesReference = 0
        });
    }

    protected override void HandlePauseRequestAsync(IRequestResponder<PauseArguments> responder)
    {
        PauseReceived.TrySetResult(responder.Arguments);
        responder.SetResponse(null);
    }
}
