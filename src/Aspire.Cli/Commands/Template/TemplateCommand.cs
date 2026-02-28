// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.ToolsAndConfiguration;

    public TemplateCommand(
        TemplateListCommand listCommand,
        TemplateSearchCommand searchCommand,
        TemplateRefreshCommand refreshCommand,
        TemplateNewManifestCommand newManifestCommand,
        TemplateNewIndexCommand newIndexCommand,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("template", "Manage git-based Aspire templates", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Subcommands.Add(listCommand);
        Subcommands.Add(searchCommand);
        Subcommands.Add(refreshCommand);
        Subcommands.Add(newManifestCommand);
        Subcommands.Add(newIndexCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }
}
