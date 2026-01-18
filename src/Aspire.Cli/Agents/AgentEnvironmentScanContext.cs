// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Context passed to agent environment scanners to collect detected applicators.
/// </summary>
internal sealed class AgentEnvironmentScanContext
{
    private readonly List<AgentEnvironmentApplicator> _applicators = [];
    private readonly HashSet<string> _skillFileApplicatorPaths = new(StringComparer.OrdinalIgnoreCase);

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
    /// Gets or sets a value indicating whether a Playwright applicator has been added.
    /// This is used to ensure only one applicator for Playwright is added across all scanners.
    /// </summary>
    public bool PlaywrightApplicatorAdded { get; set; }

    /// <summary>
    /// Stores the Playwright configuration callbacks from each scanner.
    /// These will be executed if the user selects to configure Playwright.
    /// </summary>
    private readonly List<Func<CancellationToken, Task>> _playwrightConfigurationCallbacks = [];

    /// <summary>
    /// Adds a Playwright configuration callback for a specific environment.
    /// </summary>
    /// <param name="callback">The callback to execute if Playwright is configured.</param>
    public void AddPlaywrightConfigurationCallback(Func<CancellationToken, Task> callback)
    {
        _playwrightConfigurationCallbacks.Add(callback);
    }

    /// <summary>
    /// Gets all registered Playwright configuration callbacks.
    /// </summary>
    public IReadOnlyList<Func<CancellationToken, Task>> PlaywrightConfigurationCallbacks => _playwrightConfigurationCallbacks;

    /// <summary>
    /// Checks if a skill file applicator has already been added for the specified path.
    /// </summary>
    /// <param name="skillRelativePath">The relative path to the skill file.</param>
    /// <returns>True if an applicator has already been added for this path.</returns>
    public bool HasSkillFileApplicator(string skillRelativePath)
    {
        return _skillFileApplicatorPaths.Contains(skillRelativePath);
    }

    /// <summary>
    /// Marks a skill file path as having an applicator added.
    /// </summary>
    /// <param name="skillRelativePath">The relative path to the skill file.</param>
    public void MarkSkillFileApplicatorAdded(string skillRelativePath)
    {
        _skillFileApplicatorPaths.Add(skillRelativePath);
    }

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
