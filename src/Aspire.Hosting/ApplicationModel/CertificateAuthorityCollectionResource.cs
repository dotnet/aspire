// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a collection of certificate authorities within the application model.
/// </summary>
/// <remarks>
/// This class implements <see cref="IResourceWithoutLifetime"/> and provides access to
/// the name and annotations associated with the certificate authority collection.
/// </remarks>
public class CertificateAuthorityCollection : Resource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateAuthorityCollection"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the certificate authority collection resource.</param>
    public CertificateAuthorityCollection(string name) : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);
    }

    /// <summary>
    /// Gets the <see cref="X509Certificate2Collection"/> of certificates for this resource.
    /// </summary>
    public X509Certificate2Collection Certificates { get; } = new();
}