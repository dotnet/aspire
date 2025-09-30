// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Context information for deployment image tag callback functions.
/// </summary>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class DeploymentImageTagCallbackAnnotationContext
{
    /// <summary>
    /// Gets the resource associated with the deployment image tag.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// Gets the cancellation token associated with the callback context.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Represents an annotation for a deployment-specific tag that can be applied to resources during deployment.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class DeploymentImageTagCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentImageTagCallbackAnnotation"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The synchronous callback that returns the deployment tag name.</param>
    public DeploymentImageTagCallbackAnnotation(Func<DeploymentImageTagCallbackAnnotationContext, string> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = context => Task.FromResult(callback(context));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentImageTagCallbackAnnotation"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="callback">The asynchronous callback that returns the deployment tag name.</param>
    public DeploymentImageTagCallbackAnnotation(Func<DeploymentImageTagCallbackAnnotationContext, Task<string>> callback)
    {
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Gets the callback that returns the deployment tag name.
    /// </summary>
    public Func<DeploymentImageTagCallbackAnnotationContext, Task<string>> Callback { get; }
}
