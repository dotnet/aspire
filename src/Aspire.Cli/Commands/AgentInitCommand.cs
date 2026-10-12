// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Completions;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Git;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Selects agents, independent assets, and an installation scope before applying changes.
/// </summary>
internal sealed class AgentInitCommand : BaseCommand
{
    internal static readonly Option<string?> s_workspaceRootOption = new("--workspace-root")
    {
        Description = AgentCommandStrings.InitCommand_WorkspaceRootOptionDescription
    };

    internal static readonly Option<AgentConfirmation?> s_mcpOption = AgentSetupService.CreateAssetOption("--mcp", AgentCommandStrings.InitCommand_McpOptionDescription);
    internal static readonly Option<AgentConfirmation?> s_playwrightOption = AgentSetupService.CreateAssetOption("--playwright", AgentCommandStrings.InitCommand_PlaywrightOptionDescription);
    internal static readonly Option<AgentConfirmation?> s_dotnetInspectOption = AgentSetupService.CreateAssetOption("--dotnet-inspect", AgentCommandStrings.InitCommand_DotnetInspectOptionDescription);
    internal static readonly Option<AgentConfirmation?> s_aspireSkillsOption = AgentSetupService.CreateAssetOption("--aspire-skills", AgentCommandStrings.InitCommand_AspireSkillsOptionDescription);
    internal Option<string?> AgentsOption { get; }
    internal Option<AgentConfigurationScope> ScopeOption { get; } = CreateScopeOption();

    private readonly AgentSetupService _setup;
    private readonly ITelemetryHookConfigurator _hooks;
    private readonly IGitRepository _gitRepository;
    private readonly ICliHostEnvironment _hostEnvironment;

    public AgentInitCommand(
        AgentSetupService setup,
        ITelemetryHookConfigurator hooks,
        IGitRepository gitRepository,
        CommonCommandServices services)
        : base("init", AgentCommandStrings.InitCommand_Description, services)
    {
        _setup = setup;
        _hooks = hooks;
        _gitRepository = gitRepository;
        _hostEnvironment = services.HostEnvironment;
        AgentsOption = AgentSetupService.CreateAgentsOption();

        Options.Add(s_workspaceRootOption);
        Options.Add(s_mcpOption);
        Options.Add(s_playwrightOption);
        Options.Add(s_dotnetInspectOption);
        Options.Add(s_aspireSkillsOption);
        Options.Add(AgentsOption);
        Options.Add(ScopeOption);
    }

    internal Task<CommandResult> ExecuteCommandAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return ExecuteAsync(parseResult, cancellationToken);
    }

    internal async Task<AgentInitExecutionResult> PromptAndChainAsync(
        IInteractionService interactionService,
        int previousResultExitCode,
        DirectoryInfo workspaceRoot,
        PromptBinding<bool> agentInitBinding,
        PromptBinding<string?> agentsBinding,
        PromptBinding<bool> playwrightBinding,
        PromptBinding<bool> dotnetInspectBinding,
        PromptBinding<bool> aspireSkillsBinding,
        PromptBinding<AgentConfigurationScope> scopeBinding,
        CancellationToken cancellationToken)
    {
        if (previousResultExitCode != CliExitCodes.Success)
        {
            return new(previousResultExitCode, []);
        }

        interactionService.DisplayEmptyLine();
        var runAgentInit = await interactionService.PromptConfirmAsync(
            SharedCommandStrings.PromptRunAgentInit,
            binding: agentInitBinding,
            cancellationToken: cancellationToken);

        // Chained new/init flows cannot configure MCP; standalone commands own that consent.
        return runAgentInit
            ? await ExecuteAgentInitAsync(workspaceRoot, ExecutionContext.WorkingDirectory,
                agentsBinding, mcpBinding: null, playwrightBinding, dotnetInspectBinding,
                aspireSkillsBinding, scopeBinding, parseResult: null, cancellationToken)
            : new(CliExitCodes.Success, []);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var result = await ExecuteAgentInitAsync(null, ExecutionContext.WorkingDirectory,
            PromptBinding.Create(parseResult, AgentsOption),
            AgentSetupService.CreateAssetBinding(parseResult, s_mcpOption, defaultValue: false),
            AgentSetupService.CreateAssetBinding(parseResult, s_playwrightOption, defaultValue: false),
            AgentSetupService.CreateAssetBinding(parseResult, s_dotnetInspectOption, defaultValue: false),
            AgentSetupService.CreateAssetBinding(parseResult, s_aspireSkillsOption, defaultValue: true),
            PromptBinding.Create(parseResult, ScopeOption, AgentConfigurationScope.Project),
            parseResult, cancellationToken);
        return CommandResult.FromExitCode(result.ExitCode);
    }

    private static Option<AgentConfigurationScope> CreateScopeOption()
    {
        var option = new Option<AgentConfigurationScope>("--scope")
        {
            Description = AgentCommandStrings.InitCommand_ScopeOptionDescription,
            Recursive = true,
            Arity = ArgumentArity.ExactlyOne
        };
        option.Validators.Add(result =>
        {
            if (result.Tokens.Count == 1 &&
                Enum.TryParse<AgentConfigurationScope>(result.Tokens[0].Value, ignoreCase: true, out _) &&
                !Enum.GetNames<AgentConfigurationScope>().Contains(result.Tokens[0].Value, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InvalidScope, result.Tokens[0].Value));
            }
        });
        option.CompletionSources.Clear();
        option.CompletionSources.Add(static _ => [new CompletionItem("project"), new CompletionItem("global")]);
        return option;
    }

    private async Task<DirectoryInfo> PromptForWorkspaceRootAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var gitRoot = await _gitRepository.GetRootAsync(cancellationToken);
        var defaultWorkspaceRoot = gitRoot ?? ExecutionContext.WorkingDirectory;
        var workspaceRootPath = await InteractionService.PromptForFilePathAsync(
            McpCommandStrings.InitCommand_WorkspaceRootPrompt,
            binding: PromptBinding.Create(parseResult, s_workspaceRootOption, defaultWorkspaceRoot.FullName),
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
            directory: true,
            cancellationToken: cancellationToken);

        return new DirectoryInfo(workspaceRootPath);
    }

    private async Task<AgentInitExecutionResult> ExecuteAgentInitAsync(
        DirectoryInfo? workspaceRoot,
        DirectoryInfo workingDirectory,
        PromptBinding<string?> agentsBinding,
        PromptBinding<bool>? mcpBinding,
        PromptBinding<bool> playwrightBinding,
        PromptBinding<bool> dotnetInspectBinding,
        PromptBinding<bool> aspireSkillsBinding,
        PromptBinding<AgentConfigurationScope> scopeBinding,
        ParseResult? parseResult,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (agentsWereProvided, requestedAgents) = agentsBinding.Resolve();
        if (agentsWereProvided && string.Equals(requestedAgents, ConsoleInteractionService.NoneChoice, StringComparison.OrdinalIgnoreCase) ||
            IsDisabled(mcpBinding) && IsDisabled(playwrightBinding) &&
            IsDisabled(dotnetInspectBinding) && IsDisabled(aspireSkillsBinding))
        {
            InteractionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_NoConfigurationSelected);
            return new(CliExitCodes.Success, []);
        }

        var (scopeWasProvided, requestedScope) = scopeBinding.Resolve();
        var defaultScope = scopeBinding.DefaultValue;
        var scope = scopeWasProvided ? requestedScope : defaultScope;
        if (parseResult is not null &&
            (scope is AgentConfigurationScope.Project || parseResult.GetResult(s_workspaceRootOption) is { Implicit: false }))
        {
            workspaceRoot = await PromptForWorkspaceRootAsync(parseResult, cancellationToken);
        }
        var discovery = await _setup.ScanAsync(workingDirectory, workspaceRoot, cancellationToken);
        var clients = await _setup.SelectAgentsAsync(agentsBinding, discovery, _hostEnvironment.SupportsInteractiveInput, cancellationToken);
        if (clients is null)
        {
            return new(CliExitCodes.InvalidCommand, []);
        }
        if (clients.Count == 0)
        {
            InteractionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_NoConfigurationSelected);
            return new(CliExitCodes.Success, []);
        }

        var mcp = mcpBinding is not null && await InteractionService.PromptConfirmAsync(
            AgentCommandStrings.InitCommand_ConfigureMcpServerPrompt,
            binding: mcpBinding,
            cancellationToken: cancellationToken);
        var playwright = await InteractionService.PromptConfirmAsync(
            McpCommandStrings.InitCommand_ConfigurePlaywrightPrompt,
            binding: playwrightBinding,
            cancellationToken: cancellationToken);
        var dotnetInspect = await InteractionService.PromptConfirmAsync(
            AgentCommandStrings.InitCommand_ConfigureDotnetInspectPrompt,
            binding: dotnetInspectBinding,
            cancellationToken: cancellationToken);
        var aspireSkills = await _setup.PromptForPluginAsync(
            aspireSkillsBinding,
            AgentCommandStrings.InitCommand_ConfigureAspireSkillsPrompt,
            cancellationToken);
        var assets = new AgentAssetSelection(mcp, playwright, dotnetInspect, aspireSkills);

        if (!assets.HasAssets)
        {
            InteractionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_NoConfigurationSelected);
            return new(CliExitCodes.Success, []);
        }

        if (playwright || dotnetInspect)
        {
            InteractionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_SharedSkillsNotice);
        }

        var request = new AgentInitRequest(workspaceRoot, assets, scope, clients.Distinct().ToArray(), discovery.Detections);
        if (!scopeWasProvided && _hostEnvironment.SupportsInteractiveInput)
        {
            scope = await InteractionService.PromptForSelectionAsync<AgentConfigurationScope>(
                AgentCommandStrings.InitCommand_SelectScopePrompt,
                defaultScope is AgentConfigurationScope.Project
                    ? [AgentConfigurationScope.Project, AgentConfigurationScope.Global]
                    : [AgentConfigurationScope.Global, AgentConfigurationScope.Project],
                value => _setup.DescribeScope(request with { Scope = value }),
                cancellationToken: cancellationToken);
            request = request with { Scope = scope };
        }
        InteractionService.DisplaySubtleMessage(_setup.DescribeScope(request));

        if (aspireSkills)
        {
            await _setup.WarnAboutLocalSkillsAsync(workingDirectory, request, cancellationToken);
        }

        using var activity = Telemetry.StartReportedActivity("AgentInit.Configure");
        activity?.SetTag("aspire.agent.clients", string.Join(",", clients.Select(static client => client.DisplayName)));
        activity?.SetTag("aspire.agent.mcp", mcp);
        activity?.SetTag("aspire.agent.playwright", playwright);
        activity?.SetTag("aspire.agent.dotnet_inspect", dotnetInspect);
        activity?.SetTag("aspire.agent.aspire_skills", aspireSkills);
        activity?.SetTag("aspire.agent.scope", scope is AgentConfigurationScope.Project ? "project" : "global");

        var result = await _setup.ConfigureAsync(request, _hooks, cancellationToken);

        _setup.DisplayResults(result);
        activity?.SetTag("aspire.agent.configuration_failed", result.HasErrors);
        activity?.SetTag("aspire.agent.configuration_warnings", result.HasWarnings);
        if (result.HasErrors)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        return new(result.HasErrors ? CliExitCodes.InvalidCommand : CliExitCodes.Success, result.RegisteredEnvironments);

        bool IsDisabled(PromptBinding<bool>? binding)
        {
            if (binding is null)
            {
                return true;
            }
            var (provided, requested) = binding.Resolve();
            return provided ? !requested : !_hostEnvironment.SupportsInteractiveInput && !binding.DefaultValue;
        }
    }
}

internal readonly record struct AgentInitExecutionResult(int ExitCode, IReadOnlyList<IAgentEnvironmentScanner> RegisteredEnvironments);
