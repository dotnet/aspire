// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Assistant;

/// <summary>
/// Factory for creating agent connections.
/// </summary>
public interface IAgentConnectionFactory
{
    /// <summary>
    /// Checks whether the agent backend is available and ready to accept connections.
    /// This may involve checking for CLI availability, network connectivity, etc.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the agent is available; otherwise false.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new agent connection with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration for the connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new agent connection.</returns>
    Task<IAgentConnection> CreateConnectionAsync(AgentConnectionConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the available models for the agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available model names.</returns>
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
