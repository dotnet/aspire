// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateSearchCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string> s_keywordArgument = new("keyword")
    {
        Description = "Search keyword to filter templates by name, description, or tags"
    };

    public TemplateSearchCommand(
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("search", "Search templates by keyword", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Arguments.Add(s_keywordArgument);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var keyword = parseResult.GetValue(s_keywordArgument);
        InteractionService.DisplayMessage("information", $"Git-based template search is not yet implemented. Keyword: {keyword}");
        return Task.FromResult(ExitCodeConstants.Success);
    }
}
