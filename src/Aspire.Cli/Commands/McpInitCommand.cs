// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Agents;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command that initializes MCP server configuration for detected agent environments.
/// </summary>
internal sealed class McpInitCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IAgentEnvironmentDetector _agentEnvironmentDetector;
    private readonly IAgentFingerprintService _agentFingerprintService;

    public McpInitCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAgentEnvironmentDetector agentEnvironmentDetector,
        IAgentFingerprintService agentFingerprintService)
        : base("init", McpCommandStrings.InitCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(agentEnvironmentDetector);
        ArgumentNullException.ThrowIfNull(agentFingerprintService);

        _interactionService = interactionService;
        _agentEnvironmentDetector = agentEnvironmentDetector;
        _agentFingerprintService = agentFingerprintService;
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var allApplicators = await _agentEnvironmentDetector.DetectAsync(ExecutionContext.WorkingDirectory, cancellationToken);
        var applicators = await _agentFingerprintService.FilterAcknowledgedAsync(allApplicators, cancellationToken);

        if (applicators.Length == 0)
        {
            _interactionService.DisplaySubtleMessage(McpCommandStrings.InitCommand_NoAgentEnvironmentsDetected);
            return ExitCodeConstants.Success;
        }

        // Ask the user if they want to configure agent environments now
        var promptMessage = string.Format(CultureInfo.CurrentCulture, McpCommandStrings.InitCommand_AgentConfigurationPrompt, applicators.Length);
        var configureChoice = await _interactionService.PromptForSelectionAsync(
            promptMessage,
            [McpCommandStrings.InitCommand_AgentConfigurationYes, McpCommandStrings.InitCommand_AgentConfigurationNo, McpCommandStrings.InitCommand_AgentConfigurationMaybeLater],
            choice => choice,
            cancellationToken);

        if (string.Equals(configureChoice, McpCommandStrings.InitCommand_AgentConfigurationNo, StringComparison.OrdinalIgnoreCase))
        {
            // User said "No" - record all as acknowledged so we don't ask again
            await _agentFingerprintService.RecordAcknowledgedAsync(applicators, cancellationToken);
            return ExitCodeConstants.Success;
        }

        if (string.Equals(configureChoice, McpCommandStrings.InitCommand_AgentConfigurationMaybeLater, StringComparison.OrdinalIgnoreCase))
        {
            // User said "Maybe later" - don't record, so we ask again next time
            return ExitCodeConstants.Success;
        }

        // User said "Yes" - show the selection prompt
        var selectedApplicators = await _interactionService.PromptForSelectionsAsync(
            McpCommandStrings.InitCommand_AgentConfigurationSelectPrompt,
            applicators,
            applicator => applicator.Description,
            cancellationToken);

        // Record all presented applicators as acknowledged (not just selected ones)
        // This prevents re-prompting for applicators the user chose not to configure
        await _agentFingerprintService.RecordAcknowledgedAsync(applicators, cancellationToken);

        foreach (var applicator in selectedApplicators)
        {
            await applicator.ApplyAsync(cancellationToken);
        }

        _interactionService.DisplaySuccess(McpCommandStrings.InitCommand_ConfigurationComplete);

        return ExitCodeConstants.Success;
    }
}
