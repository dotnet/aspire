// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Aspire.Cli.Certificates;
using Aspire.Cli.Resources;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if the dotnet dev-certs HTTPS certificate is trusted and detects multiple certificates.
/// </summary>
internal sealed class DevCertsCheck(ILogger<DevCertsCheck> logger) : IEnvironmentCheck
{
    private const string SslCertDirEnvVar = "SSL_CERT_DIR";
    private const string DevCertsOpenSslCertDirEnvVar = "DOTNET_DEV_CERTS_OPENSSL_CERTIFICATE_DIRECTORY";

    public int Order => 35; // After SDK check (30), before container checks (40+)

    private static readonly string s_trustFixCommand = string.Format(CultureInfo.InvariantCulture, DoctorCommandStrings.DevCertsTrustFixFormat, "aspire certs trust");
    private static readonly string s_cleanAndTrustFixCommand = string.Format(CultureInfo.InvariantCulture, DoctorCommandStrings.DevCertsCleanAndTrustFixFormat, "aspire certs clean", "aspire certs trust");

    public Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var certInfos = GetCertificateInfos();
            var results = EvaluateCertificateResults(certInfos);

            return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>(results);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking dev-certs");
            return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>([new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Unable to check HTTPS development certificate",
                Details = ex.Message
            }]);
        }
    }

    /// <summary>
    /// Evaluates certificate information and produces the appropriate check results.
    /// </summary>
    /// <param name="certInfos">Pre-computed certificate information including trust level, thumbprint, and version.</param>
    /// <returns>The list of environment check results.</returns>
    internal static List<EnvironmentCheckResult> EvaluateCertificateResults(
        List<CertificateInfo> certInfos)
    {
        if (certInfos.Count == 0)
        {
            return [new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = DoctorCommandStrings.DevCertsNoCertificateMessage,
                Details = DoctorCommandStrings.DevCertsNoCertificateDetails,
                Fix = s_trustFixCommand,
                Link = "https://aka.ms/aspire-prerequisites#dev-certs"
            }];
        }

        var trustedCount = certInfos.Count(c => c.TrustLevel != CertificateManager.TrustLevel.None);
        var fullyTrustedCount = certInfos.Count(c => c.TrustLevel == CertificateManager.TrustLevel.Full);
        var partiallyTrustedCount = certInfos.Count(c => c.TrustLevel == CertificateManager.TrustLevel.Partial);

        // Check for old certificate versions among trusted certificates
        var oldTrustedVersions = certInfos
            .Where(c => c.TrustLevel != CertificateManager.TrustLevel.None && c.Version < X509Certificate2Extensions.MinimumCertificateVersionSupportingContainerTrust)
            .Select(c => c.Version)
            .ToList();

        var results = new List<EnvironmentCheckResult>();

        // Check for multiple dev certificates (in My store)
        if (certInfos.Count > 1)
        {
            var certDetails = string.Join(", ", certInfos.Select(c =>
            {
                var trustLabel = c.TrustLevel switch
                {
                    CertificateManager.TrustLevel.Full => $" {DoctorCommandStrings.DevCertsTrustLabelFull}",
                    CertificateManager.TrustLevel.Partial => $" {DoctorCommandStrings.DevCertsTrustLabelPartial}",
                    _ => ""
                };
                return $"v{c.Version} ({c.Thumbprint[..8]}...){trustLabel}";
            }));

            if (trustedCount == 0)
            {
                results.Add(new EnvironmentCheckResult
                {
                    Category = "environment",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsMultipleNoneTrustedMessageFormat, certInfos.Count),
                    Details = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsMultipleNoneTrustedDetailsFormat, certDetails),
                    Fix = s_cleanAndTrustFixCommand,
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                });
            }
            else if (trustedCount < certInfos.Count)
            {
                results.Add(new EnvironmentCheckResult
                {
                    Category = "environment",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsMultipleSomeUntrustedMessageFormat, certInfos.Count),
                    Details = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsMultipleSomeUntrustedDetailsFormat, certDetails),
                    Fix = s_cleanAndTrustFixCommand,
                    Link = "https://aka.ms/aspire-prerequisites#dev-certs"
                });
            }
            // else: all certificates are trusted — no warning needed
            else
            {
                results.Add(new EnvironmentCheckResult
                {
                    Category = "environment",
                    Name = "dev-certs",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = DoctorCommandStrings.DevCertsTrustedMessage
                });
            }
        }
        else if (trustedCount == 0)
        {
            // Single certificate that's not trusted - provide diagnostic info
            var cert = certInfos[0];
            results.Add(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = DoctorCommandStrings.DevCertsNotTrustedMessage,
                Details = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsNotTrustedDetailsFormat, cert.Thumbprint),
                Fix = s_trustFixCommand,
                Link = "https://aka.ms/aspire-prerequisites#dev-certs"
            });
        }
        else if (partiallyTrustedCount > 0 && fullyTrustedCount == 0)
        {
            // Certificate is partially trusted (Linux with SSL_CERT_DIR not configured)
            var devCertsTrustPath = GetDevCertsTrustPath();
            results.Add(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Warning,
                Message = DoctorCommandStrings.DevCertsPartiallyTrustedMessage,
                Details = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsPartiallyTrustedDetailsFormat, devCertsTrustPath),
                Fix = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsPartiallyTrustedFixFormat, BuildSslCertDirFixCommand(devCertsTrustPath)),
                Link = "https://aka.ms/aspire-prerequisites#dev-certs"
            });
        }
        else
        {
            // Trusted certificate - success case
            results.Add(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "dev-certs",
                Status = EnvironmentCheckStatus.Pass,
                Message = DoctorCommandStrings.DevCertsTrustedMessage
            });
        }

        // Warn about old certificate versions
        if (oldTrustedVersions.Count > 0)
        {
            var versions = string.Join(", ", oldTrustedVersions.Select(v => $"v{v}"));
            results.Add(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "dev-certs-version",
                Status = EnvironmentCheckStatus.Warning,
                Message = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsOldVersionMessageFormat, versions),
                Details = string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.DevCertsOldVersionDetailsFormat, X509Certificate2Extensions.MinimumCertificateVersionSupportingContainerTrust),
                Fix = s_cleanAndTrustFixCommand,
                Link = "https://aka.ms/aspire-prerequisites#dev-certs"
            });
        }

        return results;
    }

    /// <summary>
    /// Loads developer certificates and builds pre-computed certificate information.
    /// Used by <see cref="CheckAsync"/>.
    /// </summary>
    private List<CertificateInfo> GetCertificateInfos()
    {
        var devCertificates = GetDeveloperCertificates();
        try
        {
            return devCertificates.Select(c =>
            {
                var trustLevel = GetCertificateTrustLevel(c);
                return new CertificateInfo(trustLevel, c.Thumbprint, c.GetCertificateVersion());
            }).ToList();
        }
        finally
        {
            foreach (var cert in devCertificates)
            {
                cert.Dispose();
            }
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
    /// Gets the trust level of a certificate.
    /// </summary>
    private CertificateManager.TrustLevel GetCertificateTrustLevel(X509Certificate2 certificate)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, use 'security verify-cert' to check trust (same as dotnet dev-certs)
            return IsCertificateTrustedOnMacOS(certificate) ? CertificateManager.TrustLevel.Full : CertificateManager.TrustLevel.None;
        }

        // Check if the certificate exists in the Root stores
        if (!IsCertificateInRootStore(certificate))
        {
            return CertificateManager.TrustLevel.None;
        }

        // On Linux, check if SSL_CERT_DIR is configured properly
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !IsSslCertDirConfigured())
        {
            return CertificateManager.TrustLevel.Partial;
        }

        return CertificateManager.TrustLevel.Full;
    }

    /// <summary>
    /// Gets the dev-certs trust path, respecting the DOTNET_DEV_CERTS_OPENSSL_CERTIFICATE_DIRECTORY override.
    /// </summary>
    private static string GetDevCertsTrustPath()
    {
        var overridePath = Environment.GetEnvironmentVariable(DevCertsOpenSslCertDirEnvVar);
        return !string.IsNullOrEmpty(overridePath)
            ? overridePath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "dev-certs", "trust");
    }

    /// <summary>
    /// Checks if SSL_CERT_DIR is configured to include the dev-certs trust path.
    /// </summary>
    private static bool IsSslCertDirConfigured()
    {
        var devCertsTrustPath = GetDevCertsTrustPath();
        var currentSslCertDir = Environment.GetEnvironmentVariable(SslCertDirEnvVar);

        if (string.IsNullOrEmpty(currentSslCertDir))
        {
            return false;
        }

        var paths = currentSslCertDir.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return paths.Any(p => string.Equals(p.TrimEnd(Path.DirectorySeparatorChar), devCertsTrustPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Builds the appropriate shell command for fixing SSL_CERT_DIR configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always includes <c>$SSL_CERT_DIR</c> to preserve any existing value. If the variable
    /// is unset, the empty expansion produces a harmless leading colon which OpenSSL treats
    /// as the default certificate directory.
    /// </para>
    /// <para>
    /// When possible, also includes the system OpenSSL certificate directory detected via
    /// 'openssl version -d' (mirrors UnixCertificateManager.TryGetOpenSslDirectory).
    /// </para>
    /// </remarks>
    private static string BuildSslCertDirFixCommand(string devCertsTrustPath)
    {
        // Always prepend $SSL_CERT_DIR to preserve any existing value.
        // If unset, the empty expansion is harmless.
        if (CertificateHelpers.TryGetOpenSslDirectory(out var openSslDir))
        {
            var systemCertsPath = Path.Combine(openSslDir, "certs");
            return $"export SSL_CERT_DIR=\"$SSL_CERT_DIR:{systemCertsPath}:{devCertsTrustPath}\"";
        }

        return $"export SSL_CERT_DIR=\"$SSL_CERT_DIR:{devCertsTrustPath}\"";
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

/// <summary>
/// Pre-computed certificate information for evaluation without accessing the certificate store.
/// </summary>
internal sealed record CertificateInfo(CertificateManager.TrustLevel TrustLevel, string Thumbprint, int Version);
