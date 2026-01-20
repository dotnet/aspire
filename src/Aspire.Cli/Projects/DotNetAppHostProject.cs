// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.Dcp;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Handler for .NET AppHost projects (.csproj and single-file .cs).
/// </summary>
internal sealed class DotNetAppHostProject : IAppHostProject
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IFeatures _features;
    private readonly ILogger<DotNetAppHostProject> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IProjectUpdater _projectUpdater;
    private readonly RunningInstanceManager _runningInstanceManager;
    private readonly IDcpLauncher _dcpLauncher;
    private readonly IDcpClient _dcpClient;

    private static readonly string[] s_detectionPatterns = ["*.csproj", "*.fsproj", "*.vbproj", "apphost.cs"];
    private static readonly string[] s_projectExtensions = [".csproj", ".fsproj", ".vbproj"];

    // Session suffixes are deterministic per project path for consistent resource naming across CLI restarts.
    // This allows the apphost to clean up stale resources from previous runs.
    private readonly Dictionary<string, string> _sessionSuffixes = new(StringComparer.OrdinalIgnoreCase);

    public DotNetAppHostProject(
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        AspireCliTelemetry telemetry,
        IFeatures features,
        IProjectUpdater projectUpdater,
        IDcpLauncher dcpLauncher,
        IDcpClient dcpClient,
        ILogger<DotNetAppHostProject> logger,
        TimeProvider? timeProvider = null)
    {
        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _telemetry = telemetry;
        _features = features;
        _projectUpdater = projectUpdater;
        _dcpLauncher = dcpLauncher;
        _dcpClient = dcpClient;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _runningInstanceManager = new RunningInstanceManager(_logger, _interactionService, _timeProvider);
    }

    // ═══════════════════════════════════════════════════════════════
    // IDENTITY
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string LanguageId => KnownLanguageId.CSharp;

    /// <inheritdoc />
    public string DisplayName => "C# (.NET)";

    // ═══════════════════════════════════════════════════════════════
    // DETECTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Task<string[]> GetDetectionPatternsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(s_detectionPatterns);

    /// <inheritdoc />
    public bool CanHandle(FileInfo appHostFile)
    {
        var extension = appHostFile.Extension.ToLowerInvariant();

        // Handle project files (.csproj, .fsproj, .vbproj)
        if (s_projectExtensions.Contains(extension))
        {
            // We can handle any project file - ValidateAsync will do deeper validation
            return true;
        }

        // Handle single-file apphosts (apphost.cs)
        if (extension == ".cs" && appHostFile.Name.Equals("apphost.cs", StringComparison.OrdinalIgnoreCase))
        {
            // Check for #:sdk Aspire.AppHost.Sdk directive
            return IsValidSingleFileAppHost(appHostFile);
        }

        return false;
    }

    private static bool IsValidSingleFileAppHost(FileInfo candidateFile)
    {
        // Check no sibling .csproj files exist
        var siblingCsprojFiles = candidateFile.Directory!.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly);
        if (siblingCsprojFiles.Any())
        {
            return false;
        }

        // Check for #:sdk Aspire.AppHost.Sdk directive
        try
        {
            using var reader = candidateFile.OpenText();
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith("#:sdk Aspire.AppHost.Sdk", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string? AppHostFileName => "apphost.cs";

    // ═══════════════════════════════════════════════════════════════
    // EXECUTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<AppHostValidationResult> ValidateAppHostAsync(FileInfo appHostFile, CancellationToken cancellationToken)
    {
        var isSingleFile = appHostFile.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);

        if (isSingleFile)
        {
            // For single-file apphosts, validate that:
            // 1. No sibling .csproj files exist (otherwise it's part of a project)
            // 2. The file contains the #:sdk Aspire.AppHost.Sdk directive
            return new AppHostValidationResult(IsValid: IsValidSingleFileAppHost(appHostFile));
        }

        // For project files, check if it's a valid Aspire AppHost using GetAppHostInformationAsync
        var information = await _runner.GetAppHostInformationAsync(appHostFile, new DotNetCliRunnerInvocationOptions(), cancellationToken);

        if (information.ExitCode == 0 && information.Info?.IsAspireHost == true)
        {
            return new AppHostValidationResult(IsValid: true);
        }

        // Check if it's possibly an unbuildable AppHost (has the right name pattern but couldn't be validated)
        var isPossiblyUnbuildable = IsPossiblyUnbuildableAppHost(appHostFile);

        return new AppHostValidationResult(
            IsValid: false,
            IsPossiblyUnbuildable: isPossiblyUnbuildable);
    }

    private static bool IsPossiblyUnbuildableAppHost(FileInfo projectFile)
    {
        var fileNameSuggestsAppHost = projectFile.Name.EndsWith("AppHost.csproj", StringComparison.OrdinalIgnoreCase);
        var folderContainsAppHostCSharpFile = projectFile.Directory!
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .Any(f => f.Name.Equals("AppHost.cs", StringComparison.OrdinalIgnoreCase));
        return fileNameSuggestsAppHost || folderContainsAppHostCSharpFile;
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var effectiveAppHostFile = context.AppHostFile;
        var isExtensionHost = ExtensionHelper.IsExtensionHost(_interactionService, out _, out _);

        var buildOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, AppHostInfo? Info)? appHostCompatibilityCheck = null;

        using var activity = _telemetry.ActivitySource.StartActivity("run");

        var isSingleFileAppHost = effectiveAppHostFile.Extension != ".csproj";

        var env = new Dictionary<string, string>(context.EnvironmentVariables);

        // Use a deterministic session suffix based on the project path for resource naming.
        // This suffix is consistent across CLI restarts for the same project,
        // allowing the apphost to clean up stale resources from previous runs.
        var projectPath = effectiveAppHostFile.FullName;
        var sessionSuffix = GetSessionSuffix(projectPath);
        env["DcpPublisher__ResourceNameSuffix"] = sessionSuffix;
        _logger.LogDebug("Using session suffix for resource naming: {SessionSuffix} (project: {ProjectPath})", sessionSuffix, projectPath);

        if (context.WaitForDebugger)
        {
            env[KnownConfigNames.WaitForDebugger] = "true";
        }

        try
        {
            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
        }
        catch
        {
            // Signal that build/preparation failed so RunCommand doesn't hang waiting
            context.BuildCompletionSource?.TrySetResult(false);
            throw;
        }

        var watch = !isSingleFileAppHost && (context.Watch || _features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, defaultValue: false) || (isExtensionHost && !context.StartDebugSession));

        try
        {
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
                        // Set OutputCollector so RunCommand can display errors
                        context.OutputCollector = buildOutputCollector;
                        context.BuildCompletionSource?.TrySetResult(false);
                        return ExitCodeConstants.FailedToBuildArtifacts;
                    }
                }
            }

            if (isSingleFileAppHost)
            {
                // TODO: Add logic to read SDK version from *.cs file.
                appHostCompatibilityCheck = (true, true, new AppHostInfo(
                    IsAspireHost: true,
                    AspireHostingVersion: VersionHelper.GetDefaultTemplateVersion(),
                    DcpCliPath: null,
                    DcpExtensionsPath: null,
                    DcpBinPath: null,
                    DashboardPath: null,
                    ContainerRuntime: null));
            }
            else
            {
                appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostFile, _telemetry, context.WorkingDirectory, cancellationToken);
            }
        }
        catch
        {
            // Signal that build/preparation failed so RunCommand doesn't hang waiting
            context.BuildCompletionSource?.TrySetResult(false);
            throw;
        }

        if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
        {
            context.BuildCompletionSource?.TrySetResult(false);
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        // The backchannel completion source is the contract with RunCommand
        // We signal this when the backchannel is ready, RunCommand uses it for UX
        // Declared early so it can be used in resource URL polling
        var backchannelCompletionSource = context.BackchannelCompletionSource ?? new TaskCompletionSource<IAppHostCliBackchannel>();

        // Launch CLI-owned DCP if the feature is enabled
        DcpSession? dcpSession = null;
        var dcpEnabled = _features.IsFeatureEnabled(KnownFeatures.DcpEnabled, defaultValue: false);
        _logger.LogDebug("CLI-owned DCP feature enabled: {DcpEnabled}", dcpEnabled);

        if (dcpEnabled)
        {
            var appHostInfo = appHostCompatibilityCheck?.Info;
            _logger.LogDebug("DcpCliPath from AppHost: {DcpCliPath}", appHostInfo?.DcpCliPath ?? "(null)");
            _logger.LogDebug("DashboardPath from AppHost: {DashboardPath}", appHostInfo?.DashboardPath ?? "(null)");

            if (appHostInfo != null && !string.IsNullOrEmpty(appHostInfo.DcpCliPath))
            {
                try
                {
                    dcpSession = await _interactionService.ShowStatusAsync(
                        "Starting DCP...",
                        async () => await _dcpLauncher.LaunchAsync(appHostInfo, cancellationToken));

                    // Pass kubeconfig path to AppHost so it knows to use CLI-owned DCP
                    env["DCP_KUBECONFIG_PATH"] = dcpSession.KubeconfigPath;
                    _logger.LogDebug("CLI-owned DCP started with kubeconfig at {KubeconfigPath}", dcpSession.KubeconfigPath);

                    // Create CLI-owned dashboard via DCP if dashboard path is available
                    if (!string.IsNullOrEmpty(appHostInfo.DashboardPath))
                    {
                        var dashboardHelper = new DcpDashboardHelper(_dcpClient, _logger);
                        await dashboardHelper.CreateCliOwnedDashboardAsync(
                            appHostInfo,
                            effectiveAppHostFile.FullName,
                            dcpSession,
                            env,
                            cancellationToken);

                        // Stream dashboard logs when debug mode is enabled
                        if (context.Debug)
                        {
                            _ = Task.Run(() => dashboardHelper.StreamDashboardLogsAsync(cancellationToken), cancellationToken);
                        }

                        // Start polling for resource service URL once backchannel is ready
                        var sessionDir = dcpSession.SessionDir;
                        _ = PollAndUpdateResourceServiceUrlAsync(backchannelCompletionSource.Task, sessionDir, watch, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start CLI-owned DCP. Falling back to AppHost-owned DCP.");
                    _interactionService.DisplayMessage("warning", $"Failed to start DCP: {ex.Message}. Falling back to default mode.");
                    dcpSession = null;
                }
            }
            else
            {
                _logger.LogDebug("CLI-owned DCP skipped: DcpCliPath not available");
            }
        }

        // Create collector and store in context for exception handling
        // This must be set BEFORE signaling build completion to avoid a race condition
        var runOutputCollector = new OutputCollector();
        context.OutputCollector = runOutputCollector;

        // Signal that build/preparation is complete
        context.BuildCompletionSource?.TrySetResult(true);

        var runOptions = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = runOutputCollector.AppendOutput,
            StandardErrorCallback = runOutputCollector.AppendError,
            StartDebugSession = context.StartDebugSession,
            Debug = context.Debug
        };

        if (isSingleFileAppHost)
        {
            ConfigureSingleFileEnvironment(effectiveAppHostFile, env);
        }

        // Start the apphost - the runner will signal the backchannel when ready
        // Note: DCP has its own orphan detection and will shut itself down when the CLI exits
        return await _runner.RunAsync(
            effectiveAppHostFile,
            watch,
            !watch,
            context.UnmatchedTokens,
            env,
            backchannelCompletionSource,
            runOptions,
            cancellationToken);
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

    /// <inheritdoc />
    public async Task<int> PublishAsync(PublishContext context, CancellationToken cancellationToken)
    {
        var effectiveAppHostFile = context.AppHostFile;
        var isSingleFileAppHost = effectiveAppHostFile.Extension != ".csproj";
        var env = new Dictionary<string, string>(context.EnvironmentVariables);

        // Check compatibility for project-based apphosts
        if (!isSingleFileAppHost)
        {
            var compatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(
                _runner,
                _interactionService,
                effectiveAppHostFile,
                _telemetry,
                context.WorkingDirectory,
                cancellationToken);

            if (!compatibilityCheck.IsCompatibleAppHost)
            {
                var exception = new AppHostIncompatibleException(
                    $"The app host is not compatible. Aspire.Hosting version: {compatibilityCheck.Info?.AspireHostingVersion}",
                    "Aspire.Hosting");
                // Signal the backchannel completion source so the caller doesn't wait forever
                context.BackchannelCompletionSource?.TrySetException(exception);
                throw exception;
            }

            // Build the apphost
            var buildOutputCollector = new OutputCollector();
            var buildOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = buildOutputCollector.AppendOutput,
                StandardErrorCallback = buildOutputCollector.AppendError,
            };

            var buildExitCode = await AppHostHelper.BuildAppHostAsync(
                _runner,
                _interactionService,
                effectiveAppHostFile,
                buildOptions,
                context.WorkingDirectory,
                cancellationToken);

            if (buildExitCode != 0)
            {
                // Set OutputCollector so PipelineCommandBase can display errors
                context.OutputCollector = buildOutputCollector;
                // Signal the backchannel completion source so the caller doesn't wait forever
                context.BackchannelCompletionSource?.TrySetException(
                    new InvalidOperationException("The app host build failed."));
                return ExitCodeConstants.FailedToBuildArtifacts;
            }
        }

        // Create collector and store in context for exception handling
        var runOutputCollector = new OutputCollector();
        context.OutputCollector = runOutputCollector;

        var runOptions = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = runOutputCollector.AppendOutput,
            StandardErrorCallback = runOutputCollector.AppendError,
            NoLaunchProfile = true,
            NoExtensionLaunch = true
        };

        if (isSingleFileAppHost)
        {
            ConfigureSingleFileEnvironment(effectiveAppHostFile, env);
        }

        return await _runner.RunAsync(
            effectiveAppHostFile,
            watch: false,
            noBuild: true,
            context.Arguments,
            env,
            context.BackchannelCompletionSource,
            runOptions,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AddPackageAsync(AddPackageContext context, CancellationToken cancellationToken)
    {
        var outputCollector = new OutputCollector();
        context.OutputCollector = outputCollector;

        var options = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = outputCollector.AppendOutput,
            StandardErrorCallback = outputCollector.AppendError,
        };
        var result = await _runner.AddPackageAsync(
            context.AppHostFile,
            context.PackageId,
            context.PackageVersion,
            context.Source,
            options,
            cancellationToken);

        return result == 0;
    }

    /// <inheritdoc />
    public async Task<UpdatePackagesResult> UpdatePackagesAsync(UpdatePackagesContext context, CancellationToken cancellationToken)
    {
        var result = await _projectUpdater.UpdateProjectAsync(context.AppHostFile, context.Channel, cancellationToken);
        return new UpdatePackagesResult { UpdatesApplied = result.UpdatedApplied };
    }

    /// <inheritdoc />
    public async Task<bool> CheckAndHandleRunningInstanceAsync(FileInfo appHostFile, DirectoryInfo homeDirectory, CancellationToken cancellationToken)
    {
        var auxiliarySocketPath = AppHostHelper.ComputeAuxiliarySocketPath(appHostFile.FullName, homeDirectory.FullName);

        // Check if the socket file exists
        if (!File.Exists(auxiliarySocketPath))
        {
            return true; // No running instance, continue
        }

        // Stop the running instance (no prompt per mitchdenny's request)
        return await _runningInstanceManager.StopRunningInstanceAsync(auxiliarySocketPath, cancellationToken);
    }

    /// <summary>
    /// Monitors for the resource service URL from the AppHost and updates the dashboard config file.
    /// In watch mode, uses long-polling to efficiently detect URL changes when AppHost restarts.
    /// </summary>
    private async Task PollAndUpdateResourceServiceUrlAsync(
        Task<IAppHostCliBackchannel> backchannelTask,
        string dcpSessionDir,
        bool watchMode,
        CancellationToken cancellationToken)
    {
        string? lastUrl = null;
        const int InitialPollDelayMs = 500;

        _logger.LogDebug("Starting resource service URL monitoring (session dir: {SessionDir}, watch mode: {WatchMode})", dcpSessionDir, watchMode);

        try
        {
            // Wait for backchannel to be ready
            var backchannel = await backchannelTask.ConfigureAwait(false);

            // Initial poll with retries to wait for DashboardServiceHost to start
            for (var attempt = 0; attempt < 30 && !cancellationToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    var urlInfo = await backchannel.GetResourceServiceUrlAsync(cancellationToken).ConfigureAwait(false);
                    if (urlInfo?.Url is not null)
                    {
                        if (lastUrl != urlInfo.Url)
                        {
                            _logger.LogInformation("Resource service URL: {Url}", urlInfo.Url);
                            await Dcp.DcpDashboardHelper.UpdateResourceServiceUrlAsync(dcpSessionDir, urlInfo.Url, cancellationToken);
                            lastUrl = urlInfo.Url;
                        }
                        break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogDebug(ex, "Failed to get resource service URL (attempt {Attempt})", attempt + 1);
                }

                await Task.Delay(InitialPollDelayMs, cancellationToken).ConfigureAwait(false);
            }

            // In watch mode, continue monitoring for URL changes (AppHost restarts)
            if (watchMode && lastUrl is not null)
            {
                _logger.LogDebug("Watch mode enabled, monitoring for resource service URL changes");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var urlInfo = await backchannel.WaitForResourceServiceUrlChangeAsync(lastUrl, cancellationToken).ConfigureAwait(false);
                        if (urlInfo?.Url is not null && urlInfo.Url != lastUrl)
                        {
                            _logger.LogInformation("Resource service URL changed: {Url}", urlInfo.Url);
                            await Dcp.DcpDashboardHelper.UpdateResourceServiceUrlAsync(dcpSessionDir, urlInfo.Url, cancellationToken);
                            lastUrl = urlInfo.Url;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogDebug(ex, "Error monitoring resource service URL, retrying...");
                        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Resource service URL monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resource service URL monitoring failed");
        }
    }

    /// <summary>
    /// Gets a deterministic session suffix for the given project path.
    /// Caches the suffix to ensure consistency within the CLI session.
    /// </summary>
    private string GetSessionSuffix(string projectPath)
    {
        if (!_sessionSuffixes.TryGetValue(projectPath, out var suffix))
        {
            suffix = GenerateSessionSuffix(projectPath);
            _sessionSuffixes[projectPath] = suffix;
        }
        return suffix;
    }

    /// <summary>
    /// Generates an 8-character lowercase suffix for resource naming.
    /// This suffix is deterministic based on the project path,
    /// ensuring the same project always gets the same suffix.
    /// </summary>
    private static string GenerateSessionSuffix(string projectPath)
    {
        // Generate a deterministic suffix based on the project path
        // This ensures the same project always gets the same suffix,
        // allowing cleanup of resources from previous runs
        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(projectPath));
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        var suffix = new char[8];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = chars[hashBytes[i] % chars.Length];
        }
        return new string(suffix);
    }
}
