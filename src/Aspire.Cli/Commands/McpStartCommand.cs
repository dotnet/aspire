// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp.Docs;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Legacy command 'aspire mcp start' that delegates to the new AgentMcpCommand.
/// This is kept for backward compatibility but is hidden from help.
/// </summary>
internal sealed class McpStartCommand : BaseCommand
{
    private readonly AgentMcpCommand _agentMcpCommand;

    public McpStartCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILoggerFactory loggerFactory,
        ILogger<AgentMcpCommand> agentMcpLogger,
        IPackagingService packagingService,
        IEnvironmentChecker environmentChecker,
        IDocsSearchService docsSearchService,
        IDocsIndexService docsIndexService,
        AspireCliTelemetry telemetry)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        // Create the AgentMcpCommand to delegate execution to
        _agentMcpCommand = new AgentMcpCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            auxiliaryBackchannelMonitor,
            loggerFactory,
            agentMcpLogger,
            packagingService,
            environmentChecker,
            docsSearchService,
            docsIndexService,
            telemetry);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Display deprecation warning to stderr (all MCP logging goes to stderr)
        InteractionService.DisplayMarkupLine($"[yellow]⚠ {McpCommandStrings.DeprecatedCommandWarning}[/]");
        
        // Delegate to the new AgentMcpCommand
        return _agentMcpCommand.ExecuteCommandAsync(parseResult, cancellationToken);
    }
}
