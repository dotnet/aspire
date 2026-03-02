// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Aspire.Shared.UserSecrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Semver;
using Spectre.Console;

namespace Aspire.Cli.Projects;

/// <summary>
/// Handler for guest (non-.NET) AppHost projects.
/// Supports any language registered via <see cref="ILanguageDiscovery"/>.
/// </summary>
internal sealed class GuestAppHostProject : IAppHostProject
{
    private const string GeneratedFolderName = ".modules";

    private readonly IInteractionService _interactionService;
    private readonly IAppHostCliBackchannel _backchannel;
    private readonly IAppHostServerProjectFactory _appHostServerProjectFactory;
    private readonly ICertificateService _certificateService;
    private readonly IDotNetCliRunner _runner;
    private readonly IPackagingService _packagingService;
    private readonly IConfiguration _configuration;
    private readonly IFeatures _features;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly ILogger<GuestAppHostProject> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly RunningInstanceManager _runningInstanceManager;

    // Language is always resolved via constructor
    private readonly LanguageInfo _resolvedLanguage;
    private GuestRuntime? _guestRuntime;

    public GuestAppHostProject(
        LanguageInfo language,
        IInteractionService interactionService,
        IAppHostCliBackchannel backchannel,
        IAppHostServerProjectFactory appHostServerProjectFactory,
        ICertificateService certificateService,
        IDotNetCliRunner runner,
        IPackagingService packagingService,
        IConfiguration configuration,
        IFeatures features,
        ILanguageDiscovery languageDiscovery,
        ILogger<GuestAppHostProject> logger,
        TimeProvider? timeProvider = null)
    {
        _resolvedLanguage = language;
        _interactionService = interactionService;
        _backchannel = backchannel;
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _certificateService = certificateService;
        _runner = runner;
        _packagingService = packagingService;
        _configuration = configuration;
        _features = features;
        _languageDiscovery = languageDiscovery;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _runningInstanceManager = new RunningInstanceManager(_logger, _interactionService, _timeProvider);
    }

    // ═══════════════════════════════════════════════════════════════
    // IDENTITY (Always resolved via constructor)
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string LanguageId => _resolvedLanguage.LanguageId;

    /// <inheritdoc />
    public string DisplayName => _resolvedLanguage.DisplayName;

    /// <summary>
    /// Gets the effective SDK version from configuration (inherits from parent directories)
    /// or falls back to the default SDK version.
    /// </summary>
    private string GetEffectiveSdkVersion()
    {
        // IConfiguration merges settings from parent directories and global settings
        // The key "sdkVersion" is the flattened key from settings.json
        var configuredVersion = _configuration["sdkVersion"];
        if (!string.IsNullOrEmpty(configuredVersion))
        {
            _logger.LogDebug("Using SDK version from configuration: {Version}", configuredVersion);
            return configuredVersion;
        }
        
        _logger.LogDebug("Using default SDK version: {Version}", DotNetBasedAppHostServerProject.DefaultSdkVersion);
        return DotNetBasedAppHostServerProject.DefaultSdkVersion;
    }

    // ═══════════════════════════════════════════════════════════════
    // DETECTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Task<string[]> GetDetectionPatternsAsync(CancellationToken cancellationToken = default)
    {
        // Return the detection patterns for this specific language
        return Task.FromResult(_resolvedLanguage.DetectionPatterns);
    }

    /// <inheritdoc />
    public bool CanHandle(FileInfo appHostFile)
    {
        // Check if file matches this language's detection patterns
        return _resolvedLanguage.DetectionPatterns.Any(p => 
            appHostFile.Name.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string? AppHostFileName => _resolvedLanguage.DetectionPatterns.FirstOrDefault();

    /// <inheritdoc />
    public bool IsUsingProjectReferences(FileInfo appHostFile)
    {
        return AspireRepositoryDetector.DetectRepositoryRoot(appHostFile.Directory?.FullName) is not null;
    }

    /// <summary>
    /// Gets all packages including the code generation package for the current language.
    /// </summary>
    private async Task<List<(string Name, string Version)>> GetAllPackagesAsync(
        AspireJsonConfiguration config,
        CancellationToken cancellationToken)
    {
        var defaultSdkVersion = GetEffectiveSdkVersion();
        var packages = config.GetAllPackages(defaultSdkVersion).ToList();
        var codeGenPackage = await _languageDiscovery.GetPackageForLanguageAsync(_resolvedLanguage.LanguageId, cancellationToken);
        if (codeGenPackage is not null)
        {
            var codeGenVersion = config.GetEffectiveSdkVersion(defaultSdkVersion);
            packages.Add((codeGenPackage, codeGenVersion));
        }
        return packages;
    }

    private AspireJsonConfiguration LoadConfiguration(DirectoryInfo directory)
    {
        var effectiveSdkVersion = GetEffectiveSdkVersion();
        return AspireJsonConfiguration.LoadOrCreate(directory.FullName, effectiveSdkVersion);
    }

    private string GetPrepareSdkVersion(AspireJsonConfiguration config)
    {
        return config.GetEffectiveSdkVersion(GetEffectiveSdkVersion());
    }

    /// <summary>
    /// Prepares the AppHost server (creates files and builds for dev mode, restores packages for prebuilt mode).
    /// </summary>
    private static async Task<(bool Success, OutputCollector? Output, string? ChannelName, bool NeedsCodeGen)> PrepareAppHostServerAsync(
        IAppHostServerProject appHostServerProject,
        string sdkVersion,
        List<(string Name, string Version)> packages,
        CancellationToken cancellationToken)
    {
        var result = await appHostServerProject.PrepareAsync(sdkVersion, packages, cancellationToken);
        return (result.Success, result.Output, result.ChannelName, result.NeedsCodeGeneration);
    }

    /// <summary>
    /// Builds the AppHost server project and generates SDK code.
    /// </summary>
    private async Task BuildAndGenerateSdkAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        var appHostServerProject = await _appHostServerProjectFactory.CreateAsync(directory.FullName, cancellationToken);

        // Step 1: Load config - source of truth for SDK version and packages
        var config = LoadConfiguration(directory);
        var packages = await GetAllPackagesAsync(config, cancellationToken);
        var sdkVersion = GetPrepareSdkVersion(config);

        var (buildSuccess, buildOutput, _, _) = await PrepareAppHostServerAsync(appHostServerProject, sdkVersion, packages, cancellationToken);
        if (!buildSuccess)
        {
            if (buildOutput is not null)
            {
                _interactionService.DisplayLines(buildOutput.GetLines());
            }
            _interactionService.DisplayError("Failed to prepare AppHost server.");
            return;
        }

        // Step 2: Start the AppHost server temporarily for code generation
        var currentPid = Environment.ProcessId;
        var (socketPath, serverProcess, _) = appHostServerProject.Run(currentPid);

        try
        {
            // Step 3: Connect to server
            await using var rpcClient = await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);

            // Step 4: Install dependencies using GuestRuntime (best effort - don't block code generation)
            await InstallDependenciesAsync(directory, rpcClient, cancellationToken);

            // Step 5: Generate SDK code via RPC
            await GenerateCodeViaRpcAsync(
                directory.FullName,
                rpcClient,
                packages,
                cancellationToken);
        }
        finally
        {
            // Step 6: Stop the server (we were just generating code)
            if (!serverProcess.HasExited)
            {
                try
                {
                    serverProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process after code generation");
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EXECUTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Task<AppHostValidationResult> ValidateAppHostAsync(FileInfo appHostFile, CancellationToken cancellationToken)
    {
        // Check if the file exists
        if (!appHostFile.Exists)
        {
            _logger.LogDebug("AppHost file {File} does not exist", appHostFile.FullName);
            return Task.FromResult(new AppHostValidationResult(IsValid: false));
        }

        // Use the resolved language's detection patterns (set in constructor)
        var patterns = _resolvedLanguage.DetectionPatterns;
        if (!patterns.Any(p => appHostFile.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("AppHost file {File} does not match {Language} detection patterns: {Patterns}", 
                appHostFile.Name, _resolvedLanguage.DisplayName, string.Join(", ", patterns));
            return Task.FromResult(new AppHostValidationResult(IsValid: false));
        }

        // Guest languages don't have the "possibly unbuildable" concept
        // Detailed validation is delegated to the server-side language support
        _logger.LogDebug("Validated {Language} AppHost: {File}", _resolvedLanguage.DisplayName, appHostFile.FullName);
        return Task.FromResult(new AppHostValidationResult(IsValid: true));
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Running {Language} AppHost: {AppHostFile}", DisplayName, appHostFile.FullName);

        try
        {
            // Step 1: Ensure certificates are trusted
            Dictionary<string, string> certEnvVars;
            try
            {
                var certResult = await _certificateService.EnsureCertificatesTrustedAsync(cancellationToken);
                certEnvVars = new Dictionary<string, string>(certResult.EnvironmentVariables);
            }
            catch
            {
                context.BuildCompletionSource?.TrySetResult(false);
                throw;
            }

            // Build phase: build AppHost server (dependency install happens after server starts)
            var appHostServerProject = await _appHostServerProjectFactory.CreateAsync(directory.FullName, cancellationToken);

            // Load config - source of truth for SDK version and packages
            var config = LoadConfiguration(directory);
            var packages = await GetAllPackagesAsync(config, cancellationToken);
            var sdkVersion = GetPrepareSdkVersion(config);

            var buildResult = await _interactionService.ShowStatusAsync(
                "Preparing Aspire server...",
                async () =>
                {
                    // Prepare the AppHost server (build for dev mode, restore for prebuilt)
                    var (prepareSuccess, prepareOutput, channelName, needsCodeGen) = await PrepareAppHostServerAsync(appHostServerProject, sdkVersion, packages, cancellationToken);
                    if (!prepareSuccess)
                    {
                        return (Success: false, Output: prepareOutput, Error: "Failed to prepare app host.", ChannelName: (string?)null, NeedsCodeGen: false);
                    }

                    return (Success: true, Output: prepareOutput, Error: (string?)null, ChannelName: channelName, NeedsCodeGen: needsCodeGen);
                }, emoji: KnownEmojis.Gear);

            // Save the channel to settings.json if available (config already has SdkVersion)
            if (buildResult.ChannelName is not null)
            {
                config.Channel = buildResult.ChannelName;
                config.Save(directory.FullName);
            }

            if (!buildResult.Success)
            {
                // Set OutputCollector so RunCommand can display errors
                context.OutputCollector = buildResult.Output;
                context.BuildCompletionSource?.TrySetResult(false);
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Store output collector in context for exception handling by RunCommand
            // This must be set BEFORE signaling build completion to avoid a race condition
            context.OutputCollector = buildResult.Output;

            // Signal that build/preparation is complete
            context.BuildCompletionSource?.TrySetResult(true);

            // Read launch settings and set shared environment variables
            var launchSettingsEnvVars = GetServerEnvironmentVariables(directory);

            // Apply certificate environment variables (e.g., SSL_CERT_DIR on Linux)
            foreach (var kvp in certEnvVars)
            {
                launchSettingsEnvVars[kvp.Key] = kvp.Value;
            }

            // Generate a backchannel socket path for CLI to connect to AppHost server
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to AppHost server so it opens a server for CLI communication
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Pass synthetic UserSecretsId so AppHost Server can read secrets set via 'aspire secret'
            launchSettingsEnvVars[KnownConfigNames.AspireUserSecretsId] = UserSecretsPathHelper.ComputeSyntheticUserSecretsId(appHostFile.FullName);

            // Check if hot reload (watch mode) is enabled
            var enableHotReload = _features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, defaultValue: false);

            // Start the AppHost server process
            var currentPid = Environment.ProcessId;
            var (socketPath, appHostServerProcess, appHostServerOutputCollector) = appHostServerProject.Run(currentPid, launchSettingsEnvVars, debug: context.Debug);

            // The backchannel completion source is the contract with RunCommand
            // We signal this when the backchannel is ready, RunCommand uses it for UX
            var backchannelCompletionSource = context.BackchannelCompletionSource ?? new TaskCompletionSource<IAppHostCliBackchannel>();

            // Start connecting to the backchannel (for dashboard URLs, logs, etc.)
            _ = StartBackchannelConnectionAsync(appHostServerProcess, backchannelSocketPath, backchannelCompletionSource, enableHotReload, cancellationToken);

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (appHostServerProcess.HasExited)
            {
                _interactionService.DisplayLines(appHostServerOutputCollector.GetLines());
                _interactionService.DisplayError("App host exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Step 5: Connect to server for RPC calls
            await using var rpcClient = await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);

            // Step 6: Install dependencies (using GuestRuntime)
            // The GuestRuntime will skip if the RuntimeSpec doesn't have InstallDependencies configured
            var installResult = await InstallDependenciesAsync(directory, rpcClient, cancellationToken);
            if (installResult != 0)
            {
                context.BackchannelCompletionSource?.TrySetException(
                    new InvalidOperationException($"Failed to install {DisplayName} dependencies."));

                if (!appHostServerProcess.HasExited)
                {
                    try
                    {
                        appHostServerProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing AppHost server process after dependency install failure");
                    }
                }

                return installResult;
            }

            // Step 7: Generate SDK code via RPC if needed
            if (buildResult.NeedsCodeGen)
            {
                await GenerateCodeViaRpcAsync(
                    directory.FullName,
                    rpcClient,
                    packages,
                    cancellationToken);
            }

            // Step 8: Execute the guest apphost

            // Pass the socket path, project directory, and apphost file path to the guest process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath,
                ["ASPIRE_PROJECT_DIRECTORY"] = directory.FullName,
                ["ASPIRE_APPHOST_FILEPATH"] = appHostFile.FullName
            };

            // Start guest apphost - it will connect to AppHost server, define resources
            // When hot reload is enabled, use watch mode
            var (guestExitCode, guestOutput) = await ExecuteGuestAppHostAsync(
                appHostFile, directory, environmentVariables, enableHotReload, rpcClient, cancellationToken);

            if (guestExitCode != 0)
            {
                _logger.LogError("{Language} apphost exited with code {ExitCode}", DisplayName, guestExitCode);

                // Display the output (same pattern as DotNetCliRunner)
                _interactionService.DisplayLines(guestOutput.GetLines());

                // Signal failure to RunCommand so it doesn't hang waiting for the backchannel
                var error = new InvalidOperationException($"The {DisplayName} apphost failed.");
                context.BackchannelCompletionSource?.TrySetException(error);

                // Kill the AppHost server since the apphost failed
                if (!appHostServerProcess.HasExited)
                {
                    try
                    {
                        appHostServerProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing AppHost server process after {Language} failure", DisplayName);
                    }
                }

                return guestExitCode;
            }

            // In watch mode, wait for server to exit (Ctrl+C or orphan detection)
            // In non-watch mode, kill the server now that the apphost has exited
            if (!enableHotReload && !appHostServerProcess.HasExited)
            {
                try
                {
                    appHostServerProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process");
                }
            }

            await appHostServerProcess.WaitForExitAsync(cancellationToken);

            return appHostServerProcess.ExitCode;
        }
        catch (OperationCanceledException)
        {
            // Signal that build/preparation failed so RunCommand doesn't hang waiting
            context.BuildCompletionSource?.TrySetResult(false);
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            // Signal that build/preparation failed so RunCommand doesn't hang waiting
            context.BuildCompletionSource?.TrySetResult(false);
            _logger.LogError(ex, "Failed to run {Language} AppHost", DisplayName);
            _interactionService.DisplayError($"Failed to run {DisplayName} AppHost: {ex.Message}");
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    private Dictionary<string, string> GetServerEnvironmentVariables(DirectoryInfo directory)
    {
        var envVars = ReadLaunchSettingsEnvironmentVariables(directory) ?? new Dictionary<string, string>();

        // Support ASPIRE_ENVIRONMENT from the launch profile to set both DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT
        envVars.TryGetValue("ASPIRE_ENVIRONMENT", out var environment);
        environment ??= "Development";

        // Set the environment for the AppHost server process
        envVars["DOTNET_ENVIRONMENT"] = environment;
        envVars["ASPNETCORE_ENVIRONMENT"] = environment;

        return envVars;
    }

    private Dictionary<string, string>? ReadLaunchSettingsEnvironmentVariables(DirectoryInfo directory)
    {
        // For guest apphosts, look for apphost.run.json
        // similar to how .NET single-file apphosts use apphost.run.json
        var apphostRunPath = Path.Combine(directory.FullName, "apphost.run.json");
        var launchSettingsPath = Path.Combine(directory.FullName, "Properties", "launchSettings.json");

        var configPath = File.Exists(apphostRunPath) ? apphostRunPath : launchSettingsPath;

        if (!File.Exists(configPath))
        {
            _logger.LogDebug("No apphost.run.json or launchSettings.json found in {Path}", directory.FullName);
            return null;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("profiles", out var profiles))
            {
                return null;
            }

            // Try to find the 'https' profile first, then fall back to the first profile
            JsonElement? profileElement = null;
            if (profiles.TryGetProperty("https", out var httpsProfile))
            {
                profileElement = httpsProfile;
            }
            else
            {
                // Use the first profile
                using var enumerator = profiles.EnumerateObject();
                if (enumerator.MoveNext())
                {
                    profileElement = enumerator.Current.Value;
                }
            }

            if (profileElement == null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();

            // Read applicationUrl and convert to ASPNETCORE_URLS
            if (profileElement.Value.TryGetProperty("applicationUrl", out var appUrl) &&
                appUrl.ValueKind == JsonValueKind.String)
            {
                result["ASPNETCORE_URLS"] = appUrl.GetString()!;
            }

            // Read environment variables
            if (profileElement.Value.TryGetProperty("environmentVariables", out var envVars))
            {
                foreach (var prop in envVars.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        result[prop.Name] = prop.Value.GetString()!;
                    }
                }
            }

            if (result.Count == 0)
            {
                return null;
            }

            _logger.LogDebug("Read {Count} environment variables from apphost.run.json", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read launchSettings.json");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<int> PublishAsync(PublishContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Publishing guest AppHost: {AppHostFile}", appHostFile.FullName);

        try
        {
            // Step 1: Load config - source of truth for SDK version and packages
            var appHostServerProject = await _appHostServerProjectFactory.CreateAsync(directory.FullName, cancellationToken);
            var config = LoadConfiguration(directory);
            var packages = await GetAllPackagesAsync(config, cancellationToken);
            var sdkVersion = GetPrepareSdkVersion(config);

            // Prepare the AppHost server (build for dev mode, restore for prebuilt)
            var (prepareSuccess, prepareOutput, _, needsCodeGen) = await PrepareAppHostServerAsync(appHostServerProject, sdkVersion, packages, cancellationToken);
            if (!prepareSuccess)
            {
                // Set OutputCollector so PipelineCommandBase can display errors
                context.OutputCollector = prepareOutput;
                // Signal the backchannel completion source so the caller doesn't wait forever
                context.BackchannelCompletionSource?.TrySetException(
                    new InvalidOperationException("The app host preparation failed."));
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Store output collector in context for exception handling
            context.OutputCollector = prepareOutput;

            // Read launch settings and set shared environment variables
            var launchSettingsEnvVars = GetServerEnvironmentVariables(directory);

            // Generate a backchannel socket path for CLI to connect to AppHost server
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to AppHost server so it opens a server
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Pass synthetic UserSecretsId so AppHost Server can read secrets set via 'aspire secret'
            launchSettingsEnvVars[KnownConfigNames.AspireUserSecretsId] = UserSecretsPathHelper.ComputeSyntheticUserSecretsId(appHostFile.FullName);

            // Step 2: Start the AppHost server process(it opens the backchannel for progress reporting)
            var currentPid = Environment.ProcessId;
            var (jsonRpcSocketPath, appHostServerProcess, appHostServerOutputCollector) = appHostServerProject.Run(currentPid, launchSettingsEnvVars, debug: context.Debug);

            // Start connecting to the backchannel
            if (context.BackchannelCompletionSource is not null)
            {
                _ = StartBackchannelConnectionAsync(appHostServerProcess, backchannelSocketPath, context.BackchannelCompletionSource, enableHotReload: false, cancellationToken);
            }

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (appHostServerProcess.HasExited)
            {
                _interactionService.DisplayLines(appHostServerOutputCollector.GetLines());
                _interactionService.DisplayError("App host exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Step 3: Connect to server for RPC calls
            await using var rpcClient = await AppHostRpcClient.ConnectAsync(jsonRpcSocketPath, cancellationToken);

            // Step 4: Install dependencies if needed (using GuestRuntime)
            // The GuestRuntime will skip if the RuntimeSpec doesn't have InstallDependencies configured
            var installResult = await InstallDependenciesAsync(directory, rpcClient, cancellationToken);
            if (installResult != 0)
            {
                context.BackchannelCompletionSource?.TrySetException(
                    new InvalidOperationException($"Failed to install {DisplayName} dependencies."));

                if (!appHostServerProcess.HasExited)
                {
                    try
                    {
                        appHostServerProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing AppHost server process after dependency install failure");
                    }
                }

                return installResult;
            }

            // Step 5: Generate code via RPC if needed
            if (needsCodeGen)
            {
                await GenerateCodeViaRpcAsync(
                    directory.FullName,
                    rpcClient,
                    packages,
                    cancellationToken);
            }

            // Pass the socket path, project directory, and apphost file path to the guest process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = jsonRpcSocketPath,
                ["ASPIRE_PROJECT_DIRECTORY"] = directory.FullName,
                ["ASPIRE_APPHOST_FILEPATH"] = appHostFile.FullName
            };

            // Step 6: Execute the guest apphost for publishing
            // Pass the publish arguments (e.g., --operation publish --step deploy)
            var (guestExitCode, guestOutput) = await ExecuteGuestAppHostForPublishAsync(
                appHostFile, directory, environmentVariables, context.Arguments, rpcClient, cancellationToken);

            if (guestExitCode != 0)
            {
                _logger.LogError("{Language} apphost exited with code {ExitCode}", DisplayName, guestExitCode);

                // Display the output (same pattern as DotNetCliRunner)
                _interactionService.DisplayLines(guestOutput.GetLines());

                // Signal failure so callers don't hang waiting for the backchannel
                var error = new InvalidOperationException($"The {DisplayName} apphost failed.");
                context.BackchannelCompletionSource?.TrySetException(error);

                // Kill the AppHost server since the apphost failed
                if (!appHostServerProcess.HasExited)
                {
                    try
                    {
                        appHostServerProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing AppHost server process after {Language} failure", DisplayName);
                    }
                }

                return guestExitCode;
            }

            // Kill the server after the guest apphost exits
            if (!appHostServerProcess.HasExited)
            {
                try
                {
                    appHostServerProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process");
                }
            }

            await appHostServerProcess.WaitForExitAsync(cancellationToken);

            return appHostServerProcess.ExitCode;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {Language} AppHost", DisplayName);
            _interactionService.DisplayError($"Failed to publish {DisplayName} AppHost: {ex.Message}");
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    /// <summary>
    /// Gets the backchannel socket path for CLI communication.
    /// </summary>
    private static string GetBackchannelSocketPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var aspireCliPath = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");
        Directory.CreateDirectory(aspireCliPath);
        var socketName = $"{Guid.NewGuid():N}.sock";
        return Path.Combine(aspireCliPath, socketName);
    }

    /// <summary>
    /// Starts connecting to the AppHost server's backchannel server.
    /// </summary>
    private async Task StartBackchannelConnectionAsync(
        Process process,
        string socketPath,
        TaskCompletionSource<IAppHostCliBackchannel> backchannelCompletionSource,
        bool enableHotReload,
        CancellationToken cancellationToken)
    {
        const int ConnectionTimeoutSeconds = 60;

        var startTime = DateTimeOffset.UtcNow;
        var connectionAttempts = 0;

        _logger.LogDebug("Starting backchannel connection to AppHost server at {SocketPath}", socketPath);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogTrace("Attempting to connect to AppHost server backchannel at {SocketPath} (attempt {Attempt})", socketPath, ++connectionAttempts);
                // Pass enableHotReload as autoReconnect - the backchannel will handle reconnection internally
                await _backchannel.ConnectAsync(socketPath, autoReconnect: enableHotReload, cancellationToken).ConfigureAwait(false);
                backchannelCompletionSource.TrySetResult(_backchannel);
                _logger.LogDebug("Connected to AppHost server backchannel at {SocketPath}", socketPath);
                return;
            }
            catch (SocketException ex) when (process.HasExited && process.ExitCode != 0)
            {
                _logger.LogError("AppHost server process has exited. Unable to connect to backchannel at {SocketPath}", socketPath);
                var backchannelException = new FailedToConnectBackchannelConnection($"AppHost server process has exited unexpectedly.", ex);
                backchannelCompletionSource.TrySetException(backchannelException);
                return;
            }
            catch (SocketException)
            {
                var waitingFor = DateTimeOffset.UtcNow - startTime;

                // Timeout after ConnectionTimeoutSeconds - the AppHost server should have started by now
                if (waitingFor > TimeSpan.FromSeconds(ConnectionTimeoutSeconds))
                {
                    _logger.LogError("Timed out waiting for AppHost server to start after {Timeout} seconds", ConnectionTimeoutSeconds);
                    var timeoutException = new TimeoutException($"Timed out waiting for AppHost server to start after {ConnectionTimeoutSeconds} seconds. Check the debug logs for more details.");
                    backchannelCompletionSource.TrySetException(timeoutException);
                    return;
                }

                // Slow down polling after 10 seconds
                if (waitingFor > TimeSpan.FromSeconds(10))
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to AppHost server backchannel");
                backchannelCompletionSource.TrySetException(ex);
                return;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> AddPackageAsync(AddPackageContext context, CancellationToken cancellationToken)
    {
        var directory = context.AppHostFile.Directory;
        if (directory is null)
        {
            return false;
        }

        // Load config - source of truth for SDK version and packages
        var config = LoadConfiguration(directory);

        // Update .aspire/settings.json with the new package
        config.AddOrUpdatePackage(context.PackageId, context.PackageVersion);
        config.Save(directory.FullName);

        // Build and regenerate SDK code with the new package
        await BuildAndGenerateSdkAsync(directory, cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<UpdatePackagesResult> UpdatePackagesAsync(UpdatePackagesContext context, CancellationToken cancellationToken)
    {
        var directory = context.AppHostFile.Directory;
        if (directory is null)
        {
            return new UpdatePackagesResult { UpdatesApplied = false };
        }

        // Load config - source of truth for SDK version and packages
        var config = LoadConfiguration(directory);

        // Find updates for SDK version and packages
        string? newSdkVersion = null;
        var updates = await _interactionService.ShowStatusAsync(
            UpdateCommandStrings.AnalyzingProjectStatus,
            async () =>
            {
                var packageUpdates = new List<(string PackageId, string CurrentVersion, string NewVersion)>();

                // Check for SDK version update (silently - it's an implementation detail)
                try
                {
                    var sdkPackages = await context.Channel.GetPackagesAsync("Aspire.Hosting", directory, cancellationToken);
                    var latestSdkPackage = sdkPackages
                        .Where(p => SemVersion.TryParse(p.Version, SemVersionStyles.Strict, out _))
                        .OrderByDescending(p => SemVersion.Parse(p.Version, SemVersionStyles.Strict), SemVersion.PrecedenceComparer)
                        .FirstOrDefault();

                    if (latestSdkPackage is not null && latestSdkPackage.Version != config.SdkVersion)
                    {
                        newSdkVersion = latestSdkPackage.Version;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check for SDK version updates");
                }

                // Check for package updates
                if (config.Packages is not null)
                {
                    foreach (var (packageId, currentVersion) in config.Packages)
                    {
                        try
                        {
                            var packages = await context.Channel.GetPackagesAsync(packageId, directory, cancellationToken);
                            var latestPackage = packages
                                .Where(p => SemVersion.TryParse(p.Version, SemVersionStyles.Strict, out _))
                                .OrderByDescending(p => SemVersion.Parse(p.Version, SemVersionStyles.Strict), SemVersion.PrecedenceComparer)
                                .FirstOrDefault();

                            if (latestPackage is not null && latestPackage.Version != currentVersion)
                            {
                                packageUpdates.Add((packageId, currentVersion, latestPackage.Version));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to check for updates to package {PackageId}", packageId);
                        }
                    }
                }

                return packageUpdates;
            });

        if (updates.Count == 0 && newSdkVersion is null)
        {
            _interactionService.DisplayMessage(KnownEmojis.CheckMark, UpdateCommandStrings.ProjectUpToDateMessage);
            return new UpdatePackagesResult { UpdatesApplied = false };
        }

        // Display pending updates
        _interactionService.DisplayEmptyLine();
        if (newSdkVersion is not null)
        {
            _interactionService.DisplayMessage(KnownEmojis.Package, $"[bold yellow]Aspire SDK[/] [bold green]{config.SdkVersion.EscapeMarkup()}[/] to [bold green]{newSdkVersion.EscapeMarkup()}[/]", allowMarkup: true);
        }
        foreach (var (packageId, currentVersion, newVersion) in updates)
        {
            _interactionService.DisplayMessage(KnownEmojis.Package, $"[bold yellow]{packageId.EscapeMarkup()}[/] [bold green]{currentVersion.EscapeMarkup()}[/] to [bold green]{newVersion.EscapeMarkup()}[/]", allowMarkup: true);
        }
        _interactionService.DisplayEmptyLine();

        // Confirm with user
        if (!await _interactionService.ConfirmAsync(UpdateCommandStrings.PerformUpdatesPrompt, true, cancellationToken))
        {
            return new UpdatePackagesResult { UpdatesApplied = false };
        }

        // Apply updates to settings.json
        if (newSdkVersion is not null)
        {
            config.SdkVersion = newSdkVersion;
        }
        // Update channel if it's an explicit channel (not the implicit/default one)
        if (context.Channel.Type == Packaging.PackageChannelType.Explicit)
        {
            config.Channel = context.Channel.Name;
        }
        foreach (var (packageId, _, newVersion) in updates)
        {
            config.AddOrUpdatePackage(packageId, newVersion);
        }
        config.Save(directory.FullName);

        // Rebuild and regenerate SDK code with updated packages
        _interactionService.DisplayEmptyLine();
        _interactionService.DisplaySubtleMessage("Regenerating SDK code with updated packages...");
        await BuildAndGenerateSdkAsync(directory, cancellationToken);

        _interactionService.DisplayEmptyLine();
        _interactionService.DisplaySuccess(UpdateCommandStrings.UpdateSuccessfulMessage);

        return new UpdatePackagesResult { UpdatesApplied = true };
    }

    /// <inheritdoc />
    public async Task<RunningInstanceResult> FindAndStopRunningInstanceAsync(FileInfo appHostFile, DirectoryInfo homeDirectory, CancellationToken cancellationToken)
    {
        // For guest projects, we use the AppHost server's path to compute the socket path
        // The AppHost server is created in a subdirectory of the guest apphost directory
        var directory = appHostFile.Directory;
        if (directory is null)
        {
            return RunningInstanceResult.NoRunningInstance; // No directory, nothing to check
        }

        var appHostServerProject = await _appHostServerProjectFactory.CreateAsync(directory.FullName, cancellationToken);
        var genericAppHostPath = appHostServerProject.GetInstanceIdentifier();

        // Find matching sockets for this AppHost
        var matchingSockets = AppHostHelper.FindMatchingSockets(genericAppHostPath, homeDirectory.FullName);

        // Check if any socket files exist
        if (matchingSockets.Length == 0)
        {
            return RunningInstanceResult.NoRunningInstance; // No running instance, continue
        }

        // Stop all running instances
        var stopTasks = matchingSockets.Select(socketPath => 
            _runningInstanceManager.StopRunningInstanceAsync(socketPath, cancellationToken));
        var results = await Task.WhenAll(stopTasks);
        return results.All(r => r) ? RunningInstanceResult.InstanceStopped : RunningInstanceResult.StopFailed;
    }

    /// <summary>
    /// Generates SDK code by calling the AppHost server's generateCode RPC method.
    /// </summary>
    private async Task GenerateCodeViaRpcAsync(
        string appPath,
        IAppHostRpcClient rpcClient,
        IEnumerable<(string PackageId, string Version)> packages,
        CancellationToken cancellationToken)
    {
        var packagesList = packages.ToList();

        // Use CodeGenerator (e.g., "TypeScript") not LanguageId (e.g., "typescript/nodejs")
        // The code generator is registered by its Language property, not the runtime ID
        var codeGenerator = _resolvedLanguage.CodeGenerator;

        _logger.LogDebug("Generating {CodeGenerator} code via RPC for {Count} packages", codeGenerator, packagesList.Count);

        // Use the typed RPC method
        var files = await rpcClient.GenerateCodeAsync(codeGenerator, cancellationToken);

        // Write generated files to the output directory
        var outputPath = Path.Combine(appPath, GeneratedFolderName);
        Directory.CreateDirectory(outputPath);

        foreach (var (fileName, content) in files)
        {
            var filePath = Path.Combine(outputPath, fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }

        // Write generation hash for caching
        SaveGenerationHash(outputPath, packagesList);

        _logger.LogInformation("Generated {Count} {CodeGenerator} files in {Path}",
            files.Count, codeGenerator, outputPath);
    }

    /// <summary>
    /// Saves a hash of the packages to avoid regenerating code unnecessarily.
    /// </summary>
    private static void SaveGenerationHash(string generatedPath, List<(string PackageId, string Version)> packages)
    {
        var hashPath = Path.Combine(generatedPath, ".codegen-hash");
        var hash = ComputePackagesHash(packages);
        File.WriteAllText(hashPath, hash);
    }

    /// <summary>
    /// Computes a hash of the package list for caching purposes.
    /// </summary>
    private static string ComputePackagesHash(List<(string PackageId, string Version)> packages)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (packageId, version) in packages.OrderBy(p => p.PackageId))
        {
            sb.Append(packageId);
            sb.Append(':');
            sb.Append(version);
            sb.Append(';');
        }
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }

    // ═══════════════════════════════════════════════════════════════
    // RUNTIME MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures the GuestRuntime is created.
    /// </summary>
    private async Task EnsureRuntimeCreatedAsync(
        IAppHostRpcClient rpcClient,
        CancellationToken cancellationToken)
    {
        if (_guestRuntime is null)
        {
            var runtimeSpec = await rpcClient.GetRuntimeSpecAsync(_resolvedLanguage.LanguageId, cancellationToken);
            _guestRuntime = new GuestRuntime(runtimeSpec, _logger);

            _logger.LogDebug("Created GuestRuntime for {Language}: Execute={Command} {Args}",
                _resolvedLanguage.LanguageId,
                runtimeSpec.Execute.Command,
                string.Join(" ", runtimeSpec.Execute.Args));
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // GUEST RUNTIME HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Installs dependencies for the guest AppHost using GuestRuntime.
    /// </summary>
    private async Task<int> InstallDependenciesAsync(
        DirectoryInfo directory,
        IAppHostRpcClient rpcClient,
        CancellationToken cancellationToken)
    {
        await EnsureRuntimeCreatedAsync(rpcClient, cancellationToken);

        if (_guestRuntime is null)
        {
            _interactionService.DisplayError("GuestRuntime not initialized. This is a bug.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        var result = await _guestRuntime.InstallDependenciesAsync(directory, cancellationToken);
        if (result != 0)
        {
            _interactionService.DisplayError($"Failed to install {_resolvedLanguage?.DisplayName ?? "guest"} dependencies.");
        }

        return result;
    }

    /// <summary>
    /// Executes the guest AppHost using GuestRuntime.
    /// </summary>
    private async Task<(int ExitCode, OutputCollector Output)> ExecuteGuestAppHostAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        bool watchMode,
        IAppHostRpcClient rpcClient,
        CancellationToken cancellationToken)
    {
        await EnsureRuntimeCreatedAsync(rpcClient, cancellationToken);

        if (_guestRuntime is null)
        {
            _interactionService.DisplayError("GuestRuntime not initialized. This is a bug.");
            return (ExitCodeConstants.FailedToDotnetRunAppHost, new OutputCollector());
        }

        return await _guestRuntime.RunAsync(appHostFile, directory, environmentVariables, watchMode, cancellationToken);
    }

    /// <summary>
    /// Executes the guest AppHost for publishing using GuestRuntime.
    /// </summary>
    private async Task<(int ExitCode, OutputCollector Output)> ExecuteGuestAppHostForPublishAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        string[]? publishArgs,
        IAppHostRpcClient rpcClient,
        CancellationToken cancellationToken)
    {
        await EnsureRuntimeCreatedAsync(rpcClient, cancellationToken);

        if (_guestRuntime is null)
        {
            _interactionService.DisplayError("GuestRuntime not initialized. This is a bug.");
            return (ExitCodeConstants.FailedToDotnetRunAppHost, new OutputCollector());
        }

        return await _guestRuntime.PublishAsync(appHostFile, directory, environmentVariables, publishArgs, cancellationToken);
    }

    /// <summary>
    /// Computes a deterministic synthetic UserSecretsId from the AppHost file path.
    /// </summary>
    public Task<string?> GetUserSecretsIdAsync(FileInfo appHostFile, bool autoInit, CancellationToken cancellationToken)
    {
        var id = UserSecretsPathHelper.ComputeSyntheticUserSecretsId(appHostFile.FullName);
        return Task.FromResult<string?>(id);
    }
}
