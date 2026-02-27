// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateListCommand(
    IFeatures features,
    ICliUpdateNotifier updateNotifier,
    CliExecutionContext executionContext,
    IInteractionService interactionService,
    AspireCliTelemetry telemetry)
    : BaseTemplateSubCommand("list", "List available templates from all configured sources", features, updateNotifier, executionContext, interactionService, telemetry)
{
    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        InteractionService.DisplayMessage("information", "Git-based template listing is not yet implemented.");
        return Task.FromResult(ExitCodeConstants.Success);
    }
}
