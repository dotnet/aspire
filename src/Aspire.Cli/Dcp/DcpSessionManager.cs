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
        // Use CreateTempSubdirectory which generates shorter random suffixes than GUIDs
        // This is important because Unix domain socket paths have a max length of 104 chars on macOS
        var tempDir = Directory.CreateTempSubdirectory("aspire.");
        var sessionDir = tempDir.FullName;

        var kubeconfigPath = Path.Combine(sessionDir, "kubeconfig");
        var logSocketPath = Path.Combine(sessionDir, "log.sock");

        return new DcpSession
        {
            SessionDir = sessionDir,
            KubeconfigPath = kubeconfigPath,
            LogSocketPath = logSocketPath
        };
    }
}
