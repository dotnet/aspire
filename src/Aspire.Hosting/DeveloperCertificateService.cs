// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting;

internal class DeveloperCertificateService : IDeveloperCertificateService
{
    private readonly Lazy<ImmutableList<X509Certificate2>> _certificates;
    private readonly Lazy<bool> _supportsContainerTrust;
    private readonly Lazy<bool> _supportsTlsTermination;

    public DeveloperCertificateService(ILogger<DeveloperCertificateService> logger, IConfiguration configuration, DistributedApplicationOptions options)
    {
        _certificates = new Lazy<ImmutableList<X509Certificate2>>(() =>
        {
            try
            {
                var devCerts = new List<X509Certificate2>();

                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var now = DateTime.UtcNow;
                devCerts.AddRange(
                    // Order by version and expiration date descending to get the most recent, highest version first.
                    // OpenSSL will only check the first self-signed certificate in the bundle that matches a given domain,
                    // so we want to ensure the certificate that will be used by ASP.NET Core is the first one in the bundle.
                    // Match the ordering logic ASP.NET Core uses: https://github.com/dotnet/aspnetcore/blob/0aefdae365ff9b73b52961acafd227309524ce3c/src/Shared/CertificateGeneration/CertificateManager.cs#L122
                    store.Certificates
                        .Where(c => c.IsAspNetCoreDevelopmentCertificate())
                        .Where(c => c.NotBefore <= now && now <= c.NotAfter)
                        .OrderByDescending(c => c.GetCertificateVersion())
                        .Take(1));

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
            logger.LogDebug("Developer certificate TLS termination support: {Available}", supportsTlsTermination);
            return supportsTlsTermination;
        });

        // Environment variable config > DistributedApplicationOptions > default true
        TrustCertificate = configuration.GetBool(KnownConfigNames.TrustDeveloperCertificate) ??
            options.TrustDeveloperCertificate ??
            true;
    }

    /// <inheritdoc />
    public ImmutableList<X509Certificate2> Certificates => _certificates.Value;

    /// <inheritdoc />
    public bool SupportsContainerTrust => _supportsContainerTrust.Value;

    /// <inheritdoc />
    public bool TrustCertificate { get; }

    public bool SupportsTlsTermination => _supportsTlsTermination.Value;
}
