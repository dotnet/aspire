// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class UpdateCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IPackagingService _packagingService;
    private readonly IProjectUpdater _projectUpdater;

    public UpdateCommand(IProjectLocator projectLocator, IPackagingService packagingService, IProjectUpdater projectUpdater, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext) : base("update", UpdateCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(projectUpdater);

        _projectLocator = projectLocator;
        _packagingService = packagingService;
        _projectUpdater = projectUpdater;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = UpdateCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var projectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);
            if (projectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var channels = await _packagingService.GetChannelsAsync(cancellationToken);

            var channel = await InteractionService.PromptForSelectionAsync(UpdateCommandStrings.SelectChannelPrompt, channels, (c) => c.Name, cancellationToken);

            await _projectUpdater.UpdateProjectAsync(projectFile!, channel, cancellationToken);
        }
        catch (ProjectUpdaterException ex)
        {
            var message = Markup.Escape(ex.Message);
            InteractionService.DisplayError(message);
            return ExitCodeConstants.FailedToUpgradeProject;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, InteractionService);
        }

        return 0;
    }
}
