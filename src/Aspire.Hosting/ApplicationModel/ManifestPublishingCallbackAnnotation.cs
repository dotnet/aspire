// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to be executed during manifest publishing.
/// </summary>
public class ManifestPublishingCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestPublishingCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">A callback which provides access to <see cref="ManifestPublishingContext"/> which can be used for controlling JSON output into the manifest.</param>
    public ManifestPublishingCallbackAnnotation(Action<ManifestPublishingContext>? callback)
    {
        if (callback is not null)
        {
            Callback = context =>
            {
                callback(context);
                return Task.CompletedTask;
            };
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestPublishingCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">A callback which provides access to <see cref="ManifestPublishingContext"/> which can be used for controlling JSON output into the manifest.</param>
    public ManifestPublishingCallbackAnnotation(Func<ManifestPublishingContext, Task>? callback)
    {
        Callback = callback;
    }

    /// <summary>
    /// Gets the callback action for publishing the manifest.
    /// </summary>
    public Func<ManifestPublishingContext, Task>? Callback { get; }

    /// <summary>
    /// Represents a <see langword="null"/>-based callback annotation for manifest 
    /// publishing used in scenarios where it's ignored.
    /// </summary>
    public static ManifestPublishingCallbackAnnotation Ignore { get; } = new(null);
}
