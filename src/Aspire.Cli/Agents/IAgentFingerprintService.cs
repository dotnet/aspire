// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Service for tracking acknowledged agent environment applicators.
/// Stores fingerprints in global configuration to prevent re-prompting users.
/// </summary>
internal interface IAgentFingerprintService
{
    /// <summary>
    /// Checks if an applicator has been previously acknowledged by the user.
    /// </summary>
    /// <param name="applicator">The applicator to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the applicator has been acknowledged, false otherwise.</returns>
    Task<bool> IsAcknowledgedAsync(AgentEnvironmentApplicator applicator, CancellationToken cancellationToken);

    /// <summary>
    /// Filters out applicators that have already been acknowledged by the user.
    /// </summary>
    /// <param name="applicators">The applicators to filter.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Applicators that have not been acknowledged.</returns>
    Task<AgentEnvironmentApplicator[]> FilterAcknowledgedAsync(IEnumerable<AgentEnvironmentApplicator> applicators, CancellationToken cancellationToken);

    /// <summary>
    /// Records that the user has acknowledged the specified applicators.
    /// This prevents them from being shown again on subsequent runs.
    /// </summary>
    /// <param name="applicators">The applicators that were presented to the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RecordAcknowledgedAsync(IEnumerable<AgentEnvironmentApplicator> applicators, CancellationToken cancellationToken);
}
