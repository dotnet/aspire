// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECERTIFICATES001

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers certificate trust configuration for resources that require it.
/// </summary>
internal class CertificateTrustExecutionConfigurationGatherer : IExecutionConfigurationGatherer
{
    private readonly Func<CertificateTrustScope, CertificateTrustExecutionConfigurationContext> _configContextFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="CertificateTrustExecutionConfigurationGatherer"/>.
    /// </summary>
    /// <param name="configContextFactory">A factory for configuring certificate trust configuration properties.</param>
    public CertificateTrustExecutionConfigurationGatherer(Func<CertificateTrustScope, CertificateTrustExecutionConfigurationContext> configContextFactory)
    {
        _configContextFactory = configContextFactory;
    }

    /// <inheritdoc/>
    public async ValueTask GatherAsync(IExecutionConfigurationGathererContext context, IResource resource, ILogger resourceLogger, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var developerCertificateService = executionContext.ServiceProvider.GetRequiredService<IDeveloperCertificateService>();
        var trustDevCert = developerCertificateService.TrustCertificate;

        // Add additional certificate trust configuration metadata
        var additionalData = new CertificateTrustExecutionConfigurationData();
        context.AddAdditionalData(additionalData);

        additionalData.Scope = CertificateTrustScope.Append;
        var certificates = new X509Certificate2Collection();
        if (resource.TryGetLastAnnotation<CertificateAuthorityCollectionAnnotation>(out var caAnnotation))
        {
            foreach (var certCollection in caAnnotation.CertificateAuthorityCollections)
            {
                certificates.AddRange(certCollection.Certificates);
            }

            trustDevCert = caAnnotation.TrustDeveloperCertificates.GetValueOrDefault(trustDevCert);
            additionalData.Scope = caAnnotation.Scope.GetValueOrDefault(additionalData.Scope);
        }

        if (additionalData.Scope == CertificateTrustScope.None)
        {
            // No certificate trust configuration to apply
            return;
        }

        if (additionalData.Scope == CertificateTrustScope.System)
        {
            // Read the system root certificates and add them to the collection
            certificates.AddRootCertificates();
        }

        if (executionContext.IsRunMode && trustDevCert)
        {
            foreach (var cert in developerCertificateService.Certificates)
            {
                certificates.Add(cert);
            }
        }

        additionalData.Certificates.AddRange(certificates);

        if (!additionalData.Certificates.Any())
        {
            // No certificates to configure
            resourceLogger.LogInformation("No custom certificate authorities to configure for '{ResourceName}'. Default certificate authority trust behavior will be used.", resource.Name);
            return;
        }

        var configurationContext = _configContextFactory(additionalData.Scope);

        // Set up tracked PKCS#12 reference so we can detect if a callback references it
        additionalData.Pkcs12BundlePathReference = configurationContext.Pkcs12BundlePath;
        additionalData.Pkcs12BundlePassword = configurationContext.Pkcs12BundlePassword;

        // Apply default OpenSSL environment configuration for certificate trust
        context.EnvironmentVariables["SSL_CERT_DIR"] = configurationContext.CertificateDirectoriesPath;

        if (additionalData.Scope != CertificateTrustScope.Append)
        {
            context.EnvironmentVariables["SSL_CERT_FILE"] = configurationContext.CertificateBundlePath;
        }

        var callbackContext = new CertificateTrustConfigurationCallbackAnnotationContext
        {
            ExecutionContext = executionContext,
            Resource = resource,
            Scope = additionalData.Scope,
            CertificateBundlePath = configurationContext.CertificateBundlePath,
            CertificateDirectoriesPath = configurationContext.CertificateDirectoriesPath,
            // Must use the tracked reference to ensure proper tracking of usage
            Pkcs12BundlePath = additionalData.Pkcs12BundlePathReference!,
            Pkcs12BundlePassword = additionalData.Pkcs12BundlePassword,
            Arguments = context.Arguments,
            EnvironmentVariables = context.EnvironmentVariables,
            CancellationToken = cancellationToken,
        };

        if (resource.TryGetAnnotationsOfType<CertificateTrustConfigurationCallbackAnnotation>(out var callbacks))
        {
            foreach (var callback in callbacks)
            {
                await callback.Callback(callbackContext).ConfigureAwait(false);
            }
        }

        if (additionalData.Scope == CertificateTrustScope.System)
        {
            resourceLogger.LogInformation("Resource '{ResourceName}' has a certificate trust scope of '{Scope}'. Automatically including system root certificates in the trusted configuration.", resource.Name, Enum.GetName(additionalData.Scope));
        }

    }
}

/// <summary>
/// Metadata about the resource certificate trust configuration.
/// </summary>
[Experimental("ASPIRECERTIFICATES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class CertificateTrustExecutionConfigurationData : IExecutionConfigurationData
{
    private ReferenceExpression? _pkcs12BundlePathReference;
    private TrackedReference? _trackedPkcs12BundlePathReference;

    /// <summary>
    /// The certificate trust scope for the resource.
    /// </summary>
    public CertificateTrustScope Scope { get; internal set; }

    /// <summary>
    /// The collection of certificates to trust.
    /// </summary>
    public X509Certificate2Collection Certificates { get; } = new();

    /// <summary>
    /// Reference expression that will resolve to the path of the PKCS#12 trust store bundle.
    /// </summary>
    public ReferenceExpression? Pkcs12BundlePathReference
    {
        get
        {
            return _pkcs12BundlePathReference;
        }
        internal set
        {
            if (value is null)
            {
                _trackedPkcs12BundlePathReference = null;
                _pkcs12BundlePathReference = null;
            }
            else
            {
                _trackedPkcs12BundlePathReference = new TrackedReference(value);
                _pkcs12BundlePathReference = ReferenceExpression.Create($"{_trackedPkcs12BundlePathReference}");
            }
        }
    }

    /// <summary>
    /// Indicates whether the PKCS#12 bundle path was actually referenced in the resource configuration.
    /// </summary>
    public bool IsPkcs12BundlePathReferenced => _trackedPkcs12BundlePathReference?.WasResolved ?? false;

    /// <summary>
    /// The password for the PKCS#12 trust store bundle.
    /// </summary>
    public string Pkcs12BundlePassword { get; internal set; } = string.Empty;

    private class TrackedReference : IValueProvider, IManifestExpressionProvider
    {
        private readonly ReferenceExpression _reference;

        public TrackedReference(ReferenceExpression reference)
        {
            _reference = reference;
        }

        public bool WasResolved { get; internal set; }

        public string ValueExpression => _reference.ValueExpression;

        public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
        {
            WasResolved = true;

            return _reference.GetValueAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Context for configuring certificate trust configuration properties.
/// </summary>
[Experimental("ASPIRECERTIFICATES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class CertificateTrustExecutionConfigurationContext
{
    /// <summary>
    /// The path to the PEM certificate bundle file in the resource context (e.g., container filesystem).
    /// </summary>
    public required ReferenceExpression CertificateBundlePath { get; init; }

    /// <summary>
    /// The path(s) to the certificate directories in the resource context (e.g., container filesystem).
    /// </summary>
    public required ReferenceExpression CertificateDirectoriesPath { get; init; }

    /// <summary>
    /// The path to the PKCS#12 trust store bundle file in the resource context (e.g., container filesystem).
    /// Only generated if a resource's certificate trust configuration callback references this path.
    /// </summary>
    public required ReferenceExpression Pkcs12BundlePath { get; init; }

    /// <summary>
    /// The password for the PKCS#12 trust store bundle. Defaults to an empty string.
    /// </summary>
    public string Pkcs12BundlePassword { get; init; } = string.Empty;
}
