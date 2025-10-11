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
/// <param name="resource">The resource for which the callback is being invoked.</param>
/// <param name="scope">The <see cref="CustomCertificateAuthoritiesScope"/> setting for the resource.</param>
/// <param name="certificates">The <see cref="X509Certificate2Collection"/> of certificates to modify.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
public sealed class ContainerCertificateTrustCallbackAnnotationContext(IResource resource, CustomCertificateAuthoritiesScope scope, X509Certificate2Collection certificates, CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets the resource to which the annotation is applied.
    /// </summary>
    public IResource Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));

    /// <summary>
    /// Gets the <see cref="CustomCertificateAuthoritiesScope"/> of trust for the resource.
    /// </summary>
    public CustomCertificateAuthoritiesScope Scope { get; } = scope;

    /// <summary>
    /// Gets the <see cref="X509Certificate2Collection"/> of certificates for this resource.
    /// </summary>
    public X509Certificate2Collection Certificates { get; } = certificates ?? throw new ArgumentNullException(nameof(certificates));

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
    /// </summary>
    public List<string> CertificatesDirectoryEnvironment { get; } = new() { "SSL_CERT_DIR" };

    /// <summary>
    /// The path in the container where the custom certificate bundle and individual certificate files will be placed.
    /// Defaults to /usr/lib/ssl/aspire which is a non-standard path to avoid conflicts with existing certificate stores.
    /// </summary>
    public string CustomCertificatesContainerFilePath { get; set; } = "/usr/lib/ssl/aspire";

    /// <summary>
    /// List of default certificate bundle files in the container that will be replaced if the resource scope of trust is
    /// set to <see cref="CustomCertificateAuthoritiesScope.Override"/>. Defaults to common Linux paths for CA certificates
    /// to maximize compatibility, but can be overriden with specific paths for a given resource if needed.
    /// </summary>
    public List<string> DefaultContainerCertificateAuthorityBundlePaths { get; } = new()
    {
        "/etc/ssl/certs/ca-certificates.crt",       // Debian/Ubuntu/Gentoo etc.
        "/etc/pki/tls/certs/ca-bundle.crt",         // Fedora/RHEL 6
        "/etc/ssl/ca-bundle.pem",                   // OpenSUSE
        "/etc/pki/tls/cacert.pem",                  // OpenELEC
        "/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem", // CentOS/RHEL 7
        "/etc/ssl/cert.pem",                        // Alpine Linux
    };

    /// <summary>
    /// List of default certificate directories in a container that should be appended to the custom certificate directories in
    /// <see cref="CustomCertificateAuthoritiesScope.Append"/> mode. Defaults to common Linux paths for CA certificates.
    /// </summary>
    public List<string> DefaultContainerCertificatesDirectoryPaths { get; } = new() { "/etc/ssl/certs", "/usr/local/share/ca-certificates", "/etc/pki/tls/certs" };

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
