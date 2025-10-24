// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that allows overriding default certificate paths for container resources.
/// </summary>
public sealed class ContainerCertificatePathsAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The default destination path in the container under which custom certificates will be placed.
    /// </summary>
    public const string DefaultCustomCertificatesDestination = "/usr/lib/ssl/aspire";

    /// <summary>
    /// Default paths to default certificate bundle files in a container.
    /// </summary>
    public static ImmutableList<string> DefaultCertificateBundlePaths = ImmutableList.Create(
        // Debian/Ubuntu/Gentoo etc.
        "/etc/ssl/certs/ca-certificates.crt",
        // Fedora/RHEL 6
        "/etc/pki/tls/certs/ca-bundle.crt",
        // OpenSUSE
        "/etc/ssl/ca-bundle.pem",
        // OpenELEC
        "/etc/pki/tls/cacert.pem",
        // CentOS/RHEL 7
        "/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem",
        // Alpine Linux
        "/etc/ssl/cert.pem");

    /// <summary>
    /// Default paths to default directories containing individual CA certificates in a container.
    /// </summary>
    public static ImmutableList<string> DefaultCertificateDirectoriesPaths = ImmutableList.Create(
        "/etc/ssl/certs",
        "/usr/local/share/ca-certificates",
        "/etc/pki/tls/certs");

    /// <summary>
    /// Paths to default certificate bundle files on the host that should be replaced when the resource's
    /// <see cref="CertificateTrustScope"/> is set to <see cref="CertificateTrustScope.Override"/> or
    /// <see cref="CertificateTrustScope.System"/>.
    /// If not set, a set of common default paths for popular Linux distributions will be used.
    /// </summary>
    public List<string>? DefaultCertificateBundles { get; init; }

    /// <summary>
    /// Paths to default directories containing individual CA certificates on the host that should be appended
    /// when the resource's <see cref="CertificateTrustScope"/> is set to <see cref="CertificateTrustScope.Append"/>.
    /// If not set, a set of common default paths for popular Linux distributions will be used.
    /// </summary>
    public List<string>? DefaultCertificateDirectories { get; init; }

    /// <summary>
    /// The destination path in the container under which custom certificates will be placed.
    /// If not set, defaults to /usr/lib/ssl/aspire.
    /// </summary>
    public string? CustomCertificatesDestination { get; init; }
}
