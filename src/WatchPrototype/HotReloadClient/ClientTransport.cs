// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Transport abstraction for communication between dotnet-watch (server) and the hot reload agent (client).
/// Similar to the agent-side <c>Transport</c> abstraction, but for the server side.
/// </summary>
internal abstract class ClientTransport : IDisposable
{
    /// <summary>
    /// Configure transport-specific environment variables for the target process.
    /// May start the transport server (e.g., Kestrel for WebSocket) to determine the endpoint.
    /// </summary>
    public abstract void ConfigureEnvironment(IDictionary<string, string> env);

    /// <summary>
    /// Initiates connection with the agent in the target process.
    /// Returns a task that completes when the connection is established.
    /// The task is started (hot) immediately so the transport is listening before the process launches.
    /// </summary>
    public abstract Task WaitForConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes a message to the transport: a request type byte followed by optional payload data.
    /// </summary>
    /// <param name="type">The request type byte.</param>
    /// <param name="writePayload">Optional callback to serialize payload data to the stream. Null for notification-only messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract ValueTask WriteAsync(byte type, Func<Stream, CancellationToken, ValueTask>? writePayload, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the next response from the transport.
    /// Returns null if the connection has been lost.
    /// </summary>
    public abstract ValueTask<ClientTransportResponse?> ReadAsync(CancellationToken cancellationToken);

    public abstract void Dispose();
}
