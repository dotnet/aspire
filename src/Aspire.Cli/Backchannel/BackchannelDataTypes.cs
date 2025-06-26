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
/// Represents a single line of output to be displayed, along with its associated stream (such as "stdout" or "stderr").
/// </summary>
internal sealed class DisplayLineState(string stream, string line)
{
    /// <summary>
    /// Gets the name of the stream the line belongs to (e.g., "stdout", "stderr").
    /// </summary>
    public string Stream { get; } = stream;

    /// <summary>
    /// Gets the content of the line to be displayed.
    /// </summary>
    public string Line { get; } = line;
}

/// <summary>
/// Envelope for publishing activity items sent over the backchannel.
/// </summary>
internal sealed class PublishingActivity
{
    /// <summary>
    /// Gets the type discriminator for the publishing activity item.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the data containing all properties for the publishing activity item.
    /// </summary>
    public required PublishingActivityData Data { get; init; }
}

/// <summary>
/// Common data for all publishing activity items.
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
    public string CompletionState { get; init; } = CompletionStates.InProgress;

    /// <summary>
    /// Gets a value indicating whether the publishing activity is complete.
    /// </summary>
    public bool IsComplete => CompletionState is not CompletionStates.InProgress;

    /// <summary>
    /// Gets a value indicating whether the publishing activity encountered an error.
    /// </summary>
    public bool IsError => CompletionState is CompletionStates.CompletedWithError;

    /// <summary>
    /// Gets a value indicating whether the publishing activity completed with warnings.
    /// </summary>
    public bool IsWarning => CompletionState is CompletionStates.CompletedWithWarning;

    /// <summary>
    /// Gets the identifier of the step this task belongs to (only applicable for tasks).
    /// </summary>
    public string? StepId { get; init; }

    /// <summary>
    /// Gets the completion message for the publishing activity (optional).
    /// </summary>
    public string? CompletionMessage { get; init; }
}

/// <summary>
/// Constants for publishing activity item types.
/// </summary>
internal static class PublishingActivityTypes
{
    public const string Step = "step";
    public const string Task = "task";
    public const string PublishComplete = "publish-complete";
}

internal class BackchannelLogEntry
{
    public required EventId EventId { get; set; }
    public required LogLevel LogLevel { get; set; }
    public required string Message { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string CategoryName { get; set; }
}

/// <summary>
/// Constants for completion state values.
/// </summary>
internal static class CompletionStates
{
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string CompletedWithWarning = "CompletedWithWarning";
    public const string CompletedWithError = "CompletedWithError";
}
