// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Legacy MCP command for backward compatibility. Hidden in favor of 'aspire agent' command.
/// </summary>
internal sealed class McpCommand : BaseCommand
{
    public McpCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        McpStartCommand startCommand,
        McpInitCommand initCommand,
        AspireCliTelemetry telemetry)
        : base("mcp", McpCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        // Mark as hidden - use 'aspire agent' instead
        Hidden = true;

        Subcommands.Add(startCommand);
        Subcommands.Add(initCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }
}
