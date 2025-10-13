// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Defines the scope of custom certificate authorities for a resource. The default is <see cref="Append"/>.
/// </summary>
public enum CustomCertificateAuthoritiesScope
{
    /// <summary>
    /// Append the specified certificate authorities to the default set of trusted CAs for a resource.
    /// </summary>
    Append,
    /// <summary>
    /// Replace the default set of trusted CAs for a resource with the specified certificate authorities.
    /// </summary>
    Override,
}

/// <summary>
/// An annotation that indicates a resource is referencing a certificate authority collection.
/// </summary>
public sealed class CertificateAuthorityCollectionAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the <see cref="global::CertificateAuthorityCollection"/> that is being referenced.
    /// </summary>
    public List<CertificateAuthorityCollection> CertificateAuthorityCollections { get; internal set; } = new List<CertificateAuthorityCollection>();

    /// <summary>
    /// Gets a value indicating whether platform developer certificates should be considered trusted.
    /// </summary>
    public bool? TrustDeveloperCertificates { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the resource should attempt to override its default CA trust behavior in
    /// favor of the provided certificates (not all resources will support this).
    /// </summary>
    public CustomCertificateAuthoritiesScope? Scope { get; internal set; }
}
