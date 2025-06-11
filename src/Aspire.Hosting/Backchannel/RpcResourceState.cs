// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// Represents the state of a resource reported via RPC.
/// </summary>
internal class RpcResourceState
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the type of the resource.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the state of the resource.
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Gets the endpoints associated with the resource.
    /// </summary>
    public required string[] Endpoints { get; init; }

    /// <summary>
    /// Gets the health status of the resource.
    /// </summary>
    public string? Health { get; init; }
}

internal class DashboardUrls
{
    public required string BaseUrlWithLoginToken { get; init; }
    public string? CodespacesUrlWithLoginToken { get; init; }
}

internal class PublishingActivity
{
    public required string Id { get; init; }
    public required string StatusText { get; init; }
    public bool IsComplete { get; init; }
    public bool IsError { get; init; }
}
