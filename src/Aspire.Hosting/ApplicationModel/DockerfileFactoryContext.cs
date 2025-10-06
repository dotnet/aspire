// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides context for Dockerfile factory functions.
/// </summary>
public sealed class DockerfileFactoryContext
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for resolving dependencies required by Dockerfile factory functions.
    /// <para>
    /// The service provider typically contains services such as <c>IHostEnvironment</c>, <c>ILogger</c>, and configuration objects relevant to the application model.
    /// Factory functions can use this provider to obtain required services for generating Dockerfiles.
    /// </para>
    /// <example>
    /// <code>
    /// var logger = context.Services.GetRequiredService&lt;ILogger&gt;();
    /// </code>
    /// </example>
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
