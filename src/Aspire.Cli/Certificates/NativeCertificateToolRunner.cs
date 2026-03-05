// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Aspire.Cli.DotNet;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Aspire.Cli.Certificates;

/// <summary>
/// Certificate tool runner that uses the native CertificateManager directly (no subprocess needed).
/// </summary>
internal sealed class NativeCertificateToolRunner(CertificateManager certificateManager) : ICertificateToolRunner
{
    public Task<(int ExitCode, CertificateTrustResult? Result)> CheckHttpCertificateMachineReadableAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken)
    {
        var availableCertificates = certificateManager.ListCertificates(
            StoreName.My, StoreLocation.CurrentUser, isValid: true);

        try
        {
            var now = DateTimeOffset.Now;
            var certInfos = availableCertificates.Select(cert =>
            {
                var status = certificateManager.CheckCertificateState(cert);
                var trustLevel = status.Success
                    ? certificateManager.GetTrustLevel(cert).ToString()
                    : DevCertTrustLevel.None;

                return new DevCertInfo
                {
                    Thumbprint = cert.Thumbprint,
                    Subject = cert.Subject,
                    SubjectAlternativeNames = GetSanExtension(cert),
                    Version = CertificateManager.GetCertificateVersion(cert),
                    ValidityNotBefore = cert.NotBefore,
                    ValidityNotAfter = cert.NotAfter,
                    IsHttpsDevelopmentCertificate = CertificateManager.IsHttpsDevelopmentCertificate(cert),
                    IsExportable = certificateManager.IsExportable(cert),
                    TrustLevel = trustLevel
                };
            }).ToList();

            var validCerts = certInfos
                .Where(c => c.IsHttpsDevelopmentCertificate && c.ValidityNotBefore <= now && now <= c.ValidityNotAfter)
                .OrderByDescending(c => c.Version)
                .ToList();

            var highestVersionedCert = validCerts.FirstOrDefault();

            var result = new CertificateTrustResult
            {
                HasCertificates = validCerts.Count > 0,
                TrustLevel = highestVersionedCert?.TrustLevel,
                Certificates = certInfos
            };

            return Task.FromResult((0, (CertificateTrustResult?)result));
        }
        finally
        {
            CertificateManager.DisposeCertificates(availableCertificates);
        }
    }

    public Task<int> TrustHttpCertificateAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var result = certificateManager.EnsureAspNetCoreHttpsDevelopmentCertificate(
            now, now.Add(TimeSpan.FromDays(365)),
            trust: true);

        return Task.FromResult(result switch
        {
            EnsureCertificateResult.Succeeded or
            EnsureCertificateResult.ValidCertificatePresent or
            EnsureCertificateResult.ExistingHttpsCertificateTrusted or
            EnsureCertificateResult.NewHttpsCertificateTrusted => 0,
            EnsureCertificateResult.UserCancelledTrustStep => 5,
            _ => 4 // ErrorTrustingTheCertificate
        });
    }

    private static string[]? GetSanExtension(X509Certificate2 cert)
    {
        var dnsNames = new List<string>();
        foreach (var extension in cert.Extensions)
        {
            if (extension is X509SubjectAlternativeNameExtension sanExtension)
            {
                foreach (var dns in sanExtension.EnumerateDnsNames())
                {
                    dnsNames.Add(dns);
                }
            }
        }
        return dnsNames.Count > 0 ? dnsNames.ToArray() : null;
    }
}
