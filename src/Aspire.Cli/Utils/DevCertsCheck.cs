// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Checks if the dotnet dev-certs HTTPS certificate is trusted.
/// </summary>
internal sealed class DevCertsCheck(ILogger<DevCertsCheck> logger) : IEnvironmentCheck
{
    private static readonly TimeSpan s_processTimeout = TimeSpan.FromSeconds(30);

    public int Order => 35; // After SDK check (30), before container checks (40+)

    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if dev-certs tool is available and certificate is trusted
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "dev-certs https --check --trust",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process is null)
            {
                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = "Unable to check HTTPS development certificate",
                    Details = "Failed to start dotnet dev-certs process"
                }];
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_processTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill();
                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = "HTTPS development certificate check timed out"
                }];
            }

            // Exit code 0 means certificate exists and is trusted
            // Exit code 1 means certificate doesn't exist or isn't trusted
            if (process.ExitCode == 0)
            {
                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "HTTPS development certificate is trusted"
                }];
            }

            // Certificate is not trusted
            return [new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = "HTTPS development certificate is not trusted",
                Fix = "Run: dotnet dev-certs https --trust",
                Link = "https://aka.ms/aspire-prerequisites#dev-certs"
            }];
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking dev-certs");
            return [new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Unable to check HTTPS development certificate",
                Details = ex.Message
            }];
        }
    }
}
