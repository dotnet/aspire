// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="CertificateAuthorityCollection"/>.
/// </summary>
public static class CertificateAuthorityCollectionResourceExtensions
{
    /// <summary>
    /// Adds a new <see cref="CertificateAuthorityCollection"/> to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the certificate authority collection resource.</param>
    /// <returns>An <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/> instance.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> AddCertificateAuthorityCollection(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var resource = new CertificateAuthorityCollection(name);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Adds a certificate to the <see cref="CertificateAuthorityCollection.Certificates"/> collection.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</param>
    /// <param name="certificate">The certificate to add.</param>
    /// <returns>The updated <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> WithCertificate(this IResourceBuilder<CertificateAuthorityCollection> builder, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificate);

        builder.Resource.Certificates.Add(certificate);
        return builder;
    }

    /// <summary>
    /// Adds a collection of certificates to the <see cref="CertificateAuthorityCollection.Certificates"/> collection.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</param>
    /// <param name="certificates">The collection of certificates to add.</param>
    /// <returns>The updated <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> WithCertificates(this IResourceBuilder<CertificateAuthorityCollection> builder, X509Certificate2Collection certificates)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificates);

        builder.Resource.Certificates.AddRange(certificates);
        return builder;
    }

    /// <summary>
    /// Adds certificates from a PEM file to the <see cref="CertificateAuthorityCollection.Certificates"/> collection.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{CertificateAuthorityCollection}"/>.</param>
    /// <param name="filePath">The path to the PEM file.</param>
    /// <returns>The updated <see cref="IResourceBuilder{CertificateAuthorityCollection}"/>.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> WithCertificatesFromFile(this IResourceBuilder<CertificateAuthorityCollection> builder, string filePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(filePath);

        var certificates = new X509Certificate2Collection();
        certificates.ImportFromPemFile(filePath);
        builder.Resource.Certificates.AddRange(certificates);
        return builder;
    }
}