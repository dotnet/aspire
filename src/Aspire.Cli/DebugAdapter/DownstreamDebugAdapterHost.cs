// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Protocol;

namespace Aspire.Hosting.Cli.DebugAdapter;

/// <summary>
/// Handles the downstream connection to the real debug adapter (debugger).
/// Receives events and reverse requests from the debugger and forwards them upstream.
/// </summary>
internal sealed class DownstreamDebugAdapterHost : DebugAdapterHostBase
{
    private readonly RequestRouter _router = new();
    private Action<DebugEvent>? _onEventReceived;
    private Func<RunInTerminalArguments, Task<RunInTerminalResponse>>? _onRunInTerminalRequest;
    private Func<StartDebuggingArguments, Task>? _onStartDebuggingRequest;

    /// <summary>
    /// Creates a new downstream debug adapter host.
    /// </summary>
    /// <param name="adapterStdIn">Stream to write requests to the adapter (adapter's stdin).</param>
    /// <param name="adapterStdOut">Stream to read responses/events from the adapter (adapter's stdout).</param>
    /// <param name="options">Protocol options.</param>
    public DownstreamDebugAdapterHost(Stream adapterStdIn, Stream adapterStdOut, DebugProtocolOptions options)
    {
        InitializeProtocolHost(adapterStdIn, adapterStdOut, options);
        Protocol.EventReceived += OnProtocolEventReceived;
    }

    /// <summary>
    /// Sets the callback for when events are received from the downstream adapter.
    /// </summary>
    public void OnEventReceived(Action<DebugEvent> handler)
    {
        _onEventReceived = handler;
    }

    /// <summary>
    /// Sets the callback for when a RunInTerminal request is received from the downstream adapter.
    /// </summary>
    public void OnRunInTerminalRequest(Func<RunInTerminalArguments, Task<RunInTerminalResponse>> handler)
    {
        _onRunInTerminalRequest = handler;
    }

    /// <summary>
    /// Sets the callback for when a StartDebugging request is received from the downstream adapter.
    /// </summary>
    public void OnStartDebuggingRequest(Func<StartDebuggingArguments, Task> handler)
    {
        _onStartDebuggingRequest = handler;
    }

    /// <summary>
    /// Gets the request router for tracking pending requests.
    /// </summary>
    public RequestRouter Router => _router;

    /// <summary>
    /// Starts the protocol host.
    /// </summary>
    public void Run()
    {
        Protocol.Run();
    }

    /// <summary>
    /// Waits for the protocol reader to complete.
    /// </summary>
    public void WaitForReader()
    {
        Protocol.WaitForReader();
    }

    /// <summary>
    /// Sends a request to the downstream adapter.
    /// </summary>
    public void SendRequest<TArgs, TResponse>(
        DebugRequestWithResponse<TArgs, TResponse> request,
        Action<TResponse> onSuccess,
        Action<ProtocolException> onError)
        where TArgs : class, new()
        where TResponse : ResponseBody
    {
        Protocol.SendRequest(
            request,
            (args, response) => onSuccess(response),
            (args, error) => onError(error));
    }

    /// <summary>
    /// Sends a request with no response body to the downstream adapter.
    /// </summary>
    public void SendRequest<TArgs>(
        DebugRequest<TArgs> request,
        Action onSuccess,
        Action<ProtocolException> onError)
        where TArgs : class, new()
    {
        Protocol.SendRequest(
            request,
            args => onSuccess(),
            (args, error) => onError(error));
    }

    private void OnProtocolEventReceived(object? sender, EventReceivedEventArgs e)
    {
        if (e.Body is null)
        {
            return;
        }

        try
        {
            _onEventReceived?.Invoke(e.Body);
        }
        catch
        {
            // Swallow exceptions to prevent a single event failure from disrupting the middleware
        }
    }

    /// <inheritdoc/>
    protected override async void HandleRunInTerminalRequestAsync(IRequestResponder<RunInTerminalArguments, RunInTerminalResponse> responder)
    {
        if (_onRunInTerminalRequest is null)
        {
            base.HandleRunInTerminalRequestAsync(responder);
            return;
        }

        try
        {
            var response = await _onRunInTerminalRequest(responder.Arguments).ConfigureAwait(false);
            responder.SetResponse(response);
        }
        catch (ProtocolException ex)
        {
            responder.SetError(ex);
        }
        catch (Exception ex)
        {
            responder.SetError(new ProtocolException(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    protected override async void HandleStartDebuggingRequestAsync(IRequestResponder<StartDebuggingArguments> responder)
    {
        if (_onStartDebuggingRequest is null)
        {
            base.HandleStartDebuggingRequestAsync(responder);
            return;
        }

        try
        {
            await _onStartDebuggingRequest(responder.Arguments).ConfigureAwait(false);
            responder.SetResponse(null);
        }
        catch (ProtocolException ex)
        {
            responder.SetError(ex);
        }
        catch (Exception ex)
        {
            responder.SetError(new ProtocolException(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    protected override void HandleProtocolError(Exception ex)
    {
        // Protocol errors are logged but don't crash the middleware
    }
}
