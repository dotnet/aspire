// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Aspire.Dashboard.Model.Assistant;

/// <summary>
/// Configuration for creating an agent connection.
/// </summary>
public sealed record AgentConnectionConfig
{
    /// <summary>
    /// The model to use for the agent (e.g., "gpt-4.1", "gpt-4o", "claude-sonnet-4.5").
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The tools available to the agent.
    /// </summary>
    public IReadOnlyList<AIFunction>? Tools { get; init; }

    /// <summary>
    /// Optional system message to configure agent behavior.
    /// </summary>
    public string? SystemMessage { get; init; }

    /// <summary>
    /// Whether to enable streaming responses. Defaults to true.
    /// </summary>
    public bool Streaming { get; init; } = true;
}
