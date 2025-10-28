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
    public const string MinimumSdkVersion = "10.0.100-rc.2.25502.107";

    /// <summary>
    /// The base URL for downloading .NET SDK archives.
    /// </summary>
    private const string DotNetBuildsBaseUrl = "https://dotnetbuilds.blob.core.windows.net/public/Sdk";

    /// <summary>
    /// The timeout for HTTP download operations.
    /// </summary>
    private static readonly TimeSpan s_downloadTimeout = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The buffer size for downloading SDK archives.
    /// </summary>
    private const int DownloadBufferSize = 81920; // 80 KB

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
        catch
        {
            // If we can't start the process, the SDK is not available
            return (false, null, minimumVersion, forceInstall);
        }
    }

    /// <summary>
    /// Downloads the .NET SDK archive with progress indication.
    /// </summary>
    /// <param name="sdkVersion">The SDK version to download.</param>
    /// <param name="destinationPath">The path where the archive should be saved.</param>
    /// <param name="progressCallback">Optional callback to report download progress. Parameters: bytesDownloaded, totalBytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous download operation.</returns>
    private async Task DownloadSdkArchiveAsync(string sdkVersion, string destinationPath, Action<long, long>? progressCallback, CancellationToken cancellationToken)
    {
        var downloadUrl = GetSdkDownloadUrl(sdkVersion);
        logger.LogDebug("Downloading SDK from {Url}", downloadUrl);

        using var httpClient = new HttpClient { Timeout = s_downloadTimeout };
        using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = File.Create(destinationPath);
        
        var buffer = new byte[DownloadBufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            progressCallback?.Invoke(totalBytesRead, totalBytes);
        }
        
        logger.LogDebug("SDK archive downloaded to {Path}", destinationPath);
    }

    /// <summary>
    /// Gets the download URL for the specified SDK version.
    /// </summary>
    /// <param name="sdkVersion">The SDK version.</param>
    /// <returns>The download URL.</returns>
    private static string GetSdkDownloadUrl(string sdkVersion)
    {
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64"
        };

        string rid;
        string extension;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            rid = $"win-{architecture}";
            extension = "zip";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            rid = $"linux-{architecture}";
            extension = "tar.gz";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            rid = $"osx-{architecture}";
            extension = "tar.gz";
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported");
        }

        // Use the dotnetbuilds blob storage URL pattern
        // Format: https://dotnetbuilds.blob.core.windows.net/public/Sdk/{version}/dotnet-sdk-{version}-{rid}.{extension}
        return $"{DotNetBuildsBaseUrl}/{sdkVersion}/dotnet-sdk-{sdkVersion}-{rid}.{extension}";
    }

    /// <inheritdoc />
    public async Task InstallAsync(Action<long, long>? progressCallback = null, CancellationToken cancellationToken = default)
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

        // Download the SDK archive with progress indication
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz";
        var archivePath = Path.Combine(sdksDirectory, $"dotnet-sdk-{sdkVersion}.{extension}");
        
        try
        {
            // Download if the archive doesn't already exist
            if (!File.Exists(archivePath))
            {
                await DownloadSdkArchiveAsync(sdkVersion, archivePath, progressCallback, cancellationToken);
            }
            else
            {
                logger.LogDebug("SDK archive already exists at {Path}, skipping download", archivePath);
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
                // PowerShell script arguments with ZipPath
                installProcess.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Version {sdkVersion} -InstallDir \"{sdkInstallPath}\" -NoPath -ZipPath \"{archivePath}\"";
            }
            else
            {
                // Bash script arguments with zip-path
                installProcess.StartInfo.Arguments = $"\"{scriptPath}\" --version {sdkVersion} --install-dir \"{sdkInstallPath}\" --no-path --zip-path \"{archivePath}\"";
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
        finally
        {
            // Clean up the install script and downloaded archive
            // This runs even if an exception occurs during installation
            try
            {
                File.Delete(scriptPath);
                File.Delete(archivePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
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
        // Special handling for .NET 10 RC requirement - allow any .NET 10.x version
        if (requiredVersionString == MinimumSdkVersion)
        {
            // If we require 10.0.100-rc.2.25502.107, accept any version that is >= 10.0.0
            return installedVersion.Major >= 10;
        }

        // For all other requirements, use strict version comparison
        return SemVersion.ComparePrecedence(installedVersion, requiredVersion) >= 0;
    }
}