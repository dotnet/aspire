// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Handle returned by <see cref="TerminalBuilderExtensions.WithUdsWorkload"/> containing
/// information about the allocated terminal socket.
/// </summary>
internal sealed class TerminalHostWorkloadHandle
{
    /// <summary>
    /// Creates a new workload handle.
    /// </summary>
    /// <param name="socketPath">The path to the Unix domain socket.</param>
    /// <param name="adapter">The UDS workload adapter.</param>
    internal TerminalHostWorkloadHandle(string socketPath, UdsWorkloadAdapter adapter)
    {
        SocketPath = socketPath;
        Adapter = adapter;
    }

    /// <summary>
    /// Gets the path to the Unix domain socket where clients should connect.
    /// </summary>
    public string SocketPath { get; }

    /// <summary>
    /// Gets the underlying UDS workload adapter.
    /// </summary>
    internal UdsWorkloadAdapter Adapter { get; }
}
