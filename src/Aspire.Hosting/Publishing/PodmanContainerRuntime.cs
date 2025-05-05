// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

internal sealed class PodmanContainerRuntime(ILogger<PodmanContainerRuntime> logger) : IContainerRuntime
{
    private async Task<(int ExitCode, string? Output)> RunDockerBuildAsync(string contextPath, string dockerfilePath, string imageName, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "podman",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("--file");
        startInfo.ArgumentList.Add(dockerfilePath);
        startInfo.ArgumentList.Add("--tag");
        startInfo.ArgumentList.Add(imageName);
        startInfo.ArgumentList.Add("--quiet");
        startInfo.ArgumentList.Add(contextPath);

        logger.LogInformation("Running Podman CLI with arguments: {Arguments}", startInfo.ArgumentList);

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new DistributedApplicationException("Failed to start Podman CLI.");
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            var stdout = process.StandardOutput.ReadToEnd();

            logger.LogError(
                "Podman CLI failed with exit code {ExitCode}. Output: {Stdout}, Error: {Stderr}",
                process.ExitCode,
                stdout,
                stderr);

            return (process.ExitCode, null);
        }
        else
        {
            var stdout = process.StandardOutput.ReadToEnd();
            logger.LogInformation("Podman CLI succeeded. Output: {Stdout}", stdout);
            return (process.ExitCode, stdout);
        }
    }

    public async Task<string> BuildImageAsync(string contextPath, string dockerfilePath, string imageName, CancellationToken cancellationToken)
    {
        var result = await RunDockerBuildAsync(
            contextPath,
            dockerfilePath,
            imageName,
            cancellationToken).ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            return result.Output!;
        }
        else
        {
            throw new DistributedApplicationException($"Docker build failed with exit code {result.ExitCode}.");
        }
    }}