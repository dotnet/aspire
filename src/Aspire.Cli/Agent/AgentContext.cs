// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agent;

/// <summary>
/// Holds the current project context for the agent session.
/// </summary>
internal sealed class AgentContext
{
    /// <summary>
    /// Gets the AppHost project file, if available.
    /// </summary>
    public FileInfo? AppHostProject { get; init; }

    /// <summary>
    /// Gets whether we're running in offline mode (no AppHost).
    /// </summary>
    public bool IsOfflineMode => AppHostProject is null;

    /// <summary>
    /// Gets the working directory for the session.
    /// </summary>
    public DirectoryInfo WorkingDirectory { get; init; } = new(Environment.CurrentDirectory);

    /// <summary>
    /// Gets the list of resources discovered from the AppHost.
    /// </summary>
    public List<DiscoveredResource> Resources { get; init; } = [];

    /// <summary>
    /// Gets a summary of the project context for injection into the system prompt.
    /// </summary>
    public string GetContextSummary()
    {
        if (IsOfflineMode)
        {
            return $"""
                Working directory: {WorkingDirectory.FullName}
                Mode: Offline (no AppHost detected)
                Available actions: Create new projects, run diagnostics
                """;
        }

        var resourceSummary = Resources.Count > 0
            ? string.Join("\n", Resources.Select(r => $"  - {r.Name} ({r.Type}): {r.State}"))
            : "  (no resources discovered yet)";

        return $"""
            Working directory: {WorkingDirectory.FullName}
            AppHost: {AppHostProject!.FullName}
            Mode: Online (AppHost available)
            Resources:
            {resourceSummary}
            """;
    }
}

/// <summary>
/// Represents a resource discovered from the AppHost.
/// </summary>
internal sealed record DiscoveredResource(
    string Name,
    string Type,
    string State,
    IReadOnlyList<string> Endpoints);
