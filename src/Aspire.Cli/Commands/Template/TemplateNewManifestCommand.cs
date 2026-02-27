// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateNewManifestCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string?> s_pathArgument = new("path")
    {
        Description = "Directory to create aspire-template.json in (defaults to current directory)",
        Arity = ArgumentArity.ZeroOrOne
    };

    public TemplateNewManifestCommand(
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("new", "Scaffold a new aspire-template.json manifest", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Arguments.Add(s_pathArgument);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var path = parseResult.GetValue(s_pathArgument);
        InteractionService.DisplayMessage("information", $"Template manifest scaffolding is not yet implemented.{(path is not null ? $" Path: {path}" : "")}");
        return Task.FromResult(ExitCodeConstants.Success);
    }
}
