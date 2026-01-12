// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Dcp;

/// <summary>
/// Creates DCP sessions with temporary directories for kubeconfig and log socket.
/// </summary>
internal sealed class DcpSessionManager : IDcpSessionManager
{
    /// <inheritdoc />
    public DcpSession CreateSession()
    {
        // Create a temporary directory for this DCP session
        var sessionDir = Path.Combine(Path.GetTempPath(), $"aspire-dcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sessionDir);

        var kubeconfigPath = Path.Combine(sessionDir, "kubeconfig");
        var logSocketPath = Path.Combine(sessionDir, "output.sock");

        return new DcpSession
        {
            SessionDir = sessionDir,
            KubeconfigPath = kubeconfigPath,
            LogSocketPath = logSocketPath
        };
    }
}
