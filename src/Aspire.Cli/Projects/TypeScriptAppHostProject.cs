// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Aspire.Hosting.CodeGeneration;
using Aspire.Hosting.CodeGeneration.TypeScript;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Handler for TypeScript AppHost projects (apphost.ts).
/// </summary>
internal sealed class TypeScriptAppHostProject : IAppHostProject
{
    // Constants for running instance detection
    private const int ProcessTerminationTimeoutMs = 10000; // Wait up to 10 seconds for processes to terminate
    private const int ProcessTerminationPollIntervalMs = 250; // Check process status every 250ms
    private const string GeneratedFolderName = ".modules";

    private readonly IInteractionService _interactionService;
    private readonly IAppHostCliBackchannel _backchannel;
    private readonly IAppHostServerProjectFactory _appHostServerProjectFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TypeScriptAppHostProject> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly CodeGeneratorService _codeGeneratorService;
    private readonly TypeScriptCodeGenerator _typeScriptGenerator;

    private static readonly string[] s_detectionPatterns = ["apphost.ts"];

    public TypeScriptAppHostProject(
        IInteractionService interactionService,
        IAppHostCliBackchannel backchannel,
        IAppHostServerProjectFactory appHostServerProjectFactory,
        IConfiguration configuration,
        ILogger<TypeScriptAppHostProject> logger,
        TimeProvider? timeProvider = null)
    {
        _interactionService = interactionService;
        _backchannel = backchannel;
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _codeGeneratorService = new CodeGeneratorService();
        _typeScriptGenerator = new TypeScriptCodeGenerator();
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
            // For more information, see: https://learn.microsoft.com/dotnet/aspire

            // Import from the generated module (created by 'aspire run' code generation)
            import { createBuilder } from './.modules/distributed-application.js';

            // Create the distributed application builder
            const builder = await createBuilder();

            // Add your resources here, for example:
            // const redis = await builder.addContainer("cache", "redis:latest");
            // const postgres = await builder.addPostgres("db");

            // Build and run the application
            const app = builder.build();
            await app.run();
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
    }

    // ═══════════════════════════════════════════════════════════════
    // EXECUTION
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Task<bool> ValidateAsync(FileInfo appHostFile, CancellationToken cancellationToken)
    {
        // Check if the file exists and has the correct extension
        if (!appHostFile.Exists)
        {
            return Task.FromResult(false);
        }

        if (!appHostFile.Name.Equals("apphost.ts", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        // Check for package.json in the same directory
        var directory = appHostFile.Directory;
        if (directory is null)
        {
            return Task.FromResult(false);
        }

        var hasPackageJson = File.Exists(Path.Combine(directory.FullName, "package.json"));

        return Task.FromResult(hasPackageJson);
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Running TypeScript AppHost: {AppHostFile}", appHostFile.FullName);

        Process? appHostServerProcess = null;

        try
        {
            // Step 1: Check if node_modules exists, run npm install if needed
            var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                var npmInstallResult = await _interactionService.ShowStatusAsync(
                    ":package: Installing npm dependencies...",
                    () => RunNpmInstallAsync(directory, cancellationToken));

                if (npmInstallResult != 0)
                {
                    _interactionService.DisplayError("Failed to install npm dependencies.");
                    context.BuildCompletionSource?.TrySetResult(false);
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }

            // Step 2: Get package references and build AppHost server FIRST (code gen needs assemblies)
            var packages = GetPackageReferences(directory).ToList();
            var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
            var socketPath = appHostServerProject.GetSocketPath();

            // Create the AppHost server project files
            await _interactionService.ShowStatusAsync(
                ":gear: Preparing AppHost server...",
                () => appHostServerProject.CreateProjectFilesAsync(packages, cancellationToken));

            // Build the AppHost server (must happen before code generation!)
            var buildSuccess = await appHostServerProject.BuildAsync(_interactionService);
            if (!buildSuccess)
            {
                _interactionService.DisplayError("Failed to build AppHost server.");
                context.BuildCompletionSource?.TrySetResult(false);
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Step 3: Run code generation now that assemblies are built
            if (NeedsGeneration(directory.FullName, packages))
            {
                await _interactionService.ShowStatusAsync(
                    ":gear: Generating TypeScript SDK...",
                    async () =>
                    {
                        await GenerateCodeAsync(
                            directory.FullName,
                            appHostServerProject.BuildPath,
                            packages,
                            cancellationToken);
                        return true;
                    });
            }

            // Signal that build/preparation is complete
            context.BuildCompletionSource?.TrySetResult(true);

            // Read launchSettings.json if it exists, or create defaults
            var launchSettingsEnvVars = ReadLaunchSettingsEnvironmentVariables(directory) ?? new Dictionary<string, string>();

            // Generate a backchannel socket path for CLI to connect to AppHost server
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to AppHost server so it opens a server for CLI communication
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Start the AppHost server process
            _interactionService.DisplayMessage("rocket", "Starting AppHost server...");
            var currentPid = Environment.ProcessId;
            var (process, appHostServerOutputCollector) = appHostServerProject.Run(socketPath, currentPid, launchSettingsEnvVars);
            appHostServerProcess = process;

            // The backchannel completion source is the contract with RunCommand
            // We signal this when the backchannel is ready, RunCommand uses it for UX
            var backchannelCompletionSource = context.BackchannelCompletionSource ?? new TaskCompletionSource<IAppHostCliBackchannel>();

            // Start connecting to the backchannel (for dashboard URLs, logs, etc.)
            _ = StartBackchannelConnectionAsync(appHostServerProcess, backchannelSocketPath, backchannelCompletionSource, cancellationToken);

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (appHostServerProcess.HasExited)
            {
                _interactionService.DisplayLines(appHostServerOutputCollector.GetLines());
                _interactionService.DisplayError("AppHost server exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Step 4: Execute the TypeScript apphost
            _interactionService.DisplayMessage("rocket", "Starting TypeScript AppHost...");

            // Pass the socket path to the TypeScript process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath
            };

            // Start TypeScript apphost - it will connect to AppHost server, define resources, then exit
            var pendingTypeScript = ExecuteTypeScriptAppHostAsync(appHostFile, directory, environmentVariables, cancellationToken);

            // Wait for TypeScript to finish defining resources
            var typeScriptExitCode = await pendingTypeScript;
            if (typeScriptExitCode != 0)
            {
                _logger.LogError("TypeScript apphost exited with code {ExitCode}", typeScriptExitCode);
            }

            // Wait for the AppHost server to exit (Ctrl+C)
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
        finally
        {
            // Clean up the AppHost server process
            if (appHostServerProcess is not null && !appHostServerProcess.HasExited)
            {
                try
                {
                    appHostServerProcess.Kill(entireProcessTree: true);
                    appHostServerProcess.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process");
                }
            }
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

    private async Task<int> ExecuteTypeScriptAppHostAsync(
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
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var jsFile = Path.ChangeExtension(appHostFile.FullName, ".js");
            var distJsFile = Path.Combine(directory.FullName, "dist", "apphost.js");

            var targetFile = File.Exists(distJsFile) ? distJsFile : jsFile;

            if (!File.Exists(targetFile))
            {
                _interactionService.DisplayError($"Compiled JavaScript file not found: {targetFile}");
                _interactionService.DisplayMessage("info", "Try running 'npx tsc' to compile your TypeScript first.");
                return ExitCodeConstants.FailedToBuildArtifacts;
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

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                Console.Error.WriteLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // CLI was cancelled (e.g., Ctrl+C), gracefully terminate the Node process
            _logger.LogDebug("Cancellation requested, terminating TypeScript process");

            if (!process.HasExited)
            {
                try
                {
                    // Try graceful termination first (SIGTERM on Unix, TerminateProcess on Windows)
                    process.Kill(entireProcessTree: true);

                    // Give it a moment to exit
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Process didn't exit in time, it will be cleaned up when disposed
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error terminating TypeScript process");
                }
            }

            throw; // Re-throw to signal cancellation to caller
        }
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

        Process? appHostServerProcess = null;

        try
        {
            // Step 1: Check if node_modules exists, run npm install if needed
            var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                var npmInstallResult = await _interactionService.ShowStatusAsync(
                    ":package: Installing npm dependencies...",
                    () => RunNpmInstallAsync(directory, cancellationToken));

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

            // Create the AppHost server project files
            await _interactionService.ShowStatusAsync(
                ":gear: Preparing AppHost server...",
                () => appHostServerProject.CreateProjectFilesAsync(packages, cancellationToken));

            // Build the AppHost server
            var buildSuccess = await appHostServerProject.BuildAsync(_interactionService);
            if (!buildSuccess)
            {
                _interactionService.DisplayError("Failed to build AppHost server.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Step 3: Run code generation now that assemblies are built
            if (NeedsGeneration(directory.FullName, packages))
            {
                await _interactionService.ShowStatusAsync(
                    ":gear: Generating TypeScript SDK...",
                    async () =>
                    {
                        await GenerateCodeAsync(
                            directory.FullName,
                            appHostServerProject.BuildPath,
                            packages,
                            cancellationToken);
                        return true;
                    });
            }

            // Read launchSettings.json if it exists
            var launchSettingsEnvVars = ReadLaunchSettingsEnvironmentVariables(directory) ?? new Dictionary<string, string>();

            // Generate a backchannel socket path for CLI to connect to AppHost server
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to AppHost server so it opens a server
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Start the AppHost server process (it opens the backchannel for progress reporting)
            var currentPid = Environment.ProcessId;

            // AppHost server doesn't receive publish args - those go to the TypeScript app
            var (process, appHostServerOutputCollector) = appHostServerProject.Run(jsonRpcSocketPath, currentPid, launchSettingsEnvVars);
            appHostServerProcess = process;

            // Start connecting to the backchannel
            if (context.BackchannelCompletionSource is not null)
            {
                _ = StartBackchannelConnectionAsync(appHostServerProcess, backchannelSocketPath, context.BackchannelCompletionSource, cancellationToken);
            }

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (appHostServerProcess.HasExited)
            {
                _interactionService.DisplayLines(appHostServerOutputCollector.GetLines());
                _interactionService.DisplayError("AppHost server exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Pass the socket path to the TypeScript process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = jsonRpcSocketPath
            };

            // Execute the TypeScript apphost - this defines resources and triggers the publish
            // Pass the publish arguments to the TypeScript app (e.g., --operation publish --step deploy)
            var typeScriptExitCode = await ExecuteTypeScriptAppHostAsync(appHostFile, directory, environmentVariables, cancellationToken, context.Arguments);

            if (typeScriptExitCode != 0)
            {
                _logger.LogError("TypeScript apphost exited with code {ExitCode}", typeScriptExitCode);
                return typeScriptExitCode;
            }

            // Wait for the AppHost server to complete the publish pipeline
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
        finally
        {
            // Clean up the AppHost server process
            if (appHostServerProcess is not null && !appHostServerProcess.HasExited)
            {
                try
                {
                    appHostServerProcess.Kill(entireProcessTree: true);
                    appHostServerProcess.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process");
                }
            }
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
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var connectionAttempts = 0;

        _logger.LogDebug("Starting backchannel connection to AppHost server at {SocketPath}", socketPath);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogTrace("Attempting to connect to AppHost server backchannel at {SocketPath} (attempt {Attempt})", socketPath, ++connectionAttempts);
                await _backchannel.ConnectAsync(socketPath, cancellationToken).ConfigureAwait(false);
                backchannelCompletionSource.SetResult(_backchannel);
                _logger.LogDebug("Connected to AppHost server backchannel at {SocketPath}", socketPath);
                return;
            }
            catch (SocketException ex) when (process.HasExited && process.ExitCode != 0)
            {
                _logger.LogError("AppHost server process has exited. Unable to connect to backchannel at {SocketPath}", socketPath);
                var backchannelException = new FailedToConnectBackchannelConnection($"AppHost server process has exited unexpectedly.", process, ex);
                backchannelCompletionSource.SetException(backchannelException);
                return;
            }
            catch (SocketException)
            {
                // Slow down polling after 10 seconds
                var waitingFor = DateTimeOffset.UtcNow - startTime;
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
                backchannelCompletionSource.SetException(ex);
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

        // Regenerate TypeScript SDK code
        var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
        var packages = GetPackageReferences(directory).ToList();
        await GenerateCodeAsync(
            directory.FullName,
            appHostServerProject.BuildPath,
            packages,
            cancellationToken);

        return true;
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

        return _codeGeneratorService.NeedsGeneration(appPath, packages, GeneratedFolderName);
    }

    /// <summary>
    /// Generates TypeScript SDK code for the specified app path.
    /// </summary>
    private async Task GenerateCodeAsync(
        string appPath,
        string buildPath,
        IEnumerable<(string PackageId, string Version)> packages,
        CancellationToken cancellationToken)
    {
        var packagesList = packages.ToList();
        _logger.LogDebug("Generating TypeScript code for {Count} packages", packagesList.Count);

        // Build assembly search paths
        var searchPaths = BuildAssemblySearchPaths(buildPath);

        // Use the shared code generator service
        var fileCount = await _codeGeneratorService.GenerateAsync(
            appPath,
            _typeScriptGenerator,
            packagesList,
            searchPaths,
            GeneratedFolderName,
            cancellationToken);

        _logger.LogInformation("Generated {Count} TypeScript files in {Path}",
            fileCount, Path.Combine(appPath, GeneratedFolderName));
    }

    /// <summary>
    /// Builds the list of paths to search for assemblies.
    /// </summary>
    private static List<string> BuildAssemblySearchPaths(string buildPath)
    {
        var searchPaths = new List<string> { buildPath };

        // Add NuGet cache if available
        var nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");
        if (Directory.Exists(nugetCache))
        {
            searchPaths.Add(nugetCache);
        }

        // Add runtime directory
        var runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        searchPaths.Add(runtimeDirectory);

        // Add ASP.NET Core shared framework directory (contains HealthChecks, etc.)
        var aspnetCoreDirectory = runtimeDirectory.Replace("Microsoft.NETCore.App", "Microsoft.AspNetCore.App");
        if (Directory.Exists(aspnetCoreDirectory))
        {
            searchPaths.Add(aspnetCoreDirectory);
        }

        return searchPaths;
    }
}
