// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Dcp;

/// <summary>
/// Represents a DCP session managed by the CLI.
/// Contains paths to session resources and handles cleanup on disposal.
/// </summary>
internal sealed class DcpSession : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// The session directory containing kubeconfig and log socket.
    /// </summary>
    public required string SessionDir { get; init; }

    /// <summary>
    /// Path to the kubeconfig file used by the DCP instance.
    /// </summary>
    public required string KubeconfigPath { get; init; }

    /// <summary>
    /// Path to the Unix domain socket for DCP log streaming.
    /// </summary>
    public required string LogSocketPath { get; init; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Don't delete session directory here - DCP will clean it up via DCP_SESSION_FOLDER
        // when it exits (it's monitoring our process via --monitor flag)
    }
}
