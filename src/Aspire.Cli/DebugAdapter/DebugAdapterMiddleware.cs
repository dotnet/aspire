// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Protocol;

namespace Aspire.Hosting.Cli.DebugAdapter;

/// <summary>
/// A debug adapter protocol middleware that bridges communication between an upstream
/// debug adapter client (IDE) and a downstream debug adapter server (debugger).
/// Requests, responses, and events are forwarded bidirectionally with optional interception.
/// </summary>
internal sealed class DebugAdapterMiddleware : DebugAdapterBase
{
    private readonly DownstreamDebugAdapterHost _downstream;
    private readonly RequestRouter _upstreamRouter = new();

    /// <summary>
    /// Creates a new debug adapter middleware instance.
    /// </summary>
    public DebugAdapterMiddleware(
        Stream upstreamIn,
        Stream upstreamOut,
        Stream downstreamIn,
        Stream downstreamOut)
        : this(upstreamIn, upstreamOut, downstreamIn, downstreamOut, DebugProtocolOptions.None)
    {
    }

    /// <summary>
    /// Creates a new debug adapter middleware instance with protocol options.
    /// </summary>
    public DebugAdapterMiddleware(
        Stream upstreamIn,
        Stream upstreamOut,
        Stream downstreamIn,
        Stream downstreamOut,
        DebugProtocolOptions options)
    {
        // Initialize upstream connection (we act as a debug adapter server to the IDE)
        InitializeProtocolClient(upstreamIn, upstreamOut, options);

        // Initialize downstream connection (we act as a debug adapter host/client to the real debugger)
        // Note: DebugProtocolHost expects (stdIn, stdOut) where stdIn is for writing TO the adapter
        // and stdOut is for reading FROM the adapter
        _downstream = new DownstreamDebugAdapterHost(downstreamOut, downstreamIn, options);

        // Wire up forwarding from downstream to upstream
        _downstream.OnEventReceived(ForwardEventToUpstream);
        _downstream.OnRunInTerminalRequest(ForwardRunInTerminalToUpstream);
        _downstream.OnStartDebuggingRequest(ForwardStartDebuggingToUpstream);
    }

    /// <summary>
    /// Starts the middleware, processing messages on both connections.
    /// </summary>
    public void Run()
    {
        _downstream.Run();
        Protocol.Run();
    }

    /// <summary>
    /// Waits for both protocol readers to complete.
    /// </summary>
    public void WaitForReaders()
    {
        Protocol.WaitForReader();
        _downstream.WaitForReader();
    }

    #region Event Forwarding (Downstream -> Upstream)

    private void ForwardEventToUpstream(DebugEvent evt)
    {
        Protocol.SendEvent(evt);
    }

    #endregion

    #region Reverse Request Forwarding (Downstream -> Upstream)

    private Task<RunInTerminalResponse> ForwardRunInTerminalToUpstream(RunInTerminalArguments args)
    {
        var request = new RunInTerminalRequest();
        CopyArgs(args, request.Args);

        var tcs = new TaskCompletionSource<RunInTerminalResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        Protocol.SendClientRequest(
            request,
            (_, response) => tcs.TrySetResult(response),
            (_, error) => tcs.TrySetException(error));

        return tcs.Task;
    }

    private Task ForwardStartDebuggingToUpstream(StartDebuggingArguments args)
    {
        var request = new StartDebuggingRequest();
        CopyArgs(args, request.Args);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Protocol.SendClientRequest(
            request,
            _ => tcs.TrySetResult(),
            (_, error) => tcs.TrySetException(error));

        return tcs.Task;
    }

    #endregion

    #region Args Copying Helper

    /// <summary>
    /// Copies all public writable properties from source to target using reflection.
    /// </summary>
    private static void CopyArgs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(TArgs source, TArgs target) where TArgs : class
    {
        var properties = typeof(TArgs).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(source);
                prop.SetValue(target, value);
            }
        }
    }

    #endregion

    #region Request Forwarding Helpers (Upstream -> Downstream)

    /// <summary>
    /// Forwards a request with a response body to the downstream adapter.
    /// </summary>
    private void ForwardRequest<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs, TResponse>(IRequestResponder<TArgs, TResponse> responder)
        where TRequest : DebugRequestWithResponse<TArgs, TResponse>, new()
        where TArgs : class, new()
        where TResponse : ResponseBody
    {
        var request = new TRequest();
        CopyArgs(responder.Arguments, request.Args);

        _downstream.SendRequest(
            request,
            responder.SetResponse,
            responder.SetError);
    }

    /// <summary>
    /// Forwards a request with no response body to the downstream adapter.
    /// </summary>
    private void ForwardRequestNoBody<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(IRequestResponder<TArgs> responder)
        where TRequest : DebugRequest<TArgs>, new()
        where TArgs : class, new()
    {
        var request = new TRequest();
        CopyArgs(responder.Arguments, request.Args);

        _downstream.SendRequest(
            request,
            () => responder.SetResponse(null),
            responder.SetError);
    }

    #endregion

    #region Connection Failure Handling

    private void FailAllPending(string message)
    {
        var error = new ProtocolException(message);
        _downstream.Router.FailAll(error);
        _upstreamRouter.FailAll(error);
    }

    #endregion

    #region Side-Channel Injection API

    /// <summary>
    /// Injects a request to the downstream adapter from the middleware itself.
    /// </summary>
    public Task<TResponse> InjectDownstreamRequestAsync<TArgs, TResponse>(
        DebugRequestWithResponse<TArgs, TResponse> request,
        CancellationToken cancellationToken = default)
        where TArgs : class, new()
        where TResponse : ResponseBody
    {
        var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var correlationId = _downstream.Router.RegisterSideChannel(tcs, cancellationToken);

        _downstream.SendRequest(
            request,
            response => _downstream.Router.Complete(correlationId, response),
            error => _downstream.Router.Fail(correlationId, error));

        return tcs.Task;
    }

    /// <summary>
    /// Injects a request with no response body to the downstream adapter.
    /// </summary>
    public Task InjectDownstreamRequestAsync<TArgs>(
        DebugRequest<TArgs> request,
        CancellationToken cancellationToken = default)
        where TArgs : class, new()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var correlationId = _downstream.Router.RegisterSideChannelNoBody(tcs, cancellationToken);

        _downstream.SendRequest(
            request,
            () => _downstream.Router.CompleteNoBody(correlationId),
            error => _downstream.Router.Fail(correlationId, error));

        return tcs.Task;
    }

    /// <summary>
    /// Injects a reverse request to the upstream client from the middleware itself.
    /// </summary>
    public Task<TResponse> InjectUpstreamRequestAsync<TArgs, TResponse>(
        DebugClientRequestWithResponse<TArgs, TResponse> request,
        CancellationToken cancellationToken = default)
        where TArgs : class, new()
        where TResponse : ResponseBody
    {
        var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var correlationId = _upstreamRouter.RegisterSideChannel(tcs, cancellationToken);

        Protocol.SendClientRequest(
            request,
            (_, response) => _upstreamRouter.Complete(correlationId, response),
            (_, error) => _upstreamRouter.Fail(correlationId, error));

        return tcs.Task;
    }

    /// <summary>
    /// Injects a reverse request with no response body to the upstream client.
    /// </summary>
    public Task InjectUpstreamRequestAsync<TArgs>(
        DebugClientRequest<TArgs> request,
        CancellationToken cancellationToken = default)
        where TArgs : class, new()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var correlationId = _upstreamRouter.RegisterSideChannelNoBody(tcs, cancellationToken);

        Protocol.SendClientRequest(
            request,
            _ => _upstreamRouter.CompleteNoBody(correlationId),
            (_, error) => _upstreamRouter.Fail(correlationId, error));

        return tcs.Task;
    }

    #endregion

    #region Request Handlers - Forwarded to Downstream

    protected override void HandleInitializeRequestAsync(IRequestResponder<InitializeArguments, InitializeResponse> responder)
        => ForwardRequest<InitializeRequest, InitializeArguments, InitializeResponse>(responder);

    protected override void HandleLaunchRequestAsync(IRequestResponder<LaunchArguments> responder)
        => ForwardRequestNoBody<LaunchRequest, LaunchArguments>(responder);

    protected override void HandleAttachRequestAsync(IRequestResponder<AttachArguments> responder)
        => ForwardRequestNoBody<AttachRequest, AttachArguments>(responder);

    protected override void HandleDisconnectRequestAsync(IRequestResponder<DisconnectArguments> responder)
    {
        FailAllPending("Debug session disconnected");
        ForwardRequestNoBody<DisconnectRequest, DisconnectArguments>(responder);
    }

    protected override void HandleTerminateRequestAsync(IRequestResponder<TerminateArguments> responder)
        => ForwardRequestNoBody<TerminateRequest, TerminateArguments>(responder);

    protected override void HandleRestartRequestAsync(IRequestResponder<RestartArguments> responder)
        => ForwardRequestNoBody<RestartRequest, RestartArguments>(responder);

    protected override void HandleConfigurationDoneRequestAsync(IRequestResponder<ConfigurationDoneArguments> responder)
        => ForwardRequestNoBody<ConfigurationDoneRequest, ConfigurationDoneArguments>(responder);

    protected override void HandleContinueRequestAsync(IRequestResponder<ContinueArguments, ContinueResponse> responder)
        => ForwardRequest<ContinueRequest, ContinueArguments, ContinueResponse>(responder);

    protected override void HandleNextRequestAsync(IRequestResponder<NextArguments> responder)
        => ForwardRequestNoBody<NextRequest, NextArguments>(responder);

    protected override void HandleStepInRequestAsync(IRequestResponder<StepInArguments> responder)
        => ForwardRequestNoBody<StepInRequest, StepInArguments>(responder);

    protected override void HandleStepOutRequestAsync(IRequestResponder<StepOutArguments> responder)
        => ForwardRequestNoBody<StepOutRequest, StepOutArguments>(responder);

    protected override void HandleStepBackRequestAsync(IRequestResponder<StepBackArguments> responder)
        => ForwardRequestNoBody<StepBackRequest, StepBackArguments>(responder);

    protected override void HandleReverseContinueRequestAsync(IRequestResponder<ReverseContinueArguments> responder)
        => ForwardRequestNoBody<ReverseContinueRequest, ReverseContinueArguments>(responder);

    protected override void HandleRestartFrameRequestAsync(IRequestResponder<RestartFrameArguments> responder)
        => ForwardRequestNoBody<RestartFrameRequest, RestartFrameArguments>(responder);

    protected override void HandleGotoRequestAsync(IRequestResponder<GotoArguments> responder)
        => ForwardRequestNoBody<GotoRequest, GotoArguments>(responder);

    protected override void HandlePauseRequestAsync(IRequestResponder<PauseArguments> responder)
        => ForwardRequestNoBody<PauseRequest, PauseArguments>(responder);

    protected override void HandleStackTraceRequestAsync(IRequestResponder<StackTraceArguments, StackTraceResponse> responder)
        => ForwardRequest<StackTraceRequest, StackTraceArguments, StackTraceResponse>(responder);

    protected override void HandleScopesRequestAsync(IRequestResponder<ScopesArguments, ScopesResponse> responder)
        => ForwardRequest<ScopesRequest, ScopesArguments, ScopesResponse>(responder);

    protected override void HandleVariablesRequestAsync(IRequestResponder<VariablesArguments, VariablesResponse> responder)
        => ForwardRequest<VariablesRequest, VariablesArguments, VariablesResponse>(responder);

    protected override void HandleSetVariableRequestAsync(IRequestResponder<SetVariableArguments, SetVariableResponse> responder)
        => ForwardRequest<SetVariableRequest, SetVariableArguments, SetVariableResponse>(responder);

    protected override void HandleSourceRequestAsync(IRequestResponder<SourceArguments, SourceResponse> responder)
        => ForwardRequest<SourceRequest, SourceArguments, SourceResponse>(responder);

    protected override void HandleThreadsRequestAsync(IRequestResponder<ThreadsArguments, ThreadsResponse> responder)
        => ForwardRequest<ThreadsRequest, ThreadsArguments, ThreadsResponse>(responder);

    protected override void HandleTerminateThreadsRequestAsync(IRequestResponder<TerminateThreadsArguments> responder)
        => ForwardRequestNoBody<TerminateThreadsRequest, TerminateThreadsArguments>(responder);

    protected override void HandleModulesRequestAsync(IRequestResponder<ModulesArguments, ModulesResponse> responder)
        => ForwardRequest<ModulesRequest, ModulesArguments, ModulesResponse>(responder);

    protected override void HandleLoadedSourcesRequestAsync(IRequestResponder<LoadedSourcesArguments, LoadedSourcesResponse> responder)
        => ForwardRequest<LoadedSourcesRequest, LoadedSourcesArguments, LoadedSourcesResponse>(responder);

    protected override void HandleEvaluateRequestAsync(IRequestResponder<EvaluateArguments, EvaluateResponse> responder)
        => ForwardRequest<EvaluateRequest, EvaluateArguments, EvaluateResponse>(responder);

    protected override void HandleSetExpressionRequestAsync(IRequestResponder<SetExpressionArguments, SetExpressionResponse> responder)
        => ForwardRequest<SetExpressionRequest, SetExpressionArguments, SetExpressionResponse>(responder);

    protected override void HandleStepInTargetsRequestAsync(IRequestResponder<StepInTargetsArguments, StepInTargetsResponse> responder)
        => ForwardRequest<StepInTargetsRequest, StepInTargetsArguments, StepInTargetsResponse>(responder);

    protected override void HandleGotoTargetsRequestAsync(IRequestResponder<GotoTargetsArguments, GotoTargetsResponse> responder)
        => ForwardRequest<GotoTargetsRequest, GotoTargetsArguments, GotoTargetsResponse>(responder);

    protected override void HandleCompletionsRequestAsync(IRequestResponder<CompletionsArguments, CompletionsResponse> responder)
        => ForwardRequest<CompletionsRequest, CompletionsArguments, CompletionsResponse>(responder);

    protected override void HandleExceptionInfoRequestAsync(IRequestResponder<ExceptionInfoArguments, ExceptionInfoResponse> responder)
        => ForwardRequest<ExceptionInfoRequest, ExceptionInfoArguments, ExceptionInfoResponse>(responder);

    protected override void HandleReadMemoryRequestAsync(IRequestResponder<ReadMemoryArguments, ReadMemoryResponse> responder)
        => ForwardRequest<ReadMemoryRequest, ReadMemoryArguments, ReadMemoryResponse>(responder);

    protected override void HandleWriteMemoryRequestAsync(IRequestResponder<WriteMemoryArguments, WriteMemoryResponse> responder)
        => ForwardRequest<WriteMemoryRequest, WriteMemoryArguments, WriteMemoryResponse>(responder);

    protected override void HandleDisassembleRequestAsync(IRequestResponder<DisassembleArguments, DisassembleResponse> responder)
        => ForwardRequest<DisassembleRequest, DisassembleArguments, DisassembleResponse>(responder);

    protected override void HandleCancelRequestAsync(IRequestResponder<CancelArguments> responder)
        => ForwardRequestNoBody<CancelRequest, CancelArguments>(responder);

    protected override void HandleBreakpointLocationsRequestAsync(IRequestResponder<BreakpointLocationsArguments, BreakpointLocationsResponse> responder)
        => ForwardRequest<BreakpointLocationsRequest, BreakpointLocationsArguments, BreakpointLocationsResponse>(responder);

    protected override void HandleSetBreakpointsRequestAsync(IRequestResponder<SetBreakpointsArguments, SetBreakpointsResponse> responder)
        => ForwardRequest<SetBreakpointsRequest, SetBreakpointsArguments, SetBreakpointsResponse>(responder);

    protected override void HandleSetFunctionBreakpointsRequestAsync(IRequestResponder<SetFunctionBreakpointsArguments, SetFunctionBreakpointsResponse> responder)
        => ForwardRequest<SetFunctionBreakpointsRequest, SetFunctionBreakpointsArguments, SetFunctionBreakpointsResponse>(responder);

    protected override void HandleSetExceptionBreakpointsRequestAsync(IRequestResponder<SetExceptionBreakpointsArguments, SetExceptionBreakpointsResponse> responder)
        => ForwardRequest<SetExceptionBreakpointsRequest, SetExceptionBreakpointsArguments, SetExceptionBreakpointsResponse>(responder);

    protected override void HandleDataBreakpointInfoRequestAsync(IRequestResponder<DataBreakpointInfoArguments, DataBreakpointInfoResponse> responder)
        => ForwardRequest<DataBreakpointInfoRequest, DataBreakpointInfoArguments, DataBreakpointInfoResponse>(responder);

    protected override void HandleSetDataBreakpointsRequestAsync(IRequestResponder<SetDataBreakpointsArguments, SetDataBreakpointsResponse> responder)
        => ForwardRequest<SetDataBreakpointsRequest, SetDataBreakpointsArguments, SetDataBreakpointsResponse>(responder);

    protected override void HandleSetInstructionBreakpointsRequestAsync(IRequestResponder<SetInstructionBreakpointsArguments, SetInstructionBreakpointsResponse> responder)
        => ForwardRequest<SetInstructionBreakpointsRequest, SetInstructionBreakpointsArguments, SetInstructionBreakpointsResponse>(responder);

    protected override void HandleLocationsRequestAsync(IRequestResponder<LocationsArguments, LocationsResponse> responder)
        => ForwardRequest<LocationsRequest, LocationsArguments, LocationsResponse>(responder);

    #endregion
}
