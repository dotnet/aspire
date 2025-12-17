// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Checks if Docker Engine (vs Docker Desktop) is installed and provides tunnel guidance.
/// </summary>
internal sealed class DockerEngineCheck(ILogger<DockerEngineCheck> logger) : IEnvironmentCheck
{
    private static readonly TimeSpan s_processTimeout = TimeSpan.FromSeconds(10);

    public int Order => 50; // Process check - more expensive, runs after container runtime check

    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Docker is available
            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process is null)
            {
                // Docker not available, skip this check (ContainerRuntimeCheck handles this case)
                return [];
            }

            using var versionTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            versionTimeoutCts.CancelAfter(s_processTimeout);

            try
            {
                await process.WaitForExitAsync(versionTimeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill();
                return []; // Timeout, skip this check
            }

            if (process.ExitCode != 0)
            {
                // Docker not available, skip this check (ContainerRuntimeCheck handles this case)
                return [];
            }

            var isDockerDesktop = await IsDockerDesktopAsync(cancellationToken);

            if (isDockerDesktop)
            {
                return [new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "docker-engine",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "Docker Desktop detected"
                }];
            }

            // Docker Engine detected - check if tunnel is already enabled
            var tunnelEnabled = Environment.GetEnvironmentVariable("ASPIRE_ENABLE_CONTAINER_TUNNEL");
            if (string.Equals(tunnelEnabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return [new EnvironmentCheckResult
                {
                    Category = "container",
                    Name = "docker-engine",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "Docker Engine with container tunnel enabled"
                }];
            }

            // Warn about tunnel requirement
            return [new EnvironmentCheckResult
            {
                Category = "container",
                Name = "docker-engine",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Docker Engine requires Aspire's container tunnel to allow containers to reach applications running on the host",
                Fix = "Set environment variable: ASPIRE_ENABLE_CONTAINER_TUNNEL=true",
                Link = "https://aka.ms/aspire-prerequisites#docker-engine"
            }];
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking Docker Engine");
            // Skip this check on error (ContainerRuntimeCheck handles container availability)
            return [];
        }
    }

    private static async Task<bool> IsDockerDesktopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check Docker context to see if it's Desktop
            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "context inspect",
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
                var output = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                await process.WaitForExitAsync(timeoutCts.Token);

                // Docker Desktop context usually contains "desktop" in the name or endpoint
                return output.Contains("desktop", StringComparison.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill();
                return false; // Timeout, assume not Docker Desktop
            }
        }
        catch
        {
            return false;
        }
    }
}
