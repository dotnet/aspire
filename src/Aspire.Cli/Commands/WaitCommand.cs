// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class WaitCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly ILogger<WaitCommand> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly Argument<string> s_resourceArgument = new("resource")
    {
        Description = WaitCommandStrings.ResourceArgumentDescription
    };

    private static readonly Option<string> s_statusOption = new("--status")
    {
        Description = WaitCommandStrings.StatusOptionDescription,
        DefaultValueFactory = _ => "healthy"
    };

    private static readonly Option<int> s_timeoutOption = new("--timeout")
    {
        Description = WaitCommandStrings.TimeoutOptionDescription,
        DefaultValueFactory = _ => 120
    };

    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = WaitCommandStrings.ProjectOptionDescription
    };

    // Terminal states where the resource has stopped running
    private static readonly string[] s_terminalStates = ["Finished", "Exited", "FailedToStart"];

    // Failed states that indicate the resource won't recover
    private static readonly string[] s_failedStates = ["FailedToStart", "RuntimeUnhealthy"];

    public WaitCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<WaitCommand> logger,
        AspireCliTelemetry telemetry,
        TimeProvider? timeProvider = null)
        : base("wait", WaitCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        Arguments.Add(s_resourceArgument);
        Options.Add(s_statusOption);
        Options.Add(s_timeoutOption);
        Options.Add(s_projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var resourceName = parseResult.GetValue(s_resourceArgument)!;
        var status = parseResult.GetValue(s_statusOption)!.ToLowerInvariant();
        var timeoutSeconds = parseResult.GetValue(s_timeoutOption);
        var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);

        // Validate status value
        if (!IsValidStatus(status))
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.InvalidStatusValue, status));
            return ExitCodeConstants.InvalidCommand;
        }

        // Validate timeout
        if (timeoutSeconds <= 0)
        {
            _interactionService.DisplayError(WaitCommandStrings.TimeoutMustBePositive);
            return ExitCodeConstants.InvalidCommand;
        }

        // Resolve connection to a running AppHost
        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            WaitCommandStrings.ScanningForRunningAppHosts,
            WaitCommandStrings.SelectAppHost,
            WaitCommandStrings.NoInScopeAppHostsShowingAll,
            WaitCommandStrings.NoRunningAppHostsFound,
            cancellationToken);

        if (!result.Success)
        {
            _interactionService.DisplayError(result.ErrorMessage ?? WaitCommandStrings.NoRunningAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }

        var connection = result.Connection!;

        return await WaitForResourceAsync(connection, resourceName, status, timeoutSeconds, cancellationToken);
    }

    private async Task<int> WaitForResourceAsync(
        IAppHostAuxiliaryBackchannel connection,
        string resourceName,
        string status,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var statusLabel = GetStatusLabel(status);

        _logger.LogDebug("Waiting for resource '{ResourceName}' to reach status '{Status}' with timeout {Timeout}s", resourceName, status, timeoutSeconds);

        // Verify the resource exists before starting the wait loop
        var initialSnapshots = await connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);
        if (!initialSnapshots.Any(s => string.Equals(s.Name, resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.ResourceNotFound, resourceName));
            return ExitCodeConstants.WaitResourceFailed;
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds), _timeProvider);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var startTimestamp = _timeProvider.GetTimestamp();

        var exitCode = await _interactionService.ShowStatusAsync(
            string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.WaitingForResource, resourceName, statusLabel),
            async () =>
            {
                try
                {
                    await foreach (var snapshot in connection.WatchResourceSnapshotsAsync(linkedCts.Token).ConfigureAwait(false))
                    {
                        // Only process snapshots for the target resource
                        if (!string.Equals(snapshot.Name, resourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        _logger.LogDebug("Resource '{ResourceName}' state: {State}, health: {HealthStatus}", resourceName, snapshot.State, snapshot.HealthStatus);

                        if (IsTargetStatusReached(snapshot, status))
                        {
                            return ExitCodeConstants.Success;
                        }

                        // When waiting for "healthy" or "up", check if the resource has entered a terminal failure state
                        if (status is not "down" && IsFailedState(snapshot))
                        {
                            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.ResourceEnteredFailedState, resourceName, snapshot.State));
                            return ExitCodeConstants.WaitResourceFailed;
                        }

                        // When waiting for "healthy" or "up", check if the resource exited
                        if (status is not "down" && IsTerminalState(snapshot))
                        {
                            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.ResourceEnteredFailedState, resourceName, snapshot.State));
                            return ExitCodeConstants.WaitResourceFailed;
                        }
                    }

                    // Stream ended without reaching target status
                    return ExitCodeConstants.WaitTimeout;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.WaitTimedOut, resourceName, statusLabel, timeoutSeconds));
                    return ExitCodeConstants.WaitTimeout;
                }
            });

        // Reset cursor position after spinner
        _interactionService.DisplayPlainText("");

        if (exitCode == ExitCodeConstants.Success)
        {
            var elapsed = _timeProvider.GetElapsedTime(startTimestamp);
            _interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, WaitCommandStrings.ResourceReachedTargetStatus, resourceName, statusLabel, elapsed.TotalSeconds));
        }

        return exitCode;
    }

    private static bool IsTargetStatusReached(ResourceSnapshot snapshot, string status)
    {
        return status switch
        {
            "up" => string.Equals(snapshot.State, "Running", StringComparison.OrdinalIgnoreCase),
            "healthy" => string.Equals(snapshot.State, "Running", StringComparison.OrdinalIgnoreCase)
                         && IsHealthy(snapshot),
            "down" => IsTerminalState(snapshot),
            _ => false
        };
    }

    private static bool IsHealthy(ResourceSnapshot snapshot)
    {
        // If health reports exist, require an explicit "Healthy" status
        if (snapshot.HealthReports.Length > 0)
        {
            return string.Equals(snapshot.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase);
        }

        // No health checks configured — treat as healthy when running
        return snapshot.HealthStatus is null;
    }

    private static bool IsTerminalState(ResourceSnapshot snapshot)
    {
        return s_terminalStates.Any(s => string.Equals(snapshot.State, s, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFailedState(ResourceSnapshot snapshot)
    {
        return s_failedStates.Any(s => string.Equals(snapshot.State, s, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidStatus(string status)
    {
        return status is "healthy" or "up" or "down";
    }

    private static string GetStatusLabel(string status)
    {
        return status switch
        {
            "up" => "up (running)",
            "healthy" => "healthy",
            "down" => "down",
            _ => status
        };
    }
}
