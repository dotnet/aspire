// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that indicates a resource wants to manage how it needs to be configured to use a specific TLS certificate pair.
/// </summary>
/// <param name="callback">The callback used to configure the resource to use a specific TLS certificate pair.</param>
public sealed class CertificateKeyPairConfigurationCallbackAnnotation(Func<CertificateKeyPairConfigurationCallbackAnnotationContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback to invoke to configure the resource to use a specific TLS certificate key pair for HTTPS endpoints.
    /// </summary>
    public Func<CertificateKeyPairConfigurationCallbackAnnotationContext, Task> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}

/// <summary>
/// Context provided to a <see cref="CertificateKeyPairConfigurationCallbackAnnotation"/> callback.
/// </summary>
public sealed class CertificateKeyPairConfigurationCallbackAnnotationContext
{
    /// <summary>
    /// Gets the <see cref="DistributedApplicationExecutionContext"/> for this session.
    /// </summary>
    public required DistributedApplicationExecutionContext ExecutionContext { get; init; }

    /// <summary>
    /// Gets the resource to which the annotation is applied.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// Gets the command line arguments associated with the callback context. Values can be either a string or a path
    /// value provider such as <see cref="CertificatePath"/> or <see cref="KeyPath"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// builder.AddContainer("my-resource", "my-image:latest")
    ///    .WithCertificateKeyPairConfiguration(ctx =>
    ///    {
    ///        ctx.Arguments.Add("--certificate");
    ///        ctx.Arguments.Add(ctx.CertificatePath);
    ///        ctx.Arguments.Add("--key");
    ///        ctx.Arguments.Add(ctx.KeyPath);
    ///        return Task.CompletedTask;
    ///    });
    /// </code>
    /// </example>
    /// </remarks>
    public required List<object> Arguments { get; init; }

    /// <summary>
    /// Gets the environment variables required to configure a certificate key pair for the resource.
    /// The dictionary key is the environment variable name; the value can be either a string or a path
    /// value provider such as <see cref="CertificatePath"/> or <see cref="KeyPath"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// builder.AddContainer("my-resource", "my-image:latest")
    ///     .WithCertificateKeyPairConfiguration(ctx =>
    ///     {
    ///         ctx.EnvironmentVariables["Kestrel__Certificates__Path"] = ctx.CertificatePath;
    ///         ctx.EnvironmentVariables["Kestrel__Certificates__KeyPath"] = ctx.KeyPath;
    ///         return Task.CompletedTask;
    ///     });
    /// </code>
    /// </example>
    /// </remarks>
    public required Dictionary<string, object?> EnvironmentVariables { get; init; }

    /// <summary>
    /// A value provider that will resolve to a path to the certificate file.
    /// </summary>
    public required ReferenceExpression CertificatePath { get; init; }

    /// <summary>
    /// A value provider that will resolve to a path to the private key for the certificate.
    /// </summary>
    public required ReferenceExpression KeyPath { get; init; }

    /// <summary>
    /// A value provider that will resolve to a path to a PFX file for the key pair.
    /// </summary>
    public required ReferenceExpression PfxPath { get; init; }

    /// <summary>
    /// A value provider that will resolve to the password for the private key, if applicable.
    /// </summary>
    public required IValueProvider? Password { get; init; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}
