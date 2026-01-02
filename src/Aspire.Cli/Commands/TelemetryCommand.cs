// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class TelemetryCommand : BaseCommand
{
    public TelemetryCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        TelemetryFieldsCommand fieldsCommand,
        TelemetryTracesCommand tracesCommand,
        TelemetryLogsCommand logsCommand,
        TelemetryMetricsCommand metricsCommand)
        : base("telemetry", TelemetryCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(fieldsCommand);
        ArgumentNullException.ThrowIfNull(tracesCommand);
        ArgumentNullException.ThrowIfNull(logsCommand);
        ArgumentNullException.ThrowIfNull(metricsCommand);

        // Common options that will be available to all subcommands
        var projectOption = new Option<FileInfo?>("--project", "-p")
        {
            Description = TelemetryCommandStrings.ProjectOptionDescription,
            Recursive = true
        };
        Options.Add(projectOption);

        var dashboardUrlOption = new Option<string?>("--dashboard-url")
        {
            Description = TelemetryCommandStrings.DashboardUrlOptionDescription,
            Recursive = true
        };
        Options.Add(dashboardUrlOption);

        var apiKeyOption = new Option<string?>("--api-key")
        {
            Description = TelemetryCommandStrings.ApiKeyOptionDescription,
            Recursive = true
        };
        Options.Add(apiKeyOption);

        // Subcommands
        Subcommands.Add(fieldsCommand);
        Subcommands.Add(tracesCommand);
        Subcommands.Add(logsCommand);
        Subcommands.Add(metricsCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }
}
