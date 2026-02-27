// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateNewIndexCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string?> s_pathArgument = new("path")
    {
        Description = "Directory to create aspire-template-index.json in (defaults to current directory)",
        Arity = ArgumentArity.ZeroOrOne
    };

    public TemplateNewIndexCommand(
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("new-index", "Scaffold a new aspire-template-index.json index file", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Arguments.Add(s_pathArgument);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var path = parseResult.GetValue(s_pathArgument);
        InteractionService.DisplayMessage("information", $"Template index scaffolding is not yet implemented.{(path is not null ? $" Path: {path}" : "")}");
        return Task.FromResult(ExitCodeConstants.Success);
    }
}
