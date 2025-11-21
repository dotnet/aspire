// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Aspire.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.DotNet;

/// <summary>
/// Default implementation of <see cref="IDotNetSdkInstaller"/> that checks for dotnet on the system PATH.
/// </summary>
internal sealed class DotNetSdkInstaller(IFeatures features, IConfiguration configuration, CliExecutionContext executionContext, IDotNetCliRunner dotNetCliRunner, ILogger<DotNetSdkInstaller> logger) : IDotNetSdkInstaller
{
    /// <summary>
    /// The minimum .NET SDK version required for Aspire.
    /// </summary>
    public const string MinimumSdkVersion = "10.0.100";

    /// <inheritdoc />
    public async Task<(bool Success, string? HighestVersion, string MinimumRequiredVersion, bool ForceInstall)> CheckAsync(CancellationToken cancellationToken = default)
    {
        var minimumVersion = GetEffectiveMinimumSdkVersion();
        
        // Check if alwaysInstallSdk is enabled - this forces installation even when SDK check passes
        var alwaysInstallSdk = configuration["alwaysInstallSdk"];
        var forceInstall = !string.IsNullOrEmpty(alwaysInstallSdk) && 
                          bool.TryParse(alwaysInstallSdk, out var alwaysInstall) && 
                          alwaysInstall;
        
        // First check if we already have the SDK installed in our private sdks directory
        if (!forceInstall)
        {
            var sdksDirectory = GetSdksDirectory();
            var sdkInstallPath = Path.Combine(sdksDirectory, "dotnet", minimumVersion);
            var dotnetExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? Path.Combine(sdkInstallPath, "dotnet.exe")
                : Path.Combine(sdkInstallPath, "dotnet");

            if (File.Exists(dotnetExecutable))
            {
                logger.LogDebug("Found private SDK installation at {Path}", sdkInstallPath);
                return (true, minimumVersion, minimumVersion, false);
            }
        }
        
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
        catch (Exception ex) when (ex is not OperationCanceledException) // If cancellation is requested let that bubble up.
        {
            // If we can't start the process, the SDK is not available
            return (false, null, minimumVersion, forceInstall);
        }
    }

    /// <inheritdoc />
    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        var sdkVersion = GetEffectiveMinimumSdkVersion();
        var sdksDirectory = GetSdksDirectory();
        var sdkInstallPath = Path.Combine(sdksDirectory, "dotnet", sdkVersion);

        // Check if SDK is already installed in the private location
        if (Directory.Exists(sdkInstallPath))
        {
            // SDK already installed, nothing to do
            return;
        }

        // Create the sdks directory if it doesn't exist
        Directory.CreateDirectory(sdksDirectory);

        // Determine which install script to use based on the platform
        var (resourceName, scriptFileName, scriptRunner) = GetInstallScriptInfo();

        // Extract the install script from embedded resources
        var scriptPath = Path.Combine(sdksDirectory, scriptFileName);
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var fileStream = File.Create(scriptPath);
        await resourceStream.CopyToAsync(fileStream, cancellationToken);

        // Make the script executable on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Set execute permission on Unix systems
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var mode = File.GetUnixFileMode(scriptPath);
                    mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                    File.SetUnixFileMode(scriptPath, mode);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to set executable permission on {ScriptPath}", scriptPath);
                }
            }
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
            string? line;
            while ((line = await installProcess.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
            {
                logger.LogDebug("dotnet-install stdout: {Line}", line);
            }
        }, cancellationToken);

        var stderrTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await installProcess.StandardError.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
            {
                logger.LogDebug("dotnet-install stderr: {Line}", line);
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
        
        // After installation, call dotnet nuget config paths to initialize NuGet
        // This is important on Windows where NuGet needs to create initial config on first use
        logger.LogDebug("Initializing NuGet configuration for private SDK installation");
        try
        {
            var options = new DotNetCliRunnerInvocationOptions();
            var (exitCode, _) = await dotNetCliRunner.GetNuGetConfigPathsAsync(
                new DirectoryInfo(Environment.CurrentDirectory), 
                options, 
                cancellationToken);
            
            if (exitCode == 0)
            {
                logger.LogDebug("NuGet configuration initialized successfully");
            }
            else
            {
                logger.LogDebug("NuGet configuration initialization returned exit code {ExitCode}", exitCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to initialize NuGet configuration, continuing anyway");
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
    /// Gets the directory where .NET SDKs are stored.
    /// </summary>
    /// <returns>The full path to the sdks directory.</returns>
    private string GetSdksDirectory()
    {
        return executionContext.SdksDirectory.FullName;
    }

    /// <summary>
    /// Gets the install script information based on the current platform.
    /// </summary>
    /// <returns>A tuple containing the embedded resource name, script file name, and script runner command.</returns>
    private static (string ResourceName, string ScriptFileName, string ScriptRunner) GetInstallScriptInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Try pwsh first (PowerShell Core), then fall back to powershell (Windows PowerShell)
            var powerShellExecutable = GetAvailablePowerShell();
            return (
                "Aspire.Cli.Resources.dotnet-install.ps1",
                "dotnet-install.ps1",
                powerShellExecutable
            );
        }
        else
        {
            return (
                "Aspire.Cli.Resources.dotnet-install.sh",
                "dotnet-install.sh",
                "bash"
            );
        }
    }

    /// <summary>
    /// Determines which PowerShell executable is available on the system.
    /// Tries pwsh (PowerShell Core) first, then falls back to powershell (Windows PowerShell).
    /// </summary>
    /// <returns>The name of the available PowerShell executable.</returns>
    private static string GetAvailablePowerShell()
    {
        // Try pwsh first (PowerShell Core - cross-platform)
        if (IsPowerShellAvailable("pwsh"))
        {
            return "pwsh";
        }

        // Fall back to powershell (Windows PowerShell)
        if (IsPowerShellAvailable("powershell"))
        {
            return "powershell";
        }

        // Default to powershell if neither can be verified
        // The installation will fail later with a clear error if it's not available
        return "powershell";
    }

    /// <summary>
    /// Checks if a PowerShell executable is available by running it with --version.
    /// </summary>
    /// <param name="executable">The PowerShell executable name to check (pwsh or powershell).</param>
    /// <returns>True if the executable is available and responds to --version.</returns>
    private static bool IsPowerShellAvailable(string executable)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(1000); // Wait up to 1 second

            return process.ExitCode == 0;
        }
        catch
        {
            // If the executable doesn't exist or can't be started, return false
            return false;
        }
    }

    /// <summary>
    /// Gets the effective minimum SDK version based on configuration.
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
        // Special handling for .NET 10 RTM requirement - allow any .NET 10.x version
        if (requiredVersionString == MinimumSdkVersion)
        {
            // If we require 10.0.100, accept any version that is >= 10.0.0
            return installedVersion.Major >= 10;
        }

        // For all other requirements, use strict version comparison
        return SemVersion.ComparePrecedence(installedVersion, requiredVersion) >= 0;
    }
}
