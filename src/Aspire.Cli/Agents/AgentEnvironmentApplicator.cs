// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Represents an agent environment that was detected and can be configured.
/// </summary>
internal sealed class AgentEnvironmentApplicator
{
    /// <summary>
    /// Gets the description of the agent environment shown in the selection prompt.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the unique fingerprint for this applicator.
    /// Used to store user preferences about whether they've declined to enable this environment.
    /// </summary>
    public required string Fingerprint { get; init; }

    /// <summary>
    /// Gets the callback that applies the configuration for this agent environment.
    /// </summary>
    public required Func<CancellationToken, Task> ApplyCallback { get; init; }

    /// <summary>
    /// Applies the configuration changes to enable the agent environment.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        await ApplyCallback(cancellationToken);
    }
}
