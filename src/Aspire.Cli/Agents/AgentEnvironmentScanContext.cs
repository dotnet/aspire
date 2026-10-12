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
    /// Gets the root directory of the repository/workspace.
    /// This is typically the git repository root if available, otherwise the working directory.
    /// Scanners should use this as the boundary for searches instead of searching up the directory tree.
    /// </summary>
    public required DirectoryInfo RepositoryRoot { get; init; }

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

    /// <summary>
    /// Records a detected agent environment. Used to scope
    /// telemetry hook registration to the environments the user actually has, independent of whether the
    /// Aspire MCP server still needs configuring.
    /// </summary>
    /// <param name="environment">The detected agent environment.</param>
    public void AddDetectedEnvironment(AgentEnvironment environment)
    {
        DetectedEnvironments |= environment;
    }

    /// <summary>
    /// Gets the detected agent environments.
    /// </summary>
    public AgentEnvironment DetectedEnvironments { get; private set; }
}
