// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Commands;

/// <summary>
/// Parent command for AI agent integrations. Contains subcommands for MCP server and initialization.
/// </summary>
internal sealed class AgentCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IInteractionService _interactionService;
    private readonly AgentMcpCommand _mcpCommand;
    private readonly AgentInitCommand _initCommand;

    public AgentCommand(
        AgentMcpCommand mcpCommand,
        AgentInitCommand initCommand,
        IConfiguration configuration,
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("agent", AgentCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        ArgumentNullException.ThrowIfNull(mcpCommand);
        ArgumentNullException.ThrowIfNull(initCommand);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(interactionService);

        _mcpCommand = mcpCommand;
        _initCommand = initCommand;
        _configuration = configuration;
        _interactionService = interactionService;

        Subcommands.Add(mcpCommand);
        Subcommands.Add(initCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is not "true")
        {
            new HelpAction().Invoke(parseResult);
            return ExitCodeConstants.InvalidCommand;
        }

        // Prompt for the subcommand that the user wants to execute
        var subcommand = await _interactionService.PromptForSelectionAsync(
            AgentCommandStrings.ExtensionActionPrompt,
            new BaseCommand[] { _mcpCommand, _initCommand },
            cmd =>
            {
                Debug.Assert(cmd.Description is not null);
                return cmd.Description.TrimEnd('.');
            },
            cancellationToken);

        if (subcommand == _mcpCommand)
        {
            return await _mcpCommand.InteractiveExecuteAsync(cancellationToken);
        }
        else
        {
            return await _initCommand.InteractiveExecuteAsync(cancellationToken);
        }
    }
}
