// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if the dotnet dev-certs HTTPS certificate is trusted and detects multiple certificates.
/// </summary>
/// <remarks>
/// This check uses the machine-readable format (--check-trust-machine-readable) available in .NET 10+
/// to get detailed certificate information including trust levels. It warns when multiple certificates
/// are detected, which can cause confusion about which certificate will be used. Falls back to the
/// legacy --check --trust method for older .NET SDK versions.
/// </remarks>
internal sealed class DevCertsCheck(ILogger<DevCertsCheck> logger) : IEnvironmentCheck
{
    private static readonly TimeSpan s_processTimeout = TimeSpan.FromSeconds(30);

    public int Order => 35; // After SDK check (30), before container checks (40+)

    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to use the new machine-readable format for .NET 10+
            var certificates = await GetCertificatesAsync(cancellationToken);
            
            if (certificates is null)
            {
                // Fallback to old method if machine-readable format is not available
                return await CheckLegacyAsync(cancellationToken);
            }

            // Count how many certificates are valid and trusted
            var validAndTrustedCerts = certificates.Where(c => 
                c.IsHttpsDevelopmentCertificate && 
                c.TrustLevel == "Full").ToList();
            
            var validCerts = certificates.Where(c => c.IsHttpsDevelopmentCertificate).ToList();

            // If we have multiple valid certificates, warn the user
            if (validCerts.Count > 1)
            {
                if (validAndTrustedCerts.Count == 0)
                {
                    return [new EnvironmentCheckResult
                    {
                        Category = "sdk",
                        Name = "dev-certs",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"Multiple HTTPS development certificates found ({validCerts.Count} certificates), but none are trusted",
                        Details = "Having multiple development certificates can cause confusion. None of them are currently trusted.",
                        Fix = "Run 'dotnet dev-certs https --clean' to remove all certificates, then run 'dotnet dev-certs https --trust' to create a new one.",
                        Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                    }];
                }
                
                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"Multiple HTTPS development certificates found ({validCerts.Count} certificates)",
                    Details = "Having multiple development certificates can cause confusion when selecting which certificate to use.",
                    Fix = "Run 'dotnet dev-certs https --clean' to remove all certificates, then run 'dotnet dev-certs https --trust' to create a new one.",
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                }];
            }

            // Single certificate case
            if (validAndTrustedCerts.Count == 1)
            {
                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "HTTPS development certificate is trusted"
                }];
            }

            if (validCerts.Count == 1)
            {
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

            // No valid certificates found
            return [new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = "No HTTPS development certificate found",
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

    private async Task<List<DevCertificate>?> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "dev-certs https --check-trust-machine-readable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process is null)
            {
                return null;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_processTimeout);

            string output = string.Empty;
            string errorOutput = string.Empty;
            try
            {
                output = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                errorOutput = await process.StandardError.ReadToEndAsync(timeoutCts.Token);
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            // If the command failed or returned error (e.g., old .NET version), return null to fallback
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                logger.LogDebug("Machine-readable dev-certs check not available. Exit code: {ExitCode}, Error: {Error}", process.ExitCode, errorOutput);
                return null;
            }

            // Parse JSON output using source-generated serialization
            var certificates = JsonSerializer.Deserialize(output, JsonSourceGenerationContext.Default.ListDevCertificate);

            return certificates;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error getting certificates in machine-readable format");
            return null;
        }
    }

    private async Task<IReadOnlyList<EnvironmentCheckResult>> CheckLegacyAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Fallback to the old --check --trust method
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
                process.Kill(entireProcessTree: true);
                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = "HTTPS development certificate check timed out"
                }];
            }

            // Exit code 0 means certificate exists and is trusted
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

            // Certificate is not trusted or doesn't exist
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
            logger.LogDebug(ex, "Error in legacy dev-certs check");
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

/// <summary>
/// Represents a development certificate returned by dotnet dev-certs.
/// </summary>
internal sealed class DevCertificate
{
    public string Thumbprint { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public bool IsHttpsDevelopmentCertificate { get; init; }
    public bool IsExportable { get; init; }
    public string TrustLevel { get; init; } = string.Empty;
}
