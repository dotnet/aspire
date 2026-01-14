// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipes;
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
/// Handler for TypeScript AppHost projects (apphost.ts).
/// </summary>
internal sealed class TypeScriptAppHostProject : IAppHostProject
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
    private readonly ILogger<TypeScriptAppHostProject> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly RunningInstanceManager _runningInstanceManager;

    private static readonly string[] s_detectionPatterns = ["apphost.ts"];

    public TypeScriptAppHostProject(
        IInteractionService interactionService,
        IAppHostCliBackchannel backchannel,
        IAppHostServerProjectFactory appHostServerProjectFactory,
        ICertificateService certificateService,
        IDotNetCliRunner runner,
        IPackagingService packagingService,
        IConfiguration configuration,
        IFeatures features,
        ILogger<TypeScriptAppHostProject> logger,
        TimeProvider? timeProvider = null)
    {
        _interactionService = interactionService;
        _backchannel = backchannel;
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _certificateService = certificateService;
        _runner = runner;
        _packagingService = packagingService;
        _configuration = configuration;
        _features = features;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _runningInstanceManager = new RunningInstanceManager(_logger, _interactionService, _timeProvider);
    }

    // ═══════════════════════════════════════════════════════════════
    // IDENTITY
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string LanguageId => KnownLanguageId.TypeScript;

    /// <inheritdoc />
    public string DisplayName => "TypeScript (Node.js)";

    // ═══════════════════════════════════════════════════════════════
    // DETECTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string[] DetectionPatterns => s_detectionPatterns;

    /// <inheritdoc />
    public bool CanHandle(FileInfo appHostFile)
    {
        // Must be named apphost.ts
        if (!appHostFile.Name.Equals("apphost.ts", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check no sibling .csproj files (those take precedence)
        var siblingCsprojFiles = appHostFile.Directory!.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly);
        if (siblingCsprojFiles.Any())
        {
            return false;
        }

        // Check for package.json
        var directory = appHostFile.Directory!;
        var hasPackageJson = File.Exists(Path.Combine(directory.FullName, "package.json"));

        return hasPackageJson;
    }

    // ═══════════════════════════════════════════════════════════════
    // CREATION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string AppHostFileName => "apphost.ts";

    /// <inheritdoc />
    public async Task ScaffoldAsync(DirectoryInfo directory, string? projectName, CancellationToken cancellationToken)
    {
        var appHostPath = Path.Combine(directory.FullName, "apphost.ts");
        var packageJsonPath = Path.Combine(directory.FullName, "package.json");

        // Create a TypeScript apphost that uses the generated Aspire SDK
        var appHostContent = """
            // Aspire TypeScript AppHost
            // For more information, see: https://aspire.dev

            import { createBuilder } from './.modules/aspire.js';

            const builder = await createBuilder();

            // Add your resources here, for example:
            // const redis = await builder.addContainer("cache", "redis:latest");
            // const postgres = await builder.addPostgres("db");

            await builder.build().run();
            """;

        await File.WriteAllTextAsync(appHostPath, appHostContent, cancellationToken);

        // Create package.json if it doesn't exist
        if (!File.Exists(packageJsonPath))
        {
            var packageName = projectName?.ToLowerInvariant() ?? "aspire-apphost";
            var packageJsonContent = $$"""
                {
                  "name": "{{packageName}}",
                  "version": "1.0.0",
                  "type": "module",
                  "scripts": {
                    "start": "aspire run"
                  },
                  "dependencies": {
                    "vscode-jsonrpc": "^8.2.0"
                  },
                  "devDependencies": {
                    "tsx": "^4.19.0",
                    "typescript": "^5.3.0",
                    "@types/node": "^20.0.0"
                  }
                }
                """;

            await File.WriteAllTextAsync(packageJsonPath, packageJsonContent, cancellationToken);
        }

        // Create apphost.run.json for dashboard/OTLP configuration
        var apphostRunJsonPath = Path.Combine(directory.FullName, "apphost.run.json");
        if (!File.Exists(apphostRunJsonPath))
        {
            // Generate random 5-digit ports (10000-65000)
            var httpsPort = Random.Shared.Next(10000, 65000);
            var httpPort = Random.Shared.Next(10000, 65000);
            var otlpPort = Random.Shared.Next(10000, 65000);
            var resourceServicePort = Random.Shared.Next(10000, 65000);

            var apphostRunJsonContent = $$"""
                {
                  "profiles": {
                    "https": {
                      "applicationUrl": "https://localhost:{{httpsPort}};http://localhost:{{httpPort}}",
                      "environmentVariables": {
                        "ASPNETCORE_ENVIRONMENT": "Development",
                        "DOTNET_ENVIRONMENT": "Development",
                        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:{{otlpPort}}",
                        "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:{{resourceServicePort}}"
                      }
                    }
                  }
                }
                """;

            await File.WriteAllTextAsync(apphostRunJsonPath, apphostRunJsonContent, cancellationToken);
        }

        // Build the AppHost server and generate TypeScript SDK
        await BuildAndGenerateSdkAsync(directory, cancellationToken);
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
    /// Builds the AppHost server project and generates the TypeScript SDK.
    /// </summary>
    private async Task BuildAndGenerateSdkAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        // Step 1: Run npm install if node_modules doesn't exist
        var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            var npmInstallResult = await RunNpmInstallAsync(directory, cancellationToken);
            if (npmInstallResult != 0)
            {
                _interactionService.DisplayError("Failed to install npm dependencies.");
                return;
            }
        }

        // Step 2: Get package references and build AppHost server
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

        // Step 3: Start the AppHost server temporarily for code generation
        var currentPid = Environment.ProcessId;
        var (serverProcess, _) = appHostServerProject.Run(socketPath, currentPid, new Dictionary<string, string>());

        try
        {
            // Step 4: Generate TypeScript SDK via RPC
            await GenerateCodeViaRpcAsync(
                directory.FullName,
                socketPath,
                packages,
                cancellationToken);
        }
        finally
        {
            // Step 5: Stop the server (we were just generating code)
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

        // TypeScript doesn't have the "possibly unbuildable" concept
        return Task.FromResult(new AppHostValidationResult(IsValid: hasPackageJson));
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Running TypeScript AppHost: {AppHostFile}", appHostFile.FullName);

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

            // Build phase: npm install, build AppHost server, generate SDK
            var packages = GetPackageReferences(directory).ToList();
            var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
            var socketPath = appHostServerProject.GetSocketPath();

            var buildResult = await _interactionService.ShowStatusAsync(
                ":hammer_and_wrench:  Building app host...",
                async () =>
                {
                    // Run npm install if node_modules doesn't exist
                    var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
                    if (!Directory.Exists(nodeModulesPath))
                    {
                        var npmInstallResult = await RunNpmInstallAsync(directory, cancellationToken);
                        if (npmInstallResult != 0)
                        {
                            return (Success: false, Output: new OutputCollector(), Error: "Failed to install npm dependencies.", ChannelName: (string?)null, NeedsCodeGen: false);
                        }
                    }

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

            // Step 5: Generate TypeScript SDK via RPC if needed
            if (buildResult.NeedsCodeGen)
            {
                await GenerateCodeViaRpcAsync(
                    directory.FullName,
                    socketPath,
                    packages,
                    cancellationToken);
            }

            // Step 6: Execute the TypeScript apphost

            // Pass the socket path to the TypeScript process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath
            };

            // Start TypeScript apphost - it will connect to AppHost server, define resources
            // When hot reload is enabled, use nodemon to watch for changes and restart
            var pendingTypeScript = enableHotReload
                ? ExecuteWithNodemonAsync(appHostFile, directory, environmentVariables, cancellationToken)
                : ExecuteTypeScriptAppHostAsync(appHostFile, directory, environmentVariables, cancellationToken);

            // Wait for TypeScript to finish defining resources
            var (typeScriptExitCode, typeScriptOutput) = await pendingTypeScript;
            if (typeScriptExitCode != 0)
            {
                _logger.LogError("TypeScript apphost exited with code {ExitCode}", typeScriptExitCode);

                // Display the output from TypeScript (same pattern as DotNetCliRunner)
                _interactionService.DisplayLines(typeScriptOutput.GetLines());

                // Signal failure to RunCommand so it doesn't hang waiting for the backchannel
                var error = new InvalidOperationException("The TypeScript apphost failed.");
                context.BackchannelCompletionSource?.TrySetException(error);

                // Kill the AppHost server since TypeScript failed
                if (!appHostServerProcess.HasExited)
                {
                    try
                    {
                        appHostServerProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing AppHost server process after TypeScript failure");
                    }
                }

                return typeScriptExitCode;
            }

            // In watch mode, wait for server to exit (Ctrl+C or orphan detection)
            // In non-watch mode, kill the server now that TypeScript has exited
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
            _logger.LogError(ex, "Failed to run TypeScript AppHost");
            _interactionService.DisplayError($"Failed to run TypeScript AppHost: {ex.Message}");
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
        // For TypeScript apphosts, look for apphost.run.json
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

    private async Task<int> RunNpmInstallAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        var npmPath = FindNpmPath();
        if (npmPath is null)
        {
            _interactionService.DisplayError("npm not found. Please install Node.js and ensure npm is in your PATH.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = npmPath,
            Arguments = "install",
            WorkingDirectory = directory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private async Task<(int ExitCode, OutputCollector Output)> ExecuteTypeScriptAppHostAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        CancellationToken cancellationToken,
        string[]? additionalArgs = null)
    {
        // Try to find npx for running tsx directly, or use node if compiled
        var npxPath = FindNpxPath();

        // Build the additional arguments string
        var argsString = additionalArgs is { Length: > 0 }
            ? " " + string.Join(" ", additionalArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))
            : "";

        ProcessStartInfo startInfo;

        if (npxPath is not null)
        {
            // Use npx tsx to run TypeScript directly
            startInfo = new ProcessStartInfo
            {
                FileName = npxPath,
                Arguments = $"tsx \"{appHostFile.FullName}\"{argsString}",
                WorkingDirectory = directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        else
        {
            // Fall back to node with compiled JavaScript
            var nodePath = FindNodePath();
            if (nodePath is null)
            {
                _interactionService.DisplayError("node not found. Please install Node.js and ensure it is in your PATH.");
                return (ExitCodeConstants.FailedToDotnetRunAppHost, new OutputCollector());
            }

            var jsFile = Path.ChangeExtension(appHostFile.FullName, ".js");
            var distJsFile = Path.Combine(directory.FullName, "dist", "apphost.js");

            var targetFile = File.Exists(distJsFile) ? distJsFile : jsFile;

            if (!File.Exists(targetFile))
            {
                _interactionService.DisplayError($"Compiled JavaScript file not found: {targetFile}");
                _interactionService.DisplayMessage("info", "Try running 'npx tsc' to compile your TypeScript first.");
                return (ExitCodeConstants.FailedToBuildArtifacts, new OutputCollector());
            }

            startInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = $"\"{targetFile}\"{argsString}",
                WorkingDirectory = directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        // Add environment variables
        foreach (var (key, value) in environmentVariables)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        using var process = new Process { StartInfo = startInfo };

        // Capture output for error reporting (same pattern as DotNetCliRunner)
        var outputCollector = new OutputCollector();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("tsx({ProcessId}) {Identifier}: {Line}", process.Id, "stdout", e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("tsx({ProcessId}) {Identifier}: {Line}", process.Id, "stderr", e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, outputCollector);
    }

    /// <summary>
    /// Executes the TypeScript apphost using nodemon for hot reload.
    /// Nodemon watches for file changes and automatically restarts the TypeScript process.
    /// </summary>
    private async Task<(int ExitCode, OutputCollector Output)> ExecuteWithNodemonAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        CancellationToken cancellationToken)
    {
        var npxPath = FindNpxPath();
        if (npxPath is null)
        {
            _interactionService.DisplayError("npx not found. Please install Node.js and ensure npm is in your PATH.");
            return (ExitCodeConstants.FailedToDotnetRunAppHost, new OutputCollector());
        }

        // Check if nodemon is installed locally before trying to use it
        // This prevents npx from hanging while trying to install nodemon interactively
        var nodemonPath = Path.Combine(directory.FullName, "node_modules", ".bin", "nodemon");
        if (!File.Exists(nodemonPath))
        {
            _interactionService.DisplayError("nodemon is not installed. Please run 'npm install nodemon --save-dev' to enable hot reload.");
            return (ExitCodeConstants.FailedToDotnetRunAppHost, new OutputCollector());
        }

        // Use nodemon to watch for file changes and restart the TypeScript apphost
        // --watch . : Watch the current directory
        // --ext ts,json : Watch .ts and .json files
        // --ignore node_modules/ : Ignore node_modules
        // --ignore .modules/ : Ignore generated modules
        // --exec "npx tsx apphost.ts" : Execute the TypeScript file with tsx
        var startInfo = new ProcessStartInfo
        {
            FileName = npxPath,
            Arguments = $"nodemon --signal SIGTERM --watch . --ext ts,json --ignore node_modules/ --ignore .modules/ --exec \"npx tsx {appHostFile.Name}\"",
            WorkingDirectory = directory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Add environment variables
        foreach (var (key, value) in environmentVariables)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        using var process = new Process { StartInfo = startInfo };

        // Capture output for error reporting
        var outputCollector = new OutputCollector();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("nodemon({ProcessId}) {Identifier}: {Line}", process.Id, "stdout", e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("nodemon({ProcessId}) {Identifier}: {Line}", process.Id, "stderr", e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, outputCollector);
    }

    private static string? FindNpmPath()
    {
        return PathLookupHelper.FindFullPathFromPath("npm");
    }

    private static string? FindNpxPath()
    {
        return PathLookupHelper.FindFullPathFromPath("npx");
    }

    private static string? FindNodePath()
    {
        return PathLookupHelper.FindFullPathFromPath("node");
    }

    /// <inheritdoc />
    public async Task<int> PublishAsync(PublishContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Publishing TypeScript AppHost: {AppHostFile}", appHostFile.FullName);

        try
        {
            // Step 1: Check if node_modules exists, run npm install if needed
            var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                var npmInstallResult = await RunNpmInstallAsync(directory, cancellationToken);
                if (npmInstallResult != 0)
                {
                    _interactionService.DisplayError("Failed to install npm dependencies.");
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }

            // Step 2: Get package references and build AppHost server
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

            // Start the AppHost server process (it opens the backchannel for progress reporting)
            var currentPid = Environment.ProcessId;
            var (appHostServerProcess, appHostServerOutputCollector) = appHostServerProject.Run(jsonRpcSocketPath, currentPid, launchSettingsEnvVars, debug: context.Debug);

            // Start connecting to the backchannel
            if (context.BackchannelCompletionSource is not null)
            {
                _ = StartBackchannelConnectionAsync(appHostServerProcess, backchannelSocketPath, context.BackchannelCompletionSource, enableHotReload: false, cancellationToken);
            }

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            // Step 3: Generate code via RPC now that server is running
            if (needsCodeGen)
            {
                await GenerateCodeViaRpcAsync(
                    directory.FullName,
                    jsonRpcSocketPath,
                    packages,
                    cancellationToken);
            }

            if (appHostServerProcess.HasExited)
            {
                _interactionService.DisplayLines(appHostServerOutputCollector.GetLines());
                _interactionService.DisplayError("App host exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Pass the socket path to the TypeScript process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = jsonRpcSocketPath
            };

            // Execute the TypeScript apphost - this defines resources and triggers the publish
            // Pass the publish arguments to the TypeScript app (e.g., --operation publish --step deploy)
            var (typeScriptExitCode, typeScriptOutput) = await ExecuteTypeScriptAppHostAsync(appHostFile, directory, environmentVariables, cancellationToken, context.Arguments);

            if (typeScriptExitCode != 0)
            {
                _logger.LogError("TypeScript apphost exited with code {ExitCode}", typeScriptExitCode);

                // Display the output from TypeScript (same pattern as DotNetCliRunner)
                _interactionService.DisplayLines(typeScriptOutput.GetLines());

                // Signal failure so callers don't hang waiting for the backchannel
                var error = new InvalidOperationException("The TypeScript apphost failed.");
                context.BackchannelCompletionSource?.TrySetException(error);

                // Kill the AppHost server since TypeScript failed
                if (!appHostServerProcess.HasExited)
                {
                    try
                    {
                        appHostServerProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing AppHost server process after TypeScript failure");
                    }
                }

                return typeScriptExitCode;
            }

            // Kill the server after TypeScript exits
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
            _logger.LogError(ex, "Failed to publish TypeScript AppHost");
            _interactionService.DisplayError($"Failed to publish TypeScript AppHost: {ex.Message}");
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

        // Build and regenerate TypeScript SDK with the new package
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

        // Rebuild and regenerate TypeScript SDK with updated packages
        _interactionService.DisplayEmptyLine();
        _interactionService.DisplaySubtleMessage("Regenerating TypeScript SDK with updated packages...");
        await BuildAndGenerateSdkAsync(directory, cancellationToken);

        _interactionService.DisplayEmptyLine();
        _interactionService.DisplaySuccess(UpdateCommandStrings.UpdateSuccessfulMessage);

        return new UpdatePackagesResult { UpdatesApplied = true };
    }

    /// <inheritdoc />
    public async Task<bool> CheckAndHandleRunningInstanceAsync(FileInfo appHostFile, DirectoryInfo homeDirectory, CancellationToken cancellationToken)
    {
        // For TypeScript projects, we use the AppHost server's path to compute the socket path
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
    /// Generates TypeScript SDK code by calling the AppHost server's generateCode RPC method.
    /// </summary>
    private async Task GenerateCodeViaRpcAsync(
        string appPath,
        string socketPath,
        IEnumerable<(string PackageId, string Version)> packages,
        CancellationToken cancellationToken)
    {
        var packagesList = packages.ToList();
        _logger.LogDebug("Generating TypeScript code via RPC for {Count} packages", packagesList.Count);

        // Connect to the AppHost server using platform-appropriate transport
        Stream stream;
        var connected = false;
        var startTime = DateTimeOffset.UtcNow;

        if (OperatingSystem.IsWindows())
        {
            // On Windows, use named pipes (matches JsonRpcServer behavior)
            var pipeClient = new NamedPipeClientStream(".", socketPath, PipeDirection.InOut, PipeOptions.Asynchronous);

            // Wait for server to be ready (retry for up to 30 seconds)
            while (!connected && (DateTimeOffset.UtcNow - startTime) < TimeSpan.FromSeconds(30))
            {
                try
                {
                    await pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    connected = true;
                }
                catch (TimeoutException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!connected)
            {
                pipeClient.Dispose();
                throw new InvalidOperationException($"Failed to connect to AppHost server at {socketPath}");
            }

            stream = pipeClient;
        }
        else
        {
            // On Unix/macOS, use Unix domain sockets (matches JsonRpcServer behavior)
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);

            // Wait for server to be ready (retry for up to 30 seconds)
            while (!connected && (DateTimeOffset.UtcNow - startTime) < TimeSpan.FromSeconds(30))
            {
                try
                {
                    await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    connected = true;
                }
                catch (SocketException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!connected)
            {
                socket.Dispose();
                throw new InvalidOperationException($"Failed to connect to AppHost server at {socketPath}");
            }

            stream = new NetworkStream(socket, ownsSocket: true);
        }

        using (stream)
        {
            // Set up JSON-RPC connection with AOT-compatible JSON formatter
            var formatter = Backchannel.BackchannelJsonSerializerContext.CreateRpcMessageFormatter();
            var handler = new StreamJsonRpc.HeaderDelimitedMessageHandler(stream, stream, formatter);
            using var jsonRpc = new StreamJsonRpc.JsonRpc(handler);
            jsonRpc.StartListening();

            // Call generateCode RPC method
            _logger.LogDebug("Calling generateCode RPC method");
            var files = await jsonRpc.InvokeWithCancellationAsync<Dictionary<string, string>>("generateCode", ["TypeScript"], cancellationToken);

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

            _logger.LogInformation("Generated {Count} TypeScript files in {Path}",
                files.Count, outputPath);
        }
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
}
