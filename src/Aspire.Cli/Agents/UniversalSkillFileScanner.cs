// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Agents;

/// <summary>
/// Scanner that always offers to create the universal skill file at .agents/skills/aspire/SKILL.md.
/// This follows the agent skills convention (https://agentskills.io) and is offered regardless of
/// which specific coding agents are detected.
/// </summary>
internal sealed class UniversalSkillFileScanner : IAgentEnvironmentScanner
{
    private static readonly string s_skillFilePath = Path.Combine(".agents", "skills", CommonAgentApplicators.AspireSkillName, "SKILL.md");
    private const string SkillFileDescription = "Create Aspire skill file (.agents/skills/aspire/SKILL.md)";

    private readonly ILogger<UniversalSkillFileScanner> _logger;

    public UniversalSkillFileScanner(ILogger<UniversalSkillFileScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Adding universal skill file applicator for .agents/skills/aspire/SKILL.md");

        // Always offer the universal skill file location
        CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            context.RepositoryRoot,
            s_skillFilePath,
            SkillFileDescription);

        return Task.CompletedTask;
    }
}
