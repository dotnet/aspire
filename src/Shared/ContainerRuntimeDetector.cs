// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Shared;

/// <summary>
/// Discovers container runtimes using Aspire probes and DCP's runtime priority order.
/// </summary>
internal static class ContainerRuntimeDetector
{
    // Availability takes precedence, followed by the same runtime order used by DCP.
    // https://github.com/microsoft/dcp/blob/main/internal/containers/runtimes/runtime.go
    private static readonly ContainerRuntimeProbe[] s_knownRuntimes =
    [
        new(KnownContainerRuntimes.Docker, "Docker", 0),
        new(KnownContainerRuntimes.Podman, "Podman", 1),
        new WslcContainerRuntimeProbe()
    ];

    /// <summary>
    /// Gets the probes supported on the host operating system, in discovery order.
    /// </summary>
    internal static IEnumerable<ContainerRuntimeProbe> GetKnownRuntimes(bool isWindows)
    {
        return s_knownRuntimes.Where(runtime => runtime.GetUnsupportedHostError(isWindows) is null);
    }

    /// <summary>
    /// Finds the best available runtime, or checks only an explicitly configured runtime.
    /// </summary>
    public static async Task<ContainerRuntimeInfo?> FindAvailableRuntimeAsync(
        string? configuredRuntime = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<string>? supportedRuntimes = null)
    {
        if (configuredRuntime is not null)
        {
            logger?.LogDebug("Checking explicitly configured runtime: {Runtime}", configuredRuntime);
            var runtime = FindProbe(configuredRuntime) ?? new ContainerRuntimeProbe(configuredRuntime, configuredRuntime, int.MaxValue);
            return await runtime.CheckAsync(logger, cancellationToken).ConfigureAwait(false);
        }

        var runtimes = GetKnownRuntimes(OperatingSystem.IsWindows())
            .Where(runtime => supportedRuntimes is null || supportedRuntimes.Contains(runtime.Executable, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        logger?.LogDebug("Auto-detecting container runtime, probing {Count} known runtimes...", runtimes.Length);
        var results = await Task.WhenAll(runtimes.Select(runtime => runtime.CheckAsync(logger, cancellationToken))).ConfigureAwait(false);

        return FindBestRuntime(results);
    }

    /// <summary>
    /// Checks a runtime using its registered probe or the common Docker-compatible probe.
    /// </summary>
    public static Task<ContainerRuntimeInfo> CheckRuntimeAsync(
        string executable,
        string name,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var runtime = FindProbe(executable) ?? new ContainerRuntimeProbe(executable, name, int.MaxValue);
        return runtime.CheckAsync(logger, cancellationToken);
    }

    /// <summary>
    /// Selects by availability, then runtime priority, independently of result order.
    /// </summary>
    public static ContainerRuntimeInfo? FindBestRuntime(IEnumerable<ContainerRuntimeInfo> results)
    {
        ContainerRuntimeInfo? best = null;
        foreach (var candidate in results)
        {
            if (best is null)
            {
                best = candidate;
                continue;
            }

            if (!best.IsInstalled && candidate.IsInstalled)
            {
                best = candidate;
            }
            else if (!best.IsRunning && candidate.IsRunning)
            {
                best = candidate;
            }
            else if (candidate.Priority < best.Priority
                && candidate.IsInstalled == best.IsInstalled
                && candidate.IsRunning == best.IsRunning)
            {
                best = candidate;
            }
        }

        return best;
    }

    private static ContainerRuntimeProbe? FindProbe(string executable)
    {
        return s_knownRuntimes.FirstOrDefault(runtime =>
            string.Equals(runtime.Executable, executable, StringComparison.OrdinalIgnoreCase));
    }
}
