// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agent;

/// <summary>
/// Represents an event from the agent during message processing.
/// </summary>
internal abstract record AgentEvent;

/// <summary>
/// Event indicating the assistant is starting to process.
/// </summary>
internal sealed record AssistantTurnStartEvent : AgentEvent;

/// <summary>
/// Event containing a streaming text delta from the assistant.
/// </summary>
internal sealed record AssistantMessageDeltaEvent(string Content) : AgentEvent;

/// <summary>
/// Event containing the complete assistant message.
/// </summary>
internal sealed record AssistantMessageEvent(string Content) : AgentEvent;

/// <summary>
/// Event indicating a tool is starting execution.
/// </summary>
internal sealed record ToolExecutionStartEvent(string ToolName, string? Arguments) : AgentEvent;

/// <summary>
/// Event indicating a tool has completed execution.
/// </summary>
internal sealed record ToolExecutionCompleteEvent(string ToolName, string? Result, bool Success) : AgentEvent;

/// <summary>
/// Event indicating the session is idle (processing complete).
/// </summary>
internal sealed record SessionIdleEvent : AgentEvent;

/// <summary>
/// Event indicating an error occurred.
/// </summary>
internal sealed record SessionErrorEvent(string Message) : AgentEvent;
