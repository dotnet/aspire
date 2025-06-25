// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

namespace Aspire.Hosting.Backchannel;

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
/// Envelope for publishing activities sent over the backchannel.
/// </summary>
internal sealed class PublishingActivity
{
    /// <summary>
    /// Gets the type discriminator for the publishing activity.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the data containing all properties for the publishing activity.
    /// </summary>
    public required PublishingActivityData Data { get; init; }
}

/// <summary>
/// Common data for all publishing activities.
/// </summary>
internal sealed class PublishingActivityData
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
    /// Gets the completion state of the publishing activity.
    /// </summary>
    public required Publishing.CompletionState CompletionState { get; init; }

    /// <summary>
    /// Gets the identifier of the step this task belongs to (only applicable for tasks).
    /// </summary>
    public string? StepId { get; init; }

    /// <summary>
    /// Gets the optional completion message for tasks (appears as dimmed child text).
    /// </summary>
    public string? CompletionMessage { get; init; }
}

/// <summary>
/// Constants for publishing activity types.
/// </summary>
internal static class PublishingActivityTypes
{
    public const string Step = "step";
    public const string Task = "task";
    public const string PublishComplete = "publish-complete";
}
