// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;
using StreamJsonRpc;

namespace Aspire.Cli.Commands;

internal sealed class RunCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;
    private readonly AspireCliTelemetry _telemetry;

    public RunCommand(IDotNetCliRunner runner, IInteractionService interactionService, ICertificateService certificateService, IProjectLocator projectLocator, IAnsiConsole ansiConsole, AspireCliTelemetry telemetry)
        : base("run", RunCommandStrings.Description)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(telemetry);

        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _projectLocator = projectLocator;
        _ansiConsole = ansiConsole;
        _telemetry = telemetry;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = RunCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        var watchOption = new Option<bool>("--watch", "-w");
        watchOption.Description = RunCommandStrings.WatchArgumentDescription;
        Options.Add(watchOption);

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;
        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            var debug = parseResult.GetValue<bool>("--debug");

            var waitForDebugger = parseResult.GetValue<bool>("--wait-for-debugger");

            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);

            var watch = parseResult.GetValue<bool>("--watch");

            if (!watch)
            {
                var buildOptions = new DotNetCliRunnerInvocationOptions
                {
                    StandardOutputCallback = buildOutputCollector.AppendOutput,
                    StandardErrorCallback = buildOutputCollector.AppendError,
                };

                var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, _interactionService, effectiveAppHostProjectFile, buildOptions, cancellationToken);

                if (buildExitCode != 0)
                {
                    _interactionService.DisplayLines(buildOutputCollector.GetLines());
                    _interactionService.DisplayError(InteractionServiceStrings.ProjectCouldNotBeBuilt);
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, _telemetry, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
            };

            var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostProjectFile,
                watch,
                !watch,
                unmatchedTokens,
                env,
                backchannelCompletitionSource,
                runOptions,
                cancellationToken);

            var logFileNameCompletionSource = new TaskCompletionSource<FileInfo>();

            // Wait for the backchannel to be established.
            var backchannel = await _interactionService.ShowStatusAsync("Connecting to app host...", async () =>
            {
                return await backchannelCompletitionSource.Task.WaitAsync(cancellationToken);
            });

            var pendingLogCapture = Task.Run(async () =>
            {
                var logFile = GetAppHostLogFile();
                logFileNameCompletionSource.SetResult(logFile);

                await CaptureAppHostLogsAsync(logFile, backchannel, cancellationToken);
            }, cancellationToken);

            var logFile = await logFileNameCompletionSource.Task;
            _ansiConsole.MarkupLine($"[bold]App Host Log:[/] {logFile.FullName}");

            var dashboardUrls = await _interactionService.ShowStatusAsync("Starting dashboard...", async () =>
            {
                return await backchannel.GetDashboardUrlsAsync(cancellationToken);
            });

            _interactionService.DisplayDashboardUrls(dashboardUrls);

            // The reason we choose to display the codespaces URL on status if Codespaces is available
            // is that the redirect mechanics for forward ports strips the token off the localhost
            // variant of the URL.
            var dashboardUrlToDisplayOnStatus = dashboardUrls.CodespacesUrlWithLoginToken ?? dashboardUrls.BaseUrlWithLoginToken;

            await _interactionService.ShowStatusAsync($"Dashboard: {dashboardUrlToDisplayOnStatus}", async () =>
            {
                try
                {
                    var resourceStates = backchannel.GetResourceStatesAsync(cancellationToken);
                    await foreach (var resourceState in resourceStates.WithCancellation(cancellationToken))
                    {
                        ProcessResourceState(resourceState);
                    }
                }
                catch (ConnectionLostException) when (cancellationToken.IsCancellationRequested)
                {
                    // Just swallow this exception because this is an orderly shutdown of the backchannel.
                }

                return Task.CompletedTask;
            });

            await pendingLogCapture;
            return await pendingRun;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.ProjectFileDoesntExist, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionDoesntExist);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.MultipleProjectFilesFound, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedMultipleAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedNoCsprojFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (AppHostIncompatibleException ex)
        {
            return _interactionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
        }
        catch (CertificateServiceException ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message));
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message));
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    private static FileInfo GetAppHostLogFile()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var logsPath = Path.Combine(homeDirectory, ".aspire", "cli", "logs");
        var logFilePath = Path.Combine(logsPath, $"apphost-{Environment.ProcessId}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.log");
        var logFile = new FileInfo(logFilePath);
        return logFile;
    }

    private static async Task CaptureAppHostLogsAsync(FileInfo logFile, IAppHostBackchannel backchannel, CancellationToken cancellationToken)
    {
        try
        {
            if (!logFile.Directory!.Exists)
            {
                logFile.Directory.Create();
            }

            using var streamWriter = new StreamWriter(logFile.FullName, append: true)
            {
                AutoFlush = true
            };

            var logEntries = backchannel.GetAppHostLogEntriesAsync(cancellationToken);

            await foreach (var entry in logEntries.WithCancellation(cancellationToken))
            {
                await streamWriter.WriteLineAsync($"{entry.Timestamp:HH:mm:ss} [{entry.LogLevel}] {entry.CategoryName}: {entry.Message}");
            }
        }
        catch (ConnectionLostException) when (cancellationToken.IsCancellationRequested)
        {
            // Just swallow this exception because this is an orderly shutdown of the backchannel.
            return;
        }
    }

    private readonly Dictionary<string, RpcResourceState> _resourceStates = new();

    public void ProcessResourceState(RpcResourceState resourceState)
    {
        if (_resourceStates.TryGetValue(resourceState.Resource, out var existingResourceState))
        {
            if (resourceState.State != existingResourceState.State || resourceState.Health != existingResourceState.Health)
            {
                DisplayResourceState(resourceState.Resource, resourceState.State, resourceState.Health);
            }

            if (resourceState.Endpoints.Except(existingResourceState.Endpoints) is { } endpoints && endpoints.Any())
            {
                foreach (var endpoint in endpoints)
                {
                    DisplayEndpoint(resourceState.Resource, endpoint);
                }
            }

            _resourceStates[resourceState.Resource] = resourceState;
        }
        else
        {
            DisplayResourceState(resourceState.Resource, resourceState.State, resourceState.Health);

            if (resourceState.Endpoints is { } endpoints)
            {
                foreach (var endpoint in endpoints)
                {
                    DisplayEndpoint(resourceState.Resource, endpoint);
                }
            }

            _resourceStates[resourceState.Resource] = resourceState;
        }

        void DisplayEndpoint(string resourceName, string endpoint)
        {
            _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]has endpoint[/] [link={endpoint}]{endpoint}[/]");
        }

        void DisplayResourceState(string resourceName, string state, string? health)
        {
            Action action = (state, health) switch
            {
                { state: "Waiting" or "Finished" or "NotStarted", health: _ } =>
                    () => _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]is[/] [bold]{state}[/]"),

                {state: "Stopping" or "Exited" or "FailedToStart", health: _ } => () =>
                    _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]is[/] [bold red]{state}[/]"),

                { state: "Starting", health: _ } =>
                    () => _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]is[/] [bold green]{state}[/]"),

                { state: "Running", health: "Unhealthy" or "Degraded" } =>
                    () => _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]is[/] [bold yellow]{health}[/]"),
                    
                { state: "Running", health: "Healthy" } =>
                    () => _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]is[/] [bold green]{state}[/]"),
                    
                { state: { Length: > 0 } } => () => _ansiConsole.MarkupLine($"[bold]{resourceName}[/] [grey]is[/] [bold hotpink]{state}[/]"),
                _ => () => { // No op if state is null or empty }
                }
            };
            action?.Invoke();
        }
    }
}
