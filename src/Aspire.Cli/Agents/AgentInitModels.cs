// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Identifies a configuration or payload managed during agent setup.
/// </summary>
internal enum AgentAssetKind
{
    Mcp,
    Playwright,
    DotnetInspect,
    AspireSkills,
    BundledAspireSkills,
    TelemetryHooks
}

internal enum AspireSkillsMode
{
    Bundled,
    Native,
    None
}

/// <summary>
/// The independent asset choices made after selecting agents and before scope.
/// </summary>
internal sealed record AgentAssetSelection(bool Mcp, bool Playwright, bool DotnetInspect, AspireSkillsMode SkillsMode)
{
    public bool AspireSkills => SkillsMode is AspireSkillsMode.Native;
    public bool BundledSkills => SkillsMode is AspireSkillsMode.Bundled;
    public bool HasAssets => Mcp || Playwright || DotnetInspect || SkillsMode is not AspireSkillsMode.None;
}

/// <summary>
/// Selected agents and scope, with no workspace required for global configuration.
/// </summary>
internal sealed record AgentInitRequest(
    DirectoryInfo? WorkspaceRoot,
    AgentAssetSelection Assets,
    AgentConfigurationScope Scope,
    IReadOnlyList<IAgentEnvironmentScanner> Environments,
    IReadOnlyList<AgentClientDetection> Detections);

internal sealed record AgentDiscovery(
    IReadOnlyList<IAgentEnvironmentScanner> Environments,
    IReadOnlyList<AgentClientDetection> Detections)
{
    public AgentEnvironment DetectedEnvironments => Detections.Aggregate(
        AgentEnvironment.None, static (environments, detection) => environments | detection.Client);
}

internal enum AgentConfirmation
{
    Yes,
    No
}

internal enum AgentConfigurationStatus
{
    Configured,
    Unchanged,
    Skipped,
    Blocked,
    Failed
}

/// <summary>
/// Describes the outcome at one native configuration or skill destination.
/// </summary>
internal sealed record AgentTargetResult(
    AgentAssetKind Asset,
    IReadOnlyList<IAgentEnvironmentScanner> Environments,
    string TargetPath,
    AgentConfigurationScope Scope,
    AgentConfigurationStatus Status,
    string? Message);

/// <summary>
/// Aggregates configuration outcomes without claiming client-owned content was acquired.
/// </summary>
internal sealed record AgentInitResult(IReadOnlyList<AgentTargetResult> Targets)
{
    public bool HasErrors => Targets.Any(static target =>
        target.Asset is not AgentAssetKind.TelemetryHooks &&
        target.Status is AgentConfigurationStatus.Blocked or AgentConfigurationStatus.Failed);

    public bool HasWarnings => Targets.Any(static target =>
        target.Status is AgentConfigurationStatus.Skipped or AgentConfigurationStatus.Blocked or AgentConfigurationStatus.Failed);

    public IReadOnlyList<IAgentEnvironmentScanner> RegisteredEnvironments => Targets
        .Where(static target => target.Asset is AgentAssetKind.AspireSkills or AgentAssetKind.BundledAspireSkills &&
            target.Status is AgentConfigurationStatus.Configured or AgentConfigurationStatus.Unchanged)
        .SelectMany(static target => target.Environments)
        .Distinct()
        .ToArray();
}
