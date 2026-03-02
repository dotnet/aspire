// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Agents;
using Aspire.Cli.Configuration;
using Aspire.Cli.Git;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command that initializes agent environment configuration for detected agents.
/// This is the new command under 'aspire agent init'.
/// </summary>
internal sealed class AgentInitCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IAgentEnvironmentDetector _agentEnvironmentDetector;
    private readonly IGitRepository _gitRepository;

    /// <summary>
    /// AgentInitCommand does not need template package metadata prefetching.
    /// </summary>
    public bool PrefetchesTemplatePackageMetadata => false;

    /// <summary>
    /// AgentInitCommand does not need CLI package metadata prefetching.
    /// </summary>
    public bool PrefetchesCliPackageMetadata => false;

    public AgentInitCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAgentEnvironmentDetector agentEnvironmentDetector,
        IGitRepository gitRepository,
        AspireCliTelemetry telemetry)
        : base("init", AgentCommandStrings.InitCommand_Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _agentEnvironmentDetector = agentEnvironmentDetector;
        _gitRepository = gitRepository;
    }

    protected override bool UpdateNotificationsEnabled => false;

    /// <summary>
    /// Public entry point for executing the init command.
    /// This allows McpInitCommand to delegate to this implementation.
    /// </summary>
    internal Task<int> ExecuteCommandAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return ExecuteAsync(parseResult, cancellationToken);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Try to discover the git repository root to use as the default workspace root
        var gitRoot = await _gitRepository.GetRootAsync(cancellationToken);
        var defaultWorkspaceRoot = gitRoot ?? ExecutionContext.WorkingDirectory;

        // Prompt the user for the workspace root
        var workspaceRootPath = await _interactionService.PromptForStringAsync(
            McpCommandStrings.InitCommand_WorkspaceRootPrompt,
            defaultValue: defaultWorkspaceRoot.FullName,
            validator: path =>
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return ValidationResult.Error(McpCommandStrings.InitCommand_WorkspaceRootRequired);
                }

                if (!Directory.Exists(path))
                {
                    return ValidationResult.Error(string.Format(CultureInfo.InvariantCulture, McpCommandStrings.InitCommand_WorkspaceRootNotFound, path));
                }

                return ValidationResult.Success();
            },
            cancellationToken: cancellationToken);

        var workspaceRoot = new DirectoryInfo(workspaceRootPath);

        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = ExecutionContext.WorkingDirectory,
            RepositoryRoot = workspaceRoot
        };

        var applicators = await _interactionService.ShowStatusAsync(
            McpCommandStrings.InitCommand_DetectingAgentEnvironments,
            async () => await _agentEnvironmentDetector.DetectAsync(context, cancellationToken));

        if (applicators.Length == 0)
        {
            _interactionService.DisplaySubtleMessage(McpCommandStrings.InitCommand_NoAgentEnvironmentsDetected);
            return ExitCodeConstants.Success;
        }

        // Group applicators by prompt group and sort by priority
        var groupedApplicators = applicators
            .GroupBy(a => a.PromptGroup)
            .OrderBy(g => g.Key.Priority)
            .ToList();

        var selectedApplicators = new List<AgentEnvironmentApplicator>();

        // Present each group of prompts in priority order
        foreach (var group in groupedApplicators)
        {
            // Get the prompt text for this group
            var promptText = group.Key.Name switch
            {
                "ConfigUpdates" => AgentCommandStrings.ConfigUpdatesSelectPrompt,
                "AgentEnvironments" => McpCommandStrings.InitCommand_AgentConfigurationSelectPrompt,
                "AdditionalOptions" => McpCommandStrings.InitCommand_AdditionalOptionsSelectPrompt,
                _ => $"Select {group.Key.Name}:"
            };

            // Sort applicators within the group by priority
            var sortedApplicators = group.OrderBy(a => a.Priority).ToArray();

            // Prompt user for selection from this group
            var selected = await _interactionService.PromptForSelectionsAsync(
                promptText,
                sortedApplicators,
                applicator => applicator.Description,
                cancellationToken);

            selectedApplicators.AddRange(selected);
        }

        // Apply all selected applicators
        var hasErrors = false;
        foreach (var applicator in selectedApplicators)
        {
            try
            {
                await applicator.ApplyAsync(cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _interactionService.DisplayError(ex.Message);
                if (ex.InnerException is JsonException)
                {
                    _interactionService.DisplaySubtleMessage(
                        string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.SkippedMalformedConfigFile, applicator.Description));
                }
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            _interactionService.DisplayMessage(KnownEmojis.Warning, AgentCommandStrings.ConfigurationCompletedWithErrors);
        }
        else
        {
            _interactionService.DisplaySuccess(McpCommandStrings.InitCommand_ConfigurationComplete);
        }

        return hasErrors ? ExitCodeConstants.InvalidCommand : ExitCodeConstants.Success;
    }
}
