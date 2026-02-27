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

internal sealed class TemplateSearchCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string> s_keywordArgument = new("keyword")
    {
        Description = "Search keyword to filter templates by name or tags"
    };

    private readonly IGitTemplateIndexService _indexService;

    public TemplateSearchCommand(
        IGitTemplateIndexService indexService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("search", "Search templates by keyword", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _indexService = indexService;
        Arguments.Add(s_keywordArgument);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var keyword = parseResult.GetValue(s_keywordArgument)!;

        var allTemplates = await InteractionService.ShowStatusAsync(
            ":magnifying_glass_tilted_right: Searching templates...",
            () => _indexService.GetTemplatesAsync(cancellationToken: cancellationToken));

        var matches = allTemplates.Where(t => Matches(t, keyword)).ToList();

        if (matches.Count == 0)
        {
            InteractionService.DisplayMessage("information", $"No templates matching '{keyword}'.");
            return ExitCodeConstants.Success;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]Name[/]").NoWrap());
        table.AddColumn(new TableColumn("[bold]Source[/]"));
        table.AddColumn(new TableColumn("[bold]Language[/]"));
        table.AddColumn(new TableColumn("[bold]Tags[/]"));

        foreach (var t in matches)
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

    private static bool Matches(ResolvedTemplate template, string keyword)
    {
        if (template.Entry.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (template.Entry.Tags is { Count: > 0 })
        {
            return template.Entry.Tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
