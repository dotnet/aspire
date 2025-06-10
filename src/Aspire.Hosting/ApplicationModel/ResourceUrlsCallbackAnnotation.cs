// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to modify URLs that should be displayed for the resource.
/// </summary>
public sealed class ResourceUrlsCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUrlsCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public ResourceUrlsCallbackAnnotation(Action<ResourceUrlsCallbackContext> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Callback = c =>
        {
            callback(c);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUrlsCallbackContext"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public ResourceUrlsCallbackAnnotation(Func<ResourceUrlsCallbackContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Callback = callback;
    }

    /// <summary>
    /// Gets or sets the callback action to be executed when the URLs are being processed.
    /// </summary>
    public Func<ResourceUrlsCallbackContext, Task> Callback { get; private set; }
}
