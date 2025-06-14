// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Represents information about a resource for RPC communication.
/// </summary>
internal class RpcResourceInfo
{
    /// <summary>
    /// Gets the resource ID (resolved name for DCP resources).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the resource name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the resource type.
    /// </summary>
    public required string Type { get; init; }
}