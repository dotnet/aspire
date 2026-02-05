// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.DebugAdapter.Types;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Abstraction for reading and writing DAP protocol messages over a transport.
/// </summary>
public interface IMessageTransport : IAsyncDisposable
{
    /// <summary>
    /// Sends a protocol message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives the next protocol message, or null if the transport is closed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received message, or null if the transport has been closed.</returns>
    Task<ProtocolMessage?> ReceiveAsync(CancellationToken cancellationToken = default);
}
