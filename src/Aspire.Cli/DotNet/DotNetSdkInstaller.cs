// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.DotNet;

/// <summary>
/// Default implementation of <see cref="IDotNetSdkInstaller"/> that checks for dotnet on the system PATH.
/// </summary>
internal sealed class DotNetSdkInstaller(IFeatures features, IConfiguration configuration, CliExecutionContext executionContext, ILogger<DotNetSdkInstaller> logger) : IDotNetSdkInstaller
{
    /// <summary>
    /// The minimum .NET SDK version required for Aspire.
    /// </summary>
    public const string MinimumSdkVersion = "9.0.302";

    /// <summary>
    /// The minimum .NET SDK version required for Aspire when .NET 10 features are enabled.
    /// </summary>
    public const string MinimumSdkNet10SdkVersion = "10.0.100";

    /// <inheritdoc />
    public async Task<(bool Success, string? HighestVersion, string MinimumRequiredVersion, bool ForceInstall)> CheckAsync(CancellationToken cancellationToken = default)
    {
        var minimumVersion = GetEffectiveMinimumSdkVersion();
        
        // Check if alwaysInstallSdk is enabled - this forces installation even when SDK check passes
        var alwaysInstallSdk = configuration["alwaysInstallSdk"];
        var forceInstall = !string.IsNullOrEmpty(alwaysInstallSdk) && 
                          bool.TryParse(alwaysInstallSdk, out var alwaysInstall) && 
                          alwaysInstall;
        
        if (!features.IsFeatureEnabled(KnownFeatures.MinimumSdkCheckEnabled, true))
        {
            // If the feature is disabled, we assume the SDK is available
            return (true, null, minimumVersion, forceInstall);
        }

        try
        {
            // Add --arch flag to ensure we only get SDKs that match the current architecture
            var currentArch = GetCurrentArchitecture();
            var arguments = $"--list-sdks --arch {currentArch}";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return (false, null, minimumVersion, forceInstall);
            }

            // Parse the minimum version requirement
            if (!SemVersion.TryParse(minimumVersion, SemVersionStyles.Strict, out var minVersion))
            {
                return (false, null, minimumVersion, forceInstall);
            }

            // Parse each line of the output to find SDK versions
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            SemVersion? highestVersion = null;
            bool meetsMinimum = false;

            foreach (var line in lines)
            {
                // Each line is in format: "version [path]"
                var spaceIndex = line.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var versionString = line[..spaceIndex];
                    if (SemVersion.TryParse(versionString, SemVersionStyles.Strict, out var sdkVersion))
                    {
                        // Track the highest version
                        if (highestVersion == null || SemVersion.ComparePrecedence(sdkVersion, highestVersion) > 0)
                        {
                            highestVersion = sdkVersion;
                        }

                        // Check if this version meets the minimum requirement
                        if (MeetsMinimumRequirement(sdkVersion, minVersion, minimumVersion))
                        {
                            meetsMinimum = true;
                        }
                    }
                }
            }

            return (meetsMinimum, highestVersion?.ToString(), minimumVersion, forceInstall);
        }
        catch
        {
            // If we can't start the process, the SDK is not available
            return (false, null, minimumVersion, forceInstall);
        }
    }

    /// <inheritdoc />
    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        var sdkVersion = GetEffectiveMinimumSdkVersion();
        var runtimesDirectory = GetRuntimesDirectory();
        var sdkInstallPath = Path.Combine(runtimesDirectory, "dotnet", sdkVersion);

        // Check if SDK is already installed in the private location
        if (Directory.Exists(sdkInstallPath))
        {
            // SDK already installed, nothing to do
            return;
        }

        // Create the runtimes directory if it doesn't exist
        Directory.CreateDirectory(runtimesDirectory);

        // Determine which install script to use based on the platform
        var (scriptUrl, scriptFileName, scriptRunner) = GetInstallScriptInfo();

        // Download the install script
        var scriptPath = Path.Combine(runtimesDirectory, scriptFileName);
        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            var scriptContent = await httpClient.GetStringAsync(scriptUrl, cancellationToken);
            await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);
        }

        // Make the script executable on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var chmodProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x {scriptPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chmodProcess.Start();
            await chmodProcess.WaitForExitAsync(cancellationToken);
        }

        // Run the install script
        var installProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = scriptRunner,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // PowerShell script arguments
            installProcess.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Version {sdkVersion} -InstallDir \"{sdkInstallPath}\" -NoPath";
        }
        else
        {
            // Bash script arguments
            installProcess.StartInfo.Arguments = $"\"{scriptPath}\" --version {sdkVersion} --install-dir \"{sdkInstallPath}\" --no-path";
        }

        installProcess.Start();
        
        // Capture and log stdout and stderr
        var stdoutTask = Task.Run(async () =>
        {
            while (!installProcess.StandardOutput.EndOfStream)
            {
                var line = await installProcess.StandardOutput.ReadLineAsync(cancellationToken);
                if (line != null)
                {
                    logger.LogDebug("dotnet-install stdout: {Line}", line);
                }
            }
        }, cancellationToken);

        var stderrTask = Task.Run(async () =>
        {
            while (!installProcess.StandardError.EndOfStream)
            {
                var line = await installProcess.StandardError.ReadLineAsync(cancellationToken);
                if (line != null)
                {
                    logger.LogDebug("dotnet-install stderr: {Line}", line);
                }
            }
        }, cancellationToken);

        await installProcess.WaitForExitAsync(cancellationToken);
        
        // Wait for output capture to complete
        await Task.WhenAll(stdoutTask, stderrTask);

        if (installProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to install .NET SDK {sdkVersion}. Exit code: {installProcess.ExitCode}");
        }

        // Clean up the install script
        try
        {
            File.Delete(scriptPath);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Gets the current architecture string in the format expected by dotnet --list-sdks --arch.
    /// </summary>
    /// <returns>The architecture string (e.g., "x64", "arm64", "x86", "arm").</returns>
    private static string GetCurrentArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64" // Default to x64 for unknown architectures
        };
    }

    /// <summary>
    /// Gets the directory where .NET runtimes are stored.
    /// </summary>
    /// <returns>The full path to the runtimes directory.</returns>
    private string GetRuntimesDirectory()
    {
        return executionContext.RuntimesDirectory.FullName;
    }

    /// <summary>
    /// Gets the install script information based on the current platform.
    /// </summary>
    /// <returns>A tuple containing the script URL, script file name, and script runner command.</returns>
    private static (string ScriptUrl, string ScriptFileName, string ScriptRunner) GetInstallScriptInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return (
                "https://dot.net/v1/dotnet-install.ps1",
                "dotnet-install.ps1",
                "powershell"
            );
        }
        else
        {
            return (
                "https://dot.net/v1/dotnet-install.sh",
                "dotnet-install.sh",
                "bash"
            );
        }
    }

    /// <summary>
    /// Gets the effective minimum SDK version based on configuration and feature flags.
    /// </summary>
    /// <returns>The minimum SDK version string.</returns>
    public string GetEffectiveMinimumSdkVersion()
    {
        // Check for configuration override first
        var overrideVersion = configuration["overrideMinimumSdkVersion"];
        
        if (!string.IsNullOrEmpty(overrideVersion))
        {
            return overrideVersion;
        }
        else if (features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false) ||
                 features.IsFeatureEnabled(KnownFeatures.DefaultWatchEnabled, false))
        {
            return MinimumSdkNet10SdkVersion;
        }
        else
        {
            return MinimumSdkVersion;
        }
    }

    /// <summary>
    /// Checks if an installed SDK version meets the minimum requirement.
    /// For .NET 10.x requirements, allows any .NET 10.x version including prereleases.
    /// </summary>
    /// <param name="installedVersion">The installed SDK version.</param>
    /// <param name="requiredVersion">The required minimum version (parsed).</param>
    /// <param name="requiredVersionString">The required version string.</param>
    /// <returns>True if the installed version meets the requirement.</returns>
    private static bool MeetsMinimumRequirement(SemVersion installedVersion, SemVersion requiredVersion, string requiredVersionString)
    {
        // Special handling for .NET 10.0.100 requirement - allow any .NET 10.x version
        if (requiredVersionString == MinimumSdkNet10SdkVersion)
        {
            // If we require 10.0.100, accept any version that is >= 10.0.0
            return installedVersion.Major >= 10;
        }

        // For all other requirements, use strict version comparison
        return SemVersion.ComparePrecedence(installedVersion, requiredVersion) >= 0;
    }
}