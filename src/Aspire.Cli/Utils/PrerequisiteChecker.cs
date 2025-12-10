// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Cli.DotNet;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Result of prerequisite check.
/// </summary>
internal sealed class PrerequisiteCheckResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Warnings { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// Service for checking system prerequisites.
/// </summary>
internal interface IPrerequisiteChecker
{
    /// <summary>
    /// Checks all prerequisites for running Aspire CLI.
    /// </summary>
    Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if .NET SDK version meets minimum requirements.
    /// </summary>
    Task<(bool IsValid, string? InstalledVersion, string? Message)> CheckDotNetSdkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a container runtime (Docker/Podman) is available.
    /// </summary>
    Task<(bool IsAvailable, string? RuntimeName, string? Message)> CheckContainerRuntimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if running in WSL and provides warnings if applicable.
    /// </summary>
    (bool IsWSL, string? Warning) CheckWSLEnvironment();

    /// <summary>
    /// Checks if Docker Engine (non-Desktop) is being used and may need tunnel configuration.
    /// </summary>
    Task<(bool IsDockerEngine, string? Message)> CheckDockerEngineAsync(CancellationToken cancellationToken = default);
}

internal sealed class PrerequisiteChecker : IPrerequisiteChecker
{
    private readonly IDotNetCliRunner _dotNetRunner;
    private readonly ILogger<PrerequisiteChecker> _logger;
    
    // Minimum required .NET SDK version for Aspire CLI
    // This should be kept in sync with the SDK version specified in global.json
    private static readonly Version s_minimumDotNetVersion = new Version(10, 0);

    public PrerequisiteChecker(
        IDotNetCliRunner dotNetRunner,
        ILogger<PrerequisiteChecker> logger)
    {
        ArgumentNullException.ThrowIfNull(dotNetRunner);
        ArgumentNullException.ThrowIfNull(logger);

        _dotNetRunner = dotNetRunner;
        _logger = logger;
    }

    public async Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(CancellationToken cancellationToken = default)
    {
        var result = new PrerequisiteCheckResult();

        // Check .NET SDK
        var (sdkValid, _, sdkMessage) = await CheckDotNetSdkAsync(cancellationToken);
        if (!sdkValid)
        {
            result.Errors.Add(sdkMessage ?? ".NET SDK validation failed");
        }

        // Check container runtime
        var (runtimeAvailable, runtimeName, runtimeMessage) = await CheckContainerRuntimeAsync(cancellationToken);
        if (!runtimeAvailable)
        {
            result.Warnings.Add(runtimeMessage ?? "No container runtime detected. Docker or Podman is recommended for running Aspire applications.");
        }

        // Check WSL
        var (isWSL, wslWarning) = CheckWSLEnvironment();
        if (isWSL && wslWarning != null)
        {
            result.Warnings.Add(wslWarning);
        }

        // Check Docker Engine vs Desktop
        if (runtimeAvailable && runtimeName?.Contains("docker", StringComparison.OrdinalIgnoreCase) == true)
        {
            var (isDockerEngine, dockerEngineMessage) = await CheckDockerEngineAsync(cancellationToken);
            if (isDockerEngine && dockerEngineMessage != null)
            {
                result.Warnings.Add(dockerEngineMessage);
            }
        }

        return result;
    }

    public async Task<(bool IsValid, string? InstalledVersion, string? Message)> CheckDotNetSdkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, null, ".NET SDK not found or could not be executed.");
            }

            var versionString = (await process.StandardOutput.ReadToEndAsync(cancellationToken)).Trim();
            await process.WaitForExitAsync(cancellationToken);
            
            if (process.ExitCode == 0 && !string.IsNullOrEmpty(versionString))
            {
                if (Version.TryParse(versionString.Split('-')[0], out var installedVersion))
                {
                    if (installedVersion >= s_minimumDotNetVersion)
                    {
                        return (true, versionString, null);
                    }
                    else
                    {
                        return (false, versionString, 
                            $".NET SDK version {s_minimumDotNetVersion.Major}.{s_minimumDotNetVersion.Minor} or later is required. Detected: {versionString}");
                    }
                }
            }

            return (false, null, ".NET SDK not found or version could not be determined.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check .NET SDK version");
            return (false, null, ".NET SDK not found or could not be executed.");
        }
    }

    public async Task<(bool IsAvailable, string? RuntimeName, string? Message)> CheckContainerRuntimeAsync(CancellationToken cancellationToken = default)
    {
        // Try Docker first
        var dockerAvailable = await TryExecuteCommandAsync("docker", ["--version"], cancellationToken);
        if (dockerAvailable)
        {
            return (true, "docker", null);
        }

        // Try Podman
        var podmanAvailable = await TryExecuteCommandAsync("podman", ["--version"], cancellationToken);
        if (podmanAvailable)
        {
            return (true, "podman", null);
        }

        return (false, null, "No container runtime detected. Docker Desktop or Podman is required for running containerized resources.");
    }

    public (bool IsWSL, string? Warning) CheckWSLEnvironment()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return (false, null);
        }

        try
        {
            // Check for WSL by looking at /proc/version
            if (File.Exists("/proc/version"))
            {
                var versionInfo = File.ReadAllText("/proc/version");
                if (versionInfo.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
                    versionInfo.Contains("WSL", StringComparison.OrdinalIgnoreCase))
                {
                    var warning = "Running in WSL environment. For optimal performance, ensure WSL integration is properly configured with Docker Desktop. " +
                                  "See: https://aka.ms/aspire-setup";
                    return (true, warning);
                }
            }

            // Check WSL_DISTRO_NAME environment variable
            var wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
            if (!string.IsNullOrEmpty(wslDistro))
            {
                var warning = $"Running in WSL distribution: {wslDistro}. For optimal performance, ensure WSL integration is properly configured. " +
                              "See: https://aka.ms/aspire-setup";
                return (true, warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect WSL environment");
        }

        return (false, null);
    }

    public async Task<(bool IsDockerEngine, string? Message)> CheckDockerEngineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                // Use escaped double quotes for cross-platform compatibility
                // Single quotes don't work correctly on Windows
                Arguments = "info --format \"{{.ServerVersion}}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, null);
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                // Check if Docker Desktop is running by looking for desktop-specific context
                var contextStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    // Use escaped double quotes for cross-platform compatibility
                    Arguments = "context ls --format \"{{.Name}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var contextProcess = Process.Start(contextStartInfo);
                if (contextProcess != null)
                {
                    var contextOutput = await contextProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                    await contextProcess.WaitForExitAsync(cancellationToken);

                    if (contextProcess.ExitCode == 0)
                    {
                        // If no desktop context found, likely using Docker Engine
                        if (!contextOutput.Contains("desktop", StringComparison.OrdinalIgnoreCase))
                        {
                            var message = "Using Docker Engine (not Docker Desktop). " +
                                          "You may need to configure the Aspire tunnel for service-to-service communication. " +
                                          "See: https://aka.ms/aspire-docker-engine";
                            return (true, message);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check Docker Engine vs Desktop");
        }

        return (false, null);
    }

    private static async Task<bool> TryExecuteCommandAsync(string command, string[] arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
