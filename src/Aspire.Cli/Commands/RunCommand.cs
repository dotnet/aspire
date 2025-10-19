// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
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

namespace Aspire.Cli.Commands;

internal sealed class RunCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IConfiguration _configuration;
    private readonly IDotNetSdkInstaller _sdkInstaller;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFeatures _features;

    public RunCommand(
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        IProjectLocator projectLocator,
        IAnsiConsole ansiConsole,
        AspireCliTelemetry telemetry,
        IConfiguration configuration,
        IDotNetSdkInstaller sdkInstaller,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        IServiceProvider serviceProvider,
        CliExecutionContext executionContext)
        : base("run", RunCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sdkInstaller);

        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _projectLocator = projectLocator;
        _ansiConsole = ansiConsole;
        _telemetry = telemetry;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _sdkInstaller = sdkInstaller;
        _features = features;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = RunCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        if (ExtensionHelper.IsExtensionHost(InteractionService, out _, out _))
        {
            var startDebugOption = new Option<bool>("--start-debug-session");
            startDebugOption.Description = RunCommandStrings.StartDebugSessionArgumentDescription;
            Options.Add(startDebugOption);
        }

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var isExtensionHost = ExtensionHelper.IsExtensionHost(InteractionService, out _, out _);
        var startDebugSession = isExtensionHost && parseResult.GetValue<bool>("--start-debug-session");

        // A user may run `aspire run` in an Aspire terminal in VS Code. In this case, intercept and prompt
        // VS Code to start a debug session using the current directory
        if (ExtensionHelper.IsExtensionHost(InteractionService, out var extensionInteractionService, out _)
            && string.IsNullOrEmpty(_configuration[KnownConfigNames.ExtensionDebugSessionId]))
        {
            extensionInteractionService.DisplayConsolePlainText(RunCommandStrings.StartingDebugSessionInExtension);
            await extensionInteractionService.StartDebugSessionAsync(ExecutionContext.WorkingDirectory.FullName, passedAppHostProjectFile?.FullName, startDebugSession);
            return ExitCodeConstants.Success;
        }

        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, InteractionService, cancellationToken))
        {
            return ExitCodeConstants.SdkNotInstalled;
        }

        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;
        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

            var effectiveAppHostFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

            if (effectiveAppHostFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var isSingleFileAppHost = effectiveAppHostFile.Extension != ".csproj";

            var env = new Dictionary<string, string>();

            var debug = parseResult.GetValue<bool>("--debug");

            var waitForDebugger = parseResult.GetValue<bool>("--wait-for-debugger");

            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);

            var watch = !isSingleFileAppHost && (_features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, defaultValue: false) || (isExtensionHost && !startDebugSession));

            if (!watch)
            {
                if (!isSingleFileAppHost || isExtensionHost)
                {
                    var buildOptions = new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = buildOutputCollector.AppendOutput,
                        StandardErrorCallback = buildOutputCollector.AppendError,
                    };

                    // The extension host will build the app host project itself, so we don't need to do it here if host exists.
                    if (!ExtensionHelper.IsExtensionHost(InteractionService, out _, out var extensionBackchannel)
                        || !await extensionBackchannel.HasCapabilityAsync(KnownCapabilities.DevKit, cancellationToken)
                        || isSingleFileAppHost)
                    {
                        var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, InteractionService, effectiveAppHostFile, buildOptions, ExecutionContext.WorkingDirectory, cancellationToken);

                        if (buildExitCode != 0)
                        {
                            InteractionService.DisplayLines(buildOutputCollector.GetLines());
                            InteractionService.DisplayError(InteractionServiceStrings.ProjectCouldNotBeBuilt);
                            return ExitCodeConstants.FailedToBuildArtifacts;
                        }
                    }
                }
            }

            if (isSingleFileAppHost)
            {
                // TODO: Add logic to read SDK version from *.cs file.
                appHostCompatibilityCheck = (true, true, VersionHelper.GetDefaultTemplateVersion());
            }
            else
            {
                appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, InteractionService, effectiveAppHostFile, _telemetry, ExecutionContext.WorkingDirectory, cancellationToken);
            }

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
                StartDebugSession = startDebugSession,
                Debug = debug
            };

            var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();

            if (isSingleFileAppHost)
            {
                // TODO:  This is just fallback behavior for now. We need to decide on whether we
                //        want to treat the lack of a apphost.run.json as an error or whether we
                //        want to somehow manage this information in .aspire/settings.json and how
                //        this might work in polyglot scenarios. For the preview of this feature
                //        I'm not over investing too much time in this :)
                var runJsonFilePath = effectiveAppHostFile.FullName[..^2] + "run.json";
                if (!File.Exists(runJsonFilePath))
                {
                    env["ASPNETCORE_ENVIRONMENT"] = "Development";
                    env["DOTNET_ENVIRONMENT"] = "Development";
                    env["ASPNETCORE_URLS"] = "https://localhost:17193;http://localhost:15069";
                    env["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "https://localhost:21293";
                    env["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = "https://localhost:22086";
                }
            }

            var pendingRun = _runner.RunAsync(
                effectiveAppHostFile,
                watch,
                !watch,
                unmatchedTokens,
                env,
                backchannelCompletitionSource,
                runOptions,
                cancellationToken);

            // Wait for the backchannel to be established.
            var backchannel = await InteractionService.ShowStatusAsync(RunCommandStrings.ConnectingToAppHost, async () => { return await backchannelCompletitionSource.Task.WaitAsync(cancellationToken); });

            var logFile = GetAppHostLogFile();

            var pendingLogCapture = CaptureAppHostLogsAsync(logFile, backchannel, _interactionService, cancellationToken);

            var dashboardUrls = await InteractionService.ShowStatusAsync(RunCommandStrings.StartingDashboard, async () => { return await backchannel.GetDashboardUrlsAsync(cancellationToken); });

            if (dashboardUrls.DashboardHealthy is false)
            {
                InteractionService.DisplayError(RunCommandStrings.DashboardFailedToStart);
                InteractionService.DisplayLines(runOutputCollector.GetLines());
                return ExitCodeConstants.DashboardFailure;
            }

            _ansiConsole.WriteLine();
            var topGrid = new Grid();
            topGrid.AddColumn();
            topGrid.AddColumn();

            var topPadder = new Padder(topGrid, new Padding(3, 0));

            var dashboardsLocalizedString = RunCommandStrings.Dashboard;
            var logsLocalizedString = RunCommandStrings.Logs;
            var endpointsLocalizedString = RunCommandStrings.Endpoints;
            var appHostLocalizedString = RunCommandStrings.AppHost;

            var longestLocalizedLength = new[] { dashboardsLocalizedString, logsLocalizedString, endpointsLocalizedString, appHostLocalizedString }
                .Max(s => s.Length);

            // +1 -> accommodates the colon (:) that gets appended to each localized string
            var longestLocalizedLengthWithColon = longestLocalizedLength + 1;

            topGrid.Columns[0].Width = longestLocalizedLengthWithColon;

            var appHostRelativePath = Path.GetRelativePath(ExecutionContext.WorkingDirectory.FullName, effectiveAppHostFile.FullName);
            topGrid.AddRow(new Align(new Markup($"[bold green]{appHostLocalizedString}[/]:"), HorizontalAlignment.Right), new Text(appHostRelativePath));
            topGrid.AddRow(Text.Empty, Text.Empty);
            topGrid.AddRow(new Align(new Markup($"[bold green]{dashboardsLocalizedString}[/]:"), HorizontalAlignment.Right), new Markup($"[link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]"));
            if (dashboardUrls.CodespacesUrlWithLoginToken is { } codespacesUrlWithLoginToken)
            {
                topGrid.AddRow(Text.Empty, new Markup($"[link={codespacesUrlWithLoginToken}]{codespacesUrlWithLoginToken}[/]"));
            }

            topGrid.AddRow(Text.Empty, Text.Empty);
            topGrid.AddRow(new Align(new Markup($"[bold green]{logsLocalizedString}[/]:"), HorizontalAlignment.Right), new Text(logFile.FullName));

            _ansiConsole.Write(topPadder);

            // Use the presence of CodespacesUrlWithLoginToken to detect codespaces, as this is more reliable
            // than environment variables since it comes from the same backend detection logic
            var isCodespaces = dashboardUrls.CodespacesUrlWithLoginToken is not null;
            var isRemoteContainers = _configuration.GetValue<bool>("REMOTE_CONTAINERS", false);
            var isSshRemote = _configuration.GetValue<string?>("VSCODE_IPC_HOOK_CLI") is not null
                              && _configuration.GetValue<string?>("SSH_CONNECTION") is not null;

            AppendCtrlCMessage(longestLocalizedLengthWithColon);

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
                            // When we are appending endpoints we need
                            // to remove the CTRL-C message that was appended
                            // previously. So we can write the endpoint.
                            // We will append the CTRL-C message again after
                            // writing the endpoint.
                            ClearLines(2);

                            var endpointsGrid = new Grid();
                            endpointsGrid.AddColumn();
                            endpointsGrid.AddColumn();
                            endpointsGrid.Columns[0].Width = longestLocalizedLengthWithColon;

                            if (firstEndpoint)
                            {
                                endpointsGrid.AddRow(Text.Empty, Text.Empty);
                            }

                            endpointsGrid.AddRow(
                                firstEndpoint ? new Align(new Markup($"[bold green]{endpointsLocalizedString}[/]:"), HorizontalAlignment.Right) : Text.Empty,
                                new Markup($"[bold]{resource}[/] [grey]has endpoint[/] [link={endpoint}]{endpoint}[/]")
                            );

                            var endpointsPadder = new Padder(endpointsGrid, new Padding(3, 0));
                            _ansiConsole.Write(endpointsPadder);
                            firstEndpoint = false;

                            AppendCtrlCMessage(longestLocalizedLengthWithColon);
                        });
                    }
                }
                catch (ConnectionLostException) when (cancellationToken.IsCancellationRequested)
                {
                    // Just swallow this exception because this is an orderly shutdown of the backchannel.
                }
            }

            if (ExtensionHelper.IsExtensionHost(InteractionService, out extensionInteractionService, out _))
            {
                extensionInteractionService.DisplayDashboardUrls(dashboardUrls);
                extensionInteractionService.NotifyAppHostStartupCompleted();
            }

            await pendingLogCapture;
            return await pendingRun;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken || ex is ExtensionOperationCanceledException)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, InteractionService);
        }
        catch (AppHostIncompatibleException ex)
        {
            return InteractionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
        }
        catch (CertificateServiceException ex)
        {
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message.EscapeMarkup()));
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message.EscapeMarkup()));
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message.EscapeMarkup()));
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
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
            _ansiConsole.Write("\u001b[2K"); // Clear the line
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

    private static FileInfo GetAppHostLogFile()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var logsPath = Path.Combine(homeDirectory, ".aspire", "cli", "logs");
        var logFilePath = Path.Combine(logsPath, $"apphost-{Environment.ProcessId}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.log");
        var logFile = new FileInfo(logFilePath);
        return logFile;
    }

    private static async Task CaptureAppHostLogsAsync(FileInfo logFile, IAppHostBackchannel backchannel, IInteractionService interactionService, CancellationToken cancellationToken)
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
                        // Send only information+ level logs to the extension host.
                        extensionInteractionService.WriteDebugSessionMessage(entry.Message, entry.LogLevel is not LogLevel.Error and not LogLevel.Critical);
                    }
                }

                await streamWriter.WriteLineAsync($"{entry.Timestamp:HH:mm:ss} [{entry.LogLevel}] {entry.CategoryName}: {entry.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            // Swallow the exception if the operation was cancelled.
            return;
        }
        catch (ConnectionLostException) when (cancellationToken.IsCancellationRequested)
        {
            // Just swallow this exception because this is an orderly shutdown of the backchannel.
            return;
        }
    }

    private readonly Dictionary<string, RpcResourceState> _resourceStates = new();

    public void ProcessResourceState(RpcResourceState resourceState, Action<string, string> endpointWriter)
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
}
