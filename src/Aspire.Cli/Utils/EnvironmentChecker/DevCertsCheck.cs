// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if the dotnet dev-certs HTTPS certificate is trusted and detects multiple certificates.
/// </summary>
/// <remarks>
/// This check uses X509Store to directly examine certificates in the certificate store.
/// It checks for ASP.NET Core HTTPS development certificates by looking for the well-known OID.
/// Trust is verified by checking if the certificate is trusted by the system (using X509Chain on macOS,
/// or by checking the Root stores on Windows/Linux).
/// Warnings are issued for multiple certificates or certificates older than version 5.
/// </remarks>
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

            // Check for old certificate versions (< 5) among trusted certificates
            const int MinimumRecommendedCertificateVersion = 5;
            var oldTrustedCerts = trustedCerts.Where(c => c.GetCertificateVersion() < MinimumRecommendedCertificateVersion).ToList();

            var results = new List<EnvironmentCheckResult>();

            // Check for multiple dev certificates (in My store)
            if (devCertificates.Count > 1)
            {
                if (trustedCerts.Count == 0)
                {
                    results.Add(new EnvironmentCheckResult
                    {
                        Category = "sdk",
                        Name = "dev-certs",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"Multiple HTTPS development certificates found ({devCertificates.Count} certificates), but none are trusted",
                        Details = "Having multiple development certificates can cause confusion. None of them are currently trusted.",
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
                        Details = "Having multiple development certificates can cause confusion when selecting which certificate to use.",
                        Fix = "Run 'dotnet dev-certs https --clean' to remove all certificates, then run 'dotnet dev-certs https --trust' to create a new one.",
                        Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                    });
                }
            }
            // Check for multiple trusted certificates (in Root store)
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
                // Single certificate that's not trusted
                results.Add(new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = "HTTPS development certificate is not trusted",
                    Details = "Aspire uses HTTPS for secure communication between the dashboard and your services during local development.",
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
                    Details = "Older certificate versions (< v5) may not support container trust scenarios. Consider regenerating your development certificate.",
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
                    devCerts.Add(cert);
                }
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
    /// <remarks>
    /// On macOS, uses X509Chain to verify trust through the Keychain.
    /// On Linux, checks the OpenSSL trust directory (~/.aspnet/dev-certs/trust/).
    /// On Windows, checks if the certificate exists in the Root stores.
    /// </remarks>
    private bool IsCertificateTrusted(X509Certificate2 certificate)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, use X509Chain to check if the certificate is trusted via the Keychain
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            try
            {
                var isValid = chain.Build(certificate);
                if (isValid)
                {
                    return true;
                }

                // Check if the only error is UntrustedRoot - if so, we need to check
                // if it's actually trusted in the keychain (self-signed cert scenario)
                var hasOnlyUntrustedRootError = chain.ChainStatus.Length > 0 &&
                    chain.ChainStatus.All(s => s.Status == X509ChainStatusFlags.UntrustedRoot);

                if (hasOnlyUntrustedRootError)
                {
                    // For self-signed certs, X509Chain may still report UntrustedRoot
                    // even when the cert is trusted in keychain. Check the Root stores.
                    return IsCertificateInRootStore(certificate);
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error checking certificate trust via X509Chain");
                return false;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // On Linux, check the OpenSSL trust directory
            return IsCertificateInOpenSslTrustStore(certificate) || IsCertificateInRootStore(certificate);
        }
        else
        {
            // On Windows, check if the certificate is in the Root stores
            return IsCertificateInRootStore(certificate);
        }
    }

    /// <summary>
    /// Checks if a certificate exists in the OpenSSL trust directory on Linux.
    /// </summary>
    /// <remarks>
    /// The dotnet dev-certs tool exports trusted certificates to ~/.aspnet/dev-certs/trust/
    /// with the naming pattern: aspnetcore-localhost-{THUMBPRINT}.pem
    /// </remarks>
    private bool IsCertificateInOpenSslTrustStore(X509Certificate2 certificate)
    {
        try
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var trustDir = Path.Combine(homeDir, ".aspnet", "dev-certs", "trust");

            if (!Directory.Exists(trustDir))
            {
                logger.LogDebug("OpenSSL trust directory does not exist: {TrustDir}", trustDir);
                return false;
            }

            // Look for certificate file with matching thumbprint
            // Format: aspnetcore-localhost-{THUMBPRINT}.pem
            var expectedFileName = $"aspnetcore-localhost-{certificate.Thumbprint}.pem";
            var certPath = Path.Combine(trustDir, expectedFileName);

            if (File.Exists(certPath))
            {
                logger.LogDebug("Found trusted certificate in OpenSSL trust directory: {CertPath}", certPath);
                return true;
            }

            // Also check for any .pem files that might match by reading their content
            foreach (var pemFile in Directory.GetFiles(trustDir, "*.pem"))
            {
                try
                {
                    using var trustedCert = X509CertificateLoader.LoadCertificateFromFile(pemFile);
                    if (string.Equals(trustedCert.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogDebug("Found matching certificate in OpenSSL trust directory: {PemFile}", pemFile);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error reading PEM file: {PemFile}", pemFile);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking OpenSSL trust directory");
            return false;
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
                    if (string.Equals(cert.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
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
