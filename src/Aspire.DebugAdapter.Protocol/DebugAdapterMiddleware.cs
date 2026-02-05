// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.DebugAdapter.Types;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Base class for implementing DAP middleware that sits between a client and host,
/// forwarding messages with optional interception and modification.
/// </summary>
/// <remarks>
/// Override the On*Async methods to intercept specific message types. Return false
/// to suppress forwarding of a message. The default implementation forwards all messages.
///
/// Client messages flow: Client -> OnClient*Async -> Host
/// Host messages flow: Host -> OnHost*Async -> Client
/// </remarks>
public class DebugAdapterMiddleware
{
    /// <summary>
    /// The connection to the upstream client.
    /// </summary>
    protected DebugAdapterConnection ClientConnection { get; private set; } = null!;

    /// <summary>
    /// The connection to the downstream host/adapter.
    /// </summary>
    protected DebugAdapterConnection HostConnection { get; private set; } = null!;

    /// <summary>
    /// Called when a request is received from the client.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True to forward the request to the host; false to suppress it.</returns>
    protected virtual Task<bool> OnClientRequestAsync(RequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(true);

    /// <summary>
    /// Called when a response is received from the client (for reverse requests).
    /// </summary>
    /// <param name="response">The response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True to forward the response to the host; false to suppress it.</returns>
    protected virtual Task<bool> OnClientResponseAsync(ResponseMessage response, CancellationToken cancellationToken)
        => Task.FromResult(true);

    /// <summary>
    /// Called when a request is received from the host (reverse requests like RunInTerminal).
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True to forward the request to the client; false to suppress it.</returns>
    protected virtual Task<bool> OnHostRequestAsync(RequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(true);

    /// <summary>
    /// Called when a response is received from the host.
    /// </summary>
    /// <param name="response">The response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True to forward the response to the client; false to suppress it.</returns>
    protected virtual Task<bool> OnHostResponseAsync(ResponseMessage response, CancellationToken cancellationToken)
        => Task.FromResult(true);

    /// <summary>
    /// Called when an event is received from the host.
    /// </summary>
    /// <param name="evt">The event message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True to forward the event to the client; false to suppress it.</returns>
    protected virtual Task<bool> OnHostEventAsync(EventMessage evt, CancellationToken cancellationToken)
        => Task.FromResult(true);

    /// <summary>
    /// Runs the middleware, forwarding messages between client and host until the connection closes
    /// or cancellation is requested.
    /// </summary>
    /// <param name="clientTransport">Transport connected to the client.</param>
    /// <param name="hostTransport">Transport connected to the host/debug adapter.</param>
    /// <param name="cancellationToken">Cancellation token to stop the middleware.</param>
    public async Task RunAsync(
        IMessageTransport clientTransport,
        IMessageTransport hostTransport,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientTransport);
        ArgumentNullException.ThrowIfNull(hostTransport);

        ClientConnection = new DebugAdapterConnection(clientTransport);
        HostConnection = new DebugAdapterConnection(hostTransport);

        await using (ClientConnection)
        await using (HostConnection)
        {
            var clientTask = ProcessClientMessagesAsync(cancellationToken);
            var hostTask = ProcessHostMessagesAsync(cancellationToken);

            // Wait for either side to complete (disconnect) or cancellation
            await Task.WhenAny(clientTask, hostTask).ConfigureAwait(false);

            // Cancel the other side if still running
            // The connections will be disposed, which will cancel pending operations
        }
    }

    private async Task ProcessClientMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await ClientConnection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (message is null)
            {
                break; // Client disconnected
            }

            switch (message)
            {
                case RequestMessage request:
                    if (await OnClientRequestAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        await HostConnection.ForwardRequestAsync(request, request.Seq, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case ResponseMessage response:
                    // Response from client = reverse request response
                    // The mapping is in ClientConnection (where we forwarded the reverse request)
                    if (await OnClientResponseAsync(response, cancellationToken).ConfigureAwait(false))
                    {
                        await ClientConnection.ForwardResponseToAsync(response, HostConnection, cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        }
    }

    private async Task ProcessHostMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await HostConnection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (message is null)
            {
                break; // Host disconnected
            }

            switch (message)
            {
                case ResponseMessage response:
                    if (await OnHostResponseAsync(response, cancellationToken).ConfigureAwait(false))
                    {
                        // Remap the request_seq from host seq back to original client seq
                        // The mapping was stored in HostConnection when we forwarded the request
                        await HostConnection.ForwardResponseToAsync(response, ClientConnection, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case EventMessage evt:
                    if (await OnHostEventAsync(evt, cancellationToken).ConfigureAwait(false))
                    {
                        await ClientConnection.ForwardEventAsync(evt, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case RequestMessage request:
                    // Request from host = reverse request (e.g., RunInTerminal)
                    if (await OnHostRequestAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        await ClientConnection.ForwardRequestAsync(request, request.Seq, cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        }
    }
}
