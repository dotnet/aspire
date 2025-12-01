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
    IReadOnlyDictionary<string, AppHostConnection> Connections { get; }

    /// <summary>
    /// Gets or sets the path to the selected AppHost. When set, this AppHost will be used for MCP operations.
    /// </summary>
    string? SelectedAppHostPath { get; set; }

    /// <summary>
    /// Event raised when the selected AppHost changes.
    /// </summary>
    event Action? SelectedAppHostChanged;
}
