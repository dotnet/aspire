// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if a supported container runtime is available and running.
/// </summary>
internal sealed class ContainerRuntimeCheck(ILogger<ContainerRuntimeCheck> logger, IEnvironment environment) : IEnvironmentCheck
{
    internal const string CheckName = "container-runtime";
    private const string ExplicitSelectionSuffix = " ← explicitly selected";

    /// <summary>
    /// Minimum Docker version required for Aspire.
    /// </summary>
    public const string MinimumDockerVersion = "28.0.0";

    /// <summary>
    /// Minimum Podman version required for Aspire.
    /// </summary>
    public const string MinimumPodmanVersion = "5.0.0";

    // Matches DCP's minimum supported WSLc client and session manager version.
    public const string MinimumWslcVersion = "3.0.2.0";

    public int Order => 40; // Process check - more expensive

    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configuredRuntime = environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME")
                ?? environment.GetEnvironmentVariable("DOTNET_ASPIRE_CONTAINER_RUNTIME");

            if (!environment.IsWindows() &&
                string.Equals(configuredRuntime, KnownContainerRuntimes.Wslc, StringComparison.OrdinalIgnoreCase))
            {
                return [new EnvironmentCheckResult
                {
                    Category = EnvironmentCheckCategories.Container,
                    Name = KnownContainerRuntimes.Wslc,
                    Status = EnvironmentCheckStatus.Fail,
                    Message = "WSLc is only supported on Windows" + ExplicitSelectionSuffix,
                    Fix = "Set ASPIRE_CONTAINER_RUNTIME to docker or podman.",
                    Link = "https://aka.ms/aspire/containers"
                }];
            }

            if (configuredRuntime is not null)
            {
                var runtime = await ContainerRuntimeDetector.FindAvailableRuntimeAsync(configuredRuntime, logger, cancellationToken)
                    ?? throw new InvalidOperationException($"No detection result was returned for configured container runtime '{configuredRuntime}'.");

                return [BuildRuntimeResult(runtime, isSelected: true, hasHealthyRuntime: false, configuredRuntime, cancellationToken)];
            }

            var dockerTask = ContainerRuntimeDetector.CheckRuntimeAsync(KnownContainerRuntimes.Docker, "Docker", logger, cancellationToken);
            var podmanTask = ContainerRuntimeDetector.CheckRuntimeAsync(KnownContainerRuntimes.Podman, "Podman", logger, cancellationToken);
            var runtimes = environment.IsWindows()
                ? await Task.WhenAll(dockerTask, podmanTask, ContainerRuntimeDetector.CheckRuntimeAsync(KnownContainerRuntimes.Wslc, "WSLc", logger, cancellationToken))
                : await Task.WhenAll(dockerTask, podmanTask);

            // Select best from already-probed results (no re-probing)
            var healthyRuntime = ContainerRuntimeDetector.FindBestRuntime(
                runtimes.Where(info => info.IsHealthy && CheckRuntimePolicy(info)?.Status != EnvironmentCheckStatus.Fail));
            var selected = ContainerRuntimeDetector.FindBestRuntime(runtimes);
            var results = new List<EnvironmentCheckResult>();
            foreach (var info in runtimes.OrderBy(info => info.Priority))
            {
                if (!info.IsInstalled)
                {
                    continue;
                }

                var isSelected = selected is not null &&
                    string.Equals(info.Executable, selected.Executable, StringComparison.OrdinalIgnoreCase);

                results.Add(BuildRuntimeResult(info, isSelected, healthyRuntime is not null, configuredRuntime, cancellationToken));
            }

            if (results.Count > 0)
            {
                return results;
            }

            return [new EnvironmentCheckResult
            {
                Category = EnvironmentCheckCategories.Container,
                Name = CheckName,
                Status = EnvironmentCheckStatus.Fail,
                Message = "No container runtime detected",
                Fix = "Install Docker Desktop: https://www.docker.com/products/docker-desktop or Podman: https://podman.io/getting-started/installation" +
                    (environment.IsWindows() ? " or WSLc: https://aka.ms/aspire/containers" : ""),
                Link = "https://aka.ms/aspire/containers"
            }];
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking container runtime");
            return [new EnvironmentCheckResult
            {
                Category = EnvironmentCheckCategories.Container,
                Name = CheckName,
                Status = EnvironmentCheckStatus.Fail,
                Message = "Failed to check container runtime",
                Details = ex.Message
            }];
        }
    }

    /// <summary>
    /// Applies Aspire-specific policy checks (minimum version, Windows containers)
    /// using version info already gathered by the detector. No process spawning.
    /// </summary>
    private static EnvironmentCheckResult? CheckRuntimePolicy(ContainerRuntimeInfo info)
    {
        var minimumVersion = GetMinimumVersion(info.Name);

        // Check minimum client version
        if (info.ClientVersion is not null && minimumVersion is not null && info.ClientVersion < minimumVersion)
        {
            return WarningResult(
                $"{info.Name} client version {info.ClientVersion} is below minimum required {GetMinimumVersionString(info.Name)}",
                GetContainerRuntimeUpgradeAdvice(info.Name));
        }

        // Podman does not always expose a separate server version.
        if (info.Name is "Docker" or "WSLc" && info.ServerVersion is not null && minimumVersion is not null &&
            info.ServerVersion < minimumVersion)
        {
            return WarningResult(
                $"{info.Name} server version {info.ServerVersion} is below minimum required {GetMinimumVersionString(info.Name)}",
                GetContainerRuntimeUpgradeAdvice(info.Name));
        }

        // Docker-specific: check Windows container mode
        if (info.Name == "Docker" && string.Equals(info.ServerOs, "windows", StringComparison.OrdinalIgnoreCase))
        {
            var runtimeName = info.IsDockerDesktop ? "Docker Desktop" : "Docker";
            return new EnvironmentCheckResult
            {
                Category = EnvironmentCheckCategories.Container,
                Name = CheckName,
                Status = EnvironmentCheckStatus.Fail,
                Message = $"{runtimeName} is running in Windows container mode",
                Details = "Aspire requires Linux containers. Windows containers are not supported.",
                Fix = "Switch Docker Desktop to Linux containers mode (right-click Docker tray icon → 'Switch to Linux containers...')",
                Link = "https://aka.ms/aspire/containers"
            };
        }

        return null; // No issues
    }

    private static EnvironmentCheckResult WarningResult(string message, string fix) => new()
    {
        Category = EnvironmentCheckCategories.Container,
        Name = CheckName,
        Status = EnvironmentCheckStatus.Warning,
        Message = message,
        Fix = fix,
        Link = "https://aka.ms/aspire/containers"
    };

    private static EnvironmentCheckResult BuildRuntimeResult(
        ContainerRuntimeInfo info,
        bool isSelected,
        bool hasHealthyRuntime,
        string? configuredRuntime,
        CancellationToken _)
    {
        var selectedSuffix = isSelected
            ? configuredRuntime is not null ? ExplicitSelectionSuffix : " ← active"
            : "";

        if (!info.IsInstalled)
        {
            // Only reached for explicitly configured runtimes
            return new EnvironmentCheckResult
            {
                Category = EnvironmentCheckCategories.Container,
                Name = info.Executable,
                Status = EnvironmentCheckStatus.Fail,
                Message = $"{info.Name}: not found (configured via ASPIRE_CONTAINER_RUNTIME={configuredRuntime}){selectedSuffix}",
                Fix = GetContainerRuntimeInstallationLink(info.Name)
            };
        }

        if (!info.IsRunning)
        {
            return new EnvironmentCheckResult
            {
                Category = EnvironmentCheckCategories.Container,
                Name = info.Executable,
                Status = EnvironmentCheckStatus.Warning,
                Message = $"{info.Name}: installed but not running{selectedSuffix}",
                Fix = GetContainerRuntimeStartupAdvice(info.Name, info.IsDockerDesktop)
            };
        }

        // Runtime is healthy — apply Aspire-specific policy checks (no process spawning)
        var policyResult = CheckRuntimePolicy(info);
        if (policyResult is not null)
        {
            // Append selection info to the policy result message
            return new EnvironmentCheckResult
            {
                Category = policyResult.Category,
                Name = policyResult.Name,
                Status = hasHealthyRuntime && policyResult.Status == EnvironmentCheckStatus.Fail
                    ? EnvironmentCheckStatus.Warning
                    : policyResult.Status,
                Message = policyResult.Message + selectedSuffix,
                Fix = policyResult.Fix,
                Details = policyResult.Details,
                Link = policyResult.Link
            };
        }

        var versionSuffix = info.ClientVersion is not null ? $" v{info.ClientVersion}" : "";

        return new EnvironmentCheckResult
        {
            Category = EnvironmentCheckCategories.Container,
            Name = info.Executable,
            Status = EnvironmentCheckStatus.Pass,
            Message = $"{info.Name}{versionSuffix}: running{selectedSuffix}"
        };
    }

    private static string GetContainerRuntimeInstallationLink(string runtime)
    {
        return runtime switch
        {
            "Docker" => "Install Docker Desktop: https://www.docker.com/products/docker-desktop",
            "Podman" => "Install Podman: https://podman.io/getting-started/installation",
            "WSLc" => "Install WSLc on Windows: https://aka.ms/aspire/containers",
            _ => $"Install {runtime}"
        };
    }

    /// <summary>
    /// Gets the minimum required version for the specified container runtime.
    /// </summary>
    private static Version? GetMinimumVersion(string runtime)
    {
        var versionString = GetMinimumVersionString(runtime);

        if (versionString is not null && Version.TryParse(versionString, out var version))
        {
            if (runtime == "WSLc" && version.Revision == 0)
            {
                // WSLc reports clients as 3.0.2.0 and session managers as 3.0.2.
                // Omit a zero revision from the minimum so either form compares correctly.
                version = new Version(version.Major, version.Minor, version.Build);
            }

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
            "WSLc" => MinimumWslcVersion,
            _ => null
        };
    }

    private static string GetContainerRuntimeUpgradeAdvice(string runtime)
    {
        return runtime switch
        {
            "Docker" => $"Upgrade Docker to version {MinimumDockerVersion} or later from: https://www.docker.com/products/docker-desktop",
            "Podman" => $"Upgrade Podman to version {MinimumPodmanVersion} or later from: https://podman.io/getting-started/installation",
            "WSLc" => $"Upgrade WSLc to version {MinimumWslcVersion} or later. See: https://aka.ms/aspire/containers",
            _ => $"Upgrade {runtime} to a newer version"
        };
    }

    private static string GetContainerRuntimeStartupAdvice(string runtime, bool isDockerDesktop = false)
    {
        return runtime switch
        {
            "Docker" when isDockerDesktop => "Start Docker Desktop",
            "Docker" => "Start Docker daemon",
            "Podman" => "Start Podman service: sudo systemctl start podman",
            "WSLc" => "Ensure the WSLc session manager is running. Check its status with: wslc info --format json",
            _ => $"Start {runtime} daemon"
        };
    }
}
