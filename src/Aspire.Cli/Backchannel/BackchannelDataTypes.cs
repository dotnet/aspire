// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Represents the state of a resource reported via RPC.
/// </summary>
internal sealed class RpcResourceState
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

/// <summary>
/// Represents dashboard URLs with authentication tokens.
/// </summary>
internal sealed class DashboardUrlsState
{
    /// <summary>
    /// Gets the base dashboard URL with a login token.
    /// </summary>
    public required string BaseUrlWithLoginToken { get; init; }

    /// <summary>
    /// Gets the Codespaces dashboard URL with a login token, if available.
    /// </summary>
    public string? CodespacesUrlWithLoginToken { get; init; }
}

/// <summary>
/// Represents the activity and status of a publishing operation.
/// </summary>
internal sealed class PublishingActivityState
{
    /// <summary>
    /// Gets the unique identifier for the publishing activity.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the status text describing the publishing activity.
    /// </summary>
    public required string StatusText { get; init; }

    /// <summary>
    /// Gets a value indicating whether the publishing activity is complete.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Gets a value indicating whether the publishing activity encountered an error.
    /// </summary>
    public bool IsError { get; init; }
}

internal sealed class CommandOutput
{
    public required string Text { get; init; }
    public required LogLevel LogLevel { get; init; }
}
