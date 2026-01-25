// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli.Certificates;

/// <summary>
/// Represents information about a development certificate from `dotnet dev-certs https --check-trust-machine-readable`.
/// </summary>
internal sealed class DevCertInfo
{
    [JsonPropertyName("Thumbprint")]
    public string? Thumbprint { get; set; }

    [JsonPropertyName("Subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("X509SubjectAlternativeNameExtension")]
    public string[]? SubjectAlternativeNames { get; set; }

    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("ValidityNotBefore")]
    public DateTimeOffset ValidityNotBefore { get; set; }

    [JsonPropertyName("ValidityNotAfter")]
    public DateTimeOffset ValidityNotAfter { get; set; }

    [JsonPropertyName("IsHttpsDevelopmentCertificate")]
    public bool IsHttpsDevelopmentCertificate { get; set; }

    [JsonPropertyName("IsExportable")]
    public bool IsExportable { get; set; }

    [JsonPropertyName("TrustLevel")]
    public string? TrustLevel { get; set; }
}

/// <summary>
/// Represents the trust level of a development certificate.
/// </summary>
internal static class DevCertTrustLevel
{
    /// <summary>
    /// The certificate is fully trusted by all trust stores.
    /// </summary>
    public const string Full = "Full";

    /// <summary>
    /// The certificate is partially trusted (e.g., SSL_CERT_DIR not configured on Linux).
    /// </summary>
    public const string Partial = "Partial";

    /// <summary>
    /// The certificate is not trusted.
    /// </summary>
    public const string None = "None";
}

/// <summary>
/// The result of checking certificate trust status using machine-readable output.
/// </summary>
internal sealed class CertificateTrustResult
{
    /// <summary>
    /// Gets whether any certificate information was returned.
    /// </summary>
    public required bool HasCertificates { get; init; }

    /// <summary>
    /// Gets the trust level of the highest versioned valid certificate.
    /// </summary>
    public required string? TrustLevel { get; init; }

    /// <summary>
    /// Gets all certificate information returned by the check.
    /// </summary>
    public required IReadOnlyList<DevCertInfo> Certificates { get; init; }

    /// <summary>
    /// Gets whether the certificate is fully trusted.
    /// </summary>
    public bool IsFullyTrusted => string.Equals(TrustLevel, DevCertTrustLevel.Full, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the certificate is partially trusted.
    /// </summary>
    public bool IsPartiallyTrusted => string.Equals(TrustLevel, DevCertTrustLevel.Partial, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the certificate is not trusted at all.
    /// </summary>
    public bool IsNotTrusted => !HasCertificates || string.Equals(TrustLevel, DevCertTrustLevel.None, StringComparison.OrdinalIgnoreCase);
}
