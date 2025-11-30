// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a Let's Encrypt Certbot container resource for obtaining and renewing SSL/TLS certificates.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="domain">A parameter containing the domain name to obtain a certificate for.</param>
/// <param name="email">A parameter containing the email address for Let's Encrypt registration and notifications.</param>
public class CertbotResource(string name, ParameterResource domain, ParameterResource email) : ContainerResource(name)
{
    internal const string HttpEndpointName = "http";
    internal const string CertificatesVolumeName = "letsencrypt";
    internal const string CertificatesPath = "/etc/letsencrypt";

    private EndpointReference? _httpEndpoint;

    /// <summary>
    /// Gets the HTTP endpoint for the Certbot ACME challenge server.
    /// </summary>
    public EndpointReference HttpEndpoint => _httpEndpoint ??= new(this, HttpEndpointName);

    /// <summary>
    /// Gets the parameter that contains the domain name.
    /// </summary>
    public ParameterResource DomainParameter { get; } = domain ?? throw new ArgumentNullException(nameof(domain));

    /// <summary>
    /// Gets the parameter that contains the email address for Let's Encrypt registration.
    /// </summary>
    public ParameterResource EmailParameter { get; } = email ?? throw new ArgumentNullException(nameof(email));

    /// <summary>
    /// Gets an expression representing the path to the SSL/TLS certificate (fullchain.pem) for the domain.
    /// </summary>
    /// <remarks>
    /// The certificate path follows the Let's Encrypt convention: <c>/etc/letsencrypt/live/{domain}/fullchain.pem</c>.
    /// This property returns a <see cref="ReferenceExpression"/> that resolves to the actual path at runtime
    /// based on the domain parameter value.
    /// </remarks>
    public ReferenceExpression CertificatePath =>
        ReferenceExpression.Create($"{CertificatesPath}/live/{DomainParameter}/fullchain.pem");

    /// <summary>
    /// Gets an expression representing the path to the private key (privkey.pem) for the domain.
    /// </summary>
    /// <remarks>
    /// The private key path follows the Let's Encrypt convention: <c>/etc/letsencrypt/live/{domain}/privkey.pem</c>.
    /// This property returns a <see cref="ReferenceExpression"/> that resolves to the actual path at runtime
    /// based on the domain parameter value.
    /// </remarks>
    public ReferenceExpression PrivateKeyPath =>
        ReferenceExpression.Create($"{CertificatesPath}/live/{DomainParameter}/privkey.pem");
}
