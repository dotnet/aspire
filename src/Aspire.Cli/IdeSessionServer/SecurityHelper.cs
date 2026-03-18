// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Cli.IdeSessionServer;

/// <summary>
/// Security utilities for the IDE session server.
/// </summary>
internal static class SecurityHelper
{
    /// <summary>
    /// Generates a cryptographically secure random token encoded as base64.
    /// </summary>
    /// <returns>A 32-byte random token encoded as base64 (256 bits of entropy).</returns>
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Creates a self-signed X.509 certificate for HTTPS.
    /// </summary>
    /// <param name="commonName">The common name for the certificate (default: localhost).</param>
    /// <returns>The generated certificate.</returns>
    public static X509Certificate2 CreateSelfSignedCertificate(string commonName = "localhost")
    {
        using var rsa = RSA.Create(4096);

        var distinguishedName = new X500DistinguishedName($"CN={commonName}");

        var request = new CertificateRequest(
            distinguishedName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add Subject Alternative Name extension for localhost
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        request.CertificateExtensions.Add(sanBuilder.Build());

        // Add Key Usage extension
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        // Add Enhanced Key Usage extension for server authentication
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")], // Server Authentication
                critical: true));

        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddYears(1);

        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        // Export and re-import to ensure the private key is properly associated
        // This is necessary for SslStream on some platforms
        var pfxBytes = certificate.Export(X509ContentType.Pfx, string.Empty);
        return X509CertificateLoader.LoadPkcs12(pfxBytes, string.Empty, X509KeyStorageFlags.MachineKeySet);
    }

    /// <summary>
    /// Gets the raw certificate bytes encoded as base64.
    /// </summary>
    public static string GetCertificateBase64(X509Certificate2 certificate)
    {
        return Convert.ToBase64String(certificate.RawData);
    }

    /// <summary>
    /// Performs a timing-safe comparison of two strings.
    /// </summary>
    public static bool TimingSafeEquals(string a, string b)
    {
        if (a is null || b is null)
        {
            return a == b;
        }

        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
