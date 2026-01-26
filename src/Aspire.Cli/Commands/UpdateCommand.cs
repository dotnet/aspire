// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Exceptions;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class UpdateCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IPackagingService _packagingService;
    private readonly IAppHostProjectFactory _projectFactory;
    private readonly ILogger<UpdateCommand> _logger;
    private readonly ICliDownloader? _cliDownloader;
    private readonly ICliInstaller? _cliInstaller;
    private readonly ICliUpdateNotifier _updateNotifier;
    private readonly IFeatures _features;
    private readonly IConfigurationService _configurationService;

    public UpdateCommand(
        IProjectLocator projectLocator,
        IPackagingService packagingService,
        IAppHostProjectFactory projectFactory,
        ILogger<UpdateCommand> logger,
        ICliDownloader? cliDownloader,
        ICliInstaller? cliInstaller,
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IConfigurationService configurationService)
        : base("update", UpdateCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(projectFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(updateNotifier);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(configurationService);

        _projectLocator = projectLocator;
        _packagingService = packagingService;
        _projectFactory = projectFactory;
        _logger = logger;
        _cliDownloader = cliDownloader;
        _cliInstaller = cliInstaller;
        _updateNotifier = updateNotifier;
        _features = features;
        _configurationService = configurationService;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = UpdateCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        // Add --self option regardless of whether running as dotnet tool
        var selfOption = new Option<bool>("--self");
        selfOption.Description = "Update the Aspire CLI itself to the latest version";
        Options.Add(selfOption);

        // Customize description based on whether staging channel is enabled
        var isStagingEnabled = _features.IsFeatureEnabled(KnownFeatures.StagingChannelEnabled, false);
        
        var channelOption = new Option<string?>("--channel")
        {
            Description = isStagingEnabled 
                ? UpdateCommandStrings.ChannelOptionDescriptionWithStaging
                : UpdateCommandStrings.ChannelOptionDescription
        };
        Options.Add(channelOption);

        // Keep --quality for backward compatibility but hide it
        var qualityOption = new Option<string?>("--quality")
        {
            Description = isStagingEnabled 
                ? UpdateCommandStrings.QualityOptionDescriptionWithStaging
                : UpdateCommandStrings.QualityOptionDescription,
            Hidden = true
        };
        Options.Add(qualityOption);
    }

    protected override bool UpdateNotificationsEnabled => false;

    private static bool IsRunningAsDotNetTool()
    {
        // When running as a dotnet tool, the process path points to "dotnet" or "dotnet.exe"
        // When running as a native binary, it points to "aspire" or "aspire.exe"
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var isSelfUpdate = parseResult.GetValue<bool>("--self");

        // If --self is specified, handle CLI self-update
        if (isSelfUpdate)
        {
            // When running as a dotnet tool, print the update command instead of executing
            if (IsRunningAsDotNetTool())
            {
                InteractionService.DisplayMessage("information", UpdateCommandStrings.DotNetToolSelfUpdateMessage);
                InteractionService.DisplayPlainText("  dotnet tool update -g Aspire.Cli");
                return 0;
            }

            if (_cliDownloader is null)
            {
                InteractionService.DisplayError("CLI self-update is not available in this environment.");
                return ExitCodeConstants.InvalidCommand;
            }

            try
            {
                return await ExecuteSelfUpdateAsync(parseResult, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                InteractionService.DisplayCancellationMessage();
                return ExitCodeConstants.InvalidCommand;
            }
        }

        // Otherwise, handle project update
        try
        {
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var projectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, createSettingsFile: true, cancellationToken);
            if (projectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var allChannels = await _packagingService.GetChannelsAsync(cancellationToken);
            
            // Check if channel or quality option was provided (channel takes precedence)
            var channelName = parseResult.GetValue<string?>("--channel") ?? parseResult.GetValue<string?>("--quality");
            PackageChannel channel;
            
            if (!string.IsNullOrEmpty(channelName))
            {
                // Try to find a channel matching the provided channel/quality
                channel = allChannels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ChannelNotFoundException($"No channel found matching '{channelName}'. Valid options are: {string.Join(", ", allChannels.Select(c => c.Name))}");
            }
            else
            {
                // If there are hives (PR build directories), prompt for channel selection.
                // Otherwise, use the implicit/default channel automatically.
                var hasHives = ExecutionContext.GetPrHiveCount() > 0;
                
                if (hasHives)
                {
                    // Prompt for channel selection
                    channel = await InteractionService.PromptForSelectionAsync(
                        UpdateCommandStrings.SelectChannelPrompt,
                        allChannels,
                        (c) => $"{c.Name} ({c.SourceDetails})",
                        cancellationToken);
                }
                else
                {
                    // Use the default (implicit) channel
                    channel = allChannels.FirstOrDefault(c => c.Type is PackageChannelType.Implicit)
                        ?? allChannels.First();
                }
            }

            // Get the appropriate project handler and update packages
            var project = _projectFactory.GetProject(projectFile);
            var updateContext = new UpdatePackagesContext
            {
                AppHostFile = projectFile,
                Channel = channel
            };
            await project.UpdatePackagesAsync(updateContext, cancellationToken);

            // After successful project update, check if CLI update is available and prompt
            // Only prompt if the channel supports CLI downloads (has a non-null CliDownloadBaseUrl)
            if (_cliDownloader is not null && 
                _updateNotifier.IsUpdateAvailable() && 
                !string.IsNullOrEmpty(channel.CliDownloadBaseUrl))
            {
                var shouldUpdateCli = await InteractionService.ConfirmAsync(
                    UpdateCommandStrings.UpdateCliAfterProjectUpdatePrompt,
                    defaultValue: true,
                    cancellationToken);
                
                if (shouldUpdateCli)
                {
                    // Use the same channel that was selected for the project update
                    return await ExecuteSelfUpdateAsync(parseResult, cancellationToken, channel.Name);
                }
            }
        }
        catch (ProjectUpdaterException ex)
        {
            var message = Markup.Escape(ex.Message);
            InteractionService.DisplayError(message);
            return ExitCodeConstants.FailedToUpgradeProject;
        }
        catch (ChannelNotFoundException ex)
        {
            var message = Markup.Escape(ex.Message);
            InteractionService.DisplayError(message);
            return ExitCodeConstants.FailedToUpgradeProject;
        }
        catch (ProjectLocatorException ex)
        {
            // Check if this is a "no project found" error and prompt for self-update
            if (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
            {
                // Only prompt for self-update if not running as dotnet tool and downloader is available
                if (_cliDownloader is not null)
                {
                    var shouldUpdateCli = await InteractionService.ConfirmAsync(
                        UpdateCommandStrings.NoAppHostFoundUpdateCliPrompt,
                        defaultValue: true,
                        cancellationToken);
                    
                    if (shouldUpdateCli)
                    {
                        return await ExecuteSelfUpdateAsync(parseResult, cancellationToken);
                    }
                }
            }
            
            return HandleProjectLocatorException(ex, InteractionService);
        }
        catch (OperationCanceledException)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.FailedToUpgradeProject;
        }

        return 0;
    }

    private async Task<int> ExecuteSelfUpdateAsync(ParseResult parseResult, CancellationToken cancellationToken, string? selectedChannel = null)
    {
        var channel = selectedChannel ?? parseResult.GetValue<string?>("--channel") ?? parseResult.GetValue<string?>("--quality");

        // If channel is not specified, always prompt the user to select one.
        // This ensures they consciously choose a channel that will be saved to global settings
        // for future 'aspire new' and 'aspire init' commands.
        if (string.IsNullOrEmpty(channel))
        {
            var channels = new[] { PackageChannelNames.Stable, PackageChannelNames.Staging, PackageChannelNames.Daily };
            channel = await InteractionService.PromptForSelectionAsync(
                "Select the channel to update to:",
                channels,
                q => q,
                cancellationToken);
        }

        try
        {
            // Get current executable path for display purposes only
            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                InteractionService.DisplayError("Unable to determine the current executable path.");
                return ExitCodeConstants.InvalidCommand;
            }

            InteractionService.DisplayMessage("package", $"Current CLI location: {currentExePath}");
            InteractionService.DisplayMessage("up_arrow", $"Updating to channel: {channel}");

            // Download the latest CLI
            InteractionService.DisplayMessage("package", "Downloading...");
            var archivePath = await _cliDownloader!.DownloadLatestCliAsync(channel, cancellationToken);

            // Install using shared installer - clean up old backups for explicit update
            InteractionService.DisplayMessage("wrench", "Installing...");
            var result = await _cliInstaller!.InstallFromArchiveAsync(archivePath, cleanupBackups: true, cancellationToken);

            if (!result.Success)
            {
                InteractionService.DisplayError(result.ErrorMessage ?? "Installation failed.");
                return ExitCodeConstants.InvalidCommand;
            }

            InteractionService.DisplaySuccess($"Updated to version: {result.Version}");

            // Clean up downloaded archive
            try
            {
                var archiveDir = Path.GetDirectoryName(archivePath);
                if (archiveDir is not null && Directory.Exists(archiveDir))
                {
                    Directory.Delete(archiveDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Save the selected channel to global settings for future use with 'aspire new' and 'aspire init'
            // For stable channel, clear the setting to leave it blank (like the install scripts do)
            // For other channels (staging, daily), save the channel name
            if (string.Equals(channel, PackageChannelNames.Stable, StringComparison.OrdinalIgnoreCase))
            {
                await _configurationService.DeleteConfigurationAsync("channel", isGlobal: true, cancellationToken);
                _logger.LogDebug("Cleared global channel setting for stable channel");
            }
            else
            {
                await _configurationService.SetConfigurationAsync("channel", channel, isGlobal: true, cancellationToken);
                _logger.LogDebug("Saved global channel setting: {Channel}", channel);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.InvalidCommand;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update CLI");
            InteractionService.DisplayError($"Failed to update CLI: {ex.Message}");
            return ExitCodeConstants.InvalidCommand;
        }
    }
}
