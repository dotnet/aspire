// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
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
    private readonly IDotNetCliRunner _dotNetCliRunner;

    public UpdateCommand(IProjectLocator projectLocator, IPackagingService packagingService, IProjectUpdater projectUpdater, IInteractionService interactionService, IDotNetCliRunner dotNetCliRunner, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext) : base("update", UpdateCommandStrings.Description, features, updateNotifier, executionContext)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(projectUpdater);
        ArgumentNullException.ThrowIfNull(dotNetCliRunner);

        _projectLocator = projectLocator;
        _interactionService = interactionService;
        _packagingService = packagingService;
        _projectUpdater = projectUpdater;
        _dotNetCliRunner = dotNetCliRunner;
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

            // Step 1: Detect and warn about Aspire workload if present
            var workloadResult = await DetectAndWarnAboutAspireWorkloadAsync(cancellationToken);
            if (workloadResult != 0)
            {
                return workloadResult;
            }

            // Step 2: Update Aspire templates
            var templateResult = await UpdateAspireTemplatesAsync(channel, cancellationToken);
            if (templateResult != 0)
            {
                return templateResult;
            }

            // Step 3: Update project packages (existing behavior)
            await _projectUpdater.UpdateProjectAsync(projectFile!, channel, cancellationToken);

            _interactionService.DisplayMessage("check_mark", UpdateCommandStrings.EnvironmentUpdateComplete);
        }
        catch (ProjectUpdaterException ex)
        {
            var message = Markup.Escape(ex.Message);
            _interactionService.DisplayError(message);
            return ExitCodeConstants.FailedToUpgradeProject;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, _interactionService);
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

    private async Task<int> DetectAndWarnAboutAspireWorkloadAsync(CancellationToken cancellationToken)
    {
        var options = new DotNetCliRunnerInvocationOptions();
        
        var (exitCode, hasAspireWorkload) = await _interactionService.ShowStatusAsync(
            UpdateCommandStrings.CheckingWorkloadStatus,
            () => _dotNetCliRunner.CheckWorkloadAsync(options, cancellationToken));

        if (exitCode != 0)
        {
            // Don't fail the update if we can't detect the workload, just skip this step
            _interactionService.DisplayMessage("warning", "Unable to check for Aspire workload. Continuing with update...");
            return 0;
        }

        if (hasAspireWorkload)
        {
            _interactionService.DisplayMessage("warning", UpdateCommandStrings.WorkloadFound);
        }
        else
        {
            _interactionService.DisplayMessage("info", UpdateCommandStrings.WorkloadNotFound);
        }

        return 0;
    }

    private async Task<int> UpdateAspireTemplatesAsync(PackageChannel channel, CancellationToken cancellationToken)
    {
        var options = new DotNetCliRunnerInvocationOptions();

        try
        {
            var (templateResult, templateVersion) = await _interactionService.ShowStatusAsync(
                UpdateCommandStrings.UpdatingTemplatesStatus,
                async () =>
                {
                    // Get the latest template package from the channel
                    var templatePackages = await channel.GetTemplatePackagesAsync(ExecutionContext.WorkingDirectory, cancellationToken);
                    var latestTemplate = templatePackages
                        .Where(p => p.Id.Equals("Aspire.ProjectTemplates", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => Semver.SemVersion.Parse(p.Version))
                        .FirstOrDefault();

                    if (latestTemplate is null)
                    {
                        return (0, (string?)null);
                    }

                    // Use the channel's NuGet config if it's an explicit channel
                    FileInfo? nugetConfigFile = null;
                    string? nugetSource = null;
                    
                    if (channel.Type == PackageChannelType.Explicit && channel.Mappings is not null)
                    {
                        using var tempNuGetConfig = await TemporaryNuGetConfig.CreateAsync(channel.Mappings);
                        nugetConfigFile = tempNuGetConfig.ConfigFile;
                        // For explicit channels, we use the temp config file, not a specific source
                    }

                    return await _dotNetCliRunner.UpdateTemplateAsync(latestTemplate.Id, latestTemplate.Version, nugetConfigFile, nugetSource, options, cancellationToken);
                });

            if (templateResult != 0)
            {
                _interactionService.DisplayError(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.TemplateUpdateFailed, templateResult));
                return templateResult;
            }

            if (templateVersion is not null)
            {
                _interactionService.DisplayMessage("check_mark", string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.TemplateUpdateSuccessful, templateVersion));
            }
            else
            {
                _interactionService.DisplayMessage("info", UpdateCommandStrings.SkippingTemplateUpdate);
            }
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError($"Failed to update templates: {ex.Message}");
            return ExitCodeConstants.FailedToInstallTemplates;
        }

        return 0;
    }
}