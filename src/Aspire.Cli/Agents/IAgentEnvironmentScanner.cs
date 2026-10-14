// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents.Hooks;
using Semver;

namespace Aspire.Cli.Agents;

/// <summary>
/// Owns discovery and native configuration for an agent without writing during discovery.
/// </summary>
internal interface IAgentEnvironmentScanner
{
    /// <summary>
    /// Gets the catalog client served by this native handler.
    /// </summary>
    AgentClient Client { get; }

    /// <summary>
    /// Gets the display name shared by selection and configuration results.
    /// </summary>
    string DisplayName => Client.DisplayName;

    /// <summary>
    /// Returns immutable, versioned client evidence without changing files or configuration.
    /// </summary>
    /// <param name="context">The read-only workspace boundary.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<AgentClientDetection>> ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Gets edits in the selected scope, even when no client was detected.
    /// </summary>
    IEnumerable<AgentConfigurationTarget> GetTargets(AgentInitRequest request);

    Task<IReadOnlyList<AgentConfigurationTarget>> GetTargetsAsync(AgentInitRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AgentConfigurationTarget>>(GetTargets(request).ToArray());
    }

    /// <summary>
    /// Gets user-hook configuration for detected supported clients, independently of selection.
    /// </summary>
    AgentHookConfiguration? GetHookConfiguration(AgentInitRequest request) => null;
}

/// <summary>
/// Read-only evidence that a client is present.
/// </summary>
internal sealed record AgentClientDetection(AgentEnvironment Client, SemVersion? Version);
