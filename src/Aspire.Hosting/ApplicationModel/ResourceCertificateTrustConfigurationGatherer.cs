// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECERTIFICATES001

using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers certificate trust configuration for resources that require it.
/// </summary>
internal class ResourceCertificateTrustConfigurationGatherer : IResourceConfigurationGatherer
{
    private readonly Func<CertificateTrustScope, CertificateTrustConfigurationContext> _configContextFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="ResourceCertificateTrustConfigurationGatherer"/>.
    /// </summary>
    /// <param name="configContextFactory">A factory for configuring certificate trust configuration properties.</param>
    public ResourceCertificateTrustConfigurationGatherer(Func<CertificateTrustScope, CertificateTrustConfigurationContext> configContextFactory)
    {
        _configContextFactory = configContextFactory;
    }

    /// <inheritdoc/>
    public async ValueTask GatherAsync(IResourceConfigurationGathererContext context, CancellationToken cancellationToken = default)
    {
        var developerCertificateService = context.ExecutionContext.ServiceProvider.GetRequiredService<IDeveloperCertificateService>();
        var trustDevCert = developerCertificateService.TrustCertificate;

        // Add additional certificate trust configuration metadata
        var metadata = new CertificateTrustConfigurationMetadata();
        context.AddMetadata(metadata);

        metadata.Scope = CertificateTrustScope.Append;
        var certificates = new X509Certificate2Collection();
        if (context.Resource.TryGetLastAnnotation<CertificateAuthorityCollectionAnnotation>(out var caAnnotation))
        {
            foreach (var certCollection in caAnnotation.CertificateAuthorityCollections)
            {
                certificates.AddRange(certCollection.Certificates);
            }

            trustDevCert = caAnnotation.TrustDeveloperCertificates.GetValueOrDefault(trustDevCert);
            metadata.Scope = caAnnotation.Scope.GetValueOrDefault(metadata.Scope);
        }

        if (metadata.Scope == CertificateTrustScope.None)
        {
            // No certificate trust configuration to apply
            return;
        }

        if (metadata.Scope == CertificateTrustScope.System)
        {
            // Read the system root certificates and add them to the collection
            certificates.AddRootCertificates();
        }

        if (context.ExecutionContext.IsRunMode && trustDevCert)
        {
            foreach (var cert in developerCertificateService.Certificates)
            {
                certificates.Add(cert);
            }
        }

        metadata.Certificates.AddRange(certificates);

        if (!metadata.Certificates.Any())
        {
            // No certificates to configure
            context.ResourceLogger.LogInformation("No custom certificate authorities to configure for '{ResourceName}'. Default certificate authority trust behavior will be used.", context.Resource.Name);
            return;
        }

        var configurationContext = _configContextFactory(metadata.Scope);

        // Apply default OpenSSL environment configuration for certificate trust
        context.EnvironmentVariables["SSL_CERT_DIR"] = configurationContext.CertificateDirectoriesPath;

        if (metadata.Scope != CertificateTrustScope.Append)
        {
            context.EnvironmentVariables["SSL_CERT_FILE"] = configurationContext.CertificateBundlePath;
        }

        var callbackContext = new CertificateTrustConfigurationCallbackAnnotationContext
        {
            ExecutionContext = context.ExecutionContext,
            Resource = context.Resource,
            Scope = metadata.Scope,
            CertificateBundlePath = configurationContext.CertificateBundlePath,
            CertificateDirectoriesPath = configurationContext.CertificateDirectoriesPath,
            Arguments = context.Arguments,
            EnvironmentVariables = context.EnvironmentVariables,
            CancellationToken = cancellationToken,
        };

        if (context.Resource.TryGetAnnotationsOfType<CertificateTrustConfigurationCallbackAnnotation>(out var callbacks))
        {
            foreach (var callback in callbacks)
            {
                await callback.Callback(callbackContext).ConfigureAwait(false);
            }
        }

        if (metadata.Scope == CertificateTrustScope.System)
        {
            context.ResourceLogger.LogInformation("Resource '{ResourceName}' has a certificate trust scope of '{Scope}'. Automatically including system root certificates in the trusted configuration.", context.Resource.Name, Enum.GetName(metadata.Scope));
        }

        return;
    }
}

/// <summary>
/// Metadata about the resource certificate trust configuration.
/// </summary>
public class CertificateTrustConfigurationMetadata : IResourceConfigurationMetadata
{
    /// <summary>
    /// The certificate trust scope for the resource.
    /// </summary>
    public CertificateTrustScope Scope { get; internal set; }

    /// <summary>
    /// The collection of certificates to trust.
    /// </summary>
    public X509Certificate2Collection Certificates { get; } = new();
}

/// <summary>
/// Context for configuring certificate trust configuration properties.
/// </summary>
public class CertificateTrustConfigurationContext
{
    /// <summary>
    /// The path to the certificate bundle file in the resource context (e.g., container filesystem).
    /// </summary>
    public required ReferenceExpression CertificateBundlePath { get; init; }

    /// <summary>
    /// The path(s) to the certificate directories in the resource context (e.g., container filesystem).
    /// </summary>
    public required ReferenceExpression CertificateDirectoriesPath { get; init; }
}