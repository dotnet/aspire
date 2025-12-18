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
    /// The minimum Docker version required for Aspire.
    /// </summary>
    public const string MinimumDockerVersion = "20.10.0";

    /// <summary>
    /// The minimum Podman version required for Aspire.
    /// </summary>
    public const string MinimumPodmanVersion = "4.0.0";

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

            // Note: docker/podman version -f json may return exit code 0 even if daemon is not running
            // (it still outputs client version). We check daemon status separately with 'ps' command.
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

            // Parse the version from JSON output, falling back to text parsing if needed
            var detectedVersion = ParseVersionFromJsonOutput(versionOutput) ?? ParseVersionFromOutput(versionOutput);
            var minimumVersion = GetMinimumVersion(runtime);

            // Check if version meets minimum requirement
            if (detectedVersion is not null && minimumVersion is not null)
            {
                if (detectedVersion < minimumVersion)
                {
                    var minVersionString = GetMinimumVersionString(runtime);
                    return new EnvironmentCheckResult
                    {
                        Category = "container",
                        Name = "container-runtime",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"{runtime} version {detectedVersion} is below the minimum required version {minVersionString}",
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

            // Return pass with version info if available
            var versionSuffix = detectedVersion is not null ? $" (version {detectedVersion})" : string.Empty;
            return new EnvironmentCheckResult
            {
                Category = "container",
                Name = "container-runtime",
                Status = EnvironmentCheckStatus.Pass,
                Message = $"{runtime} detected and running{versionSuffix}"
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
    /// Parses a version number from container runtime 'version -f json' output.
    /// Handles JSON formats from both Docker and Podman.
    /// </summary>
    /// <remarks>
    /// Docker JSON format: {"Client":{"Version":"28.0.4",...},"Server":{...}}
    /// Podman JSON format: {"Client":{"Version":"4.9.3",...}}
    /// </remarks>
    internal static Version? ParseVersionFromJsonOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            // Try to get Client.Version (works for both Docker and Podman)
            if (root.TryGetProperty("Client", out var client) &&
                client.TryGetProperty("Version", out var versionElement))
            {
                var versionString = versionElement.GetString();
                if (!string.IsNullOrEmpty(versionString) && Version.TryParse(versionString, out var version))
                {
                    return version;
                }
            }

            return null;
        }
        catch (JsonException)
        {
            // JSON parsing failed, return null to allow fallback to text parsing
            return null;
        }
    }

    /// <summary>
    /// Parses a version number from container runtime --version output.
    /// Handles formats like "Docker version 20.10.17, build 100c701" and "podman version 4.3.1".
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

    private static string GetContainerRuntimeStartupAdvice(string runtime)
    {
        return runtime switch
        {
            "Docker" => "Start Docker Desktop or Docker daemon",
            "Podman" => "Start Podman service: sudo systemctl start podman",
            _ => $"Start {runtime} daemon"
        };
    }
}
