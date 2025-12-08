// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Reflection.Metadata;
using Aspire.Cli.Agents;
using Aspire.Cli.Configuration;
using Aspire.Cli.Git;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command that initializes MCP server configuration for detected agent environments.
/// </summary>
internal sealed class McpInitCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IAgentEnvironmentDetector _agentEnvironmentDetector;
    private readonly IGitRepository _gitRepository;

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
        IAgentEnvironmentDetector agentEnvironmentDetector,
        IGitRepository gitRepository)
        : base("init", McpCommandStrings.InitCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(agentEnvironmentDetector);
        ArgumentNullException.ThrowIfNull(gitRepository);

        _interactionService = interactionService;
        _agentEnvironmentDetector = agentEnvironmentDetector;
        _gitRepository = gitRepository;
    }

    protected override bool UpdateNotificationsEnabled => false;

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

        // Prompt for whether to create agent-specific instruction files
        var createAgentInstructions = await _interactionService.ConfirmAsync(
            McpCommandStrings.InitCommand_CreateAgentInstructionsPrompt,
            defaultValue: true,
            cancellationToken: cancellationToken);

        // Prompt for whether to pre-configure Playwright MCP server
        var configurePlaywright = await _interactionService.ConfirmAsync(
            McpCommandStrings.InitCommand_ConfigurePlaywrightPrompt,
            defaultValue: true,
            cancellationToken: cancellationToken);

        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = ExecutionContext.WorkingDirectory,
            RepositoryRoot = workspaceRoot,
            ConfigurePlaywrightMcpServer = configurePlaywright
        };

        var applicators = await _interactionService.ShowStatusAsync(
            McpCommandStrings.InitCommand_DetectingAgentEnvironments,
            async () => await _agentEnvironmentDetector.DetectAsync(context, cancellationToken));

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

        // Create agent instructions file if requested
        if (createAgentInstructions)
        {
            await CreateAgentInstructionsAsync(workspaceRoot, cancellationToken);
        }

        _interactionService.DisplaySuccess(McpCommandStrings.InitCommand_ConfigurationComplete);

        return ExitCodeConstants.Success;
    }

    /// <summary>
    /// Creates agent instruction file (AGENTS.md or AGENTS.aspire.md if AGENTS.md already exists).
    /// </summary>
    private static async Task CreateAgentInstructionsAsync(DirectoryInfo workspaceRoot, CancellationToken cancellationToken)
    {
        var agentsFilePath = Path.Combine(workspaceRoot.FullName, "AGENTS.md");
        var targetFilePath = agentsFilePath;

        // If AGENTS.md already exists, use AGENTS.aspire.md instead
        if (File.Exists(agentsFilePath))
        {
            targetFilePath = Path.Combine(workspaceRoot.FullName, "AGENTS.aspire.md");
        }

        // Only create the file if it doesn't already exist
        if (!File.Exists(targetFilePath))
        {
            await File.WriteAllTextAsync(targetFilePath, AgentsMdContent, cancellationToken);
        }
    }

    private const string AgentsMdContent =
        """
        # Copilot instructions

        This repository is set up to use Aspire. Aspire is an orchestrator for the entire application and will take care of configuring dependencies, building, and running the application. The resources that make up the application are defined in `apphost.cs` including application code and external dependencies.

        ## General recommendations for working with Aspire
        1. Before making any changes always run the apphost using `aspire run` and inspect the state of resources to make sure you are building from a known state.
        1. Changes to the _apphost.cs_ file will require a restart of the applicaiton to take effect.
        2. Make changes incrementally and run the aspire application using teh `aspire run` command to validate changes.
        3. Use the Aspire MCP tools to check the status of resources and debug issues.

        ## Running the application
        To run the application run the following command:

        ```
        aspire run
        ```

        If there is already an instance of the application running it will prompt to stop the existing instance. You only need to restart the application if code in `apphost.cs` is changed, but if you experience problems it can be useful to reset everything to the starting state.

        ## Checking resources
        To check the status of resources defined in the app model use the _list resources_ tool. This will show you the current state of each resource and if there are any issues. If a resource is not running as expected you can use the _execute resource command_ tool to restart it or perform other actions.

        ## Listing integrations
        IMPORTANT! When a user asks you to add a resource to the app model you should first use the _list integrations_ tool to get a list of the current versions of all the available integrations. You should try to use the version of the integration which aligns with the version of the Aspire.AppHost.Sdk. Some integration versions may have a preview suffix. Once you have identified the correct integration you should always use the _get integration docs_ tool to fetch the latest documentation for the integration and follow the links to get additional guidance.

        ## Debugging issues
        IMPORTANT! Aspire is designed to capture rich logs and telemetry for all resources defined in the app model. Use the following diagnostic tools when debugging issues with the application before making changes to make sure you are focusing on the right things.

        1. _list structured logs_; use this tool to get details about structured logs.
        2. _list console logs_; use this tool to get details about console logs.
        3. _list traces_; use this tool to get details about traces.
        4. _list trace structured logs_; use this tool to get logs related to a trace

        ## Other Aspire MCP tools

        1. _select apphost_; use this tool if working with multiple app hosts within a workspace.
        2. _list apphosts_; use this tool to get details about active app hosts.

        ## Playwright MCP server

        The playwright MCP server has also been configured in this repository and you should use it to perform functional investigations of the resources defined in the app model as you work on the codebase. To get endpoints that can be used for navigation using the playwright MCP server use the list resources tool.

        ## Updating the app host
        The user may request that you update the Aspire apphost. You can do this using the `aspire update` command. This will update the apphost to the latest version and some of the Aspire specific packages in referenced projects, however you many need to manually update other packages in the solution to ensure compatability. You can consider using the `dotnet-outdated` with the users consent. To install the `dotnet-outdated` tool use the following command:

        ```
        dotnet tool install --global dotnet-outdated-tool
        ```

        ## Persistent containers
        IMPORTANT! Consider avoiding persistent containers early during development to avoid creating state management issues when restarting the app.

        ## Aspire workload
        IMPORTANT! The aspire workload is obsolete. You should never attempt to install or use the Aspire workload.

        ## Offical documentation
        IMPORTANT! Always prefer official documentation when available. The following sites contain the official documentation for Aspire and related components

        1. https://aspire.dev
        2. 
        3. https://nuget.org (for specific integration package details)
        """;
}
