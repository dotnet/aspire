// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class StopCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IInteractionService _interactionService;
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly ILogger<StopCommand> _logger;
    private readonly TimeProvider _timeProvider;

    public StopCommand(
        IProjectLocator projectLocator,
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<StopCommand> logger,
        TimeProvider? timeProvider = null)
        : base("stop", StopCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(backchannelMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _projectLocator = projectLocator;
        _interactionService = interactionService;
        _backchannelMonitor = backchannelMonitor;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = StopCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");

        // Scan for running AppHosts with status spinner
        var connections = await _interactionService.ShowStatusAsync(
            StopCommandStrings.ScanningForRunningAppHosts,
            async () =>
            {
                await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);
                return _backchannelMonitor.Connections.Values.ToList();
            });

        if (connections.Count == 0)
        {
            _interactionService.DisplayError(StopCommandStrings.NoRunningAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }

        // Filter to in-scope AppHosts (within working directory)
        var workingDirectory = ExecutionContext.WorkingDirectory.FullName;
        var inScopeConnections = connections
            .Where(c => c.AppHostInfo?.AppHostPath is not null &&
                        Path.GetFullPath(c.AppHostInfo.AppHostPath).StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var outOfScopeConnections = connections
            .Where(c => c.AppHostInfo?.AppHostPath is not null &&
                        !Path.GetFullPath(c.AppHostInfo.AppHostPath).StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
            .ToList();

        AppHostAuxiliaryBackchannel? selectedConnection = null;

        // If --project was specified, find the matching connection
        if (passedAppHostProjectFile is not null)
        {
            var targetPath = passedAppHostProjectFile.FullName;
            var expectedSocketPath = AppHostHelper.ComputeAuxiliarySocketPath(
                targetPath,
                ExecutionContext.HomeDirectory.FullName);
            // We know the format is valid since we just computed it with ComputeAuxiliarySocketPath
            var expectedHash = AppHostHelper.ExtractHashFromSocketPath(expectedSocketPath)!;

            if (_backchannelMonitor.Connections.TryGetValue(expectedHash, out var connection))
            {
                selectedConnection = connection;
            }
            else
            {
                _interactionService.DisplayError(StopCommandStrings.NoRunningAppHostsFound);
                return ExitCodeConstants.FailedToFindProject;
            }
        }
        else if (inScopeConnections.Count == 1)
        {
            // Only one in-scope AppHost, use it
            selectedConnection = inScopeConnections[0];
        }
        else if (inScopeConnections.Count > 1)
        {
            // Multiple in-scope AppHosts running, prompt for selection
            var choices = inScopeConnections
                .Select(c =>
                {
                    var appHostPath = c.AppHostInfo?.AppHostPath ?? "Unknown";
                    var relativePath = Path.GetRelativePath(workingDirectory, appHostPath);
                    return (Display: relativePath, Connection: c);
                })
                .ToList();

            var selectedDisplay = await _interactionService.PromptForSelectionAsync(
                StopCommandStrings.SelectAppHostToStop,
                choices.Select(c => c.Display).ToArray(),
                c => c,
                cancellationToken);

            selectedConnection = choices.FirstOrDefault(c => c.Display == selectedDisplay).Connection;

            if (selectedConnection is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }
        }
        else if (outOfScopeConnections.Count > 0)
        {
            // No in-scope AppHosts, but there are out-of-scope ones - let user pick
            _interactionService.DisplayMessage("information", StopCommandStrings.NoInScopeAppHostsShowingAll);

            var choices = outOfScopeConnections
                .Select(c =>
                {
                    var path = c.AppHostInfo?.AppHostPath ?? "Unknown";
                    return (Display: path, Connection: c);
                })
                .ToList();

            var selectedDisplay = await _interactionService.PromptForSelectionAsync(
                StopCommandStrings.SelectAppHostToStop,
                choices.Select(c => c.Display).ToArray(),
                c => c,
                cancellationToken);

            selectedConnection = choices.FirstOrDefault(c => c.Display == selectedDisplay).Connection;

            if (selectedConnection is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }
        }
        else
        {
            _interactionService.DisplayError(StopCommandStrings.NoRunningAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }

        // Stop the selected AppHost
        var appHostPath = selectedConnection.AppHostInfo?.AppHostPath ?? "Unknown";
        // Use relative path for in-scope, full path for out-of-scope
        var isInScope = appHostPath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase);
        var displayPath = isInScope ? Path.GetRelativePath(workingDirectory, appHostPath) : appHostPath;
        _interactionService.DisplayMessage("package", $"Found running AppHost: {displayPath}");
        _logger.LogDebug("Stopping AppHost: {AppHostPath}", appHostPath);

        var appHostInfo = selectedConnection.AppHostInfo;

        _interactionService.DisplayMessage("stop_sign", "Sending stop signal...");

        try
        {
            await selectedConnection.StopAppHostAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send stop signal");
            _interactionService.DisplayError(StopCommandStrings.FailedToStopAppHost);
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        var stopped = await _interactionService.ShowStatusAsync(
            StopCommandStrings.StoppingAppHost,
            async () =>
            {
                try
                {
                    // Wait for processes to terminate
                    var manager = new RunningInstanceManager(_logger, _interactionService, _timeProvider);

                    if (appHostInfo is not null)
                    {
                        return await manager.MonitorProcessesForTerminationAsync(appHostInfo, cancellationToken).ConfigureAwait(false);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed while waiting for AppHost to stop");
                    return false;
                }
            });

        // Reset cursor position after spinner
        _interactionService.DisplayPlainText("");

        if (stopped)
        {
            _interactionService.DisplaySuccess(StopCommandStrings.AppHostStoppedSuccessfully);
            return ExitCodeConstants.Success;
        }
        else
        {
            _interactionService.DisplayError(StopCommandStrings.FailedToStopAppHost);
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }
}
