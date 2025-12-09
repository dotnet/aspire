// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Monitors the auxiliary backchannel directory and maintains connections to running AppHost instances.
/// </summary>
internal interface IAuxiliaryBackchannelMonitor
{
    /// <summary>
    /// Gets the collection of active AppHost connections.
    /// </summary>
    IReadOnlyDictionary<string, AppHostAuxiliaryBackchannel> Connections { get; }

    /// <summary>
    /// Gets or sets the path to the selected AppHost. When set, this AppHost will be used for MCP operations.
    /// </summary>
    string? SelectedAppHostPath { get; set; }

    /// <summary>
    /// Gets the currently selected AppHost connection based on the selection logic.
    /// Returns the explicitly selected AppHost, or the single in-scope AppHost, or null if none available.
    /// </summary>
    AppHostAuxiliaryBackchannel? SelectedConnection { get; }

    /// <summary>
    /// Gets all connections that are within the scope of the specified working directory.
    /// </summary>
    /// <param name="workingDirectory">The working directory to check against.</param>
    /// <returns>A list of in-scope connections.</returns>
    IReadOnlyList<AppHostAuxiliaryBackchannel> GetConnectionsForWorkingDirectory(DirectoryInfo workingDirectory);

    /// <summary>
    /// Event raised when the selected AppHost changes.
    /// </summary>
    event Action? SelectedAppHostChanged;
}
