// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
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

        AppHostAuxiliaryBackchannel? selectedConnection = null;

        // Fast path: If --project was specified, check directly for its socket
        if (passedAppHostProjectFile is not null)
        {
            var targetPath = passedAppHostProjectFile.FullName;
            var expectedSocketPath = AppHostHelper.ComputeAuxiliarySocketPath(
                targetPath,
                ExecutionContext.HomeDirectory.FullName);

            if (File.Exists(expectedSocketPath))
            {
                try
                {
                    selectedConnection = await AppHostAuxiliaryBackchannel.ConnectAsync(
                        expectedSocketPath, _logger, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to connect to socket at {SocketPath}", expectedSocketPath);
                }
            }

            if (selectedConnection is null)
            {
                _interactionService.DisplayError(StopCommandStrings.NoRunningAppHostsFound);
                return ExitCodeConstants.FailedToFindProject;
            }
        }
        else
        {
            // Fast path: Try to find AppHost in current directory and check its socket directly
            var searchResult = await _projectLocator.UseOrFindAppHostProjectFileAsync(
                null,
                MultipleAppHostProjectsFoundBehavior.None,
                createSettingsFile: false,
                cancellationToken);

            if (searchResult.SelectedProjectFile is not null)
            {
                var expectedSocketPath = AppHostHelper.ComputeAuxiliarySocketPath(
                    searchResult.SelectedProjectFile.FullName,
                    ExecutionContext.HomeDirectory.FullName);

                if (File.Exists(expectedSocketPath))
                {
                    try
                    {
                        selectedConnection = await AppHostAuxiliaryBackchannel.ConnectAsync(
                            expectedSocketPath, _logger, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to connect to socket at {SocketPath}", expectedSocketPath);
                    }
                }
            }

            // Slow path: If fast path didn't find anything, do a full scan
            if (selectedConnection is null)
            {
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

                if (inScopeConnections.Count == 1)
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
            }
        }

        // Stop the selected AppHost
        var appHostPath = selectedConnection.AppHostInfo?.AppHostPath ?? "Unknown";
        // Use relative path for in-scope, full path for out-of-scope
        var workingDir = ExecutionContext.WorkingDirectory.FullName;
        var isInScope = appHostPath.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase);
        var displayPath = isInScope ? Path.GetRelativePath(workingDir, appHostPath) : appHostPath;
        _interactionService.DisplayMessage("package", $"Found running AppHost: {displayPath}");
        _logger.LogDebug("Stopping AppHost: {AppHostPath}", appHostPath);

        var appHostInfo = selectedConnection.AppHostInfo;

        _interactionService.DisplayMessage("stop_sign", "Sending stop signal...");

        var rpcSucceeded = false;
        try
        {
            rpcSucceeded = await selectedConnection.StopAppHostAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send stop signal via RPC");
        }

        // If RPC didn't work (older AppHost), try sending SIGINT directly
        if (!rpcSucceeded && appHostInfo?.ProcessId is int pid)
        {
            _logger.LogDebug("RPC stop not available, sending SIGINT to PID {Pid}", pid);
            try
            {
                SendStopSignal(pid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send stop signal to process {Pid}", pid);
                _interactionService.DisplayError(StopCommandStrings.FailedToStopAppHost);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }
        }
        else if (!rpcSucceeded)
        {
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

    /// <summary>
    /// Sends a stop signal (SIGINT on Unix, CTRL+C on Windows) to a process.
    /// </summary>
    private static void SendStopSignal(int pid)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, we can't easily send CTRL+C to another process
            // Fall back to killing the process
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
        }
        else
        {
            // On Unix, send SIGINT (signal 2) which is equivalent to CTRL+C
            using var killProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "kill",
                ArgumentList = { "-2", pid.ToString(CultureInfo.InvariantCulture) },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            killProcess?.WaitForExit(5000);
        }
    }
}
