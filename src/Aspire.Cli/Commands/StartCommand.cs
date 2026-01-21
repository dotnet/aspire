// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class StartCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IInteractionService _interactionService;
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly IAnsiConsole _ansiConsole;
    private readonly ILogger<StartCommand> _logger;
    private readonly TimeProvider _timeProvider;

    public StartCommand(
        IProjectLocator projectLocator,
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IAnsiConsole ansiConsole,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<StartCommand> logger,
        TimeProvider? timeProvider = null)
        : base("start", StartCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(backchannelMonitor);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(logger);

        _projectLocator = projectLocator;
        _interactionService = interactionService;
        _backchannelMonitor = backchannelMonitor;
        _ansiConsole = ansiConsole;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = StartCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        TreatUnmatchedTokensAsErrors = false;
    }

    /// <summary>
    /// Starts an AppHost in the background by spawning a child CLI process running 'aspire run --non-interactive'.
    /// The parent waits for the auxiliary backchannel to become available, displays a summary, then exits
    /// while the child continues running.
    /// </summary>
    /// <remarks>
    /// <para><b>Failure Modes:</b></para>
    /// <list type="number">
    /// <item><b>Project not found</b>: No AppHost project found in the current directory or specified path.
    /// Returns <see cref="ExitCodeConstants.FailedToFindProject"/>.</item>
    /// <item><b>Failed to spawn child process</b>: Process.Start fails (e.g., executable not found).
    /// Returns <see cref="ExitCodeConstants.FailedToDotnetRunAppHost"/>.</item>
    /// <item><b>Child process exits early</b>: The child 'aspire run' process exits before the backchannel
    /// is established (e.g., build failure, configuration error). Detected via WaitForExitAsync racing
    /// with the poll delay. Shows exit code and log file path.
    /// Returns <see cref="ExitCodeConstants.FailedToDotnetRunAppHost"/>.</item>
    /// <item><b>Timeout waiting for backchannel</b>: The auxiliary backchannel socket doesn't appear
    /// within 120 seconds. The child process is killed. Shows timeout message and log file path.
    /// Returns <see cref="ExitCodeConstants.FailedToDotnetRunAppHost"/>.</item>
    /// </list>
    /// <para>On any failure, the log file path is displayed so the user can investigate.</para>
    /// </remarks>
    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");

        // Failure mode 1: Project not found
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

        _logger.LogDebug("Starting AppHost in background: {AppHostPath}", effectiveAppHostFile.FullName);

        // Compute the expected auxiliary socket path hash for this AppHost
        // The Connections dictionary is keyed by hash, not the full path
        var expectedSocketPath = AppHostHelper.ComputeAuxiliarySocketPath(
            effectiveAppHostFile.FullName,
            ExecutionContext.HomeDirectory.FullName);
        var expectedHash = AppHostHelper.ExtractHashFromSocketPath(expectedSocketPath);
        
        _logger.LogDebug("Waiting for socket: {SocketPath}, Hash: {Hash}", expectedSocketPath, expectedHash);

        // Build the arguments for the child CLI process
        var args = new List<string>
        {
            "run",
            "--non-interactive",
            "--project",
            effectiveAppHostFile.FullName
        };

        // Pass through any unmatched tokens
        args.AddRange(parseResult.UnmatchedTokens);

        // Get the path to the current executable
        var cliExecutable = Environment.ProcessPath ?? "aspire";

        _logger.LogDebug("Spawning child CLI: {Executable} with args: {Args}", cliExecutable, string.Join(" ", args));
        _interactionService.DisplayMessage("rocket", StartCommandStrings.StartingAppHostInBackground);

        // Failure mode 2: Failed to spawn child process
        var startInfo = new ProcessStartInfo
        {
            FileName = cliExecutable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
            WorkingDirectory = effectiveAppHostFile.Directory?.FullName ?? ExecutionContext.WorkingDirectory.FullName
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Process? childProcess;
        try
        {
            childProcess = Process.Start(startInfo);
            if (childProcess is null)
            {
                _interactionService.DisplayError(StartCommandStrings.FailedToStartAppHost);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start child CLI process");
            _interactionService.DisplayError(StartCommandStrings.FailedToStartAppHost);
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        _logger.LogDebug("Child CLI process started with PID: {PID}", childProcess.Id);

        // Failure modes 3 & 4: Wait for the auxiliary backchannel to become available
        // - Mode 3: Child exits early (build failure, config error, etc.)
        // - Mode 4: Timeout waiting for backchannel (120 seconds)
        AppHostAuxiliaryBackchannel? backchannel = null;
        var startTime = _timeProvider.GetUtcNow();
        var timeout = TimeSpan.FromSeconds(120);
        var childExitedEarly = false;
        var childExitCode = 0;

        backchannel = await _interactionService.ShowStatusAsync(
            StartCommandStrings.WaitingForAppHostToStart,
            async () =>
            {
                while (_timeProvider.GetUtcNow() - startTime < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Failure mode 3: Child process exited early
                    if (childProcess.HasExited)
                    {
                        childExitedEarly = true;
                        childExitCode = childProcess.ExitCode;
                        _logger.LogWarning("Child CLI process exited with code {ExitCode}", childExitCode);
                        return null;
                    }

                    // Trigger a scan and try to connect
                    await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);

                    // Check if we can find a connection for this AppHost (keyed by hash)
                    if (_backchannelMonitor.Connections.TryGetValue(expectedHash, out var connection))
                    {
                        return connection;
                    }

                    // Wait a bit before trying again, but short-circuit if the child process exits
                    try
                    {
                        await childProcess.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                        // If we get here, the process exited - we'll catch it at the top of the next iteration
                    }
                    catch (TimeoutException)
                    {
                        // Expected - the 500ms delay elapsed without the process exiting
                    }
                }

                // Failure mode 4: Timeout - loop exited without finding connection
                return null;
            });

        // Handle failure cases - show specific error and log file path
        if (backchannel is null)
        {
            // Compute the expected log file path for error message
            var expectedLogFile = AppHostHelper.GetLogFilePath(
                childProcess.Id,
                ExecutionContext.HomeDirectory.FullName,
                _timeProvider);

            if (childExitedEarly)
            {
                _interactionService.DisplayError(string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    StartCommandStrings.AppHostExitedWithCode,
                    childExitCode));
            }
            else
            {
                _interactionService.DisplayError(StartCommandStrings.TimeoutWaitingForAppHost);

                // Try to kill the child process if it's still running (timeout case)
                if (!childProcess.HasExited)
                {
                    try
                    {
                        childProcess.Kill();
                    }
                    catch
                    {
                        // Ignore errors when killing
                    }
                }
            }

            // Always show log file path for troubleshooting
            _interactionService.DisplayMessage("magnifying_glass_tilted_right", string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                StartCommandStrings.CheckLogsForDetails,
                expectedLogFile.FullName));

            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        var appHostInfo = backchannel.AppHostInfo;

        // Get the dashboard URLs
        var dashboardUrls = await backchannel.GetDashboardUrlsAsync(cancellationToken).ConfigureAwait(false);

        // Get the log file path
        var logFile = AppHostHelper.GetLogFilePath(
            appHostInfo?.ProcessId ?? childProcess.Id,
            ExecutionContext.HomeDirectory.FullName,
            _timeProvider);

        // Display success UX using shared rendering
        var appHostRelativePath = Path.GetRelativePath(ExecutionContext.WorkingDirectory.FullName, effectiveAppHostFile.FullName);
        var pid = appHostInfo?.ProcessId ?? childProcess.Id;
        RunCommand.RenderAppHostSummary(
            _ansiConsole,
            appHostRelativePath,
            dashboardUrls?.BaseUrlWithLoginToken,
            codespacesUrl: null,
            logFile.FullName,
            pid);
        _ansiConsole.WriteLine();

        _interactionService.DisplaySuccess(StartCommandStrings.AppHostStartedSuccessfully);

        return ExitCodeConstants.Success;
    }
}
