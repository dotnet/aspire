// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Interface for detecting agent environments in the current context.
/// </summary>
internal interface IAgentEnvironmentDetector
{
    /// <summary>
    /// Detects available agent environments by running all registered scanners.
    /// </summary>
    /// <param name="workingDirectory">The working directory to scan.</param>
    /// <param name="repositoryRoot">The root directory of the repository/workspace. Scanners use this as the boundary for searches.</param>
    /// <param name="createAgentInstructions">Whether to create agent-specific instruction files.</param>
    /// <param name="configurePlaywrightMcpServer">Whether to pre-configure the Playwright MCP server.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An array of applicators for detected agent environments.</returns>
    Task<AgentEnvironmentApplicator[]> DetectAsync(
        DirectoryInfo workingDirectory,
        DirectoryInfo repositoryRoot,
        bool createAgentInstructions,
        bool configurePlaywrightMcpServer,
        CancellationToken cancellationToken);
}
