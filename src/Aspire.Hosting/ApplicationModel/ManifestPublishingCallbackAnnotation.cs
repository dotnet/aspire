// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to be executed during manifest publishing.
/// </summary>
public class ManifestPublishingCallbackAnnotation(Action<Utf8JsonWriter>? callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback action for publishing the manifest.
    /// </summary>
    public Action<Utf8JsonWriter>? Callback { get; } = callback;
    
    /// <summary>
    /// Represents a <see langword="null"/>-based callback annotation for manifest 
    /// publishing used in scenarios where it's ignored.
    /// </summary>
    public static ManifestPublishingCallbackAnnotation Ignore { get; } = new(null);
}
