// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Rosetta;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Handler for Python AppHost projects (apphost.py).
/// </summary>
internal sealed class PythonAppHostProject : IAppHostProject
{
    private readonly IInteractionService _interactionService;
    private readonly ILogger<PythonAppHostProject> _logger;

    public PythonAppHostProject(
        IInteractionService interactionService,
        ILogger<PythonAppHostProject> logger)
    {
        _interactionService = interactionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public AppHostType SupportedType => AppHostType.Python;

    /// <inheritdoc />
    public Task<bool> ValidateAsync(FileInfo appHostFile, CancellationToken cancellationToken)
    {
        // Check if the file exists and has the correct extension
        if (!appHostFile.Exists)
        {
            return Task.FromResult(false);
        }

        if (!appHostFile.Name.Equals("apphost.py", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        // Check for pyproject.toml, requirements.txt, or aspire.json in the same directory
        var directory = appHostFile.Directory;
        if (directory is null)
        {
            return Task.FromResult(false);
        }

        var hasPyprojectToml = File.Exists(Path.Combine(directory.FullName, "pyproject.toml"));
        var hasRequirementsTxt = File.Exists(Path.Combine(directory.FullName, "requirements.txt"));
        var hasAspireJson = File.Exists(Path.Combine(directory.FullName, "aspire.json"));

        return Task.FromResult(hasPyprojectToml || hasRequirementsTxt || hasAspireJson);
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken)
    {
        var appHostFile = context.AppHostFile;
        var directory = appHostFile.Directory!;

        _logger.LogDebug("Running Python AppHost: {AppHostFile}", appHostFile.FullName);

        Process? genericAppHostProcess = null;

        try
        {
            // Step 1: Check for virtual environment and dependencies
            var venvPath = Path.Combine(directory.FullName, ".venv");
            var useUv = FindUvPath() is not null;

            if (!Directory.Exists(venvPath))
            {
                _interactionService.DisplayMessage("package", "Setting up Python environment...");

                var setupResult = await SetupPythonEnvironmentAsync(directory, useUv, cancellationToken);
                if (setupResult != 0)
                {
                    _interactionService.DisplayError("Failed to set up Python environment.");
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }
            else if (useUv)
            {
                // Run uv sync to ensure dependencies are up to date
                _interactionService.DisplayMessage("package", "Syncing Python dependencies...");
                await RunUvSyncAsync(directory, cancellationToken);
            }

            // Step 2: Start the GenericAppHost (RemoteAppHost server)
            var projectModel = new ProjectModel(directory.FullName);
            var socketPath = projectModel.GetSocketPath();

            // Read aspire.json to get package references (if any)
            var packages = GetPackageReferences(directory);

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

            // Step 3: Execute the Python apphost
            _interactionService.DisplayMessage("rocket", "Starting Python AppHost...");

            // Pass the socket path to the Python process
            var environmentVariables = new Dictionary<string, string>(context.EnvironmentVariables)
            {
                ["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath
            };

            var exitCode = await ExecutePythonAppHostAsync(appHostFile, directory, useUv, environmentVariables, cancellationToken);

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Python AppHost");
            _interactionService.DisplayError($"Failed to run Python AppHost: {ex.Message}");
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

    private async Task<int> SetupPythonEnvironmentAsync(DirectoryInfo directory, bool useUv, CancellationToken cancellationToken)
    {
        if (useUv)
        {
            return await RunUvSyncAsync(directory, cancellationToken);
        }

        // Fall back to pip if uv is not available
        var pythonPath = FindPythonPath();
        if (pythonPath is null)
        {
            _interactionService.DisplayError("Python not found. Please install Python and ensure it is in your PATH.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        // Create virtual environment
        var venvResult = await RunProcessAsync(pythonPath, "-m venv .venv", directory, cancellationToken);
        if (venvResult != 0)
        {
            return venvResult;
        }

        // Install dependencies
        var pipPath = GetVenvPipPath(directory);
        var requirementsPath = Path.Combine(directory.FullName, "requirements.txt");

        if (File.Exists(requirementsPath))
        {
            return await RunProcessAsync(pipPath, $"install -r requirements.txt", directory, cancellationToken);
        }

        return 0;
    }

    private static async Task<int> RunUvSyncAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        var uvPath = FindUvPath();
        if (uvPath is null)
        {
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        return await RunProcessAsync(uvPath, "sync", directory, cancellationToken);
    }

    private async Task<int> ExecutePythonAppHostAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        bool useUv,
        IDictionary<string, string> environmentVariables,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo;

        if (useUv)
        {
            var uvPath = FindUvPath()!;
            startInfo = new ProcessStartInfo
            {
                FileName = uvPath,
                Arguments = $"run python \"{appHostFile.FullName}\"",
                WorkingDirectory = directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        else
        {
            var pythonPath = GetVenvPythonPath(directory);
            if (!File.Exists(pythonPath))
            {
                pythonPath = FindPythonPath();
            }

            if (pythonPath is null)
            {
                _interactionService.DisplayError("Python not found. Please install Python and ensure it is in your PATH.");
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{appHostFile.FullName}\"",
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

    private static async Task<int> RunProcessAsync(string fileName, string arguments, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory.FullName,
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

    private static string? FindUvPath()
    {
        return PathLookupHelper.FindFullPathFromPath("uv");
    }

    private static string? FindPythonPath()
    {
        // Try python3 first (common on macOS/Linux), then python
        return PathLookupHelper.FindFullPathFromPath("python3") ?? PathLookupHelper.FindFullPathFromPath("python");
    }

    private static string GetVenvPythonPath(DirectoryInfo directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(directory.FullName, ".venv", "Scripts", "python.exe");
        }
        return Path.Combine(directory.FullName, ".venv", "bin", "python");
    }

    private static string GetVenvPipPath(DirectoryInfo directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(directory.FullName, ".venv", "Scripts", "pip.exe");
        }
        return Path.Combine(directory.FullName, ".venv", "bin", "pip");
    }

    /// <inheritdoc />
    public Task<bool> AddPackageAsync(AddPackageContext context, CancellationToken cancellationToken)
    {
        var directory = context.AppHostFile.Directory;
        if (directory is null)
        {
            return Task.FromResult(false);
        }

        // Update .aspire/settings.json with the new package
        var config = AspireJsonConfiguration.Load(directory.FullName) ?? new AspireJsonConfiguration();
        config.AddOrUpdatePackage(context.PackageId, context.PackageVersion);
        config.Save(directory.FullName);

        // TODO: Regenerate Python SDK code when code generation is implemented for Python
        return Task.FromResult(true);
    }
}
