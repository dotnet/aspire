// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using Aspire.Cli.Agents;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Git;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class McpCommand : BaseCommand
{
    public McpCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILoggerFactory loggerFactory,
        ILogger<McpStartCommand> logger,
        IAgentEnvironmentDetector agentEnvironmentDetector,
        IGitRepository gitRepository,
        IPackagingService packagingService)
        : base("mcp", McpCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);

        var startCommand = new McpStartCommand(interactionService, features, updateNotifier, executionContext, auxiliaryBackchannelMonitor, loggerFactory, logger, packagingService);
        Subcommands.Add(startCommand);

        var initCommand = new McpInitCommand(interactionService, features, updateNotifier, executionContext, agentEnvironmentDetector, gitRepository);
        Subcommands.Add(initCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }
}
