// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class StopCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly ILogger<StopCommand> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly Argument<string?> s_resourceArgument = new("resource")
    {
        Description = "The name of the resource to stop. If not specified, stops the entire AppHost.",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = StopCommandStrings.ProjectArgumentDescription
    };

    public StopCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<StopCommand> logger,
        AspireCliTelemetry telemetry,
        TimeProvider? timeProvider = null)
        : base("stop", StopCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        Arguments.Add(s_resourceArgument);
        Options.Add(s_projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(s_resourceArgument);
        var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);

        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            StopCommandStrings.ScanningForRunningAppHosts,
            StopCommandStrings.SelectAppHostToStop,
            StopCommandStrings.NoInScopeAppHostsShowingAll,
            StopCommandStrings.NoRunningAppHostsFound,
            cancellationToken);

        if (!result.Success)
        {
            _interactionService.DisplayMessage("information", StopCommandStrings.NoRunningAppHostsFound);
            return ExitCodeConstants.Success;
        }

        var selectedConnection = result.Connection!;

        // If a resource name is provided, stop that specific resource instead of the AppHost
        if (!string.IsNullOrEmpty(resourceName))
        {
            return await StopResourceAsync(selectedConnection, resourceName, cancellationToken);
        }

        // Stop the selected AppHost
        var appHostPath = selectedConnection.AppHostInfo?.AppHostPath ?? "Unknown";
        // Use relative path for in-scope, full path for out-of-scope
        var displayPath = selectedConnection.IsInScope 
            ? Path.GetRelativePath(ExecutionContext.WorkingDirectory.FullName, appHostPath) 
            : appHostPath;
        _interactionService.DisplayMessage("package", $"Found running AppHost: {displayPath}");
        _logger.LogDebug("Stopping AppHost: {AppHostPath}", appHostPath);

        var appHostInfo = selectedConnection.AppHostInfo;

        _interactionService.DisplayMessage("stop_sign", "Sending stop signal...");

        // Get the CLI process ID - this is the process we need to kill
        // Killing the CLI process will tear down everything including the AppHost
        var cliProcessId = appHostInfo?.CliProcessId;

        if (cliProcessId is int cliPid)
        {
            _logger.LogDebug("Sending stop signal to CLI process (PID {Pid})", cliPid);
            try
            {
                SendStopSignal(cliPid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send stop signal to CLI process {Pid}", cliPid);
                _interactionService.DisplayError(StopCommandStrings.FailedToStopAppHost);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }
        }
        else
        {
            // Fallback: Try the RPC method if we don't have CLI process ID
            _logger.LogDebug("No CLI process ID available, trying RPC stop");
            var rpcSucceeded = false;
            try
            {
                rpcSucceeded = await selectedConnection.StopAppHostAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send stop signal via RPC");
            }

            // If RPC didn't work, try sending SIGINT to AppHost process directly
            if (!rpcSucceeded && appHostInfo?.ProcessId is int appHostPid)
            {
                _logger.LogDebug("RPC stop not available, sending SIGINT to AppHost PID {Pid}", appHostPid);
                try
                {
                    SendStopSignal(appHostPid);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send stop signal to process {Pid}", appHostPid);
                    _interactionService.DisplayError(StopCommandStrings.FailedToStopAppHost);
                    return ExitCodeConstants.FailedToDotnetRunAppHost;
                }
            }
            else if (!rpcSucceeded)
            {
                _interactionService.DisplayError(StopCommandStrings.FailedToStopAppHost);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }
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
    /// Sends a stop signal to a process to terminate it and its process tree.
    /// Uses Process.Kill(entireProcessTree: true) to ensure all child processes are terminated.
    /// </summary>
    private static void SendStopSignal(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            // Process doesn't exist - already terminated
        }
        catch (InvalidOperationException)
        {
            // Process has already exited
        }
        catch (Exception)
        {
            // Some other error (e.g., permission denied) - ignore
        }
    }

    /// <summary>
    /// Stops a specific resource instead of the entire AppHost.
    /// </summary>
    private Task<int> StopResourceAsync(IAppHostAuxiliaryBackchannel connection, string resourceName, CancellationToken cancellationToken)
    {
        return ResourceCommandHelper.ExecuteResourceCommandAsync(
            connection,
            _interactionService,
            _logger,
            resourceName,
            KnownResourceCommands.StopCommand,
            "Stopping",
            "stop",
            "stopped",
            cancellationToken);
    }
}
