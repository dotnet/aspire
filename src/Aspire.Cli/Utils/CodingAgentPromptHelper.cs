// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.CodingAgent;
using Aspire.Cli.Configuration;
using Aspire.Cli.Git;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils;

/// <summary>
/// Helper for prompting users about coding agent workspace configuration.
/// </summary>
internal static class CodingAgentPromptHelper
{
    /// <summary>
    /// Prompts the user to configure their workspace for coding agents if the feature is enabled.
    /// </summary>
    /// <param name="targetDirectory">The directory where the project was created.</param>
    /// <param name="gitCliRunner">The Git CLI runner for finding repository roots.</param>
    /// <param name="codingAgentConfigurator">The configurator for setting up coding agent files.</param>
    /// <param name="interactionService">The interaction service for prompting the user.</param>
    /// <param name="features">The features configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task PromptForCodingAgentConfigurationAsync(
        DirectoryInfo targetDirectory,
        IGitCliRunner gitCliRunner,
        ICodingAgentConfigurator codingAgentConfigurator,
        IInteractionService interactionService,
        IFeatures features,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);
        ArgumentNullException.ThrowIfNull(gitCliRunner);
        ArgumentNullException.ThrowIfNull(codingAgentConfigurator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(features);

        // Check if the feature is enabled
        if (!features.IsFeatureEnabled(KnownFeatures.CopilotAssistEnabled, defaultValue: false))
        {
            return;
        }

        // Try to find the Git repository root
        var gitRoot = await gitCliRunner.FindGitRootAsync(targetDirectory, cancellationToken);
        var workspaceRoot = gitRoot ?? targetDirectory;

        // Prompt the user
        var promptMessage = """
                           # Configure workspace for Copilot and coding agents?
                           
                           Would you like to configure this workspace with files that optimize
                           the experience when using GitHub Copilot and other coding agents?
                           
                           This will:
                           - Create a `.github/copilot-instructions.md` file with Aspire-specific guidance
                           - Check for and optionally configure MCP (Model Context Protocol) settings
                           """;

        interactionService.DisplayEmptyLine();
        interactionService.DisplayMarkdown(promptMessage);
        interactionService.DisplayEmptyLine();

        var shouldConfigure = await interactionService.ConfirmAsync(
            CodingAgentStrings.ConfigureWorkspacePrompt,
            defaultValue: true,
            cancellationToken);

        if (!shouldConfigure)
        {
            return;
        }

        // Configure the workspace
        var success = await interactionService.ShowStatusAsync(
            CodingAgentStrings.ConfiguringWorkspace,
            async () => await codingAgentConfigurator.ConfigureWorkspaceAsync(workspaceRoot, cancellationToken));

        if (success)
        {
            interactionService.DisplaySuccess(CodingAgentStrings.WorkspaceConfigured);
        }
        else
        {
            interactionService.DisplayError(CodingAgentStrings.WorkspaceConfigurationFailed);
        }
    }
}
