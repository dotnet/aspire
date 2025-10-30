// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that associates a certificate pair (public/private key) with a resource.
/// </summary>
public sealed class CertificateKeyPairAnnotation : IResourceAnnotation
{
    private X509Certificate2? _certificate;
    private bool _useDeveloperCertificate;

    /// <summary>
    /// Sets an <see cref="X509Certificate2"/> instance associated with this annotation.
    /// If a certificate is provided, it must have a private key; otherwise, an <see cref="ArgumentException"/> is thrown when setting the value.
    /// </summary>
    public X509Certificate2? Certificate
    {
        get => _certificate;
        init
        {
            if (value != null && _useDeveloperCertificate)
            {
                throw new ArgumentException("Cannot set both UseDeveloperCertificate and Certificate properties.", nameof(value));
            }

            if (value?.HasPrivateKey == false)
            {
                throw new ArgumentException("The provided certificate must have a private key.", nameof(value));
            }

            try
            {
                if (value != null && value.PublicKey == null)
                {
                    throw new ArgumentException("The provided certificate must have a valid public key.", nameof(value));
                }
            }
            catch (CryptographicException ex)
            {
                throw new ArgumentException("The provided certificate is invalid.", nameof(value), ex);
            }

            _certificate = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the resource should use a platform developer certificate for it's key pair.
    /// </summary>
    public bool UseDeveloperCertificate
    {
        get => _useDeveloperCertificate;
        init
        {
            _useDeveloperCertificate = value;
            if (value && _certificate != null)
            {
                throw new ArgumentException("Cannot set both UseDeveloperCertificate and Certificate properties.", nameof(value));
            }
        }
    }

    /// <summary>
    /// Gets or sets a parameter resource that contains the password for the private key of the certificate.
    /// </summary>
    public ParameterResource? Password { get; init; }
}