// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using StreamJsonRpc;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Runner for .NET AppHost projects (.csproj and single-file .cs).
/// </summary>
internal sealed class DotNetAppHostRunner : IAppHostRunner
{
    private const int ProcessTerminationTimeoutMs = 10000;
    private const int ProcessTerminationPollIntervalMs = 250;

    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IAnsiConsole _ansiConsole;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IConfiguration _configuration;
    private readonly IFeatures _features;
    private readonly ICliHostEnvironment _hostEnvironment;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DotNetAppHostRunner> _logger;

    private readonly Dictionary<string, RpcResourceState> _resourceStates = new();

    public DotNetAppHostRunner(
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        IAnsiConsole ansiConsole,
        AspireCliTelemetry telemetry,
        IConfiguration configuration,
        IFeatures features,
        ICliHostEnvironment hostEnvironment,
        TimeProvider timeProvider,
        ILogger<DotNetAppHostRunner> logger)
    {
        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _ansiConsole = ansiConsole;
        _telemetry = telemetry;
        _configuration = configuration;
        _features = features;
        _hostEnvironment = hostEnvironment;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public AppHostType SupportedType => AppHostType.DotNetProject;

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(FileInfo appHostFile, CancellationToken cancellationToken)
    {
        var isSingleFile = appHostFile.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);

        if (isSingleFile)
        {
            // For single-file apphosts, we just check that it exists
            return appHostFile.Exists;
        }

        // For project files, check if it's a valid Aspire AppHost
        var compatibility = await AppHostHelper.CheckAppHostCompatibilityAsync(
            _runner,
            _interactionService,
            appHostFile,
            _telemetry,
            appHostFile.Directory!,
            cancellationToken);

        return compatibility.IsCompatibleAppHost;
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostRunnerContext context, CancellationToken cancellationToken)
    {
        var effectiveAppHostFile = context.AppHostFile;
        var isExtensionHost = ExtensionHelper.IsExtensionHost(_interactionService, out _, out _);

        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;

        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity("run");

            // Check for running instance if feature is enabled
            var runningInstanceDetectionEnabled = _features.IsFeatureEnabled(KnownFeatures.RunningInstanceDetectionEnabled, defaultValue: true);
            if (runningInstanceDetectionEnabled)
            {
                await CheckAndHandleRunningInstanceAsync(effectiveAppHostFile, cancellationToken);
            }

            var isSingleFileAppHost = effectiveAppHostFile.Extension != ".csproj";

            var env = new Dictionary<string, string>(context.EnvironmentVariables);

            if (context.WaitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);

            var watch = !isSingleFileAppHost && (_features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, defaultValue: false) || (isExtensionHost && !context.StartDebugSession));

            if (!watch)
            {
                if (!isSingleFileAppHost && !isExtensionHost)
                {
                    var buildOptions = new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = buildOutputCollector.AppendOutput,
                        StandardErrorCallback = buildOutputCollector.AppendError,
                    };

                    var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, _interactionService, effectiveAppHostFile, buildOptions, context.WorkingDirectory, cancellationToken);

                    if (buildExitCode != 0)
                    {
                        _interactionService.DisplayLines(buildOutputCollector.GetLines());
                        _interactionService.DisplayError(InteractionServiceStrings.ProjectCouldNotBeBuilt);
                        return ExitCodeConstants.FailedToBuildArtifacts;
                    }
                }
            }

            if (isSingleFileAppHost)
            {
                appHostCompatibilityCheck = (true, true, VersionHelper.GetDefaultTemplateVersion());
            }
            else
            {
                appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostFile, _telemetry, context.WorkingDirectory, cancellationToken);
            }

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
                StartDebugSession = context.StartDebugSession,
                Debug = context.Debug
            };

            var backchannelCompletionSource = context.BackchannelCompletionSource ?? new TaskCompletionSource<IAppHostCliBackchannel>();

            if (isSingleFileAppHost)
            {
                ConfigureSingleFileEnvironment(effectiveAppHostFile, env);
            }

            var pendingRun = _runner.RunAsync(
                effectiveAppHostFile,
                watch,
                !watch,
                context.UnmatchedTokens,
                env,
                backchannelCompletionSource,
                runOptions,
                cancellationToken);

            // Wait for the backchannel to be established
            var backchannel = await _interactionService.ShowStatusAsync(
                isExtensionHost ? InteractionServiceStrings.BuildingAppHost : RunCommandStrings.ConnectingToAppHost,
                async () => await backchannelCompletionSource.Task.WaitAsync(cancellationToken));

            var logFile = GetAppHostLogFile();
            var pendingLogCapture = CaptureAppHostLogsAsync(logFile, backchannel, _interactionService, cancellationToken);

            var dashboardUrls = await _interactionService.ShowStatusAsync(
                RunCommandStrings.StartingDashboard,
                async () => await backchannel.GetDashboardUrlsAsync(cancellationToken));

            if (dashboardUrls.DashboardHealthy is false)
            {
                _interactionService.DisplayError(RunCommandStrings.DashboardFailedToStart);
                _interactionService.DisplayLines(runOutputCollector.GetLines());
                return ExitCodeConstants.DashboardFailure;
            }

            DisplayDashboardInfo(effectiveAppHostFile, dashboardUrls, logFile, context.WorkingDirectory, isExtensionHost);

            // Handle remote environments (Codespaces, Remote Containers, SSH)
            await HandleRemoteEndpointsAsync(dashboardUrls, backchannel, cancellationToken);

            if (ExtensionHelper.IsExtensionHost(_interactionService, out var extensionInteractionService, out _))
            {
                extensionInteractionService.DisplayDashboardUrls(dashboardUrls);
                extensionInteractionService.NotifyAppHostStartupCompleted();
            }

            await pendingLogCapture;
            return await pendingRun;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken || ex is ExtensionOperationCanceledException)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (AppHostIncompatibleException ex)
        {
            return _interactionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull));
        }
        catch (CertificateServiceException ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message.EscapeMarkup()));
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message.EscapeMarkup()));
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message.EscapeMarkup()));
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    private static void ConfigureSingleFileEnvironment(FileInfo appHostFile, Dictionary<string, string> env)
    {
        var runJsonFilePath = appHostFile.FullName[..^2] + "run.json";
        if (!File.Exists(runJsonFilePath))
        {
            env["ASPNETCORE_ENVIRONMENT"] = "Development";
            env["DOTNET_ENVIRONMENT"] = "Development";
            env["ASPNETCORE_URLS"] = "https://localhost:17193;http://localhost:15069";
            env["ASPIRE_DASHBOARD_MCP_ENDPOINT_URL"] = "https://localhost:21294";
            env["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "https://localhost:21293";
            env["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = "https://localhost:22086";
        }
    }

    private void DisplayDashboardInfo(FileInfo appHostFile, DashboardUrlsState dashboardUrls, FileInfo logFile, DirectoryInfo workingDirectory, bool isExtensionHost)
    {
        _ansiConsole.WriteLine();
        var topGrid = new Grid();
        topGrid.AddColumn();
        topGrid.AddColumn();

        var topPadder = new Padder(topGrid, new Padding(3, 0));

        var dashboardsLocalizedString = RunCommandStrings.Dashboard;
        var logsLocalizedString = RunCommandStrings.Logs;
        var appHostLocalizedString = RunCommandStrings.AppHost;

        var longestLocalizedLength = new[] { dashboardsLocalizedString, logsLocalizedString, RunCommandStrings.Endpoints, appHostLocalizedString }
            .Max(s => s.Length);

        var longestLocalizedLengthWithColon = longestLocalizedLength + 1;
        topGrid.Columns[0].Width = longestLocalizedLengthWithColon;

        var appHostRelativePath = Path.GetRelativePath(workingDirectory.FullName, appHostFile.FullName);
        topGrid.AddRow(new Align(new Markup($"[bold green]{appHostLocalizedString}[/]:"), HorizontalAlignment.Right), new Text(appHostRelativePath));
        topGrid.AddRow(Text.Empty, Text.Empty);

        if (!isExtensionHost)
        {
            topGrid.AddRow(new Align(new Markup($"[bold green]{dashboardsLocalizedString}[/]:"), HorizontalAlignment.Right), new Markup($"[link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]"));
            if (dashboardUrls.CodespacesUrlWithLoginToken is { } codespacesUrlWithLoginToken)
            {
                topGrid.AddRow(Text.Empty, new Markup($"[link={codespacesUrlWithLoginToken}]{codespacesUrlWithLoginToken}[/]"));
            }
        }

        topGrid.AddRow(Text.Empty, Text.Empty);
        topGrid.AddRow(new Align(new Markup($"[bold green]{logsLocalizedString}[/]:"), HorizontalAlignment.Right), new Text(logFile.FullName));

        _ansiConsole.Write(topPadder);

        AppendCtrlCMessage(longestLocalizedLengthWithColon);
    }

    private async Task HandleRemoteEndpointsAsync(DashboardUrlsState dashboardUrls, IAppHostCliBackchannel backchannel, CancellationToken cancellationToken)
    {
        var isCodespaces = dashboardUrls.CodespacesUrlWithLoginToken is not null;
        var isRemoteContainers = _configuration.GetValue<bool>("REMOTE_CONTAINERS", false);
        var isSshRemote = _configuration.GetValue<string?>("VSCODE_IPC_HOOK_CLI") is not null
                          && _configuration.GetValue<string?>("SSH_CONNECTION") is not null;

        var longestLocalizedLength = new[] { RunCommandStrings.Dashboard, RunCommandStrings.Logs, RunCommandStrings.Endpoints, RunCommandStrings.AppHost }
            .Max(s => s.Length) + 1;

        if (isCodespaces || isRemoteContainers || isSshRemote)
        {
            bool firstEndpoint = true;

            try
            {
                var resourceStates = backchannel.GetResourceStatesAsync(cancellationToken);
                await foreach (var resourceState in resourceStates.WithCancellation(cancellationToken))
                {
                    ProcessResourceState(resourceState, (resource, endpoint) =>
                    {
                        ClearLines(2);

                        var endpointsGrid = new Grid();
                        endpointsGrid.AddColumn();
                        endpointsGrid.AddColumn();
                        endpointsGrid.Columns[0].Width = longestLocalizedLength;

                        if (firstEndpoint)
                        {
                            endpointsGrid.AddRow(Text.Empty, Text.Empty);
                        }

                        endpointsGrid.AddRow(
                            firstEndpoint ? new Align(new Markup($"[bold green]{RunCommandStrings.Endpoints}[/]:"), HorizontalAlignment.Right) : Text.Empty,
                            new Markup($"[bold]{resource}[/] [grey]has endpoint[/] [link={endpoint}]{endpoint}[/]")
                        );

                        var endpointsPadder = new Padder(endpointsGrid, new Padding(3, 0));
                        _ansiConsole.Write(endpointsPadder);
                        firstEndpoint = false;

                        AppendCtrlCMessage(longestLocalizedLength);
                    });
                }
            }
            catch (ConnectionLostException) when (cancellationToken.IsCancellationRequested)
            {
                // Orderly shutdown
            }
        }
    }

    private void ProcessResourceState(RpcResourceState resourceState, Action<string, string> endpointWriter)
    {
        if (_resourceStates.TryGetValue(resourceState.Resource, out var existingResourceState))
        {
            if (resourceState.Endpoints.Except(existingResourceState.Endpoints) is { } endpoints && endpoints.Any())
            {
                foreach (var endpoint in endpoints)
                {
                    endpointWriter(resourceState.Resource, endpoint);
                }
            }

            _resourceStates[resourceState.Resource] = resourceState;
        }
        else
        {
            if (resourceState.Endpoints is { } endpoints && endpoints.Any())
            {
                foreach (var endpoint in endpoints)
                {
                    endpointWriter(resourceState.Resource, endpoint);
                }
            }

            _resourceStates[resourceState.Resource] = resourceState;
        }
    }

    private void ClearLines(int lines)
    {
        if (lines <= 0)
        {
            return;
        }

        for (var i = 0; i < lines; i++)
        {
            _ansiConsole.Write("\u001b[1A");
            _ansiConsole.Write("\u001b[2K");
        }
    }

    private void AppendCtrlCMessage(int longestLocalizedLengthWithColon)
    {
        if (ExtensionHelper.IsExtensionHost(_interactionService, out _, out _))
        {
            return;
        }

        var ctrlCGrid = new Grid();
        ctrlCGrid.AddColumn();
        ctrlCGrid.AddColumn();
        ctrlCGrid.Columns[0].Width = longestLocalizedLengthWithColon;
        ctrlCGrid.AddRow(Text.Empty, Text.Empty);
        ctrlCGrid.AddRow(new Text(string.Empty), new Markup(RunCommandStrings.PressCtrlCToStopAppHost) { Overflow = Overflow.Ellipsis });

        var ctrlCPadder = new Padder(ctrlCGrid, new Padding(3, 0));
        _ansiConsole.Write(ctrlCPadder);
    }

    private FileInfo GetAppHostLogFile()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var logsPath = Path.Combine(homeDirectory, ".aspire", "cli", "logs");
        var logFilePath = Path.Combine(logsPath, $"apphost-{Environment.ProcessId}-{_timeProvider.GetUtcNow():yyyy-MM-dd-HH-mm-ss}.log");
        return new FileInfo(logFilePath);
    }

    private static async Task CaptureAppHostLogsAsync(FileInfo logFile, IAppHostCliBackchannel backchannel, IInteractionService interactionService, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield();

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
                if (ExtensionHelper.IsExtensionHost(interactionService, out var extensionInteractionService, out _))
                {
                    if (entry.LogLevel is not LogLevel.Trace and not LogLevel.Debug)
                    {
                        extensionInteractionService.WriteDebugSessionMessage(entry.Message, entry.LogLevel is not LogLevel.Error and not LogLevel.Critical, "\x1b[2m");
                    }
                }

                await streamWriter.WriteLineAsync($"{entry.Timestamp:HH:mm:ss} [{entry.LogLevel}] {entry.CategoryName}: {entry.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ConnectionLostException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
    }

    private async Task<bool> CheckAndHandleRunningInstanceAsync(FileInfo appHostFile, CancellationToken cancellationToken)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var auxiliarySocketPath = AppHostHelper.ComputeAuxiliarySocketPath(appHostFile.FullName, homeDirectory);

        if (!File.Exists(auxiliarySocketPath))
        {
            return true;
        }

        return await StopRunningInstanceAsync(auxiliarySocketPath, cancellationToken);
    }

    private async Task<bool> StopRunningInstanceAsync(string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            using var backchannel = await AppHostAuxiliaryBackchannel.ConnectAsync(socketPath, _logger, cancellationToken).ConfigureAwait(false);

            var appHostInfo = backchannel.AppHostInfo;

            if (appHostInfo is null)
            {
                _logger.LogDebug("Failed to stop running instance because appHostInfo was null.");
                return false;
            }

            var cliPidText = appHostInfo.CliProcessId.HasValue ? appHostInfo.CliProcessId.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
            _interactionService.DisplayMessage("stop_sign", $"Stopping previous instance (AppHost PID: {appHostInfo.ProcessId.ToString(CultureInfo.InvariantCulture)}, CLI PID: {cliPidText})");

            await backchannel.StopAppHostAsync(cancellationToken).ConfigureAwait(false);

            var stopped = await MonitorProcessesForTerminationAsync(appHostInfo, cancellationToken).ConfigureAwait(false);

            if (stopped)
            {
                _interactionService.DisplaySuccess(RunCommandStrings.RunningInstanceStopped);
            }
            else
            {
                _logger.LogDebug("Failed to stop running instance within {TimeoutMs}ms timeout.", ProcessTerminationTimeoutMs);
            }

            return stopped;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to stop running instance.");
            return false;
        }
    }

    private async Task<bool> MonitorProcessesForTerminationAsync(AppHostInformation appHostInfo, CancellationToken cancellationToken)
    {
        var startTime = _timeProvider.GetUtcNow();
        var pidsToMonitor = new List<int> { appHostInfo.ProcessId };

        if (appHostInfo.CliProcessId.HasValue)
        {
            pidsToMonitor.Add(appHostInfo.CliProcessId.Value);
        }

        while ((_timeProvider.GetUtcNow() - startTime).TotalMilliseconds < ProcessTerminationTimeoutMs)
        {
            var allStopped = true;

            foreach (var pid in pidsToMonitor)
            {
                try
                {
                    Process.GetProcessById(pid);
                    allStopped = false;
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist
                }
            }

            if (allStopped)
            {
                return true;
            }

            await Task.Delay(ProcessTerminationPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
