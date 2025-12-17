// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Defines the scope of custom certificate authorities for a resource. The default scope for most resources
/// is <see cref="Append"/>, but some resources may choose to override this default behavior.
/// </summary>
public enum CertificateTrustScope
{
    /// <summary>
    /// Disable all custom certificate authority configuration for a resource. This indicates that the resource
    /// should use its default certificate authority trust behavior without modification.
    /// </summary>
    None,
    /// <summary>
    /// Append the specified certificate authorities to the default set of trusted CAs for a resource. Not all
    /// resources support this mode, in which case custom certificate authorities may not be applied. In that case,
    /// consider using <see cref="Override"/> or <see cref="System"/> instead. This is the default mode unless
    /// otherwise specified.
    /// </summary>
    Append,
    /// <summary>
    /// Replace the default set of trusted CAs for a resource with the specified certificate authorities. This mode
    /// indicates that only the provided custom certificate authorities should be considered trusted by the resource.
    /// </summary>
    Override,
    /// <summary>
    /// Attempt to configure the resource to trust the default system certificate authorities in addition to
    /// any configured custom certificate trust. This mode is useful for resources that don't otherwise
    /// allow appending to their default trusted certificate authorities but do allow overriding the set
    /// of trusted certificates (e.g. Python, Rust, etc.).
    /// </summary>
    System,
}

/// <summary>
/// An annotation that indicates a resource is referencing a certificate authority collection.
/// </summary>
public sealed class CertificateAuthorityCollectionAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Creates a new <see cref="CertificateAuthorityCollectionAnnotation"/> instance from one or more merged <see cref="CertificateAuthorityCollectionAnnotation"/> instances.
    /// Certificate authority collections from all provided instances will be combined into the new instance, while the last values for <see cref="TrustDeveloperCertificates"/>
    /// and <see cref="Scope"/> will be used, with null values being ignored (previous value if any will be retained).
    /// </summary>
    /// <param name="other">The other <see cref="CertificateAuthorityCollectionAnnotation"/>s that will be merged to create the new instance.</param>
    /// <returns>A merged copy of the provided <see cref="CertificateAuthorityCollectionAnnotation"/> instances</returns>
    public static CertificateAuthorityCollectionAnnotation From(params CertificateAuthorityCollectionAnnotation[] other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var annotation = new CertificateAuthorityCollectionAnnotation();
        foreach (var item in other)
        {
            annotation.CertificateAuthorityCollections.AddRange(item.CertificateAuthorityCollections);
            if (item.TrustDeveloperCertificates.HasValue)
            {
                annotation.TrustDeveloperCertificates = item.TrustDeveloperCertificates;
            }
            if (item.Scope.HasValue)
            {
                annotation.Scope = item.Scope;
            }
        }

        return annotation;
    }

    /// <summary>
    /// Gets the <see cref="CertificateAuthorityCollection"/> that is being referenced.
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
    public CertificateTrustScope? Scope { get; internal set; }
}
