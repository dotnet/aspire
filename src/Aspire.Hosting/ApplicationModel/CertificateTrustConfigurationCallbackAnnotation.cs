// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that indicates a resource wants to manage how custom certificate trust is configured.
/// </summary>
/// <param name="callback">The callback used to customize certificate trust for the resource.</param>
public sealed class CertificateTrustConfigurationCallbackAnnotation(Func<CertificateTrustConfigurationCallbackAnnotationContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback to invoke to populate or modify the certificate authority collection.
    /// </summary>
    public Func<CertificateTrustConfigurationCallbackAnnotationContext, Task> Callback { get; } = callback ?? throw new ArgumentNullException(nameof(callback));
}

/// <summary>
/// Context provided to a <see cref="CertificateTrustConfigurationCallbackAnnotation"/> callback.
/// </summary>
public sealed class CertificateTrustConfigurationCallbackAnnotationContext
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
    /// value provider such as <see cref="CertificateBundlePath"/> or <see cref="CertificateDirectoriesPath"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// builder.AddContainer("my-resource", "my-image:latest")
    ///    .WithCertificateTrustConfigurationCallback(ctx =>
    ///    {
    ///        ctx.Arguments.Add("--use-system-ca");
    ///        return Task.CompletedTask;
    ///    });
    /// </code>
    /// </example>
    /// </remarks>
    public required List<object> Arguments { get; init; }

    /// <summary>
    /// Gets the environment variables required to configure certificate trust for the resource.
    /// The dictionary key is the environment variable name; the value can be either a string or a path
    /// value provider such as <see cref="CertificateBundlePath"/> or <see cref="CertificateDirectoriesPath"/>.
    /// By default the environment will always include an entry for `SSL_CERT_DIR` and may include `SSL_CERT_FILE` if
    /// <see cref="CertificateTrustScope.Override"/> or <see cref="CertificateTrustScope.System"/> is configured.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// builder.AddContainer("my-resource", "my-image:latest")
    ///     .WithCertificateTrustConfigurationCallback(ctx =>
    ///     {
    ///         ctx.EnvironmentVariables["MY_CUSTOM_CERT_VAR"] = ctx.CertificateBundlePath;
    ///         ctx.EnvironmentVariables["CERTS_DIR"] = ctx.CertificateDirectoriesPath;
    ///         return Task.CompletedTask;
    ///     });
    /// </code>
    /// </example>
    /// </remarks>
    public required Dictionary<string, object> EnvironmentVariables { get; init; }

    /// <summary>
    /// A value provider that will resolve to a path to a custom certificate bundle.
    /// </summary>
    public required ReferenceExpression CertificateBundlePath { get; init; }

    /// <summary>
    /// A value provider that will resolve to paths containing individual certificates.
    /// </summary>
    public required ReferenceExpression CertificateDirectoriesPath { get; init; }

    /// <summary>
    /// Gets the <see cref="CertificateTrustScope"/> for the resource.
    /// </summary>
    public required CertificateTrustScope Scope { get; init; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}
