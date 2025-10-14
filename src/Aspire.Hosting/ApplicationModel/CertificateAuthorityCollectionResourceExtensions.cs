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
    /// Adds a collection of certificates to the <see cref="CertificateAuthorityCollection.Certificates"/> collection.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</param>
    /// <param name="certificates">The collection of certificates to add.</param>
    /// <returns>The updated <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> WithCertificates(this IResourceBuilder<CertificateAuthorityCollection> builder, IEnumerable<X509Certificate2> certificates)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificates);

        builder.Resource.Certificates.AddRange(certificates.ToArray());
        return builder;
    }

    /// <summary>
    /// Adds certificates from a certificate store to the <see cref="CertificateAuthorityCollection.Certificates"/> collection.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</param>
    /// <param name="storeName">The name of the certificate store.</param>
    /// <param name="storeLocation">The location of the certificate store.</param>
    /// <param name="filter">An optional filter to apply to the certificates.</param>
    /// <returns>The updated <see cref="IResourceBuilder{CertificateAuthorityCollectionResource}"/>.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> WithCertificatesFromStore(this IResourceBuilder<CertificateAuthorityCollection> builder, StoreName storeName, StoreLocation storeLocation, Func<X509Certificate2, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);
        var certificates = store.Certificates as IEnumerable<X509Certificate2>;
        if (filter != null)
        {
            certificates = certificates.Where(filter);
        }
        builder.Resource.Certificates.AddRange(certificates.ToArray());
        return builder;
    }

    /// <summary>
    /// Adds certificates from a PEM file to the <see cref="CertificateAuthorityCollection.Certificates"/> collection.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{CertificateAuthorityCollection}"/>.</param>
    /// <param name="pemFilePath">The path to the PEM file.</param>
    /// <returns>The updated <see cref="IResourceBuilder{CertificateAuthorityCollection}"/>.</returns>
    public static IResourceBuilder<CertificateAuthorityCollection> WithCertificatesFromFile(this IResourceBuilder<CertificateAuthorityCollection> builder, string pemFilePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(pemFilePath);

        var certificates = new X509Certificate2Collection();
        certificates.ImportFromPemFile(pemFilePath);
        builder.Resource.Certificates.AddRange(certificates);
        return builder;
    }
}