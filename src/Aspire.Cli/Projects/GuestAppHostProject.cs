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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Semver;

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
    private readonly IAppHostServerSessionFactory _sessionFactory;
    private readonly ICertificateService _certificateService;
    private readonly IDotNetCliRunner _runner;
    private readonly IPackagingService _packagingService;
    private readonly IConfiguration _configuration;
    private readonly IFeatures _features;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly ILogger<GuestAppHostProject> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly RunningInstanceManager _runningInstanceManager;

    // Late-bound language resolution
    private string[]? _detectionPatterns;
    private LanguageInfo? _resolvedLanguage;
    private GuestRuntime? _guestRuntime;

    public GuestAppHostProject(
        IInteractionService interactionService,
        IAppHostCliBackchannel backchannel,
        IAppHostServerProjectFactory appHostServerProjectFactory,
        IAppHostServerSessionFactory sessionFactory,
        ICertificateService certificateService,
        IDotNetCliRunner runner,
        IPackagingService packagingService,
        IConfiguration configuration,
        IFeatures features,
        ILanguageDiscovery languageDiscovery,
        ILogger<GuestAppHostProject> logger,
        TimeProvider? timeProvider = null)
    {
        _interactionService = interactionService;
        _backchannel = backchannel;
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _sessionFactory = sessionFactory;
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
    // IDENTITY (Late-Bound)
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string LanguageId => _resolvedLanguage?.LanguageId ?? "guest";

    /// <inheritdoc />
    public string DisplayName => _resolvedLanguage?.DisplayName ?? "Guest Language";

    // ═══════════════════════════════════════════════════════════════
    // DETECTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string[] DetectionPatterns => _detectionPatterns ??= GetAllDetectionPatterns();

    private string[] GetAllDetectionPatterns()
    {
        // Aggregate detection patterns from all guest languages
        var languages = _languageDiscovery.GetAvailableLanguagesAsync().GetAwaiter().GetResult();
        return languages.SelectMany(l => l.DetectionPatterns).Distinct().ToArray();
    }

    /// <inheritdoc />
    public bool CanHandle(FileInfo appHostFile)
    {
        // Check if file matches any guest language detection pattern
        var patterns = DetectionPatterns;
        if (!patterns.Any(p => appHostFile.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Check no sibling .csproj files (those take precedence)
        var siblingCsprojFiles = appHostFile.Directory!.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly);
        if (siblingCsprojFiles.Any())
        {
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string AppHostFileName => _resolvedLanguage?.DetectionPatterns.FirstOrDefault() ?? "apphost.ts";

    /// <inheritdoc />
    public async Task ScaffoldAsync(DirectoryInfo directory, string? projectName, CancellationToken cancellationToken)
    {
        // Resolve language - for scaffolding, we need to detect it from context
        // (the language is typically passed from InitCommand which knows the user's selection)
        var languageId = await ResolveLanguageAsync(directory, cancellationToken);
        await SetLanguageAsync(languageId, cancellationToken);

        // Step 1: Build the AppHost server (needed for RPC to get scaffold templates)
        // Include the code generation package for scaffolding and code gen
        var codeGenPackage = await _languageDiscovery.GetPackageForLanguageAsync(languageId, cancellationToken);
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", AppHostServerProject.AspireHostVersion),
            ("Aspire.Hosting.AppHost", AppHostServerProject.AspireHostVersion),
        };
        if (codeGenPackage is not null)
        {
            packages.Add((codeGenPackage, AppHostServerProject.AspireHostVersion));
        }

        var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
        var socketPath = appHostServerProject.GetSocketPath();

        var (buildSuccess, buildOutput, channelName) = await BuildAppHostServerAsync(appHostServerProject, packages, cancellationToken);
        if (!buildSuccess)
        {
            _interactionService.DisplayLines(buildOutput.GetLines());
            _interactionService.DisplayError("Failed to build AppHost server.");
            return;
        }

        // Step 2: Start the server temporarily for scaffolding and code generation
        var currentPid = Environment.ProcessId;
        var (serverProcess, _) = appHostServerProject.Run(socketPath, currentPid, new Dictionary<string, string>());

        try
        {
            // Step 3: Connect to server and get scaffold templates via RPC
            await using var rpcClient = await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);

            var scaffoldFiles = await rpcClient.ScaffoldAppHostAsync(
                languageId,
                directory.FullName,
                projectName,
                cancellationToken);

            // Step 4: Write scaffold files to disk
            foreach (var (fileName, content) in scaffoldFiles)
            {
                var filePath = Path.Combine(directory.FullName, fileName);
                var fileDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }
                await File.WriteAllTextAsync(filePath, content, cancellationToken);
            }

            _logger.LogDebug("Wrote {Count} scaffold files", scaffoldFiles.Count);

            // Step 5: Install dependencies using GuestRuntime
            var installResult = await InstallDependenciesAsync(directory, rpcClient, cancellationToken);
            if (installResult != 0)
            {
                return;
            }

            // Step 6: Generate SDK code via RPC
            await GenerateCodeViaRpcAsync(
                directory.FullName,
                rpcClient,
                packages,
                cancellationToken);

            // Save channel and language to settings.json
            var config = AspireJsonConfiguration.Load(directory.FullName) ?? new AspireJsonConfiguration();
            if (channelName is not null)
            {
                config.Channel = channelName;
            }
            config.Language = languageId;
            config.Save(directory.FullName);
        }
        finally
        {
            // Step 7: Stop the server
            if (!serverProcess.HasExited)
            {
                try
                {
                    serverProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process after scaffolding");
                }
            }
        }
    }

    /// <summary>
    /// Creates project files and builds the AppHost server.
    /// </summary>
    private static async Task<(bool Success, OutputCollector Output, string? ChannelName)> BuildAppHostServerAsync(
        AppHostServerProject appHostServerProject,
        List<(string Name, string Version)> packages,
        CancellationToken cancellationToken)
    {
        var outputCollector = new OutputCollector();

        var (_, channelName) = await appHostServerProject.CreateProjectFilesAsync(packages, cancellationToken);
        var (buildSuccess, buildOutput) = await appHostServerProject.BuildAsync(cancellationToken);
        if (!buildSuccess)
        {
            foreach (var (_, line) in buildOutput.GetLines())
            {
                outputCollector.AppendOutput(line);
            }
        }

        return (buildSuccess, outputCollector, channelName);
    }

    /// <summary>
    /// Builds the AppHost server project and generates SDK code.
    /// </summary>
    private async Task BuildAndGenerateSdkAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        // Step 1: Get package references and build AppHost server
        var packages = GetPackageReferences(directory).ToList();
        var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
        var socketPath = appHostServerProject.GetSocketPath();

        var (buildSuccess, buildOutput, _) = await BuildAppHostServerAsync(appHostServerProject, packages, cancellationToken);
        if (!buildSuccess)
        {
            _interactionService.DisplayLines(buildOutput.GetLines());
            _interactionService.DisplayError("Failed to build AppHost server.");
            return;
        }

        // Step 2: Start the AppHost server temporarily for code generation
        var currentPid = Environment.ProcessId;
        var (serverProcess, _) = appHostServerProject.Run(socketPath, currentPid, new Dictionary<string, string>());

        try
        {
            // Step 3: Connect to server
            await using var rpcClient = await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);

            // Step 4: Install dependencies using GuestRuntime
            var installResult = await InstallDependenciesAsync(directory, rpcClient, cancellationToken);
            if (installResult != 0)
            {
                return;
            }

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
        // Check if the file exists and has the correct extension
        if (!appHostFile.Exists)
        {
            return Task.FromResult(new AppHostValidationResult(IsValid: false));
        }

        if (!appHostFile.Name.Equals("apphost.ts", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AppHostValidationResult(IsValid: false));
        }

        // Check for package.json in the same directory
        var directory = appHostFile.Directory;
        if (directory is null)
        {
            return Task.FromResult(new AppHostValidationResult(IsValid: false));
        }

        var hasPackageJson = File.Exists(Path.Combine(directory.FullName, "package.json"));

        // Guest languages don't have the "possibly unbuildable" concept
        return Task.FromResult(new AppHostValidationResult(IsValid: hasPackageJson));
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
            try
            {
                await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
            }
            catch
            {
                context.BuildCompletionSource?.TrySetResult(false);
                throw;
            }

            // Build phase: build AppHost server (dependency install happens after server starts)
            var packages = GetPackageReferences(directory).ToList();
            var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
            var socketPath = appHostServerProject.GetSocketPath();

            var buildResult = await _interactionService.ShowStatusAsync(
                ":hammer_and_wrench:  Building app host...",
                async () =>
                {
                    // Build the AppHost server
                    var (buildSuccess, buildOutput, channelName) = await BuildAppHostServerAsync(appHostServerProject, packages, cancellationToken);
                    if (!buildSuccess)
                    {
                        return (Success: false, Output: buildOutput, Error: "Failed to build app host.", ChannelName: (string?)null, NeedsCodeGen: false);
                    }

                    return (Success: true, Output: buildOutput, Error: (string?)null, ChannelName: channelName, NeedsCodeGen: NeedsGeneration(directory.FullName, packages));
                });

            // Save the channel to settings.json if available
            if (buildResult.ChannelName is not null)
            {
                var config = AspireJsonConfiguration.Load(directory.FullName) ?? new AspireJsonConfiguration();
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

            // Read launchSettings.json if it exists, or create defaults
            var launchSettingsEnvVars = ReadLaunchSettingsEnvironmentVariables(directory) ?? new Dictionary<string, string>();

            // Generate a backchannel socket path for CLI to connect to AppHost server
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to AppHost server so it opens a server for CLI communication
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Check if hot reload (watch mode) is enabled
            var enableHotReload = _features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, defaultValue: false);

            // Start the AppHost server process
            var currentPid = Environment.ProcessId;
            var (appHostServerProcess, appHostServerOutputCollector) = appHostServerProject.Run(socketPath, currentPid, launchSettingsEnvVars, debug: context.Debug);

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

            // Pass the socket path to the guest process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath
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

    private static IEnumerable<(string Name, string Version)> GetPackageReferences(DirectoryInfo directory)
    {
        // Always include the base Aspire.Hosting packages
        yield return ("Aspire.Hosting", AppHostServerProject.AspireHostVersion);
        yield return ("Aspire.Hosting.AppHost", AppHostServerProject.AspireHostVersion);

        // Read additional packages from .aspire/settings.json
        var aspireConfig = AspireJsonConfiguration.Load(directory.FullName);
        if (aspireConfig?.Packages is not null)
        {
            foreach (var (packageName, version) in aspireConfig.Packages)
            {
                // Skip base packages as they're already included
                if (string.Equals(packageName, "Aspire.Hosting", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(packageName, "Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return (packageName, version);
            }
        }
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
            // Step 1: Get package references and build AppHost server
            var packages = GetPackageReferences(directory).ToList();
            var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
            var jsonRpcSocketPath = appHostServerProject.GetSocketPath();

            // Build the AppHost server
            var (buildSuccess, buildOutput, _) = await BuildAppHostServerAsync(appHostServerProject, packages, cancellationToken);
            if (!buildSuccess)
            {
                // Set OutputCollector so PipelineCommandBase can display errors
                context.OutputCollector = buildOutput;
                // Signal the backchannel completion source so the caller doesn't wait forever
                context.BackchannelCompletionSource?.TrySetException(
                    new InvalidOperationException("The app host build failed."));
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Store output collector in context for exception handling
            context.OutputCollector = buildOutput;

            // Check if code generation is needed (we'll do it after server starts)
            var needsCodeGen = NeedsGeneration(directory.FullName, packages);

            // Read launchSettings.json if it exists
            var launchSettingsEnvVars = ReadLaunchSettingsEnvironmentVariables(directory) ?? new Dictionary<string, string>();

            // Generate a backchannel socket path for CLI to connect to AppHost server
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to AppHost server so it opens a server
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Step 2: Start the AppHost server process (it opens the backchannel for progress reporting)
            var currentPid = Environment.ProcessId;
            var (appHostServerProcess, appHostServerOutputCollector) = appHostServerProject.Run(jsonRpcSocketPath, currentPid, launchSettingsEnvVars, debug: context.Debug);

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

            // Pass the socket path to the guest process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = jsonRpcSocketPath
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
                var backchannelException = new FailedToConnectBackchannelConnection($"AppHost server process has exited unexpectedly.", process, ex);
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

        // Update .aspire/settings.json with the new package
        var config = AspireJsonConfiguration.Load(directory.FullName) ?? new AspireJsonConfiguration();
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

        // Read current packages from .aspire/settings.json
        var config = AspireJsonConfiguration.Load(directory.FullName);
        if (config?.Packages is null || config.Packages.Count == 0)
        {
            _interactionService.DisplayMessage("check_mark", UpdateCommandStrings.ProjectUpToDateMessage);
            return new UpdatePackagesResult { UpdatesApplied = false };
        }

        // Find updates for each package
        var updates = await _interactionService.ShowStatusAsync(
            UpdateCommandStrings.AnalyzingProjectStatus,
            async () =>
            {
                var packageUpdates = new List<(string PackageId, string CurrentVersion, string NewVersion)>();

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

                return packageUpdates;
            });

        if (updates.Count == 0)
        {
            _interactionService.DisplayMessage("check_mark", UpdateCommandStrings.ProjectUpToDateMessage);
            return new UpdatePackagesResult { UpdatesApplied = false };
        }

        // Display pending updates
        _interactionService.DisplayEmptyLine();
        foreach (var (packageId, currentVersion, newVersion) in updates)
        {
            _interactionService.DisplayMessage("package", $"[bold yellow]{packageId}[/] [bold green]{currentVersion}[/] to [bold green]{newVersion}[/]");
        }
        _interactionService.DisplayEmptyLine();

        // Confirm with user
        if (!await _interactionService.ConfirmAsync(UpdateCommandStrings.PerformUpdatesPrompt, true, cancellationToken))
        {
            return new UpdatePackagesResult { UpdatesApplied = false };
        }

        // Apply updates to settings.json
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
    public async Task<bool> CheckAndHandleRunningInstanceAsync(FileInfo appHostFile, DirectoryInfo homeDirectory, CancellationToken cancellationToken)
    {
        // For guest projects, we use the AppHost server's path to compute the socket path
        // The AppHost server is created in a subdirectory of the apphost.ts directory
        var directory = appHostFile.Directory;
        if (directory is null)
        {
            return true; // No directory, nothing to check
        }

        var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
        var genericAppHostPath = appHostServerProject.GetProjectFilePath();

        // Compute socket path based on the AppHost server project path
        var auxiliarySocketPath = AppHostHelper.ComputeAuxiliarySocketPath(genericAppHostPath, homeDirectory.FullName);

        // Check if the socket file exists
        if (!File.Exists(auxiliarySocketPath))
        {
            return true; // No running instance, continue
        }

        // Stop the running instance
        return await _runningInstanceManager.StopRunningInstanceAsync(auxiliarySocketPath, cancellationToken);
    }

    /// <summary>
    /// Checks if code generation is needed based on the current state.
    /// </summary>
    private bool NeedsGeneration(string appPath, IEnumerable<(string PackageId, string Version)> packages)
    {
        // In dev mode (ASPIRE_REPO_ROOT set), always regenerate to pick up code changes
        if (!string.IsNullOrEmpty(_configuration["ASPIRE_REPO_ROOT"]))
        {
            _logger.LogDebug("Dev mode detected (ASPIRE_REPO_ROOT set), skipping generation cache");
            return true;
        }

        return CheckNeedsGeneration(appPath, packages.ToList());
    }

    /// <summary>
    /// Checks if code generation is needed by comparing the hash of current packages
    /// with the stored hash from previous generation.
    /// </summary>
    private static bool CheckNeedsGeneration(string appPath, List<(string PackageId, string Version)> packages)
    {
        var generatedPath = Path.Combine(appPath, GeneratedFolderName);
        var hashPath = Path.Combine(generatedPath, ".codegen-hash");

        // If hash file doesn't exist, generation is needed
        if (!File.Exists(hashPath))
        {
            return true;
        }

        // Compare stored hash with current packages hash
        var storedHash = File.ReadAllText(hashPath).Trim();
        var currentHash = ComputePackagesHash(packages);

        return !string.Equals(storedHash, currentHash, StringComparison.OrdinalIgnoreCase);
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

        // Resolve the language if not already resolved
        var languageId = _resolvedLanguage?.LanguageId ?? await ResolveLanguageAsync(new DirectoryInfo(appPath), cancellationToken);

        _logger.LogDebug("Generating {Language} code via RPC for {Count} packages", languageId, packagesList.Count);

        // Use the typed RPC method
        var files = await rpcClient.GenerateCodeAsync(languageId, cancellationToken);

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

        _logger.LogInformation("Generated {Count} {Language} files in {Path}",
            files.Count, languageId, outputPath);
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
    // LANGUAGE RESOLUTION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves the language for this AppHost from settings or detection.
    /// </summary>
    private async Task<string> ResolveLanguageAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        // First, try settings.json
        var config = AspireJsonConfiguration.Load(directory.FullName);
        if (config?.Language is not null)
        {
            _logger.LogDebug("Using language from settings.json: {Language}", config.Language);
            return config.Language;
        }

        // Fallback to detection
        var detected = await _languageDiscovery.DetectLanguageAsync(directory, cancellationToken);
        if (detected is not null)
        {
            _logger.LogDebug("Detected language: {Language}", detected);

            // Persist detected language to settings.json so we don't re-detect every time
            config ??= new AspireJsonConfiguration();
            config.Language = detected;
            config.Save(directory.FullName);

            return detected;
        }

        throw new InvalidOperationException("Could not determine language for guest AppHost");
    }

    /// <summary>
    /// Ensures the language is resolved and GuestRuntime is created.
    /// </summary>
    private async Task EnsureLanguageResolvedAsync(
        DirectoryInfo directory,
        IAppHostRpcClient rpcClient,
        CancellationToken cancellationToken)
    {
        if (_resolvedLanguage is null)
        {
            var languageId = await ResolveLanguageAsync(directory, cancellationToken);
            var languages = await _languageDiscovery.GetAvailableLanguagesAsync(cancellationToken);
            _resolvedLanguage = languages.FirstOrDefault(l =>
                string.Equals(l.LanguageId, languageId, StringComparison.OrdinalIgnoreCase));

            if (_resolvedLanguage is null)
            {
                throw new InvalidOperationException($"Language '{languageId}' is not supported");
            }
        }

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

    /// <summary>
    /// Sets the resolved language explicitly (used during scaffolding when language is known).
    /// </summary>
    private async Task SetLanguageAsync(string languageId, CancellationToken cancellationToken)
    {
        var languages = await _languageDiscovery.GetAvailableLanguagesAsync(cancellationToken);
        _resolvedLanguage = languages.FirstOrDefault(l =>
            string.Equals(l.LanguageId, languageId, StringComparison.OrdinalIgnoreCase));

        if (_resolvedLanguage is null)
        {
            throw new InvalidOperationException($"Language '{languageId}' is not supported");
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
        await EnsureLanguageResolvedAsync(directory, rpcClient, cancellationToken);

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
        await EnsureLanguageResolvedAsync(directory, rpcClient, cancellationToken);

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
        await EnsureLanguageResolvedAsync(directory, rpcClient, cancellationToken);

        if (_guestRuntime is null)
        {
            _interactionService.DisplayError("GuestRuntime not initialized. This is a bug.");
            return (ExitCodeConstants.FailedToDotnetRunAppHost, new OutputCollector());
        }

        return await _guestRuntime.PublishAsync(appHostFile, directory, environmentVariables, publishArgs, cancellationToken);
    }
}
