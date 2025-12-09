// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Interface for communicating with an AppHost via the auxiliary backchannel.
/// </summary>
internal interface IAuxiliaryBackchannel
{
    /// <summary>
    /// Gets information about the AppHost.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The AppHost information including the fully qualified path and process ID.</returns>
    Task<AppHostInformation?> GetAppHostInformationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Dashboard MCP connection information including endpoint URL and API token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The MCP connection information, or null if the dashboard is not part of the application model.</returns>
    Task<DashboardMcpConnectionInfo?> GetDashboardMcpConnectionInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the test results by waiting for all test resources to complete.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The test results.</returns>
    Task<TestResults?> GetTestResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates an orderly shutdown of the AppHost.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the shutdown request has been sent.</returns>
    Task StopAppHostAsync(CancellationToken cancellationToken = default);
}
