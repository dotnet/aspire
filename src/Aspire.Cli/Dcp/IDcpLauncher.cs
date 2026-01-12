// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;

namespace Aspire.Cli.Dcp;

/// <summary>
/// Launches DCP instances owned by the CLI.
/// </summary>
internal interface IDcpLauncher
{
    /// <summary>
    /// Launches a DCP instance that monitors the CLI process.
    /// </summary>
    /// <param name="appHostInfo">Information about the AppHost including DCP paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The DCP session containing paths to kubeconfig and logs.</returns>
    Task<DcpSession> LaunchAsync(AppHostInfo appHostInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the DCP instance if it's still running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken);
}
