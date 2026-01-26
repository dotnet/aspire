// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Assistant;

/// <summary>
/// Represents a connection to an AI agent that can process user messages.
/// The agent owns the processing loop and communicates via events.
/// </summary>
public interface IAgentConnection : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this connection/session.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Send a message to the agent. Responses are delivered via events.
    /// </summary>
    /// <param name="prompt">The user's message/prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the message has been sent (not when processing is complete).</returns>
    Task SendAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abort the current operation.
    /// </summary>
    /// <returns>A task that completes when the abort has been requested.</returns>
    Task AbortAsync();

    /// <summary>
    /// Subscribe to agent events (streaming text, tool calls, completion, errors).
    /// </summary>
    /// <param name="handler">The event handler to invoke for each event.</param>
    /// <returns>A disposable that unsubscribes when disposed.</returns>
    IDisposable OnEvent(Action<AgentEvent> handler);
}
