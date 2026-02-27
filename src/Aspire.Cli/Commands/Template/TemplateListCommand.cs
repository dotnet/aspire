// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating.Git;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateListCommand(
    IGitTemplateIndexService indexService,
    IFeatures features,
    ICliUpdateNotifier updateNotifier,
    CliExecutionContext executionContext,
    IInteractionService interactionService,
    AspireCliTelemetry telemetry)
    : BaseTemplateSubCommand("list", "List available templates from all configured sources", features, updateNotifier, executionContext, interactionService, telemetry)
{
    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var templates = await InteractionService.ShowStatusAsync(
            ":magnifying_glass_tilted_right: Fetching templates...",
            () => indexService.GetTemplatesAsync(cancellationToken: cancellationToken));

        if (templates.Count == 0)
        {
            InteractionService.DisplayMessage("information", "No templates found. Try 'aspire template refresh' to update the index cache.");
            return ExitCodeConstants.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]Name[/]").NoWrap());
        table.AddColumn(new TableColumn("[bold]Source[/]"));
        table.AddColumn(new TableColumn("[bold]Language[/]"));
        table.AddColumn(new TableColumn("[bold]Tags[/]"));

        foreach (var t in templates)
        {
            table.AddRow(
                t.Entry.Name.EscapeMarkup(),
                t.Source.Name.EscapeMarkup(),
                (t.Entry.Language ?? "").EscapeMarkup(),
                t.Entry.Tags is { Count: > 0 } ? string.Join(", ", t.Entry.Tags).EscapeMarkup() : "");
        }

        AnsiConsole.Write(table);
        return ExitCodeConstants.Success;
    }
}
