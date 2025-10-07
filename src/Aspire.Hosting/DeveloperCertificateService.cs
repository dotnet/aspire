// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting;

internal class DeveloperCertificateService : IDeveloperCertificateService
{
    private readonly Lazy<ImmutableList<X509Certificate2>> _certificates;
    private readonly Lazy<bool> _supportsContainerTrust;

    public DeveloperCertificateService()
    {
        _certificates = new Lazy<ImmutableList<X509Certificate2>>(() =>
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var devCert = store.Certificates
                .Where(c => c.IsAspNetCoreDevelopmentCertificate())
                .Where(c => c.NotAfter > DateTime.UtcNow)
                .OrderByDescending(c => c.GetCertificateVersion())
                .ThenByDescending(c => c.NotAfter)
                .FirstOrDefault();

            var devCerts = new List<X509Certificate2>();
            if (devCert != null)
            {
                devCerts.Add(devCert);
            }

            return devCerts.ToImmutableList();
        });

        _supportsContainerTrust = new Lazy<bool>(() =>
        {
            return Certificates.Any(c => c.GetCertificateVersion() >= X509Certificate2Extensions.MinimumCertificateVersionSupportingContainerTrust);
        });
    }

    public ImmutableList<X509Certificate2> Certificates => _certificates.Value;

    public bool SupportsContainerTrust => _supportsContainerTrust.Value;
}