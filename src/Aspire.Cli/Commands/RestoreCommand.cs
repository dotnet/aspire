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
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Regenerates polyglot SDK code for a guest (non-.NET) AppHost project.
/// Always regenerates without checking the hash, unlike <c>aspire run</c> which
/// skips code generation when the package hash is unchanged.
/// </summary>
internal sealed class RestoreCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.AppCommands;

    private readonly IProjectLocator _projectLocator;
    private readonly IAppHostProjectFactory _projectFactory;
    private readonly IInteractionService _interactionService;
    private readonly ILogger<RestoreCommand> _logger;

    private static readonly OptionWithLegacy<FileInfo?> s_appHostOption = new("--apphost", "--project", SharedCommandStrings.AppHostOptionDescription);

    public RestoreCommand(
        IProjectLocator projectLocator,
        IAppHostProjectFactory projectFactory,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        ILogger<RestoreCommand> logger,
        AspireCliTelemetry telemetry)
        : base("restore", "Regenerate polyglot SDK code for a non-.NET AppHost project.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _projectLocator = projectLocator;
        _projectFactory = projectFactory;
        _interactionService = interactionService;
        _logger = logger;

        Options.Add(s_appHostOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue(s_appHostOption);

        try
        {
            using var activity = Telemetry.StartDiagnosticActivity(this.Name);

            var searchResult = await _projectLocator.UseOrFindAppHostProjectFileAsync(
                passedAppHostProjectFile,
                MultipleAppHostProjectsFoundBehavior.Prompt,
                createSettingsFile: false,
                cancellationToken);

            var effectiveAppHostFile = searchResult.SelectedProjectFile;

            if (effectiveAppHostFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var project = _projectFactory.TryGetProject(effectiveAppHostFile);

            if (project is null)
            {
                InteractionService.DisplayError("Unrecognized app host type.");
                return ExitCodeConstants.FailedToFindProject;
            }

            if (project is not GuestAppHostProject guestProject)
            {
                InteractionService.DisplayError("The restore command is only supported for polyglot (non-.NET) AppHost projects.");
                return ExitCodeConstants.InvalidCommand;
            }

            var directory = effectiveAppHostFile.Directory!;
            _logger.LogDebug("Restoring polyglot SDK code for {AppHost} in {Directory}", effectiveAppHostFile.FullName, directory.FullName);

            var success = await _interactionService.ShowStatusAsync(
                ":gear:  Restoring polyglot SDK code...",
                async () => await guestProject.BuildAndGenerateSdkAsync(directory, cancellationToken));

            if (success)
            {
                _interactionService.DisplaySuccess(
                    string.Format(CultureInfo.CurrentCulture, "Polyglot SDK code restored successfully for {0}.", effectiveAppHostFile.Name));
                return ExitCodeConstants.Success;
            }

            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, InteractionService, Telemetry);
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
    }
}
