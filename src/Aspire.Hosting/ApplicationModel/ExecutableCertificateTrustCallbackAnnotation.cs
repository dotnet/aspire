// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that indicates a resource wants to manage how custom certificate trust is configured when run as an executable.
/// </summary>
/// <param name="callback">The callback used to customize certificate trust for the resource.</param>
public sealed class ExecutableCertificateTrustCallbackAnnotation(Func<ExecutableCertificateTrustCallbackAnnotationContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback to invoke to populate or modify the certificate authority collection.
    /// </summary>
    public Func<ExecutableCertificateTrustCallbackAnnotationContext, Task> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}

/// <summary>
/// Context provided to a <see cref="ExecutableCertificateTrustCallbackAnnotation"/> callback.
/// </summary>
public sealed class ExecutableCertificateTrustCallbackAnnotationContext
{
    /// <summary>
    /// Gets the resource to which the annotation is applied.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// Gets the <see cref="CustomCertificateAuthoritiesScope"/> of trust for the resource.
    /// </summary>
    public required CustomCertificateAuthoritiesScope Scope { get; init; }

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
    /// </summary>
    public List<string> CertificatesDirectoryEnvironment { get; } = new();
}