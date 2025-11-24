// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation for configuring image push options via a callback.
/// </summary>
[Experimental("ASPIRECOMPUTE002", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public sealed class ContainerImagePushOptionsCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImagePushOptionsCallbackAnnotation"/> class.
    /// </summary>
    /// <param name="callback">The synchronous callback to configure push options.</param>
    public ContainerImagePushOptionsCallbackAnnotation(Action<ContainerImagePushOptionsCallbackContext> callback)
    {
        Callback = context =>
        {
            callback(context);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImagePushOptionsCallbackAnnotation"/> class.
    /// </summary>
    /// <param name="callback">The asynchronous callback to configure push options.</param>
    public ContainerImagePushOptionsCallbackAnnotation(Func<ContainerImagePushOptionsCallbackContext, Task> callback)
    {
        Callback = callback;
    }

    /// <summary>
    /// Gets the callback that configures image push options.
    /// </summary>
    public Func<ContainerImagePushOptionsCallbackContext, Task> Callback { get; }
}
