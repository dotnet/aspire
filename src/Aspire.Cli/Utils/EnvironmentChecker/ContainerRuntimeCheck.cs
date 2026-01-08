// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if a container runtime (Docker or Podman) is available and running.
/// </summary>
internal sealed partial class ContainerRuntimeCheck(ILogger<ContainerRuntimeCheck> logger) : IEnvironmentCheck
{
    private static readonly TimeSpan s_processTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum Docker version required for Aspire.
    /// </summary>
    public const string MinimumDockerVersion = "28.0.0";

    /// <summary>
    /// Minimum Podman version required for Aspire.
    /// </summary>
    public const string MinimumPodmanVersion = "5.0.0";

    public int Order => 40; // Process check - more expensive

    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try Docker first, then Podman
            var dockerCheck = await CheckSpecificContainerRuntimeAsync("Docker", cancellationToken);
            if (dockerCheck.Status == EnvironmentCheckStatus.Pass)
            {
                return [dockerCheck];
            }

            var podmanCheck = await CheckSpecificContainerRuntimeAsync("Podman", cancellationToken);
            if (podmanCheck.Status == EnvironmentCheckStatus.Pass)
            {
                return [podmanCheck];
            }

            // If Docker is installed but not running, prefer showing that error
            if (dockerCheck.Status == EnvironmentCheckStatus.Warning)
            {
                return [dockerCheck];
            }

            // If Podman is installed but not running, show that
            if (podmanCheck.Status == EnvironmentCheckStatus.Warning)
            {
                return [podmanCheck];
            }

            // Neither found
            return [new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Fail,
                Message = "No container runtime detected",
                Fix = "Install Docker Desktop: https://www.docker.com/products/docker-desktop or Podman: https://podman.io/getting-started/installation",
                Link = "https://aka.ms/dotnet/aspire/containers"
            }];
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking container runtime");
            return [new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Fail,
                Message = "Failed to check container runtime",
                Details = ex.Message
            }];
        }
    }

    private async Task<EnvironmentCheckResult> CheckSpecificContainerRuntimeAsync(string runtime, CancellationToken cancellationToken)
    {
        try
        {
            // Check if runtime is installed and get version using JSON format (use lowercase for process name)
            var runtimeLower = runtime.ToLowerInvariant();
            var versionProcessInfo = new ProcessStartInfo
            {
                FileName = runtimeLower,
                Arguments = "version -f json",
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

            using var versionTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            versionTimeoutCts.CancelAfter(s_processTimeout);

            string versionOutput;
            try
            {
                versionOutput = await versionProcess.StandardOutput.ReadToEndAsync(versionTimeoutCts.Token);
                await versionProcess.WaitForExitAsync(versionTimeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                versionProcess.Kill();
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"{runtime} check timed out",
                    Fix = GetContainerRuntimeStartupAdvice(runtime),
                    Link = "https://aka.ms/dotnet/aspire/containers"
                };
            }

            // Parse the version from JSON output first, even if the command failed
            // (docker version -f json outputs client info even when daemon is not running)
            var versionInfo = ParseVersionFromJsonOutput(versionOutput);
            var clientVersion = versionInfo.ClientVersion;
            var serverVersion = versionInfo.ServerVersion;
            var context = versionInfo.Context;

            // Determine if this is Docker Desktop based on context
            var isDockerDesktop = runtime == "Docker" &&
                context is not null &&
                context.Contains("desktop", StringComparison.OrdinalIgnoreCase);

            // Note: docker/podman version -f json returns exit code != 0 when daemon is not running,
            // but still outputs client version info including the context
            if (versionProcess.ExitCode != 0)
            {
                // If we got client info from JSON, CLI is installed but daemon isn't running
                if (clientVersion is not null || isDockerDesktop)
                {
                    var runtimeDescription = isDockerDesktop ? "Docker Desktop" : runtime;
                    return new EnvironmentCheckResult
                    {
                        Category = "container",
                        Name = "container-runtime",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"{runtimeDescription} is installed but not running",
                        Fix = GetContainerRuntimeStartupAdvice(runtime, isDockerDesktop),
                        Link = "https://aka.ms/dotnet/aspire/containers"
                    };
                }

                // Couldn't get client info, check if CLI is installed separately
                var isCliInstalled = await IsCliInstalledAsync(runtimeLower, cancellationToken);
                if (isCliInstalled)
                {
                    // CLI is installed but daemon isn't running
                    return new EnvironmentCheckResult
                    {
                        Category = "container",
                        Name = "container-runtime",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"{runtime} is installed but the daemon is not running",
                        Fix = GetContainerRuntimeStartupAdvice(runtime),
                        Link = "https://aka.ms/dotnet/aspire/containers"
                    };
                }

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

            // Fall back to text parsing if JSON parsing failed
            if (clientVersion is null)
            {
                clientVersion = ParseVersionFromOutput(versionOutput);
            }
            
            var minimumVersion = GetMinimumVersion(runtime);

            // Check if client version meets minimum requirement
            if (clientVersion is not null && minimumVersion is not null)
            {
                if (clientVersion < minimumVersion)
                {
                    var minVersionString = GetMinimumVersionString(runtime);
                    return new EnvironmentCheckResult
                    {
                        Category = "container",
                        Name = "container-runtime",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"{runtime} client version {clientVersion} is below the minimum required version {minVersionString}",
                        Fix = GetContainerRuntimeUpgradeAdvice(runtime),
                        Link = "https://aka.ms/dotnet/aspire/containers"
                    };
                }
            }

            // For Docker, also check server version if available
            if (runtime == "Docker" && serverVersion is not null && minimumVersion is not null)
            {
                if (serverVersion < minimumVersion)
                {
                    var minVersionString = GetMinimumVersionString(runtime);
                    return new EnvironmentCheckResult
                    {
                        Category = "container",
                        Name = "container-runtime",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"{runtime} server version {serverVersion} is below the minimum required version {minVersionString}",
                        Fix = GetContainerRuntimeUpgradeAdvice(runtime),
                        Link = "https://aka.ms/dotnet/aspire/containers"
                    };
                }
            }

            // Runtime is installed, check if it's running
            var psProcessInfo = new ProcessStartInfo
            {
                FileName = runtimeLower,
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

            using var psTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            psTimeoutCts.CancelAfter(s_processTimeout);

            try
            {
                await psProcess.WaitForExitAsync(psTimeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                psProcess.Kill();
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"{runtime} daemon not responding",
                    Fix = GetContainerRuntimeStartupAdvice(runtime),
                    Link = "https://aka.ms/dotnet/aspire/containers"
                };
            }

            if (psProcess.ExitCode != 0)
            {
                var runtimeDescription = isDockerDesktop ? "Docker Desktop" : runtime;
                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"{runtimeDescription} is installed but not running",
                    Fix = GetContainerRuntimeStartupAdvice(runtime, isDockerDesktop),
                    Link = "https://aka.ms/dotnet/aspire/containers"
                };
            }

            // Return pass with version info if available
            var versionSuffix = clientVersion is not null ? $" (version {clientVersion})" : string.Empty;
            var runtimeName = isDockerDesktop ? "Docker Desktop" : runtime;

            // For Docker Engine (not Desktop), check tunnel configuration
            if (runtime == "Docker" && !isDockerDesktop)
            {
                var tunnelEnabled = Environment.GetEnvironmentVariable("ASPIRE_ENABLE_CONTAINER_TUNNEL");
                if (!string.Equals(tunnelEnabled, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return new EnvironmentCheckResult
                    {
                        Category = "container",
                        Name = "container-runtime",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"Docker Engine detected{versionSuffix}. Aspire's container tunnel is required to allow containers to reach applications running on the host",
                        Fix = "Set environment variable: ASPIRE_ENABLE_CONTAINER_TUNNEL=true",
                        Link = "https://aka.ms/aspire-prerequisites#docker-engine"
                    };
                }

                return new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "container-runtime",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = $"Docker Engine detected and running{versionSuffix} with container tunnel enabled"
                };
            }

            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Pass,
                Message = $"{runtimeName} detected and running{versionSuffix}"
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking {Runtime}", runtime);
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Fail,
                Message = $"Failed to check {runtime}"
            };
        }
    }

    /// <summary>
    /// Parses client and server versions from container runtime 'version -f json' output.
    /// </summary>
    internal static (Version? ClientVersion, Version? ServerVersion, string? Context) ParseVersionFromJsonOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return (null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            Version? clientVersion = null;
            Version? serverVersion = null;
            string? context = null;

            // Try to get Client.Version and Client.Context
            if (root.TryGetProperty("Client", out var client))
            {
                if (client.TryGetProperty("Version", out var clientVersionElement))
                {
                    var versionString = clientVersionElement.GetString();
                    if (!string.IsNullOrEmpty(versionString))
                    {
                        Version.TryParse(versionString, out clientVersion);
                    }
                }

                if (client.TryGetProperty("Context", out var contextElement))
                {
                    context = contextElement.GetString();
                }
            }

            // Try to get Server.Version (Docker specific, may be null if daemon not running)
            if (root.TryGetProperty("Server", out var server) &&
                server.ValueKind != JsonValueKind.Null &&
                server.TryGetProperty("Version", out var serverVersionElement))
            {
                var versionString = serverVersionElement.GetString();
                if (!string.IsNullOrEmpty(versionString))
                {
                    Version.TryParse(versionString, out serverVersion);
                }
            }

            return (clientVersion, serverVersion, context);
        }
        catch (JsonException)
        {
            // JSON parsing failed, return null to allow fallback to text parsing
            return (null, null, null);
        }
    }

    /// <summary>
    /// Parses a version number from container runtime output as a fallback when JSON parsing fails.
    /// </summary>
    internal static Version? ParseVersionFromOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        // Match version patterns like "20.10.17", "4.3.1", "27.5.1" etc.
        // The pattern looks for "version" followed by a version number
        var match = VersionRegex().Match(output);
        if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
        {
            return version;
        }

        return null;
    }

    /// <summary>
    /// Gets the minimum required version for the specified container runtime.
    /// </summary>
    private static Version? GetMinimumVersion(string runtime)
    {
        var versionString = GetMinimumVersionString(runtime);

        if (versionString is not null && Version.TryParse(versionString, out var version))
        {
            return version;
        }

        return null;
    }

    /// <summary>
    /// Gets the minimum required version string for the specified container runtime.
    /// </summary>
    private static string? GetMinimumVersionString(string runtime)
    {
        return runtime switch
        {
            "Docker" => MinimumDockerVersion,
            "Podman" => MinimumPodmanVersion,
            _ => null
        };
    }

    private static string GetContainerRuntimeUpgradeAdvice(string runtime)
    {
        return runtime switch
        {
            "Docker" => $"Upgrade Docker to version {MinimumDockerVersion} or later from: https://www.docker.com/products/docker-desktop",
            "Podman" => $"Upgrade Podman to version {MinimumPodmanVersion} or later from: https://podman.io/getting-started/installation",
            _ => $"Upgrade {runtime} to a newer version"
        };
    }

    [GeneratedRegex(@"version\s+(\d+\.\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionRegex();

    private static string GetContainerRuntimeInstallationLink(string runtime)
    {
        return runtime switch
        {
            "Docker" => "Install Docker Desktop from: https://www.docker.com/products/docker-desktop",
            "Podman" => "Install Podman from: https://podman.io/getting-started/installation",
            _ => $"Install {runtime}"
        };
    }

    private static string GetContainerRuntimeStartupAdvice(string runtime, bool isDockerDesktop = false)
    {
        return runtime switch
        {
            "Docker" when isDockerDesktop => "Start Docker Desktop",
            "Docker" => "Start Docker daemon",
            "Podman" => "Start Podman service: sudo systemctl start podman",
            _ => $"Start {runtime} daemon"
        };
    }

    /// <summary>
    /// Checks if the container runtime CLI is installed by running --version (which doesn't require daemon).
    /// </summary>
    private async Task<bool> IsCliInstalledAsync(string runtimeLower, CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = runtimeLower,
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

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_processTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill();
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking if {Runtime} CLI is installed", runtimeLower);
            return false;
        }
    }
}
