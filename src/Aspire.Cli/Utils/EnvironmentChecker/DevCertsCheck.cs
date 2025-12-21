// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if the dotnet dev-certs HTTPS certificate is trusted and detects multiple certificates.
/// </summary>
internal sealed class DevCertsCheck(ILogger<DevCertsCheck> logger) : IEnvironmentCheck
{
    public int Order => 35; // After SDK check (30), before container checks (40+)

    public Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var devCertificates = GetDeveloperCertificates();

            if (devCertificates.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>([new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = "No HTTPS development certificate found",
                    Details = "Aspire uses HTTPS for secure communication between the dashboard and your services during local development.",
                    Fix = "Run: dotnet dev-certs https --trust",
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                }]);
            }

            // Check which certificates are trusted
            var trustedCerts = devCertificates.Where(IsCertificateTrusted).ToList();

            // Check for old certificate versions among trusted certificates
            var oldTrustedCerts = trustedCerts.Where(c => c.GetCertificateVersion() < X509Certificate2Extensions.MinimumCertificateVersionSupportingContainerTrust).ToList();

            var results = new List<EnvironmentCheckResult>();

            // Check for multiple dev certificates (in My store)
            if (devCertificates.Count > 1)
            {
                var certDetails = string.Join(", ", devCertificates.Select(c =>
                {
                    var version = c.GetCertificateVersion();
                    var isTrusted = trustedCerts.Contains(c);
                    return $"v{version} ({c.Thumbprint[..8]}...){(isTrusted ? " [trusted]" : "")}";
                }));

                if (trustedCerts.Count == 0)
                {
                    results.Add(new EnvironmentCheckResult
                    {
                        Category = "sdk",
                        Name = "dev-certs",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"Multiple HTTPS development certificates found ({devCertificates.Count} certificates), but none are trusted",
                        Details = $"Found certificates: {certDetails}. Having multiple certificates can cause confusion.",
                        Fix = "Run 'dotnet dev-certs https --clean' to remove all certificates, then run 'dotnet dev-certs https --trust' to create a new one.",
                        Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                    });
                }
                else
                {
                    results.Add(new EnvironmentCheckResult
                    {
                        Category = "sdk",
                        Name = "dev-certs",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"Multiple HTTPS development certificates found ({devCertificates.Count} certificates)",
                        Details = $"Found certificates: {certDetails}. Having multiple certificates can cause confusion when selecting which one to use.",
                        Fix = "Run 'dotnet dev-certs https --clean' to remove all certificates, then run 'dotnet dev-certs https --trust' to create a new one.",
                        Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                    });
                }
            }
            // Check for orphaned trusted certificates (in Root store but not in My store, or multiple certs in Root store for single cert in My store)
            // This can happen when old certificates were trusted but the certificate in My store was regenerated
            else if (trustedCerts.Count > 1)
            {
                results.Add(new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = $"HTTPS development certificate is trusted ({trustedCerts.Count} trusted certificates found)",
                    Details = "Having multiple trusted development certificates in the root store is unusual. You may want to clean up old certificates by running 'dotnet dev-certs https --clean'.",
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                });
            }
            else if (trustedCerts.Count == 0)
            {
                // Single certificate that's not trusted - provide diagnostic info
                var cert = devCertificates[0];
                results.Add(new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = "HTTPS development certificate is not trusted",
                    Details = $"Certificate {cert.Thumbprint} exists in the personal store but was not found in the trusted root store.",
                    Fix = "Run: dotnet dev-certs https --trust",
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                });
            }
            else
            {
                // Single trusted certificate - success case
                results.Add(new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "HTTPS development certificate is trusted"
                });
            }

            // Warn about old certificate versions
            if (oldTrustedCerts.Count > 0)
            {
                var versions = string.Join(", ", oldTrustedCerts.Select(c => $"v{c.GetCertificateVersion()}"));
                results.Add(new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs-version",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"HTTPS development certificate has an older version ({versions})",
                    Details = $"Older certificate versions (< v{X509Certificate2Extensions.MinimumCertificateVersionSupportingContainerTrust}) may not support container trust scenarios. Consider regenerating your development certificate. For best compatibility, use .NET SDK 10.0.101 or later.",
                    Fix = "Run 'dotnet dev-certs https --clean' to remove all certificates, then run 'dotnet dev-certs https --trust' to create a new one.",
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                });
            }

            return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>(results);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking dev-certs");
            return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>([new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Unable to check HTTPS development certificate",
                Details = ex.Message
            }]);
        }
    }

    /// <summary>
    /// Gets all ASP.NET Core development certificates from the CurrentUser/My store.
    /// </summary>
    private List<X509Certificate2> GetDeveloperCertificates()
    {
        var devCerts = new List<X509Certificate2>();

        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var now = DateTimeOffset.Now;
            foreach (var cert in store.Certificates)
            {
                // Check if it's an ASP.NET Core development certificate and is currently valid
                if (cert.IsAspNetCoreDevelopmentCertificate() &&
                    cert.NotBefore <= now && now <= cert.NotAfter)
                {
                    // Create a new instance to avoid keeping references to store certificates
                    devCerts.Add(new X509Certificate2(cert));
                }

                // Dispose the certificate from the store enumeration
                cert.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error reading certificates from CurrentUser/My store");
        }

        return devCerts;
    }

    /// <summary>
    /// Checks if a certificate is trusted by the system.
    /// </summary>
    private bool IsCertificateTrusted(X509Certificate2 certificate)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, use 'security verify-cert' to check trust (same as dotnet dev-certs)
            return IsCertificateTrustedOnMacOS(certificate);
        }

        // On Windows/Linux, check if the certificate exists in the Root stores
        return IsCertificateInRootStore(certificate);
    }

    /// <summary>
    /// Checks if a certificate is trusted on macOS using the security command.
    /// </summary>
    /// <remarks>
    /// This logic is based on ASP.NET Core's MacOSCertificateManager.GetTrustLevel method:
    /// https://github.com/dotnet/aspnetcore/blob/main/src/Shared/CertificateGeneration/MacOSCertificateManager.cs
    /// It uses 'security verify-cert' to check trust, which is the same approach used by 'dotnet dev-certs https --trust'.
    /// </remarks>
    private bool IsCertificateTrustedOnMacOS(X509Certificate2 certificate)
    {
        DirectoryInfo? tempDir = null;
        try
        {
            // Create a temporary directory for the certificate file
            tempDir = Directory.CreateTempSubdirectory("aspire-cert-");
            var tempCertPath = Path.Combine(tempDir.FullName, $"{certificate.Thumbprint}.pem");
            var pemData = certificate.ExportCertificatePem();
            File.WriteAllText(tempCertPath, pemData);

            // Use 'security verify-cert' to check trust
            var processInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"verify-cert -c \"{tempCertPath}\" -p basic -p ssl",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process is null)
            {
                logger.LogDebug("Failed to start security verify-cert process");
                return false;
            }

            process.WaitForExit(TimeSpan.FromSeconds(10));
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking certificate trust on macOS");
            return false;
        }
        finally
        {
            if (tempDir != null)
            {
                try { tempDir.Delete(recursive: true); } catch { }
            }
        }
    }

    /// <summary>
    /// Checks if a certificate exists in the trusted Root stores.
    /// </summary>
    private bool IsCertificateInRootStore(X509Certificate2 certificate)
    {
        var storeLocations = new[]
        {
            (StoreName.Root, StoreLocation.CurrentUser),
            (StoreName.Root, StoreLocation.LocalMachine)
        };

        foreach (var (storeName, storeLocation) in storeLocations)
        {
            try
            {
                using var store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly);

                foreach (var cert in store.Certificates)
                {
                    try
                    {
                        if (string.Equals(cert.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        // Dispose certificates from the store enumeration
                        cert.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error reading certificates from {StoreName}/{StoreLocation}", storeName, storeLocation);
            }
        }

        return false;
    }
}
