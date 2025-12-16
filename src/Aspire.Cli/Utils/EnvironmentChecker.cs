// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Cli.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Default implementation of <see cref="IEnvironmentChecker"/> that checks Aspire prerequisites.
/// </summary>
internal sealed class EnvironmentChecker : IEnvironmentChecker
{
    private readonly IDotNetSdkInstaller _sdkInstaller;
    private readonly ICliHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentChecker> _logger;

    public EnvironmentChecker(
        IDotNetSdkInstaller sdkInstaller,
        ICliHostEnvironment hostEnvironment,
        IConfiguration configuration,
        ILogger<EnvironmentChecker> logger)
    {
        ArgumentNullException.ThrowIfNull(sdkInstaller);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _sdkInstaller = sdkInstaller;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EnvironmentCheckResult> CheckDotNetSdkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (success, highestVersion, minimumRequiredVersion, _) = await _sdkInstaller.CheckAsync(cancellationToken);

            if (!success)
            {
                var detectedVersion = highestVersion ?? "not found";
                return new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dotnet-sdk",
                    Status = EnvironmentCheckStatus.Fail,
                    Message = $".NET {minimumRequiredVersion} SDK not found (found: {detectedVersion})",
                    Fix = $"Download from: https://dotnet.microsoft.com/download/dotnet/{minimumRequiredVersion.Split('.')[0]}",
                    Link = $"https://dotnet.microsoft.com/download/dotnet/{minimumRequiredVersion.Split('.')[0]}"
                };
            }

            var arch = GetCurrentArchitecture();
            return new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dotnet-sdk",
                Status = EnvironmentCheckStatus.Pass,
                Message = $".NET {highestVersion ?? minimumRequiredVersion} installed ({arch})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking .NET SDK");
            return new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dotnet-sdk",
                Status = EnvironmentCheckStatus.Fail,
                Message = "Failed to check .NET SDK",
                Details = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<EnvironmentCheckResult> CheckContainerRuntimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try Docker first
            var dockerCheck = await CheckSpecificContainerRuntimeAsync("docker", cancellationToken);
            if (dockerCheck.Status != EnvironmentCheckStatus.Fail)
            {
                return dockerCheck;
            }

            // Try Podman as fallback
            var podmanCheck = await CheckSpecificContainerRuntimeAsync("podman", cancellationToken);
            if (podmanCheck.Status != EnvironmentCheckStatus.Fail)
            {
                return podmanCheck;
            }

            // Neither found
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Fail,
                Message = "No container runtime detected",
                Fix = "Install Docker Desktop: https://www.docker.com/products/docker-desktop or Podman: https://podman.io/getting-started/installation",
                Link = "https://aka.ms/dotnet/aspire/containers"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking container runtime");
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Fail,
                Message = "Failed to check container runtime",
                Details = ex.Message
            };
        }
    }

    private async Task<EnvironmentCheckResult> CheckSpecificContainerRuntimeAsync(string runtime, CancellationToken cancellationToken)
    {
        try
        {
            // Check if runtime is installed
            var versionProcessInfo = new ProcessStartInfo
            {
                FileName = runtime,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var versionProcess = Process.Start(versionProcessInfo);
            if (versionProcess is null)
            {
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Fail,
                    Message = $"{runtime} not found"
                };
            }

            await versionProcess.WaitForExitAsync(cancellationToken);

            if (versionProcess.ExitCode != 0)
            {
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Fail,
                    Message = $"{runtime} not found",
                    Fix = GetContainerRuntimeInstallationLink(runtime),
                    Link = "https://aka.ms/dotnet/aspire/containers"
                };
            }

            // Runtime is installed, check if it's running
            var psProcessInfo = new ProcessStartInfo
            {
                FileName = runtime,
                Arguments = "ps",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var psProcess = Process.Start(psProcessInfo);
            if (psProcess is null)
            {
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"{runtime} installed but daemon not reachable",
                    Fix = GetContainerRuntimeStartupAdvice(runtime),
                    Link = "https://aka.ms/dotnet/aspire/containers"
                };
            }

            await psProcess.WaitForExitAsync(cancellationToken);

            if (psProcess.ExitCode != 0)
            {
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"{runtime} installed but daemon not running",
                    Fix = GetContainerRuntimeStartupAdvice(runtime),
                    Link = "https://aka.ms/dotnet/aspire/containers"
                };
            }

            // Just return that the runtime is working - Docker Engine detection is handled separately
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Pass,
                Message = $"{runtime} detected and running"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking {Runtime}", runtime);
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Fail,
                Message = $"Failed to check {runtime}"
            };
        }
    }

    /// <inheritdoc />
    public Task<EnvironmentCheckResult> CheckWslEnvironmentAsync(CancellationToken cancellationToken = default)
    {

        // WSL detection
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Not running on Linux, so not WSL
            return Task.FromResult(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "wsl",
                Status = EnvironmentCheckStatus.Pass,
                Message = "Not running in WSL"
            });
        }

        // Check for WSL-specific environment indicators
        var isWsl = IsRunningInWsl();

        if (!isWsl)
        {
            return Task.FromResult(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "wsl",
                Status = EnvironmentCheckStatus.Pass,
                Message = "Not running in WSL"
            });
        }

        // Detect WSL version
        var wslVersion = GetWslVersion();

        if (wslVersion == 1)
        {
            return Task.FromResult(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "wsl",
                Status = EnvironmentCheckStatus.Warning,
                Message = "WSL1 detected - limited container support",
                Fix = "Upgrade to WSL2 for best experience: wsl --set-version <distro> 2",
                Link = "https://aka.ms/aspire-prerequisites#wsl-setup"
            });
        }

        // WSL2 detected - just informational, not a warning unless there are known issues
        return Task.FromResult(new EnvironmentCheckResult
        {
            Category = "environment",
            Name = "wsl",
            Status = EnvironmentCheckStatus.Pass,
            Message = "WSL2 environment detected",
            Details = "If you experience container connectivity issues, ensure Docker Desktop WSL integration is enabled."
        });
    }

    /// <inheritdoc />
    public async Task<EnvironmentCheckResult> CheckDockerEngineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if docker is available
            var dockerAvailable = await IsRuntimeAvailableAsync("docker", cancellationToken);
            if (!dockerAvailable)
            {
                // No Docker installed, skip this check
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "docker-engine",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "Docker not detected (check skipped)"
                };
            }

            var isDockerDesktop = await IsDockerDesktopAsync("docker", cancellationToken);

            if (isDockerDesktop)
            {
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "docker-engine",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "Docker Desktop detected"
                };
            }

            // Docker Engine detected - warn about tunnel requirement
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "docker-engine",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Docker Engine requires Aspire tunnel for container access",
                Fix = "Run: aspire config set tunnel.enabled true",
                Link = "https://aka.ms/aspire-prerequisites#docker-engine"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking Docker Engine");
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "docker-engine",
                Status = EnvironmentCheckStatus.Pass,
                Message = "Docker Engine check skipped",
                Details = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public Task<EnvironmentCheckResult> CheckTerminalCapabilitiesAsync(CancellationToken cancellationToken = default)
    {

        var capabilities = new List<string>();

        if (_hostEnvironment.SupportsAnsi)
        {
            capabilities.Add("colors");
        }

        if (_hostEnvironment.SupportsInteractiveInput)
        {
            capabilities.Add("interactive input");
        }

        if (_hostEnvironment.SupportsInteractiveOutput)
        {
            capabilities.Add("interactive output");
        }

        if (capabilities.Count == 0)
        {
            return Task.FromResult(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "terminal",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Terminal has limited capabilities"
            });
        }

        return Task.FromResult(new EnvironmentCheckResult
        {
            Category = "environment",
            Name = "terminal",
            Status = EnvironmentCheckStatus.Pass,
            Message = $"Terminal supports: {string.Join(", ", capabilities)}"
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EnvironmentCheckResult>();

        // Check ordering: Fast first, expensive later
        // 1. Environment variable checks (instant)
        results.Add(await CheckTerminalCapabilitiesAsync(cancellationToken));
        results.Add(await CheckWslEnvironmentAsync(cancellationToken));

        // 2. File system checks (.NET SDK presence)
        results.Add(await CheckDotNetSdkAsync(cancellationToken));

        // 3. Process checks (daemon connectivity)
        results.Add(await CheckContainerRuntimeAsync(cancellationToken));
        results.Add(await CheckDockerEngineAsync(cancellationToken));

        return results;
    }

    private static string GetCurrentArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };
    }

    private static string GetContainerRuntimeInstallationLink(string runtime)
    {
        return runtime.ToLowerInvariant() switch
        {
            "podman" => "Install Podman: https://podman.io/getting-started/installation",
            _ => "Install Docker Desktop: https://www.docker.com/products/docker-desktop"
        };
    }

    private static string GetContainerRuntimeStartupAdvice(string runtime)
    {
        return runtime.ToLowerInvariant() switch
        {
            "podman" => "Start Podman machine or ensure podman service is running",
            _ => "Start Docker Desktop or ensure docker daemon is running"
        };
    }

    private static async Task<bool> IsRuntimeAvailableAsync(string runtime, CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = runtime,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process is null)
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

    private static bool IsRunningInWsl()
    {
        // Check for WSL-specific indicators
        // WSL sets the WSL_DISTRO_NAME environment variable
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WSL_DISTRO_NAME")))
        {
            return true;
        }

        // Check /proc/version for Microsoft/WSL indicators
        try
        {
            if (File.Exists("/proc/version"))
            {
                var version = File.ReadAllText("/proc/version");
                return version.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                       version.Contains("WSL", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Ignore errors reading /proc/version
        }

        return false;
    }

    private static int GetWslVersion()
    {
        // WSL2 uses a real Linux kernel, WSL1 uses a compatibility layer
        // Check for WSL version indicator in /proc/version
        try
        {
            if (File.Exists("/proc/version"))
            {
                var version = File.ReadAllText("/proc/version");
                // WSL2 typically includes "WSL2" in the kernel version
                if (version.Contains("WSL2", StringComparison.OrdinalIgnoreCase))
                {
                    return 2;
                }
                // If it says Microsoft but not WSL2, assume WSL1
                if (version.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        // Default to WSL2 if we can't determine
        return 2;
    }

    private async Task<bool> IsDockerDesktopAsync(string runtime, CancellationToken cancellationToken)
    {
        if (!runtime.Equals("docker", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            // Docker Desktop typically has specific indicators:
            // 1. docker context shows "desktop-linux" or similar
            // 2. Docker info shows Desktop-specific text
            
            // Simple heuristic: check if docker context includes "desktop"
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "context show",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                return output.Contains("desktop", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error detecting Docker Desktop");
        }

        return false;
    }
}
