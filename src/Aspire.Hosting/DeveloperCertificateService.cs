#pragma warning disable ASPIRECERTIFICATES001
#pragma warning disable ASPIREFILESYSTEM001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting;

internal class DeveloperCertificateService : IDeveloperCertificateService
{
    private readonly Lazy<ImmutableList<X509Certificate2>> _certificates;
    private readonly Lazy<bool> _supportsContainerTrust;
    private readonly Lazy<bool> _supportsTlsTermination;

    public DeveloperCertificateService(ILogger<DeveloperCertificateService> logger, IConfiguration configuration, DistributedApplicationOptions options)
    {
        TrustCertificate = configuration.GetBool(KnownConfigNames.DeveloperCertificateDefaultTrust) ??
            options.TrustDeveloperCertificate ??
            true;

        _certificates = new Lazy<ImmutableList<X509Certificate2>>(() =>
        {
            try
            {
                var devCerts = new List<X509Certificate2>();

                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                // Order by version and expiration date descending to get the most recent, highest version first.
                // OpenSSL will only check the first self-signed certificate in the bundle that matches a given domain,
                // so we want to ensure the certificate that will be used by ASP.NET Core is the first one in the bundle.
                // Match the ordering logic ASP.NET Core uses, including DateTimeOffset.Now for current time: https://github.com/dotnet/aspnetcore/blob/0aefdae365ff9b73b52961acafd227309524ce3c/src/Shared/CertificateGeneration/CertificateManager.cs#L122
                var now = DateTimeOffset.Now;

                // Get all valid ASP.NET Core development certificates
                var validCerts = store.Certificates
                    .Where(c => c.IsAspNetCoreDevelopmentCertificate())
                    .Where(c => c.NotBefore <= now && now <= c.NotAfter)
                    .ToList();

                // If any certificate has a Subject Key Identifier extension, exclude certificates without it
                if (validCerts.Any(c => c.HasSubjectKeyIdentifier()))
                {
                    validCerts = validCerts.Where(c => c.HasSubjectKeyIdentifier()).ToList();
                }

                // Take the highest version valid certificate for each unique SKI
                devCerts.AddRange(
                    validCerts
                        .GroupBy(c => c.Extensions.OfType<X509SubjectKeyIdentifierExtension>().FirstOrDefault()?.SubjectKeyIdentifier)
                        .SelectMany(g => g.OrderByDescending(c => c.GetCertificateVersion()).ThenByDescending(c => c.NotAfter).Take(1))
                        .OrderByDescending(c => c.GetCertificateVersion()).ThenByDescending(c => c.NotAfter));

                // Release the unused certificates
                foreach (var unusedCert in validCerts.Except(devCerts))
                {
                    unusedCert.Dispose();
                }

                if (devCerts.Count == 0)
                {
                    logger.LogInformation("No ASP.NET Core developer certificates found in the CurrentUser/My certificate store.");
                    return ImmutableList<X509Certificate2>.Empty;
                }

                return devCerts.ToImmutableList();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to load developer certificates from the CurrentUser/My certificate store. Automatic trust of development certificates will not be available. Reason: {Message}", ex.Message);
                return ImmutableList<X509Certificate2>.Empty;
            }
        });

        _supportsContainerTrust = new Lazy<bool>(() =>
        {
            var containerTrustAvailable = Certificates.Any(c => c.GetCertificateVersion() >= X509Certificate2Extensions.MinimumCertificateVersionSupportingContainerTrust);
            logger.LogDebug("Container trust for developer certificates is {Status}.", containerTrustAvailable ? "available" : "not available");
            return containerTrustAvailable;
        });

        _supportsTlsTermination = new Lazy<bool>(() =>
        {
            var supportsTlsTermination = Certificates.Any(c => c.HasPrivateKey);
            logger.LogDebug("Developer certificate HTTPS/TLS termination support: {Available}", supportsTlsTermination);
            return supportsTlsTermination;
        });

        // By default, only use for server authentication if trust is also enabled (and a developer certificate with a private key is available)
        UseForHttps = (configuration.GetBool(KnownConfigNames.DeveloperCertificateDefaultHttpsTermination) ??
            options.DeveloperCertificateDefaultHttpsTerminationEnabled ??
            true) && TrustCertificate && _supportsTlsTermination.Value;
    }

    /// <inheritdoc />
    public ImmutableList<X509Certificate2> Certificates => _certificates.Value;

    /// <inheritdoc />
    public bool SupportsContainerTrust => _supportsContainerTrust.Value;

    /// <inheritdoc />
    public bool TrustCertificate { get; }

    /// <inheritdoc />
    public bool UseForHttps { get; }

    internal static bool IsCertificateTrusted(IFileSystemService fileSystemService, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (OperatingSystem.IsMacOS())
        {
            // On MacOS we have to verify against the Keychain
            return IsCertificateTrustedInMacOsKeychain(fileSystemService, certificate);
        }

        try
        {
            // On Linux and Windows, we need to check if the certificate is in the Root store
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, validOnly: false);
            try
            {
                return matches.Count > 0;
            }
            finally
            {
                foreach (var cert in matches)
                {
                    cert.Dispose();
                }
            }
        }
        catch
        {
            // Ignore errors and assume not trusted
            return false;
        }
    }

    // Use the same approach as `dotnet dev-certs` to check if the certificate is trusted in the macOS keychain
    // See: https://github.com/dotnet/aspnetcore/blob/2a88012113497bac5056548f16d810738b069198/src/Shared/CertificateGeneration/MacOSCertificateManager.cs#L36-L37
    private static bool IsCertificateTrustedInMacOsKeychain(IFileSystemService fileSystemService, X509Certificate2 certificate)
    {
        try
        {
            using var tempDirectory = fileSystemService.TempDirectory.CreateTempSubdirectory("aspire-devcert-check");
            var certPath = Path.Combine(tempDirectory.Path, $"aspire-devcert-{certificate.Thumbprint}.cer");

            File.WriteAllBytes(certPath, certificate.Export(X509ContentType.Cert));

            var startInfo = new ProcessStartInfo
            {
                FileName = "security",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("verify-cert");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add("basic");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add("ssl");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(certPath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            // Ignore errors and assume not trusted
            return false;
        }
    }
}
