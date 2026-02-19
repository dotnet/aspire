// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Processes;
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

/// <summary>
/// Represents information about a detached AppHost for JSON serialization.
/// </summary>
internal sealed record DetachOutputInfo(
    string AppHostPath,
    int AppHostPid,
    int CliPid,
    string? DashboardUrl,
    string LogFile);

[JsonSerializable(typeof(DetachOutputInfo))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class RunCommandJsonContext : JsonSerializerContext
{
    private static RunCommandJsonContext? s_relaxedEscaping;

    /// <summary>
    /// Gets a context with relaxed JSON escaping for non-ASCII character support.
    /// </summary>
    public static RunCommandJsonContext RelaxedEscaping => s_relaxedEscaping ??= new(new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}

internal sealed class RunCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFeatures _features;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RunCommand> _logger;
    private readonly IAppHostProjectFactory _projectFactory;
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly Diagnostics.FileLoggerProvider _fileLoggerProvider;

    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = RunCommandStrings.ProjectArgumentDescription
    };
    private static readonly Option<bool> s_detachOption = new("--detach")
    {
        Description = RunCommandStrings.DetachArgumentDescription
    };
    private static readonly Option<OutputFormat?> s_formatOption = new("--format")
    {
        Description = RunCommandStrings.JsonArgumentDescription
    };
    private static readonly Option<bool> s_isolatedOption = new("--isolated")
    {
        Description = RunCommandStrings.IsolatedArgumentDescription
    };
    private static readonly Option<bool> s_noBuildOption = new("--no-build")
    {
        Description = RunCommandStrings.NoBuildArgumentDescription
    };
    private static readonly Option<string?> s_logFileOption = new("--log-file")
    {
        Description = "Path to write the log file (used internally by --detach).",
        Hidden = true
    };
    private readonly Option<bool>? _startDebugSessionOption;

    public RunCommand(
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        IProjectLocator projectLocator,
        IAnsiConsole ansiConsole,
        AspireCliTelemetry telemetry,
        IConfiguration configuration,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        IServiceProvider serviceProvider,
        CliExecutionContext executionContext,
        ILogger<RunCommand> logger,
        IAppHostProjectFactory projectFactory,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        Diagnostics.FileLoggerProvider fileLoggerProvider,
        TimeProvider? timeProvider)
        : base("run", RunCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _projectLocator = projectLocator;
        _ansiConsole = ansiConsole;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _features = features;
        _logger = logger;
        _projectFactory = projectFactory;
        _backchannelMonitor = backchannelMonitor;
        _fileLoggerProvider = fileLoggerProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;

        Options.Add(s_projectOption);
        Options.Add(s_detachOption);
        Options.Add(s_formatOption);
        Options.Add(s_isolatedOption);
        Options.Add(s_noBuildOption);
        Options.Add(s_logFileOption);

        if (ExtensionHelper.IsExtensionHost(InteractionService, out _, out _))
        {
            _startDebugSessionOption = new Option<bool>("--start-debug-session")
            {
                Description = RunCommandStrings.StartDebugSessionArgumentDescription
            };
            Options.Add(_startDebugSessionOption);
        }

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);
        var detach = parseResult.GetValue(s_detachOption);
        var format = parseResult.GetValue(s_formatOption);
        var isolated = parseResult.GetValue(s_isolatedOption);
        var noBuild = parseResult.GetValue(s_noBuildOption);
        var isExtensionHost = ExtensionHelper.IsExtensionHost(InteractionService, out _, out _);
        var startDebugSession = false;
        if (isExtensionHost)
        {
            Debug.Assert(_startDebugSessionOption is not null);
            startDebugSession = parseResult.GetValue(_startDebugSessionOption);
        }
        var runningInstanceDetectionEnabled = _features.IsFeatureEnabled(KnownFeatures.RunningInstanceDetectionEnabled, defaultValue: true);
        // Force option kept for backward compatibility but no longer used since prompt was removed
        // var force = runningInstanceDetectionEnabled && parseResult.GetValue<bool>("--force");

        // Validate that --format is only used with --detach
        if (format is not null && !detach)
        {
            InteractionService.DisplayError(RunCommandStrings.FormatRequiresDetach);
            return ExitCodeConstants.InvalidCommand;
        }

        // Validate that --no-build is not used when watch mode would be enabled
        // Watch mode is enabled when DefaultWatchEnabled feature is true, or when running under extension host (not in debug session)
        var watchModeEnabled = _features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, defaultValue: false) || (isExtensionHost && !startDebugSession);
        if (noBuild && watchModeEnabled)
        {
            InteractionService.DisplayError(RunCommandStrings.NoBuildNotSupportedWithWatchMode);
            return ExitCodeConstants.InvalidCommand;
        }

        // Handle detached mode - spawn child process and exit
        if (detach)
        {
            return await ExecuteDetachedAsync(parseResult, passedAppHostProjectFile, isExtensionHost, cancellationToken);
        }

        // A user may run `aspire run` in an Aspire terminal in VS Code. In this case, intercept and prompt
        // VS Code to start a debug session using the current directory
        if (ExtensionHelper.IsExtensionHost(InteractionService, out var extensionInteractionService, out _)
            && string.IsNullOrEmpty(_configuration[KnownConfigNames.ExtensionDebugSessionId]))
        {
            extensionInteractionService.DisplayConsolePlainText(RunCommandStrings.StartingDebugSessionInExtension);
            await extensionInteractionService.StartDebugSessionAsync(ExecutionContext.WorkingDirectory.FullName, passedAppHostProjectFile?.FullName, startDebugSession);
            return ExitCodeConstants.Success;
        }

        AppHostProjectContext? context = null;

        try
        {
            using var activity = Telemetry.StartDiagnosticActivity(this.Name);

            var searchResult = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, MultipleAppHostProjectsFoundBehavior.Prompt, createSettingsFile: true, cancellationToken);
            var effectiveAppHostFile = searchResult.SelectedProjectFile;

            if (effectiveAppHostFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            // Resolve the language for this file and get the appropriate handler
            var project = _projectFactory.TryGetProject(effectiveAppHostFile);
            if (project is null)
            {
                InteractionService.DisplayError("Unrecognized app host type.");
                return ExitCodeConstants.FailedToFindProject;
            }

            // Check for running instance if feature is enabled
            if (runningInstanceDetectionEnabled)
            {
                // Even if we fail to stop we won't block the apphost starting
                // to make sure we don't ever break flow. It should mostly stop
                // just fine though.
                var runningInstanceResult = await project.CheckAndHandleRunningInstanceAsync(effectiveAppHostFile, ExecutionContext.HomeDirectory, cancellationToken);

                // If in isolated mode and a running instance was stopped, warn the user
                if (isolated && runningInstanceResult == RunningInstanceResult.InstanceStopped)
                {
                    InteractionService.DisplayMessage("warning", RunCommandStrings.IsolatedModeRunningInstanceWarning);
                }
            }

            // The completion sources are the contract between RunCommand and IAppHostProject
            var buildCompletionSource = new TaskCompletionSource<bool>();
            var backchannelCompletionSource = new TaskCompletionSource<IAppHostCliBackchannel>();

            context = new AppHostProjectContext
            {
                AppHostFile = effectiveAppHostFile,
                Watch = false,
                Debug = parseResult.GetValue(RootCommand.DebugOption),
                NoBuild = noBuild,
                NoRestore = noBuild, // --no-build implies --no-restore
                WaitForDebugger = parseResult.GetValue(RootCommand.WaitForDebuggerOption),
                Isolated = isolated,
                StartDebugSession = startDebugSession,
                EnvironmentVariables = new Dictionary<string, string>(),
                UnmatchedTokens = parseResult.UnmatchedTokens.ToArray(),
                WorkingDirectory = ExecutionContext.WorkingDirectory,
                BuildCompletionSource = buildCompletionSource,
                BackchannelCompletionSource = backchannelCompletionSource
            };

            // Start the project run as a pending task - we'll handle UX while it runs
            var pendingRun = project.RunAsync(context, cancellationToken);

            // Wait for the build to complete first (project handles its own build status spinners)
            var buildSuccess = await buildCompletionSource.Task.WaitAsync(cancellationToken);
            if (!buildSuccess)
            {
                // Build failed - display captured output and return exit code
                if (context.OutputCollector is { } outputCollector)
                {
                    InteractionService.DisplayLines(outputCollector.GetLines());
                }
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ProjectCouldNotBeBuilt, ExecutionContext.LogFilePath));
                return await pendingRun;
            }

            // Now wait for the backchannel to be established
            var backchannel = await InteractionService.ShowStatusAsync(
                isExtensionHost ? InteractionServiceStrings.BuildingAppHost : RunCommandStrings.ConnectingToAppHost,
                async () => await backchannelCompletionSource.Task.WaitAsync(cancellationToken));

            // Set up log capture - writes to unified CLI log file
            var pendingLogCapture = CaptureAppHostLogsAsync(_fileLoggerProvider, backchannel, _interactionService, cancellationToken);

            // Get dashboard URLs
            var dashboardUrls = await InteractionService.ShowStatusAsync(
                RunCommandStrings.StartingDashboard,
                async () => await backchannel.GetDashboardUrlsAsync(cancellationToken));

            if (dashboardUrls.DashboardHealthy is false)
            {
                InteractionService.DisplayError(RunCommandStrings.DashboardFailedToStart);
                return ExitCodeConstants.DashboardFailure;
            }

            // Display the UX
            var appHostRelativePath = Path.GetRelativePath(ExecutionContext.WorkingDirectory.FullName, effectiveAppHostFile.FullName);
            var longestLocalizedLengthWithColon = RenderAppHostSummary(
                _ansiConsole,
                appHostRelativePath,
                dashboardUrls.BaseUrlWithLoginToken,
                dashboardUrls.CodespacesUrlWithLoginToken,
                _fileLoggerProvider.LogFilePath,
                isExtensionHost);

            // Handle remote environments (Codespaces, Remote Containers, SSH)
            var isCodespaces = dashboardUrls.CodespacesUrlWithLoginToken is not null;
            var isRemoteContainers = string.Equals(_configuration["REMOTE_CONTAINERS"], "true", StringComparison.OrdinalIgnoreCase);
            var isSshRemote = _configuration["VSCODE_IPC_HOOK_CLI"] is not null
                              && _configuration["SSH_CONNECTION"] is not null;

            AppendCtrlCMessage(longestLocalizedLengthWithColon);

            if (isCodespaces || isRemoteContainers || isSshRemote)
            {
                bool firstEndpoint = true;
                var endpointsLocalizedString = RunCommandStrings.Endpoints;

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
                            endpointsGrid.Columns[0].Width = longestLocalizedLengthWithColon;

                            if (firstEndpoint)
                            {
                                endpointsGrid.AddRow(Text.Empty, Text.Empty);
                            }

                            endpointsGrid.AddRow(
                                firstEndpoint ? new Align(new Markup($"[bold green]{endpointsLocalizedString}[/]:"), HorizontalAlignment.Right) : Text.Empty,
                                new Markup($"[bold]{resource.EscapeMarkup()}[/] [grey]has endpoint[/] [link={endpoint.EscapeMarkup()}]{endpoint.EscapeMarkup()}[/]")
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
                    // Orderly shutdown
                }
            }

            if (ExtensionHelper.IsExtensionHost(InteractionService, out var extInteractionService, out _))
            {
                extInteractionService.DisplayDashboardUrls(dashboardUrls);
                extInteractionService.NotifyAppHostStartupCompleted();
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
            return HandleProjectLocatorException(ex, InteractionService, Telemetry);
        }
        catch (AppHostIncompatibleException ex)
        {
            Telemetry.RecordError(ex.Message, ex);
            return InteractionService.DisplayIncompatibleVersionError(ex, ex.AspireHostingVersion ?? ex.RequiredCapability);
        }
        catch (CertificateServiceException ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            // Don't display raw output - it's already in the log file
            InteractionService.DisplayMessage("page_facing_up", string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.SeeLogsAt, ExecutionContext.LogFilePath.EscapeMarkup()));
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            // Don't display raw output - it's already in the log file
            InteractionService.DisplayMessage("page_facing_up", string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.SeeLogsAt, ExecutionContext.LogFilePath.EscapeMarkup()));
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

    /// <summary>
    /// Renders the AppHost summary grid with AppHost path, dashboard URL, logs path, and optionally PID.
    /// </summary>
    /// <param name="console">The console to write to.</param>
    /// <param name="appHostRelativePath">The relative path to the AppHost file.</param>
    /// <param name="dashboardUrl">The dashboard URL with login token, or null if not available.</param>
    /// <param name="codespacesUrl">The codespaces URL with login token, or null if not in codespaces.</param>
    /// <param name="logFilePath">The full path to the log file.</param>
    /// <param name="pid">The process ID to display, or null to omit the PID row.</param>
    /// <param name="isExtensionHost">Whether the AppHost is running in the Aspire extension.</param>
    /// <returns>The column width used, for subsequent grid additions.</returns>
    internal static int RenderAppHostSummary(
        IAnsiConsole console,
        string appHostRelativePath,
        string? dashboardUrl,
        string? codespacesUrl,
        string logFilePath,
        bool isExtensionHost,
        int? pid = null)
    {
        console.WriteLine();
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        var appHostLabel = RunCommandStrings.AppHost;
        var dashboardLabel = RunCommandStrings.Dashboard;
        var logsLabel = RunCommandStrings.Logs;
        var pidLabel = RunCommandStrings.ProcessId;

        // Calculate column width based on all possible labels
        var labels = new List<string> { appHostLabel, dashboardLabel, logsLabel };
        if (pid.HasValue)
        {
            labels.Add(pidLabel);
        }
        var longestLabelLength = labels.Max(s => s.Length) + 1; // +1 for colon

        grid.Columns[0].Width = longestLabelLength;

        // AppHost row
        grid.AddRow(
            new Align(new Markup($"[bold green]{appHostLabel}[/]:"), HorizontalAlignment.Right),
            new Text(appHostRelativePath));
        grid.AddRow(Text.Empty, Text.Empty);

        if (!isExtensionHost)
        {
            // Dashboard row
            if (!string.IsNullOrEmpty(dashboardUrl))
            {
                grid.AddRow(
                    new Align(new Markup($"[bold green]{dashboardLabel}[/]:"), HorizontalAlignment.Right),
                    new Markup($"[link={dashboardUrl}]{dashboardUrl}[/]"));

                // Codespaces URL (if available)
                if (!string.IsNullOrEmpty(codespacesUrl))
                {
                    grid.AddRow(Text.Empty, new Markup($"[link={codespacesUrl}]{codespacesUrl}[/]"));
                }
            }
            else
            {
                grid.AddRow(
                    new Align(new Markup($"[bold green]{dashboardLabel}[/]:"), HorizontalAlignment.Right),
                    new Markup("[dim]N/A[/]"));
            }
            grid.AddRow(Text.Empty, Text.Empty);
        }

        // Logs row
        grid.AddRow(
            new Align(new Markup($"[bold green]{logsLabel}[/]:"), HorizontalAlignment.Right),
            new Text(logFilePath));

        // PID row (if provided)
        if (pid.HasValue)
        {
            grid.AddRow(Text.Empty, Text.Empty);
            grid.AddRow(
                new Align(new Markup($"[bold green]{pidLabel}[/]:"), HorizontalAlignment.Right),
                new Text(pid.Value.ToString(CultureInfo.InvariantCulture)));
        }

        var padder = new Padder(grid, new Padding(3, 0));
        console.Write(padder);

        return longestLabelLength;
    }

    private static async Task CaptureAppHostLogsAsync(Diagnostics.FileLoggerProvider fileLoggerProvider, IAppHostCliBackchannel backchannel, IInteractionService interactionService, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield();

            var logEntries = backchannel.GetAppHostLogEntriesAsync(cancellationToken);

            await foreach (var entry in logEntries.WithCancellation(cancellationToken))
            {
                if (ExtensionHelper.IsExtensionHost(interactionService, out var extensionInteractionService, out _))
                {
                    if (entry.LogLevel is not LogLevel.Trace and not LogLevel.Debug)
                    {
                        // Send only information+ level logs to the extension host.
                        extensionInteractionService.WriteDebugSessionMessage(entry.Message, entry.LogLevel is not LogLevel.Error and not LogLevel.Critical, "\x1b[2m");
                    }
                }

                // Write to the unified log file via FileLoggerProvider
                var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var level = entry.LogLevel switch
                {
                    LogLevel.Trace => "TRCE",
                    LogLevel.Debug => "DBUG",
                    LogLevel.Information => "INFO",
                    LogLevel.Warning => "WARN",
                    LogLevel.Error => "FAIL",
                    LogLevel.Critical => "CRIT",
                    _ => entry.LogLevel.ToString().ToUpperInvariant()
                };
                fileLoggerProvider.WriteLog($"[{timestamp}] [{level}] [AppHost/{entry.CategoryName}] {entry.Message}");
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

    /// <summary>
    /// Executes the run command in detached mode by spawning a child CLI process.
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
    private async Task<int> ExecuteDetachedAsync(ParseResult parseResult, FileInfo? passedAppHostProjectFile, bool isExtensionHost, CancellationToken cancellationToken)
    {
        var format = parseResult.GetValue(s_formatOption);

        // When outputting JSON, redirect human-readable messages to stderr
        // so that only the JSON result appears on stdout.
        if (format == OutputFormat.Json)
        {
            _interactionService.UseStderrForMessages = true;
        }

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

        // Compute the expected auxiliary socket path prefix for this AppHost.
        // The hash identifies the AppHost (from project path), while the PID makes each instance unique.
        // Multiple instances of the same AppHost will have the same hash but different PIDs.
        var expectedSocketPrefix = AppHostHelper.ComputeAuxiliarySocketPrefix(
            effectiveAppHostFile.FullName,
            ExecutionContext.HomeDirectory.FullName);
        // We know the format is valid since we just computed it with ComputeAuxiliarySocketPrefix
        var expectedHash = AppHostHelper.ExtractHashFromSocketPath(expectedSocketPrefix)!;

        _logger.LogDebug("Waiting for socket with prefix: {SocketPrefix}, Hash: {Hash}", expectedSocketPrefix, expectedHash);

        // Check for running instance and stop it if found (same behavior as regular run)
        var runningInstanceDetectionEnabled = _features.IsFeatureEnabled(KnownFeatures.RunningInstanceDetectionEnabled, defaultValue: true);
        var existingSockets = AppHostHelper.FindMatchingSockets(
            effectiveAppHostFile.FullName,
            ExecutionContext.HomeDirectory.FullName);

        if (runningInstanceDetectionEnabled && existingSockets.Length > 0)
        {
            _logger.LogDebug("Found {Count} running instance(s) for this AppHost, stopping them first", existingSockets.Length);
            var manager = new RunningInstanceManager(_logger, _interactionService, _timeProvider);
            // Stop all running instances in parallel - don't block on failures
            var stopTasks = existingSockets.Select(socket =>
                manager.StopRunningInstanceAsync(socket, cancellationToken));
            await Task.WhenAll(stopTasks).ConfigureAwait(false);
        }

        // Build the arguments for the child CLI process
        // Tell the child where to write its log so we can find it on failure.
        var childLogFile = GenerateChildLogFilePath();

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
        args.AddRange(RootCommand.GetChildProcessArgs(parseResult));

        // Pass through run-specific options
        if (parseResult.GetValue(s_isolatedOption))
        {
            args.Add("--isolated");
        }
        if (parseResult.GetValue(s_noBuildOption))
        {
            args.Add("--no-build");
        }

        // Pass through any unmatched tokens (but not --detach since child shouldn't detach again)
        foreach (var token in parseResult.UnmatchedTokens)
        {
            if (token != "--detach")
            {
                args.Add(token);
            }
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

        _logger.LogDebug("Spawning child CLI: {Executable} (isDotnetHost={IsDotnetHost}) with args: {Args}",
            dotnetPath, isDotnetHost, string.Join(" ", args));
        _logger.LogDebug("Working directory: {WorkingDirectory}", ExecutionContext.WorkingDirectory.FullName);

        // Build the full argument list for the child process, including the entry assembly
        // path when running via `dotnet aspire.dll`
        var childArgs = new List<string>();
        if (isDotnetHost && !string.IsNullOrEmpty(entryAssemblyPath) && entryAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            childArgs.Add(entryAssemblyPath);
        }

        childArgs.AddRange(args);

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
                    ExecutionContext.WorkingDirectory.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start child CLI process");
                return null;
            }

            _logger.LogDebug("Child CLI process started with PID: {PID}", childProcess.Id);

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
                    _logger.LogWarning("Child CLI process exited with code {ExitCode}", childExitCode);
                    return null;
                }

                // Trigger a scan and try to connect
                await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);

                // Check if we can find a connection for this AppHost by hash
                var connection = _backchannelMonitor.GetConnectionsByHash(expectedHash).FirstOrDefault();
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
            backchannel = await _interactionService.ShowStatusAsync(
                RunCommandStrings.StartingAppHostInBackground,
                StartAndWaitForBackchannelAsync);
        }

        // Handle failure cases - show specific error and log file path
        if (backchannel is null || childProcess is null)
        {
            if (childProcess is null)
            {
                _interactionService.DisplayError(RunCommandStrings.FailedToStartAppHost);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            if (childExitedEarly)
            {
                // Show a friendly message based on well-known exit codes from the child
                _interactionService.DisplayError(GetDetachedFailureMessage(childExitCode));
            }
            else
            {
                _interactionService.DisplayError(RunCommandStrings.TimeoutWaitingForAppHost);

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

            // Point to the child's log file — it contains the actual build/runtime errors
            _interactionService.DisplayMessage("magnifying_glass_tilted_right", string.Format(
                CultureInfo.CurrentCulture,
                RunCommandStrings.CheckLogsForDetails,
                childLogFile.EscapeMarkup()));

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
            _interactionService.DisplayRawText(json);
        }
        else
        {
            // Display success UX using shared rendering
            var appHostRelativePath = Path.GetRelativePath(ExecutionContext.WorkingDirectory.FullName, effectiveAppHostFile.FullName);
            RenderAppHostSummary(
                _ansiConsole,
                appHostRelativePath,
                dashboardUrls?.BaseUrlWithLoginToken,
                codespacesUrl: null,
                childLogFile,
                isExtensionHost,
                pid);
            _ansiConsole.WriteLine();

            _interactionService.DisplaySuccess(RunCommandStrings.AppHostStartedSuccessfully);
        }

        return ExitCodeConstants.Success;
    }

    internal static string GetDetachedFailureMessage(int childExitCode)
    {
        return childExitCode switch
        {
            ExitCodeConstants.FailedToBuildArtifacts => RunCommandStrings.AppHostFailedToBuild,
            _ => string.Format(CultureInfo.CurrentCulture, RunCommandStrings.AppHostExitedWithCode, childExitCode)
        };
    }

    internal static string GenerateChildLogFilePath(string logsDirectory, TimeProvider timeProvider)
    {
        var timestamp = timeProvider.GetUtcNow().ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var uniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var fileName = $"cli_{timestamp}_detach-child_{uniqueId}.log";
        return Path.Combine(logsDirectory, fileName);
    }

    private string GenerateChildLogFilePath()
    {
        return GenerateChildLogFilePath(ExecutionContext.LogsDirectory.FullName, _timeProvider);
    }
}
