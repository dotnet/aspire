// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agent;

/// <summary>
/// Represents a session with the Aspire agent.
/// </summary>
internal interface IAgentSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the current project context for the session.
    /// </summary>
    AgentContext Context { get; }

    /// <summary>
    /// Gets whether the session is connected and ready.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Initializes the agent session with optional AppHost context.
    /// </summary>
    /// <param name="appHostProject">The AppHost project file, or null for offline mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(FileInfo? appHostProject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the agent and streams the response.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="onEvent">Callback for each event received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMessageAsync(string prompt, Action<AgentEvent> onEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts the current message processing.
    /// </summary>
    Task AbortAsync();
}

/// <summary>
/// Exception thrown when the agent session encounters an error.
/// </summary>
internal sealed class AgentSessionException : Exception
{
    public AgentSessionException(string message) : base(message) { }
    public AgentSessionException(string message, Exception innerException) : base(message, innerException) { }
}
