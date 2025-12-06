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
    /// By default, no challenge method is configured. Use <see cref="WithHttp01Challenge"/> or other challenge methods to configure how certificates are obtained.
    /// </para>
    /// <para>
    /// The certificates are stored in a shared volume named "letsencrypt" at /etc/letsencrypt.
    /// Other resources can mount this volume to access the certificates.
    /// </para>
    /// <para>
    /// Certificate permissions are automatically set to allow non-root containers to read them.
    /// </para>
    /// This version of the package defaults to the <inheritdoc cref="CertbotContainerImageTags.Tag"/> tag of the <inheritdoc cref="CertbotContainerImageTags.Image"/> container image.
    /// <example>
    /// Use in application host:
    /// <code lang="csharp">
    /// var domain = builder.AddParameter("domain");
    /// var email = builder.AddParameter("email");
    ///
    /// var certbot = builder.AddCertbot("certbot", domain, email)
    ///     .WithHttp01Challenge();
    ///
    /// var myService = builder.AddContainer("myservice", "myimage")
    ///                        .WithCertbotCertificate(certbot);
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
            .WithArgs(context =>
            {
                var certbotResource = (CertbotResource)context.Resource;

                // Only add args if a challenge method is configured
                if (certbotResource.ChallengeMethod is null)
                {
                    return;
                }

                // Common arguments for all challenge methods
                context.Args.Add("certonly");
                context.Args.Add("--non-interactive");
                context.Args.Add("--agree-tos");
                context.Args.Add("-v");
                context.Args.Add("--keep-until-expiring");

                // Challenge-specific arguments
                switch (certbotResource.ChallengeMethod)
                {
                    case CertbotChallengeMethod.Http01:
                        context.Args.Add("--standalone");
                        break;
                }

                // Always set permissions to allow non-root containers to read certificates
                context.Args.Add("--deploy-hook");
                context.Args.Add("chmod -R 755 /etc/letsencrypt/live && chmod -R 755 /etc/letsencrypt/archive");

                // Email and domain arguments
                context.Args.Add("--email");
                context.Args.Add(certbotResource.EmailParameter);
                context.Args.Add("-d");
                context.Args.Add(certbotResource.DomainParameter);
            });
    }

    /// <summary>
    /// Configures Certbot to use the HTTP-01 challenge for domain validation.
    /// </summary>
    /// <param name="builder">The Certbot resource builder.</param>
    /// <param name="port">The host port to publish for the HTTP-01 challenge. Defaults to 80.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The HTTP-01 challenge requires Certbot to be accessible on port 80 from the internet.
    /// This method configures the container to listen on the specified port and sets up
    /// the standalone mode for ACME challenge validation.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// var certbot = builder.AddCertbot("certbot", domain, email)
    ///     .WithHttp01Challenge(port: 8080);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<CertbotResource> WithHttp01Challenge(
        this IResourceBuilder<CertbotResource> builder,
        int? port = 80)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.ChallengeMethod = CertbotChallengeMethod.Http01;

        return builder
            .WithHttpEndpoint(port: port, targetPort: 80, name: CertbotResource.HttpEndpointName)
            .WithExternalHttpEndpoints();
    }

    /// <summary>
    /// Configures the container to use SSL/TLS certificates from a Certbot resource.
    /// </summary>
    /// <typeparam name="T">The type of the container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource that needs access to the certificates.</param>
    /// <param name="certbot">The Certbot resource builder.</param>
    /// <param name="mountPath">The path where the certificates volume should be mounted. Defaults to /etc/letsencrypt.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method mounts the certificates volume and ensures the container waits for the Certbot
    /// resource to complete certificate acquisition before starting.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Do not use this method together with <c>WithHttpsCertificate</c>
    /// or <c>WithHttpsCertificateConfiguration</c>
    /// at runtime, as they will conflict. However, you can use Certbot in publish mode while using the other methods in development mode
    /// by wrapping the Certbot configuration in an <c>ExecutionContext.IsPublishMode</c> check.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// var domain = builder.AddParameter("domain");
    /// var email = builder.AddParameter("email");
    ///
    /// var certbot = builder.AddCertbot("certbot", domain, email)
    ///     .WithHttp01Challenge();
    ///
    /// var yarp = builder.AddContainer("yarp", "myimage")
    ///                   .WithCertbotCertificate(certbot);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithCertbotCertificate<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<CertbotResource> certbot,
        string mountPath = CertbotResource.CertificatesPath) where T : ContainerResource, IResourceWithWaitSupport
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certbot);

        return builder
            .WithVolume(CertbotResource.CertificatesVolumeName, mountPath)
            .WaitForCompletion(certbot);
    }
}
