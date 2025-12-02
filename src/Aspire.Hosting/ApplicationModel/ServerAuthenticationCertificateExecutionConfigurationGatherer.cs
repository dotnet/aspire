// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECERTIFICATES001

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A gatherer that configures server authentication certificate configuration for a resource.
/// </summary>
internal class ServerAuthenticationCertificateExecutionConfigurationGatherer : IResourceExecutionConfigurationGatherer
{
    private readonly Func<X509Certificate2, ServerAuthenticationCertificateExecutionConfigurationContext> _configContextFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="ServerAuthenticationCertificateExecutionConfigurationGatherer"/>.
    /// </summary>
    /// <param name="configContextFactory">A factory for configuring server authentication certificate configuration properties.</param>
    public ServerAuthenticationCertificateExecutionConfigurationGatherer(Func<X509Certificate2, ServerAuthenticationCertificateExecutionConfigurationContext> configContextFactory)
    {
        _configContextFactory = configContextFactory;
    }

    /// <inheritdoc/>
    public async ValueTask GatherAsync(IResourceExecutionConfigurationGathererContext context, IResource resource, ILogger resourceLogger, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        var effectiveAnnotation = new ServerAuthenticationCertificateAnnotation();
        if (resource.TryGetLastAnnotation<ServerAuthenticationCertificateAnnotation>(out var annotation))
        {
            effectiveAnnotation = annotation;
        }

        X509Certificate2? certificate = effectiveAnnotation.Certificate;
        if (certificate is null)
        {
            var developerCertificateService = executionContext.ServiceProvider.GetRequiredService<IDeveloperCertificateService>();
            if (effectiveAnnotation.UseDeveloperCertificate.GetValueOrDefault(developerCertificateService.UseForServerAuthentication))
            {
                certificate = developerCertificateService.Certificates.FirstOrDefault();
            }
        }

        if (certificate is null)
        {
            return;
        }

        var configurationContext = _configContextFactory(certificate);

        var additionalData = new ServerAuthenticationCertificateExecutionConfigurationData
        {
            Certificate = certificate,
            KeyPathReference = configurationContext.KeyPath,
            PfxPathReference = configurationContext.PfxPath,
            Password = effectiveAnnotation.Password is not null ? await effectiveAnnotation.Password.GetValueAsync(cancellationToken).ConfigureAwait(false) : null,
        };
        context.AddAdditionalData(additionalData);

        var callbackContext = new ServerAuthenticationCertificateConfigurationCallbackAnnotationContext
        {
            ExecutionContext = executionContext,
            Resource = resource,
            Arguments = context.Arguments,
            EnvironmentVariables = context.EnvironmentVariables,
            CertificatePath = configurationContext.CertificatePath,
            // Must use the metadata references to ensure proper tracking of usage
            KeyPath = additionalData.KeyPathReference,
            PfxPath = additionalData.PfxPathReference,
            Password = effectiveAnnotation.Password,
            CancellationToken = cancellationToken,
        };

        foreach (var callback in resource.TryGetAnnotationsOfType<ServerAuthenticationCertificateConfigurationCallbackAnnotation>(out var callbacks) ? callbacks : Enumerable.Empty<ServerAuthenticationCertificateConfigurationCallbackAnnotation>())
        {
            await callback.Callback(callbackContext).ConfigureAwait(false);
        }

    }
}

/// <summary>
/// Metadata for server authentication certificate configuration.
/// </summary>
public class ServerAuthenticationCertificateExecutionConfigurationData : IResourceExecutionConfigurationData
{
    private ReferenceExpression? _keyPathReference;
    private TrackedReference? _trackedKeyPathReference;

    private ReferenceExpression? _pfxPathReference;
    private TrackedReference? _trackedPfxPathReference;

    /// <summary>
    /// The server authentication certificate for the resource, if any.
    /// </summary>
    public required X509Certificate2 Certificate { get; init; }

    /// <summary>
    /// Reference expression that will resolve to the path of the server authentication certificate key in PEM format.
    /// </summary>
    public required ReferenceExpression KeyPathReference
    {
        get
        {
            return _keyPathReference!;
        }
        set
        {
            _trackedKeyPathReference = new TrackedReference(value);
            _keyPathReference = ReferenceExpression.Create($"{_trackedKeyPathReference}");
        }
    }

    /// <summary>
    /// Indicates whether the key path was actually referenced in the resource configuration.
    /// </summary>
    public bool IsKeyPathReferenced => _trackedKeyPathReference?.WasResolved ?? false;

    /// <summary>
    /// Reference expression that will resolve to the path of the server authentication certificate in PFX format.
    /// </summary>
    public required ReferenceExpression PfxPathReference
    {
        get
        {
            return _pfxPathReference!;
        }
        set
        {
            _trackedPfxPathReference = new TrackedReference(value);
            _pfxPathReference = ReferenceExpression.Create($"{_trackedPfxPathReference}");
        }
    }

    /// <summary>
    /// Indicates whether the PFX path was actually referenced in the resource configuration.
    /// </summary>
    public bool IsPfxPathReferenced => _trackedPfxPathReference?.WasResolved ?? false;

    /// <summary>
    /// The passphrase for the server authentication certificate, if any.
    /// </summary>
    public string? Password { get; init; }

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
/// Configuration context for server authentication certificate configuration.
/// </summary>
public class ServerAuthenticationCertificateExecutionConfigurationContext
{
    /// <summary>
    /// Expression that will resolve to the path of the server authentication certificate in PEM format.
    /// For containers this will be a path inside the container.
    /// </summary>
    public required ReferenceExpression CertificatePath { get; init; }

    /// <summary>
    /// Expression that will resolve to the path of the server authentication certificate key in PEM format.
    /// For containers this will be a path inside the container.
    /// </summary>
    public required ReferenceExpression KeyPath { get; init; }

    /// <summary>
    /// Expression that will resolve to the path of the server authentication certificate in PFX format.
    /// For containers this will be a path inside the container.
    /// </summary>
    public required ReferenceExpression PfxPath { get; init; }
}