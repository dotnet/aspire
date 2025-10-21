// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that indicates a resource wants to manage how custom certificate trust is configured when run as a container.
/// </summary>
/// <param name="callback">The callback to invoke to populate or modify the certificate authority collection.</param>
public sealed class ContainerCertificateTrustCallbackAnnotation(Func<ContainerCertificateTrustCallbackAnnotationContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback to invoke to populate or modify the certificate authority collection.
    /// </summary>
    public Func<ContainerCertificateTrustCallbackAnnotationContext, Task> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}

/// <summary>
/// Context provided to a <see cref="ContainerCertificateTrustCallbackAnnotation"/> callback.
/// </summary>
public sealed class ContainerCertificateTrustCallbackAnnotationContext
{
    /// <summary>
    /// Gets the resource to which the annotation is applied.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// Gets the <see cref="CertificateTrustScope"/> of trust for the resource.
    /// </summary>
    public required CertificateTrustScope Scope { get; init; }

    /// <summary>
    /// Gets the <see cref="X509Certificate2Collection"/> of certificates for this resource.
    /// </summary>
    public required X509Certificate2Collection Certificates { get; init; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Command line arguments required to enable certificate trust for the resource.
    /// </summary>
    public List<string> CertificateTrustArguments { get; } = new();

    /// <summary>
    /// These command line arguments will be combined with the path to the custom certificates bundle.
    /// </summary>
    public List<string> CertificateBundleArguments { get; } = new();

    /// <summary>
    /// List of environment variable names that will be set with the path to the custom certificates bundle as their value.
    /// </summary>
    public List<string> CertificateBundleEnvironment { get; } = new();

    /// <summary>
    /// List of environment variable names that will be set with paths to directories containing CA certificates to trust.
    /// By default, SSL_CERT_DIR is specified, which overrides the default OpenSSL individual certificates directory paths.
    /// See: https://docs.openssl.org/3.0/man3/SSL_CTX_load_verify_locations/#description
    /// </summary>
    public List<string> CertificatesDirectoryEnvironment { get; } = new() { "SSL_CERT_DIR" };

    /// <summary>
    /// The path in the container where the custom certificate bundle and individual certificate files will be placed.
    /// Defaults to /usr/lib/ssl/aspire which is a non-standard path to avoid conflicts with existing certificate stores.
    /// </summary>
    public string CustomCertificatesContainerFilePath { get; set; } = "/usr/lib/ssl/aspire";

    /// <summary>
    /// List of default certificate bundle files in the container that will be replaced if the resource scope of trust is
    /// set to <see cref="CertificateTrustScope.Override"/>. Defaults to common Linux paths for CA certificates
    /// to maximize compatibility, but can be overriden with specific paths for a given resource if needed.
    /// See: https://go.dev/src/crypto/x509/root_linux.go
    /// </summary>
    public List<string> DefaultContainerCertificateAuthorityBundlePaths { get; } = new()
    {
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
        "/etc/ssl/cert.pem",
    };

    /// <summary>
    /// List of default certificate directories in a container that should be appended to the custom certificate directories in
    /// <see cref="CertificateTrustScope.Append"/> mode. Defaults to common Linux paths for CA certificates.
    /// See: https://go.dev/src/crypto/x509/root_linux.go
    /// </summary>
    public List<string> DefaultContainerCertificatesDirectoryPaths { get; } = new()
    {
        "/etc/ssl/certs",
        "/usr/local/share/ca-certificates",
        "/etc/pki/tls/certs"
    };
}
