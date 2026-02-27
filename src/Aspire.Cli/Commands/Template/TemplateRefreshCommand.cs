// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating.Git;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateRefreshCommand(
    IGitTemplateIndexService indexService,
    IFeatures features,
    ICliUpdateNotifier updateNotifier,
    CliExecutionContext executionContext,
    IInteractionService interactionService,
    AspireCliTelemetry telemetry)
    : BaseTemplateSubCommand("refresh", "Force refresh the template index cache", features, updateNotifier, executionContext, interactionService, telemetry)
{
    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        await InteractionService.ShowStatusAsync(
            ":counterclockwise_arrows_button: Refreshing template index cache...",
            async () =>
            {
                await indexService.RefreshAsync(cancellationToken);
                return 0;
            });

        InteractionService.DisplaySuccess("Template index cache refreshed.");
        return ExitCodeConstants.Success;
    }
}
