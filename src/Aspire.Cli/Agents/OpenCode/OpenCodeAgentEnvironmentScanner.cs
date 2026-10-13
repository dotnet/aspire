// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Agents.OpenCode;

/// <summary>
/// Scans for project use or an installation of OpenCode.
/// </summary>
internal sealed class OpenCodeAgentEnvironmentScanner(
    IOpenCodeCliRunner openCodeCliRunner,
    ILogger<OpenCodeAgentEnvironmentScanner> logger) : IAgentEnvironmentScanner
{
    /// <inheritdoc />
    public async Task<AgentEnvironment> ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Starting OpenCode environment scan in directory: {WorkingDirectory}", context.WorkingDirectory.FullName);
        logger.LogDebug("Workspace root: {RepositoryRoot}", context.RepositoryRoot.FullName);

        // Look for existing opencode.jsonc file at workspace root
        var configFilePath = Path.Combine(context.RepositoryRoot.FullName, OpenCodePaths.McpConfigFileName);
        var configFileExists = File.Exists(configFilePath);

        if (configFileExists)
        {
            logger.LogDebug("Found existing opencode.jsonc at: {ConfigFilePath}", configFilePath);

            return AgentEnvironment.OpenCode;
        }

        logger.LogDebug("No opencode.jsonc found, checking for OpenCode CLI installation...");
        var openCodeVersion = await openCodeCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        if (openCodeVersion is not null)
        {
            logger.LogDebug("Found OpenCode CLI version: {Version}", openCodeVersion);
            return AgentEnvironment.OpenCode;
        }

        logger.LogDebug("OpenCode CLI not found - skipping");

        return AgentEnvironment.None;
    }
}
