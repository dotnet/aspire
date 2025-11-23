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
}
