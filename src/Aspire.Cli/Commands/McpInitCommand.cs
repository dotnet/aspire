// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Agents;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command that initializes MCP server configuration for detected agent environments.
/// </summary>
internal sealed class McpInitCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IAgentEnvironmentDetector _agentEnvironmentDetector;

    /// <summary>
    /// McpInitCommand does not need template package metadata prefetching.
    /// </summary>
    public bool PrefetchesTemplatePackageMetadata => false;

    /// <summary>
    /// McpInitCommand does not need CLI package metadata prefetching.
    /// </summary>
    public bool PrefetchesCliPackageMetadata => false;

    public McpInitCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAgentEnvironmentDetector agentEnvironmentDetector)
        : base("init", McpCommandStrings.InitCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(agentEnvironmentDetector);

        _interactionService = interactionService;
        _agentEnvironmentDetector = agentEnvironmentDetector;
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var applicators = await _interactionService.ShowStatusAsync(
            McpCommandStrings.InitCommand_DetectingAgentEnvironments,
            async () => await _agentEnvironmentDetector.DetectAsync(ExecutionContext.WorkingDirectory, cancellationToken));

        if (applicators.Length == 0)
        {
            _interactionService.DisplaySubtleMessage(McpCommandStrings.InitCommand_NoAgentEnvironmentsDetected);
            return ExitCodeConstants.Success;
        }

        // Present the list of detected agent environments for the user to select
        var selectedApplicators = await _interactionService.PromptForSelectionsAsync(
            McpCommandStrings.InitCommand_AgentConfigurationSelectPrompt,
            applicators,
            applicator => applicator.Description,
            cancellationToken);

        foreach (var applicator in selectedApplicators)
        {
            await applicator.ApplyAsync(cancellationToken);
        }

        _interactionService.DisplaySuccess(McpCommandStrings.InitCommand_ConfigurationComplete);

        return ExitCodeConstants.Success;
    }
}
