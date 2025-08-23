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
    private readonly IInteractionService _interactionService;
    private readonly IPackagingService _packagingService;
    private readonly IProjectUpdater _projectUpdater;

    public UpdateCommand(IProjectLocator projectLocator, IPackagingService packagingService, IProjectUpdater projectUpdater, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier) : base("update", UpdateCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(projectUpdater);

        _projectLocator = projectLocator;
        _interactionService = interactionService;
        _packagingService = packagingService;
        _projectUpdater = projectUpdater;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            var projectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(null, cancellationToken);

            // Fail fast if central package management is detected, before prompting for channels.
            if (projectFile is not null && UsesCentralPackageManagement(projectFile))
            {
                _interactionService.DisplayError(UpdateCommandStrings.CentralPackageManagementNotSupported);
                return ExitCodeConstants.CentralPackageManagementNotSupported;
            }
            var channels = await _packagingService.GetChannelsAsync(cancellationToken);

            var channel = await _interactionService.PromptForSelectionAsync(UpdateCommandStrings.SelectChannelPrompt, channels, (c) => c.Name, cancellationToken);

            await _projectUpdater.UpdateProjectAsync(projectFile!, channel, cancellationToken);
        }
        catch (ProjectUpdaterException ex)
        {
            var message = Markup.Escape(ex.Message);
            _interactionService.DisplayError(message);
            return ExitCodeConstants.FailedToUpgradeProject;
        }

        return 0;
    }

    private static bool UsesCentralPackageManagement(FileInfo projectFile)
    {
        // Heuristic 1: Presence of Directory.Packages.props in directory tree.
        for (var current = projectFile.Directory; current is not null; current = current.Parent)
        {
            var directoryPackagesPropsPath = Path.Combine(current.FullName, "Directory.Packages.props");
            if (File.Exists(directoryPackagesPropsPath))
            {
                return true;
            }
        }

        // Heuristic 2: ManagePackageVersionsCentrally property inside project.
        try
        {
            var doc = new System.Xml.XmlDocument { PreserveWhitespace = true };
            doc.Load(projectFile.FullName);
            var manageNode = doc.SelectSingleNode("/Project/PropertyGroup/ManagePackageVersionsCentrally");
            if (manageNode?.InnerText.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }
        catch
        {
            // Ignore parse errors.
        }

        return false;
    }
}