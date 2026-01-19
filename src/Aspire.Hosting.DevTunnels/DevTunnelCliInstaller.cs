// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Handles OS-specific installation of the devtunnel CLI.
/// </summary>
internal sealed class DevTunnelCliInstaller
{
    private readonly ILogger _logger;

    public DevTunnelCliInstaller(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Represents the result of an install attempt.
    /// </summary>
    public enum InstallResult
    {
        Success,
        PrerequisiteMissing,
        Failed,
        UnsupportedPlatform
    }

    /// <summary>
    /// Gets a value indicating whether installation is supported on the current platform.
    /// </summary>
    public static bool IsInstallSupported =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets the install command for the current platform.
    /// </summary>
    public static string GetInstallCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "winget install Microsoft.DevTunnels";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "brew install microsoft/devtunnels/devtunnel";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "curl -sL https://aka.ms/install-devtunnel -o install-devtunnel.sh && bash install-devtunnel.sh";
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets the package manager name for the current platform.
    /// </summary>
    public static string GetPackageManagerName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "winget";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "brew";
        }
        return "script";
    }

    /// <summary>
    /// Checks if the package manager is available on the current platform.
    /// </summary>
    public static (bool IsAvailable, string? MissingMessage) CheckPackageManagerAvailable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var wingetPath = PathLookupHelper.FindFullPathFromPath("winget");
            if (wingetPath == null)
            {
                return (false, Resources.MessageStrings.DevtunnelCliWingetNotFound);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var brewPath = PathLookupHelper.FindFullPathFromPath("brew");
            if (brewPath == null)
            {
                return (false, Resources.MessageStrings.DevtunnelCliBrewNotFound);
            }
        }
        // Linux uses curl which is almost always available
        return (true, null);
    }

    /// <summary>
    /// Attempts to install the devtunnel CLI.
    /// </summary>
    public async Task<(InstallResult Result, string? ErrorMessage)> InstallAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInstallSupported)
        {
            return (InstallResult.UnsupportedPlatform, "Automatic installation is not supported on this platform.");
        }

        var (isAvailable, missingMessage) = CheckPackageManagerAvailable();
        if (!isAvailable)
        {
            return (InstallResult.PrerequisiteMissing, missingMessage);
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await InstallWithWingetAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return await InstallWithBrewAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await InstallWithScriptAsync(cancellationToken).ConfigureAwait(false);
            }

            return (InstallResult.UnsupportedPlatform, "Unsupported platform");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install devtunnel CLI");
            return (InstallResult.Failed, ex.Message);
        }
    }

    private async Task<(InstallResult Result, string? ErrorMessage)> InstallWithWingetAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Installing devtunnel CLI using winget...");

        var psi = new ProcessStartInfo
        {
            FileName = "winget",
            ArgumentList = { "install", "Microsoft.DevTunnels", "--accept-source-agreements", "--accept-package-agreements" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        return await RunInstallProcessAsync(psi, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(InstallResult Result, string? ErrorMessage)> InstallWithBrewAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Installing devtunnel CLI using brew...");

        var psi = new ProcessStartInfo
        {
            FileName = "brew",
            ArgumentList = { "install", "microsoft/devtunnels/devtunnel" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        return await RunInstallProcessAsync(psi, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(InstallResult Result, string? ErrorMessage)> InstallWithScriptAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Installing devtunnel CLI using installation script...");

        // Download script
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            ArgumentList = { "-c", "curl -sL https://aka.ms/install-devtunnel | bash" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        return await RunInstallProcessAsync(psi, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(InstallResult Result, string? ErrorMessage)> RunInstallProcessAsync(ProcessStartInfo psi, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = psi };

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _logger.LogDebug("{Output}", e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                _logger.LogDebug("{Error}", e.Data);
            }
        };

        if (!process.Start())
        {
            return (InstallResult.Failed, "Failed to start installation process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore
            }
        });

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode == 0)
        {
            _logger.LogInformation("devtunnel CLI installed successfully");
            return (InstallResult.Success, null);
        }

        var errorMessage = error.Length > 0 ? error.ToString().Trim() : output.ToString().Trim();
        _logger.LogWarning("devtunnel CLI installation failed with exit code {ExitCode}: {Error}", process.ExitCode, errorMessage);
        return (InstallResult.Failed, $"Installation failed with exit code {process.ExitCode}: {errorMessage}");
    }
}
