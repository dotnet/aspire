// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp.Docs;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command to list all available Aspire documentation pages.
/// </summary>
internal sealed class DocsListCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IDocsIndexService _docsIndexService;
    private readonly ILogger<DocsListCommand> _logger;

    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = DocsCommandStrings.FormatOptionDescription
    };

    public DocsListCommand(
        IInteractionService interactionService,
        IDocsIndexService docsIndexService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        ILogger<DocsListCommand> logger)
        : base("list", DocsCommandStrings.ListDescription, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _docsIndexService = docsIndexService;
        _logger = logger;

        Options.Add(s_formatOption);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var format = parseResult.GetValue(s_formatOption);

        _logger.LogDebug("Listing documentation pages");

        // Load docs with status indicator (only shows spinner if network fetch is needed)
        var docs = await _interactionService.ShowStatusAsync(
            DocsCommandStrings.LoadingDocumentation,
            async () => await _docsIndexService.ListDocumentsAsync(cancellationToken));

        if (docs.Count is 0)
        {
            _interactionService.DisplayError(DocsCommandStrings.NoDocumentationAvailable);
            return ExitCodeConstants.InvalidCommand;
        }

        if (format is OutputFormat.Json)
        {
            var json = JsonSerializer.Serialize(docs.ToArray(), JsonSourceGenerationContext.RelaxedEscaping.DocsListItemArray);
            _interactionService.DisplayRawText(json);
        }
        else
        {
            _interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, DocsCommandStrings.FoundDocumentationPages, docs.Count));

            var table = new Table();
            table.AddColumn("Title");
            table.AddColumn("Slug");
            table.AddColumn("Summary");

            foreach (var doc in docs)
            {
                table.AddRow(
                    Markup.Escape(doc.Title),
                    Markup.Escape(doc.Slug),
                    Markup.Escape(doc.Summary ?? ""));
            }

            AnsiConsole.Write(table);
        }

        return ExitCodeConstants.Success;
    }
}
