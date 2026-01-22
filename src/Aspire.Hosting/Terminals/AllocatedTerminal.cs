// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Represents an allocated terminal with its connection information.
/// </summary>
internal sealed record AllocatedTerminal
{
    /// <summary>
    /// Gets the unique identifier for this terminal.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the path to the Unix domain socket for workload connections.
    /// </summary>
    public required string SocketPath { get; init; }

    /// <summary>
    /// Gets the WebSocket URL for presentation connections (xterm.js clients).
    /// </summary>
    public required string WebSocketUrl { get; init; }

    /// <summary>
    /// Gets the URL to the standalone xterm.js test page.
    /// </summary>
    public required string TestPageUrl { get; init; }
}
