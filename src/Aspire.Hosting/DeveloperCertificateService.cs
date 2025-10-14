// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting;

internal class DeveloperCertificateService : IDeveloperCertificateService
{
    private readonly Lazy<ImmutableList<X509Certificate2>> _certificates;
    private readonly Lazy<bool> _supportsContainerTrust;

    public DeveloperCertificateService(ILogger<DeveloperCertificateService> logger)
    {
        _certificates = new Lazy<ImmutableList<X509Certificate2>>(() =>
        {
            try
            {
                var devCerts = new List<X509Certificate2>();

                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                devCerts.AddRange(
                    // Order by version and expiration date descending to get the most recent, highest version first.
                    // OpenSSL will only check the first self-signed certificate in the bundle that matches a given domain,
                    // so we want to ensure the certificate that will be used by ASP.NET Core is the first one in the bundle.
                    store.Certificates
                        .Where(c => c.IsAspNetCoreDevelopmentCertificate())
                        .Where(c => c.NotAfter > DateTime.UtcNow)
                        .OrderByDescending(c => c.GetCertificateVersion())
                        .ThenByDescending(c => c.NotAfter)
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
    }

    public ImmutableList<X509Certificate2> Certificates => _certificates.Value;

    public bool SupportsContainerTrust => _supportsContainerTrust.Value;
}