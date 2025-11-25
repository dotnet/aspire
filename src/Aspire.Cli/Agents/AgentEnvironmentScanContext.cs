// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Context passed to agent environment scanners to collect detected applicators.
/// </summary>
internal sealed class AgentEnvironmentScanContext
{
    private readonly List<AgentEnvironmentApplicator> _applicators = [];

    /// <summary>
    /// Gets the working directory being scanned.
    /// </summary>
    public required DirectoryInfo WorkingDirectory { get; init; }

    /// <summary>
    /// Adds an applicator to the collection of detected agent environments.
    /// </summary>
    /// <param name="applicator">The applicator to add.</param>
    public void AddApplicator(AgentEnvironmentApplicator applicator)
    {
        ArgumentNullException.ThrowIfNull(applicator);
        _applicators.Add(applicator);
    }

    /// <summary>
    /// Gets the collection of detected applicators.
    /// </summary>
    public IReadOnlyList<AgentEnvironmentApplicator> Applicators => _applicators;
}
