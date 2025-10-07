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
/// <param name="certificateAuthorityCollections">Additional certificate authority collections to include.</param>
/// <param name="trustDeveloperCertificates">A value indicating whether platform developer certificates should be considered trusted.</param>
/// <param name="scope">The <see cref="CustomCertificateAuthoritiesScope"/> of the custom certificate authorities.</param>
public sealed class CertificateAuthorityCollectionAnnotation(CertificateAuthorityCollection[]? certificateAuthorityCollections = null, bool? trustDeveloperCertificates = null, CustomCertificateAuthoritiesScope? scope = null) : IResourceAnnotation
{
    /// <summary>
    /// Gets the <see cref="global::CertificateAuthorityCollection"/> that is being referenced.
    /// </summary>
    public CertificateAuthorityCollection[] CertificateAuthorityCollections { get; internal set; } = certificateAuthorityCollections ?? [];

    /// <summary>
    /// Gets a value indicating whether platform developer certificates should be considered trusted.
    /// </summary>
    public bool? TrustDeveloperCertificates { get; internal set; } = trustDeveloperCertificates;

    /// <summary>
    /// Gets a value indicating whether the resource should attempt to override its default CA trust behavior in
    /// favor of the provided certificates (not all resources will support this).
    /// </summary>
    public CustomCertificateAuthoritiesScope? Scope { get; internal set; } = scope;
}