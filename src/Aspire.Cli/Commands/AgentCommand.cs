// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command for managing the Aspire agent experience.
/// </summary>
internal sealed class AgentCommand : BaseCommand
{
    public AgentCommand(
        AgentStartCommand startCommand,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService)
        : base("agent", "Start an AI-powered agent for building Aspire applications", features, updateNotifier, executionContext, interactionService)
    {
        Subcommands.Add(startCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Show help if no subcommand specified
        InteractionService.DisplayMessage("🤖", "Use 'aspire agent start' to launch the Aspire agent.");
        return Task.FromResult(ExitCodeConstants.Success);
    }
}
