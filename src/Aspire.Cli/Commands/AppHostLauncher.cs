// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Processes;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Encapsulates the logic for launching an AppHost in detached (background) mode.
/// Used by both RunCommand (--detach) and StartCommand (no resource).
/// When adding new launch options, add them here and wire them in both commands.
/// </summary>
internal sealed class AppHostLauncher(
    IProjectLocator projectLocator,
    CliExecutionContext executionContext,
    IFeatures features,
    IInteractionService interactionService,
    IAnsiConsole ansiConsole,
    IAuxiliaryBackchannelMonitor backchannelMonitor,
    ILogger<AppHostLauncher> logger,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Shared option for output format (JSON or table) in detached AppHost mode.
    /// </summary>
    internal static readonly Option<OutputFormat?> s_formatOption = new("--format")
    {
        Description = RunCommandStrings.JsonArgumentDescription
    };

    /// <summary>
    /// Shared option for isolated AppHost mode.
    /// </summary>
    internal static readonly Option<bool> s_isolatedOption = new("--isolated")
    {
        Description = RunCommandStrings.IsolatedArgumentDescription
    };

    /// <summary>
    /// Adds the detached launch options to a command so they appear in --help.
    /// Called by both RunCommand and StartCommand to keep options in sync.
    /// </summary>
    internal static void AddLaunchOptions(Command command)
    {
        command.Options.Add(s_formatOption);
        command.Options.Add(s_isolatedOption);
    }

    /// <summary>
    /// Launches an AppHost in detached mode, waits for the backchannel, and displays the result.
    /// </summary>
    /// <param name="passedAppHostProjectFile">The project file passed via --project, or null to auto-discover.</param>
    /// <param name="format">The output format (JSON or table).</param>
    /// <param name="isolated">Whether to run in isolated mode.</param>
    /// <param name="isExtensionHost">Whether running inside VS Code extension.</param>
    /// <param name="globalArgs">Global CLI args to forward to child process.</param>
    /// <param name="additionalArgs">Additional unmatched args to forward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    public async Task<int> LaunchDetachedAsync(
        FileInfo? passedAppHostProjectFile,
        OutputFormat? format,
        bool isolated,
        bool isExtensionHost,
        IEnumerable<string> globalArgs,
        IEnumerable<string> additionalArgs,
        CancellationToken cancellationToken)
    {
        // Route human-readable output to stderr when JSON is requested.
        if (format == OutputFormat.Json)
        {
            interactionService.Console = ConsoleOutput.Error;
        }

        // Avoid interactive project prompts in JSON mode to keep stdout parseable.
        var multipleAppHostBehavior = format == OutputFormat.Json
            ? MultipleAppHostProjectsFoundBehavior.Throw
            : MultipleAppHostProjectsFoundBehavior.Prompt;

        // Failure mode 1: Project not found
        var searchResult = await projectLocator.UseOrFindAppHostProjectFileAsync(
            passedAppHostProjectFile,
            multipleAppHostBehavior,
            createSettingsFile: false,
            cancellationToken);

        var effectiveAppHostFile = searchResult.SelectedProjectFile;

        if (effectiveAppHostFile is null)
        {
            return ExitCodeConstants.FailedToFindProject;
        }

        logger.LogDebug("Starting AppHost in background: {AppHostPath}", effectiveAppHostFile.FullName);

        // Compute the expected auxiliary socket path prefix for this AppHost.
        // The hash identifies the AppHost (from project path), while the PID makes each instance unique.
        // Multiple instances of the same AppHost will have the same hash but different PIDs.
        var expectedSocketPrefix = AppHostHelper.ComputeAuxiliarySocketPrefix(
            effectiveAppHostFile.FullName,
            executionContext.HomeDirectory.FullName);
        // We know the format is valid since we just computed it with ComputeAuxiliarySocketPrefix
        var expectedHash = AppHostHelper.ExtractHashFromSocketPath(expectedSocketPrefix)!;

        logger.LogDebug("Waiting for socket with prefix: {SocketPrefix}, Hash: {Hash}", expectedSocketPrefix, expectedHash);

        // Check for running instance and stop it if found (same behavior as regular run)
        var runningInstanceDetectionEnabled = features.IsFeatureEnabled(KnownFeatures.RunningInstanceDetectionEnabled, defaultValue: true);
        var existingSockets = AppHostHelper.FindMatchingSockets(
            effectiveAppHostFile.FullName,
            executionContext.HomeDirectory.FullName);

        if (runningInstanceDetectionEnabled && existingSockets.Length > 0)
        {
            logger.LogDebug("Found {Count} running instance(s) for this AppHost, stopping them first", existingSockets.Length);
            var manager = new RunningInstanceManager(logger, interactionService, _timeProvider);
            // Stop all running instances in parallel - don't block on failures
            var stopTasks = existingSockets.Select(socket =>
                manager.StopRunningInstanceAsync(socket, cancellationToken));
            await Task.WhenAll(stopTasks).ConfigureAwait(false);
        }

        // Build the arguments for the child CLI process
        var childLogFile = GenerateChildLogFilePath(executionContext.LogsDirectory.FullName, _timeProvider);

        var args = new List<string>
        {
            "run",
            "--non-interactive",
            "--project",
            effectiveAppHostFile.FullName,
            "--log-file",
            childLogFile
        };

        // Pass through global options that should be forwarded to child CLI
        args.AddRange(globalArgs);

        // Pass through run-specific options
        if (isolated)
        {
            args.Add("--isolated");
        }

        // Pass through any additional args
        foreach (var token in additionalArgs)
        {
            args.Add(token);
        }

        // Get the path to the current executable
        // When running as `dotnet aspire.dll`, Environment.ProcessPath returns dotnet.exe,
        // so we need to also pass the entry assembly (aspire.dll) as the first argument.
        // When running native AOT, ProcessPath IS the native executable.
        var dotnetPath = Environment.ProcessPath ?? "dotnet";
        var isDotnetHost = dotnetPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase) ||
                           dotnetPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);

        // For single-file apps, Assembly.Location is empty. Use command-line args instead.
        // args[0] when running `dotnet aspire.dll` is the dll path
        var entryAssemblyPath = Environment.GetCommandLineArgs().FirstOrDefault();

        // Build the full argument list for the child process, including the entry assembly
        // path when running via `dotnet aspire.dll`.
        var childArgs = new List<string>();
        if (isDotnetHost && !string.IsNullOrEmpty(entryAssemblyPath) && entryAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            childArgs.Add(entryAssemblyPath);
        }

        childArgs.AddRange(args);

        logger.LogDebug("Spawning child CLI: {Executable} (isDotnetHost={IsDotnetHost}) with args: {Args}",
            dotnetPath, isDotnetHost, string.Join(" ", childArgs));
        logger.LogDebug("Working directory: {WorkingDirectory}", executionContext.WorkingDirectory.FullName);

        // Start the child process and wait for the backchannel in a single status spinner
        Process? childProcess = null;
        var childExitedEarly = false;
        var childExitCode = 0;

        async Task<IAppHostAuxiliaryBackchannel?> StartAndWaitForBackchannelAsync()
        {
            // Failure mode 2: Failed to spawn child process
            try
            {
                childProcess = DetachedProcessLauncher.Start(
                    dotnetPath,
                    childArgs,
                    executionContext.WorkingDirectory.FullName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start child CLI process");
                return null;
            }

            logger.LogDebug("Child CLI process started with PID: {PID}", childProcess.Id);

            // Failure modes 3 & 4: Wait for the auxiliary backchannel to become available
            // - Mode 3: Child exits early (build failure, config error, etc.)
            // - Mode 4: Timeout waiting for backchannel (120 seconds)
            var startTime = _timeProvider.GetUtcNow();
            var timeout = TimeSpan.FromSeconds(120);

            while (_timeProvider.GetUtcNow() - startTime < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Failure mode 3: Child process exited early
                if (childProcess.HasExited)
                {
                    childExitedEarly = true;
                    childExitCode = childProcess.ExitCode;
                    logger.LogWarning("Child CLI process exited with code {ExitCode}", childExitCode);
                    return null;
                }

                // Trigger a scan and try to connect
                await backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);

                // Check if we can find a connection for this AppHost by hash
                var connection = backchannelMonitor.GetConnectionsByHash(expectedHash).FirstOrDefault();
                if (connection is not null)
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
        }

        // For JSON output, skip the status spinner to avoid contaminating stdout
        IAppHostAuxiliaryBackchannel? backchannel;
        if (format == OutputFormat.Json)
        {
            backchannel = await StartAndWaitForBackchannelAsync();
        }
        else
        {
            backchannel = await interactionService.ShowStatusAsync(
                RunCommandStrings.StartingAppHostInBackground,
                StartAndWaitForBackchannelAsync);
        }

        // Handle failure cases - show specific error and log file path
        if (backchannel is null || childProcess is null)
        {
            if (childProcess is null)
            {
                interactionService.DisplayError(RunCommandStrings.FailedToStartAppHost);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            if (childExitedEarly)
            {
                interactionService.DisplayError(GetDetachedFailureMessage(childExitCode));
            }
            else
            {
                interactionService.DisplayError(RunCommandStrings.TimeoutWaitingForAppHost);

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
            interactionService.DisplayMessage("magnifying_glass_tilted_right", string.Format(
                CultureInfo.CurrentCulture,
                RunCommandStrings.CheckLogsForDetails,
                childLogFile));

            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        var appHostInfo = backchannel.AppHostInfo;

        // Get the dashboard URLs
        var dashboardUrls = await backchannel.GetDashboardUrlsAsync(cancellationToken).ConfigureAwait(false);

        var pid = appHostInfo?.ProcessId ?? childProcess.Id;

        if (format == OutputFormat.Json)
        {
            // Output structured JSON for programmatic consumption
            var result = new DetachOutputInfo(
                effectiveAppHostFile.FullName,
                pid,
                childProcess.Id,
                dashboardUrls?.BaseUrlWithLoginToken,
                childLogFile);
            var json = JsonSerializer.Serialize(result, RunCommandJsonContext.RelaxedEscaping.DetachOutputInfo);
            interactionService.DisplayRawText(json, ConsoleOutput.Standard);
        }
        else
        {
            // Display success UX using shared rendering
            var appHostRelativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, effectiveAppHostFile.FullName);
            RunCommand.RenderAppHostSummary(
                ansiConsole,
                appHostRelativePath,
                dashboardUrls?.BaseUrlWithLoginToken,
                codespacesUrl: null,
                childLogFile,
                isExtensionHost,
                pid);
            ansiConsole.WriteLine();

            interactionService.DisplaySuccess(RunCommandStrings.AppHostStartedSuccessfully);
        }

        return ExitCodeConstants.Success;
    }

    /// <summary>
    /// Creates a user-facing error message for detached child process failures.
    /// </summary>
    internal static string GetDetachedFailureMessage(int childExitCode)
    {
        return childExitCode switch
        {
            ExitCodeConstants.FailedToBuildArtifacts => RunCommandStrings.AppHostFailedToBuild,
            _ => string.Format(CultureInfo.CurrentCulture, RunCommandStrings.AppHostExitedWithCode, childExitCode)
        };
    }

    /// <summary>
    /// Generates a unique log file path for a detached child CLI process.
    /// </summary>
    internal static string GenerateChildLogFilePath(string logsDirectory, TimeProvider timeProvider)
    {
        var timestamp = timeProvider.GetUtcNow().ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var uniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var fileName = $"cli_{timestamp}_detach-child_{uniqueId}.log";
        return Path.Combine(logsDirectory, fileName);
    }
}
