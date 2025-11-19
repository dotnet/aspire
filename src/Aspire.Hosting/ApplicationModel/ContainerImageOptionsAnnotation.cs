// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Context information for container image options callback functions.
/// </summary>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class ContainerImageOptionsCallbackAnnotationContext
{
    /// <summary>
    /// Gets the resource associated with the container image options.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// Gets the cancellation token associated with the callback context.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Represents an annotation for container image options that can be applied to resources.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class ContainerImageOptionsCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImageOptionsCallbackAnnotation"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The synchronous callback that returns the container image options.</param>
    public ContainerImageOptionsCallbackAnnotation(Func<ContainerImageOptionsCallbackAnnotationContext, ContainerImageOptions> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = context => Task.FromResult(callback(context));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImageOptionsCallbackAnnotation"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="callback">The asynchronous callback that returns the container image options.</param>
    public ContainerImageOptionsCallbackAnnotation(Func<ContainerImageOptionsCallbackAnnotationContext, Task<ContainerImageOptions>> callback)
    {
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Gets the callback that returns the container image options.
    /// </summary>
    public Func<ContainerImageOptionsCallbackAnnotationContext, Task<ContainerImageOptions>> Callback { get; }
}
