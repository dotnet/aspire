// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Well-known skill identifiers for built-in skills.
/// </summary>
internal static class KnownSkills
{
    /// <summary>
    /// Main persona skill with Aspire expertise and MCP tool knowledge.
    /// </summary>
    public const string AspirePairProgrammer = "aspire-pair-programmer";

    /// <summary>
    /// Comprehensive troubleshooting workflow skill.
    /// </summary>
    public const string TroubleshootApp = "troubleshoot-app";

    /// <summary>
    /// Resource-specific debugging workflow skill.
    /// </summary>
    public const string DebugResource = "debug-resource";

    /// <summary>
    /// Integration addition guidance skill.
    /// </summary>
    public const string AddIntegration = "add-integration";

    /// <summary>
    /// Deployment workflow skill.
    /// </summary>
    public const string DeployApp = "deploy-app";
}
