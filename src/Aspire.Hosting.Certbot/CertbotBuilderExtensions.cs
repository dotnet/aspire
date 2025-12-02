// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Certbot resources to the application model.
/// </summary>
public static class CertbotBuilderExtensions
{
    /// <summary>
    /// Adds a Certbot container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="domain">The parameter containing the domain name to obtain a certificate for.</param>
    /// <param name="email">The parameter containing the email address for certificate registration.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a Certbot container that obtains SSL/TLS certificates using the ACME protocol.
    /// Port 80 is published to the host for the ACME challenge.
    /// </para>
    /// <para>
    /// The certificates are stored in a shared volume named "letsencrypt" at /etc/letsencrypt.
    /// Other resources can mount this volume to access the certificates.
    /// </para>
    /// This version of the package defaults to the <inheritdoc cref="CertbotContainerImageTags.Tag"/> tag of the <inheritdoc cref="CertbotContainerImageTags.Image"/> container image.
    /// <example>
    /// Use in application host:
    /// <code lang="csharp">
    /// var domain = builder.AddParameter("domain");
    /// var email = builder.AddParameter("email");
    ///
    /// var certbot = builder.AddCertbot("certbot", domain, email);
    ///
    /// var myService = builder.AddContainer("myservice", "myimage")
    ///                        .WithVolume("letsencrypt", "/etc/letsencrypt");
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<CertbotResource> AddCertbot(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource> domain,
        IResourceBuilder<ParameterResource> email)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(email);

        var resource = new CertbotResource(name, domain.Resource, email.Resource);

        return builder.AddResource(resource)
            .WithImage(CertbotContainerImageTags.Image, CertbotContainerImageTags.Tag)
            .WithImageRegistry(CertbotContainerImageTags.Registry)
            .WithVolume(CertbotResource.CertificatesVolumeName, CertbotResource.CertificatesPath)
            .WithHttpEndpoint(port: 80, targetPort: 80, name: CertbotResource.HttpEndpointName)
            .WithExternalHttpEndpoints()
            .WithArgs(context =>
            {
                context.Args.Add("certonly");
                context.Args.Add("--standalone");
                context.Args.Add("--non-interactive");
                context.Args.Add("--agree-tos");
                context.Args.Add("-v");
                context.Args.Add("--keep-until-expiring");
                // Fix permissions so non-root containers can read the certs
                context.Args.Add("--deploy-hook");
                context.Args.Add("chmod -R 755 /etc/letsencrypt/live && chmod -R 755 /etc/letsencrypt/archive");
                context.Args.Add("--email");
                context.Args.Add(resource.EmailParameter);
                context.Args.Add("-d");
                context.Args.Add(resource.DomainParameter);
            });
    }

    /// <summary>
    /// Adds a reference to the certificates volume from a Certbot resource.
    /// </summary>
    /// <typeparam name="T">The type of the container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource that needs access to the certificates.</param>
    /// <param name="certbot">The Certbot resource builder.</param>
    /// <param name="mountPath">The path where the certificates volume should be mounted. Defaults to /etc/letsencrypt.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method adds the certificates volume to the specified container resource,
    /// allowing it to access SSL/TLS certificates obtained by Certbot.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// var domain = builder.AddParameter("domain");
    /// var email = builder.AddParameter("email");
    ///
    /// var certbot = builder.AddCertbot("certbot", domain, email);
    ///
    /// var yarp = builder.AddContainer("yarp", "myimage")
    ///                   .WithServerCertificates(certbot);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithServerCertificates<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<CertbotResource> certbot,
        string mountPath = CertbotResource.CertificatesPath) where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certbot);

        return builder.WithVolume(CertbotResource.CertificatesVolumeName, mountPath);
    }
}
