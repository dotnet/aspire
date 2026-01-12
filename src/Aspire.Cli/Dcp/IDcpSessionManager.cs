// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Dcp;

/// <summary>
/// Manages DCP session directories for CLI-owned DCP instances.
/// </summary>
internal interface IDcpSessionManager
{
    /// <summary>
    /// Creates a new DCP session with a temporary directory for kubeconfig and logs.
    /// </summary>
    /// <returns>A new DCP session.</returns>
    DcpSession CreateSession();
}
