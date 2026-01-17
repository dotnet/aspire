// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Agent;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Tui;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Starts the Aspire agent for interactive AI-powered development.
/// </summary>
internal sealed class AgentStartCommand : BaseCommand
{
    private readonly IAgentSession _agentSession;
    private readonly IAgentTuiRenderer _tuiRenderer;
    private readonly IProjectLocator _projectLocator;

    public AgentStartCommand(
        IAgentSession agentSession,
        IAgentTuiRenderer tuiRenderer,
        IProjectLocator projectLocator,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService)
        : base("start", "Start the Aspire agent", features, updateNotifier, executionContext, interactionService)
    {
        _agentSession = agentSession;
        _tuiRenderer = tuiRenderer;
        _projectLocator = projectLocator;

        var projectOption = new Option<FileInfo?>("--project", "-p")
        {
            Description = "The path to the AppHost project file"
        };

        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var projectFile = parseResult.GetValue<FileInfo?>("--project");

        try
        {
            // Try to locate AppHost project for context
            FileInfo? appHostProject = null;
            try
            {
                appHostProject = projectFile ?? await _projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: false, cancellationToken);
            }
            catch (ProjectLocatorException)
            {
                // No AppHost found - that's OK, we can still run in offline mode
            }

            // Initialize the agent session
            await _agentSession.InitializeAsync(appHostProject, cancellationToken);

            // Render the TUI and run the agent loop
            await _tuiRenderer.RunAsync(_agentSession, cancellationToken);

            return ExitCodeConstants.Success;
        }
        catch (AgentSessionException ex)
        {
            InteractionService.DisplayError($"Agent error: {ex.Message}");
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (OperationCanceledException)
        {
            return ExitCodeConstants.Success;
        }
    }
}
