// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if a container runtime (Docker or Podman) is available and running.
/// </summary>
internal sealed class ContainerRuntimeCheck(ILogger<ContainerRuntimeCheck> logger) : IEnvironmentCheck
{
    private static readonly TimeSpan s_processTimeout = TimeSpan.FromSeconds(10);

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
            // Check if runtime is installed (use lowercase for process name)
            var runtimeLower = runtime.ToLowerInvariant();
            var versionProcessInfo = new ProcessStartInfo
            {
                FileName = runtimeLower,
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

            using var versionTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            versionTimeoutCts.CancelAfter(s_processTimeout);

            try
            {
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
