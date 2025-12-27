// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.CodeGeneration;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Rosetta;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Handler for TypeScript AppHost projects (apphost.ts).
/// </summary>
internal sealed class TypeScriptAppHostProject : IAppHostProject
{
    private readonly IInteractionService _interactionService;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IAppHostCliBackchannel _backchannel;
    private readonly ILogger<TypeScriptAppHostProject> _logger;

    public TypeScriptAppHostProject(
        IInteractionService interactionService,
        [FromKeyedServices(AppHostType.TypeScript)] ICodeGenerator codeGenerator,
        IAppHostCliBackchannel backchannel,
        ILogger<TypeScriptAppHostProject> logger)
    {
        _interactionService = interactionService;
        _codeGenerator = codeGenerator;
        _backchannel = backchannel;
        _logger = logger;
    }

    /// <inheritdoc />
    public AppHostType SupportedType => AppHostType.TypeScript;

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

        // Check for package.json or aspire.json in the same directory
        var directory = appHostFile.Directory;
        if (directory is null)
        {
            return Task.FromResult(false);
        }

        var hasPackageJson = File.Exists(Path.Combine(directory.FullName, "package.json"));
        var hasAspireJson = File.Exists(Path.Combine(directory.FullName, "aspire.json"));

        return Task.FromResult(hasPackageJson || hasAspireJson);
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Running TypeScript AppHost: {AppHostFile}", appHostFile.FullName);

        Process? genericAppHostProcess = null;

        try
        {
            // Step 1: Check if node_modules exists, run npm install if needed
            var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                _interactionService.DisplayMessage("package", "Installing npm dependencies...");

                var npmInstallResult = await RunNpmInstallAsync(directory, cancellationToken);
                if (npmInstallResult != 0)
                {
                    _interactionService.DisplayError("Failed to install npm dependencies.");
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }

            // Step 2: Get package references and build GenericAppHost FIRST (code gen needs assemblies)
            var packages = GetPackageReferences(directory).ToList();
            var projectModel = new ProjectModel(directory.FullName);
            var socketPath = projectModel.GetSocketPath();

            // Create the GenericAppHost project files
            _interactionService.DisplayMessage("gear", "Preparing GenericAppHost...");
            projectModel.CreateProjectFiles(packages);

            // Build the GenericAppHost (must happen before code generation!)
            var buildSuccess = await projectModel.BuildAsync(_interactionService);
            if (!buildSuccess)
            {
                _interactionService.DisplayError("Failed to build GenericAppHost.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Step 3: Run code generation now that assemblies are built
            if (_codeGenerator.NeedsGeneration(directory.FullName, packages))
            {
                await _interactionService.ShowStatusAsync(
                    "Generating TypeScript SDK...",
                    async () =>
                    {
                        await _codeGenerator.GenerateAsync(
                            directory.FullName,
                            packages,
                            cancellationToken);
                        return true;
                    });
            }

            // Read launchSettings.json if it exists
            var launchSettingsEnvVars = ReadLaunchSettingsEnvironmentVariables(directory);

            // Start the GenericAppHost process
            _interactionService.DisplayMessage("rocket", "Starting GenericAppHost...");
            var currentPid = Environment.ProcessId;
            genericAppHostProcess = projectModel.Run(socketPath, currentPid, launchSettingsEnvVars);

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (genericAppHostProcess.HasExited)
            {
                _interactionService.DisplayError("GenericAppHost process exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Step 4: Execute the TypeScript apphost
            _interactionService.DisplayMessage("rocket", "Starting TypeScript AppHost...");

            // Pass the socket path to the TypeScript process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath
            };

            var exitCode = await ExecuteTypeScriptAppHostAsync(appHostFile, directory, environmentVariables, cancellationToken);

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run TypeScript AppHost");
            _interactionService.DisplayError($"Failed to run TypeScript AppHost: {ex.Message}");
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        finally
        {
            // Clean up the GenericAppHost process
            if (genericAppHostProcess is not null && !genericAppHostProcess.HasExited)
            {
                try
                {
                    genericAppHostProcess.Kill(entireProcessTree: true);
                    genericAppHostProcess.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing GenericAppHost process");
                }
            }
        }
    }

    private static IEnumerable<(string Name, string Version)> GetPackageReferences(DirectoryInfo directory)
    {
        // Always include the base Aspire.Hosting packages
        yield return ("Aspire.Hosting", ProjectModel.AspireHostVersion);
        yield return ("Aspire.Hosting.AppHost", ProjectModel.AspireHostVersion);

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

        Process? genericAppHostProcess = null;

        try
        {
            // Step 1: Check if node_modules exists, run npm install if needed
            var nodeModulesPath = Path.Combine(directory.FullName, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                _interactionService.DisplayMessage("package", "Installing npm dependencies...");

                var npmInstallResult = await RunNpmInstallAsync(directory, cancellationToken);
                if (npmInstallResult != 0)
                {
                    _interactionService.DisplayError("Failed to install npm dependencies.");
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }

            // Step 2: Get package references and build GenericAppHost
            var packages = GetPackageReferences(directory).ToList();
            var projectModel = new ProjectModel(directory.FullName);
            var jsonRpcSocketPath = projectModel.GetSocketPath();

            // Create the GenericAppHost project files
            _interactionService.DisplayMessage("gear", "Preparing GenericAppHost...");
            projectModel.CreateProjectFiles(packages);

            // Build the GenericAppHost
            var buildSuccess = await projectModel.BuildAsync(_interactionService);
            if (!buildSuccess)
            {
                _interactionService.DisplayError("Failed to build GenericAppHost.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Step 3: Run code generation now that assemblies are built
            // Note: We use DisplayMessage instead of ShowStatusAsync to avoid conflicts
            // with PipelineCommandBase's ShowStatusAsync for backchannel waiting
            if (_codeGenerator.NeedsGeneration(directory.FullName, packages))
            {
                _interactionService.DisplayMessage("gear", "Generating TypeScript SDK...");
                await _codeGenerator.GenerateAsync(
                    directory.FullName,
                    packages,
                    cancellationToken);
            }

            // Read launchSettings.json if it exists
            var launchSettingsEnvVars = ReadLaunchSettingsEnvironmentVariables(directory) ?? new Dictionary<string, string>();

            // Generate a backchannel socket path for CLI to connect to GenericAppHost
            var backchannelSocketPath = GetBackchannelSocketPath();

            // Pass the backchannel socket path to GenericAppHost so it opens a server
            launchSettingsEnvVars[KnownConfigNames.UnixSocketPath] = backchannelSocketPath;

            // Start the GenericAppHost process (it opens the backchannel for progress reporting)
            _interactionService.DisplayMessage("rocket", "Starting GenericAppHost for publish...");
            var currentPid = Environment.ProcessId;

            // GenericAppHost doesn't receive publish args - those go to the TypeScript app
            genericAppHostProcess = projectModel.Run(jsonRpcSocketPath, currentPid, launchSettingsEnvVars);

            // Start connecting to the backchannel
            if (context.BackchannelCompletionSource is not null)
            {
                _ = StartBackchannelConnectionAsync(genericAppHostProcess, backchannelSocketPath, context.BackchannelCompletionSource, cancellationToken);
            }

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (genericAppHostProcess.HasExited)
            {
                _interactionService.DisplayError("GenericAppHost process exited unexpectedly.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            // Step 4: Execute the TypeScript apphost
            // This connects to GenericAppHost via JSON-RPC and defines resources
            _interactionService.DisplayMessage("rocket", "Starting TypeScript AppHost for publish...");

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

            // Wait for the GenericAppHost to complete the publish pipeline
            await genericAppHostProcess.WaitForExitAsync(cancellationToken);

            return genericAppHostProcess.ExitCode;
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
            // Clean up the GenericAppHost process
            if (genericAppHostProcess is not null && !genericAppHostProcess.HasExited)
            {
                try
                {
                    genericAppHostProcess.Kill(entireProcessTree: true);
                    genericAppHostProcess.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing GenericAppHost process");
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
    /// Starts connecting to the GenericAppHost's backchannel server.
    /// </summary>
    private async Task StartBackchannelConnectionAsync(
        Process process,
        string socketPath,
        TaskCompletionSource<IAppHostCliBackchannel> backchannelCompletionSource,
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var connectionAttempts = 0;

        _logger.LogDebug("Starting backchannel connection to GenericAppHost at {SocketPath}", socketPath);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogTrace("Attempting to connect to GenericAppHost backchannel at {SocketPath} (attempt {Attempt})", socketPath, ++connectionAttempts);
                await _backchannel.ConnectAsync(socketPath, cancellationToken).ConfigureAwait(false);
                backchannelCompletionSource.SetResult(_backchannel);
                _logger.LogDebug("Connected to GenericAppHost backchannel at {SocketPath}", socketPath);
                return;
            }
            catch (SocketException ex) when (process.HasExited && process.ExitCode != 0)
            {
                _logger.LogError("GenericAppHost process has exited. Unable to connect to backchannel at {SocketPath}", socketPath);
                var backchannelException = new FailedToConnectBackchannelConnection($"GenericAppHost process has exited unexpectedly.", process, ex);
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
                _logger.LogError(ex, "Failed to connect to GenericAppHost backchannel");
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
        var packages = GetPackageReferences(directory).ToList();
        await _codeGenerator.GenerateAsync(
            directory.FullName,
            packages,
            cancellationToken);

        return true;
    }
}
