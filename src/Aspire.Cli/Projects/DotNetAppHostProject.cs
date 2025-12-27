// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
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
    // Constants for running instance detection
    private const int ProcessTerminationTimeoutMs = 10000; // Wait up to 10 seconds for processes to terminate
    private const int ProcessTerminationPollIntervalMs = 250; // Check process status every 250ms

    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IFeatures _features;
    private readonly ILogger<DotNetAppHostProject> _logger;
    private readonly TimeProvider _timeProvider;

    public DotNetAppHostProject(
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ILogger<DotNetAppHostProject> logger,
        TimeProvider? timeProvider = null)
    {
        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _telemetry = telemetry;
        _features = features;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
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
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var effectiveAppHostFile = context.AppHostFile;
        var isExtensionHost = ExtensionHelper.IsExtensionHost(_interactionService, out _, out _);

        var buildOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;

        using var activity = _telemetry.ActivitySource.StartActivity("run");

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
                    context.BuildCompletionSource?.TrySetResult(false);
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
            context.BuildCompletionSource?.TrySetResult(false);
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        // Signal that build/preparation is complete
        context.BuildCompletionSource?.TrySetResult(true);

        var runOptions = new DotNetCliRunnerInvocationOptions
        {
            StartDebugSession = context.StartDebugSession,
            Debug = context.Debug
        };

        // The backchannel completion source is the contract with RunCommand
        // We signal this when the backchannel is ready, RunCommand uses it for UX
        var backchannelCompletionSource = context.BackchannelCompletionSource ?? new TaskCompletionSource<IAppHostCliBackchannel>();

        if (isSingleFileAppHost)
        {
            ConfigureSingleFileEnvironment(effectiveAppHostFile, env);
        }

        // Start the apphost - the runner will signal the backchannel when ready
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
                    $"The app host is not compatible. Aspire.Hosting version: {compatibilityCheck.AspireHostingVersion}",
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
                _interactionService.DisplayLines(buildOutputCollector.GetLines());
                _interactionService.DisplayError(InteractionServiceStrings.ProjectCouldNotBeBuilt);
                // Signal the backchannel completion source so the caller doesn't wait forever
                context.BackchannelCompletionSource?.TrySetException(
                    new InvalidOperationException("The app host build failed."));
                return ExitCodeConstants.FailedToBuildArtifacts;
            }
        }

        var runOptions = new DotNetCliRunnerInvocationOptions
        {
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
        var options = new DotNetCliRunnerInvocationOptions();
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
    public async Task<bool> CheckAndHandleRunningInstanceAsync(FileInfo appHostFile, DirectoryInfo homeDirectory, CancellationToken cancellationToken)
    {
        var auxiliarySocketPath = AppHostHelper.ComputeAuxiliarySocketPath(appHostFile.FullName, homeDirectory.FullName);

        // Check if the socket file exists
        if (!File.Exists(auxiliarySocketPath))
        {
            return true; // No running instance, continue
        }

        // Stop the running instance (no prompt per mitchdenny's request)
        return await StopRunningInstanceAsync(auxiliarySocketPath, cancellationToken);
    }

    private async Task<bool> StopRunningInstanceAsync(string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            // Connect to the auxiliary backchannel
            using var backchannel = await AppHostAuxiliaryBackchannel.ConnectAsync(socketPath, _logger, cancellationToken).ConfigureAwait(false);

            // Get the AppHost information
            var appHostInfo = backchannel.AppHostInfo;

            if (appHostInfo is null)
            {
                _logger.LogWarning("Failed to get AppHost information from running instance");
                return false;
            }

            // Display message that we're stopping the previous instance
            var cliPidText = appHostInfo.CliProcessId.HasValue ? appHostInfo.CliProcessId.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
            _interactionService.DisplayMessage("stop_sign", $"Stopping previous instance (AppHost PID: {appHostInfo.ProcessId.ToString(CultureInfo.InvariantCulture)}, CLI PID: {cliPidText})");

            // Call StopAppHostAsync on the auxiliary backchannel
            await backchannel.StopAppHostAsync(cancellationToken).ConfigureAwait(false);

            // Monitor the PIDs for termination
            var stopped = await MonitorProcessesForTerminationAsync(appHostInfo, cancellationToken).ConfigureAwait(false);

            if (stopped)
            {
                _interactionService.DisplaySuccess(RunCommandStrings.RunningInstanceStopped);
            }
            else
            {
                _logger.LogWarning("Failed to stop running instance within timeout");
            }

            return stopped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop running instance");
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
                    var process = Process.GetProcessById(pid);
                    // If we can get the process, it's still running
                    allStopped = false;
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist, it has stopped
                }
            }

            if (allStopped)
            {
                return true;
            }

            await Task.Delay(ProcessTerminationPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        // Timeout reached
        return false;
    }
}
