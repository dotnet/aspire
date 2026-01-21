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

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");

        // Find the AppHost project
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
        var expectedHash = Path.GetFileName(expectedSocketPath).Replace("auxi.sock.", "", StringComparison.Ordinal);
        
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

        // Start the child process
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

        // Wait for the auxiliary backchannel to become available
        AppHostAuxiliaryBackchannel? backchannel = null;
        var startTime = _timeProvider.GetUtcNow();
        var timeout = TimeSpan.FromSeconds(120);

        backchannel = await _interactionService.ShowStatusAsync(
            StartCommandStrings.WaitingForAppHostToStart,
            async () =>
            {
                while (_timeProvider.GetUtcNow() - startTime < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check if the child process has exited unexpectedly
                    if (childProcess.HasExited)
                    {
                        _logger.LogWarning("Child CLI process exited with code {ExitCode}", childProcess.ExitCode);
                        return null;
                    }

                    // Trigger a scan and try to connect
                    await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);

                    // Check if we can find a connection for this AppHost (keyed by hash)
                    if (_backchannelMonitor.Connections.TryGetValue(expectedHash, out var connection))
                    {
                        return connection;
                    }

                    // Wait a bit before trying again
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }

                return null;
            });

        if (backchannel is null)
        {
            _interactionService.DisplayError(StartCommandStrings.FailedToStartAppHost);

            // Try to kill the child process if it's still running
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
