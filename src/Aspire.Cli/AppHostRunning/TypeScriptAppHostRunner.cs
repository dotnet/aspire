// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.CodeGeneration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Rosetta;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Runner for TypeScript AppHost projects (apphost.ts).
/// </summary>
internal sealed class TypeScriptAppHostRunner : IAppHostRunner
{
    private readonly IInteractionService _interactionService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly ILogger<TypeScriptAppHostRunner> _logger;

    public TypeScriptAppHostRunner(
        IInteractionService interactionService,
        ICodeGenerationService codeGenerationService,
        ILogger<TypeScriptAppHostRunner> logger)
    {
        _interactionService = interactionService;
        _codeGenerationService = codeGenerationService;
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
    public async Task<int> RunAsync(AppHostRunnerContext context, CancellationToken cancellationToken)
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

            // Step 2: Get package references and run code generation if needed
            var packages = GetPackageReferences(directory).ToList();

            if (_codeGenerationService.NeedsGeneration(directory.FullName, packages))
            {
                await _interactionService.ShowStatusAsync(
                    "Generating TypeScript SDK...",
                    async () =>
                    {
                        await _codeGenerationService.GenerateTypeScriptAsync(
                            directory.FullName,
                            packages,
                            cancellationToken);
                        return true;
                    });
            }

            // Step 3: Start the GenericAppHost (RemoteAppHost server)
            var projectModel = new ProjectModel(directory.FullName);
            var socketPath = projectModel.GetSocketPath();

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

            // Start the GenericAppHost process
            _interactionService.DisplayMessage("rocket", "Starting GenericAppHost...");
            var currentPid = Environment.ProcessId;
            genericAppHostProcess = projectModel.Run(socketPath, currentPid);

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

    private static IEnumerable<(string Name, string Version)> GetPackageReferences(DirectoryInfo _)
    {
        // TODO: Read aspire.json to get package references
        // For now, return the base Aspire.Hosting package
        yield return ("Aspire.Hosting", ProjectModel.AspireHostVersion);
        yield return ("Aspire.Hosting.AppHost", ProjectModel.AspireHostVersion);
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
        CancellationToken cancellationToken)
    {
        // Try to find npx for running tsx directly, or use node if compiled
        var npxPath = FindNpxPath();

        ProcessStartInfo startInfo;

        if (npxPath is not null)
        {
            // Use npx tsx to run TypeScript directly
            startInfo = new ProcessStartInfo
            {
                FileName = npxPath,
                Arguments = $"tsx \"{appHostFile.FullName}\"",
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
                Arguments = $"\"{targetFile}\"",
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

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
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
}
