// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Assistant;

/// <summary>
/// Base class for all agent events.
/// </summary>
public abstract record AgentEvent
{
    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A streaming text chunk from the agent.
/// </summary>
public sealed record AgentTextDeltaEvent : AgentEvent
{
    /// <summary>
    /// The text chunk content.
    /// </summary>
    public required string DeltaContent { get; init; }
}

/// <summary>
/// The final complete text response from the agent.
/// </summary>
public sealed record AgentTextCompleteEvent : AgentEvent
{
    /// <summary>
    /// The complete text content.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// A tool execution has started.
/// </summary>
public sealed record AgentToolStartEvent : AgentEvent
{
    /// <summary>
    /// The name of the tool being executed.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Optional description of what the tool is doing.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// A tool execution has completed.
/// </summary>
public sealed record AgentToolCompleteEvent : AgentEvent
{
    /// <summary>
    /// The name of the tool that completed.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Whether the tool execution was successful.
    /// </summary>
    public bool Success { get; init; } = true;
}

/// <summary>
/// The agent has finished processing and is idle.
/// </summary>
public sealed record AgentIdleEvent : AgentEvent;

/// <summary>
/// An error occurred during agent processing.
/// </summary>
public sealed record AgentErrorEvent : AgentEvent
{
    /// <summary>
    /// The error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The exception that caused the error, if available.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Whether the error is recoverable.
    /// </summary>
    public bool IsRecoverable { get; init; }
}

/// <summary>
/// Reasoning/thinking content from the agent (for models that support it).
/// </summary>
public sealed record AgentReasoningDeltaEvent : AgentEvent
{
    /// <summary>
    /// The reasoning text chunk.
    /// </summary>
    public required string DeltaContent { get; init; }
}

/// <summary>
/// The final complete reasoning content from the agent.
/// </summary>
public sealed record AgentReasoningCompleteEvent : AgentEvent
{
    /// <summary>
    /// The complete reasoning content.
    /// </summary>
    public required string Content { get; init; }
}
