// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Extension methods for <see cref="X509Certificate2Collection"/>.
/// </summary>
internal static class X509Certificate2Extensions
{
    // OID and friendly name for the ASP.NET Core HTTPS development certificate
    // See: https://github.com/dotnet/aspnetcore/blob/9f255d1ed217c0081e9b602fa86411b1821c2e0f/src/Shared/CertificateGeneration/CertificateManager.cs#L26-L27
    internal const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
    internal const string AspNetHttpsOidFriendlyName = "ASP.NET Core HTTPS development certificate";

    internal const int MinimumCertificateVersionSupportingContainerTrust = 4; // The minimum version of the ASP.NET Core certificate with SAN support for container domains

    /// <summary>
    /// Determines if the specified certificate is an ASP.NET Core development certificate.
    /// </summary>
    /// <param name="certificate">The <see cref="X509Certificate2"/> to check.</param>
    /// <returns>True if the certificate is an ASP.NET Core development certificate; otherwise, false.</returns>
    public static bool IsAspNetCoreDevelopmentCertificate(this X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        foreach (var extension in certificate.Extensions.OfType<X509Extension>())
        {
            if (string.Equals(AspNetHttpsOid, extension.Oid?.Value, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the version of the ASP.NET Core development certificate.
    /// </summary>
    /// <param name="certificate">The certificate to get the version from.</param>
    /// <returns>The version of the certificate (or 0 if no version is set).</returns>
    public static byte GetCertificateVersion(this X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var byteArray = certificate.Extensions.OfType<X509Extension>()
            .Where(e => string.Equals(AspNetHttpsOid, e.Oid?.Value, StringComparison.Ordinal))
            .Single()
            .RawData;

        if ((byteArray.Length == AspNetHttpsOidFriendlyName.Length && byteArray[0] == (byte)'A') || byteArray.Length == 0)
        {
            // No Version set, default to 0
            return 0b0;
        }
        else
        {
            // Version is in the only byte of the byte array.
            return byteArray[0];
        }
    }

    /// <summary>
    /// Indicates that this is an ASP.NET Core development certificate with SAN support for common container domains.
    /// </summary>
    /// <param name="certificate">The certificate to check.</param>
    /// <returns>True if the certificate supports CA trust scenarios in containers; otherwise, false.</returns>
    public static bool SupportsContainerTrust(this X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (certificate.GetCertificateVersion() < MinimumCertificateVersionSupportingContainerTrust)
        {
            return false;
        }

        return true;
    }
}