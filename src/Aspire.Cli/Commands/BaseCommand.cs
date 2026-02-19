// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected virtual bool UpdateNotificationsEnabled { get; } = true;
    private readonly CliExecutionContext _executionContext;

    protected CliExecutionContext ExecutionContext => _executionContext;

    protected IInteractionService InteractionService { get; }

    protected AspireCliTelemetry Telemetry { get; }

    protected BaseCommand(string name, string description, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IInteractionService interactionService, AspireCliTelemetry telemetry) : base(name, description)
    {
        _executionContext = executionContext;
        InteractionService = interactionService;
        Telemetry = telemetry;
        SetAction(async (parseResult, cancellationToken) =>
        {
            // Set the command on the execution context so background services can access it
            _executionContext.Command = this;

            // TODO: SDK install goes here in the future.

            var exitCode = await ExecuteAsync(parseResult, cancellationToken);

            if (UpdateNotificationsEnabled && features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, true))
            {
                try
                {
                    updateNotifier.NotifyIfUpdateAvailable();
                }
                catch
                {
                    // Ignore any errors during update check to avoid impacting the main command
                }
            }

            InteractionService.DisplayEmptyLine();

            return exitCode;
        });
    }

    protected abstract Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);

    internal static int HandleProjectLocatorException(ProjectLocatorException ex, IInteractionService interactionService, AspireCliTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentNullException.ThrowIfNull(interactionService);

        var errorMessage = ex.Message switch
        {
            var m when string.Equals(m, ErrorStrings.ProjectFileNotAppHostProject, StringComparisons.CliInputOrOutput)
                => InteractionServiceStrings.SpecifiedProjectFileNotAppHostProject,
            var m when string.Equals(m, ErrorStrings.ProjectFileDoesntExist, StringComparisons.CliInputOrOutput)
                => InteractionServiceStrings.ProjectOptionDoesntExist,
            var m when string.Equals(m, ErrorStrings.MultipleProjectFilesFound, StringComparisons.CliInputOrOutput)
                => InteractionServiceStrings.ProjectOptionNotSpecifiedMultipleAppHostsFound,
            var m when string.Equals(m, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput)
                => InteractionServiceStrings.ProjectOptionNotSpecifiedNoCsprojFound,
            var m when string.Equals(m, ErrorStrings.AppHostsMayNotBeBuildable, StringComparisons.CliInputOrOutput)
                => InteractionServiceStrings.UnbuildableAppHostsDetected,
            _ => string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message)
        };

        telemetry.RecordError(errorMessage, ex);
        interactionService.DisplayError(errorMessage);
        return ExitCodeConstants.FailedToFindProject;
    }
}
