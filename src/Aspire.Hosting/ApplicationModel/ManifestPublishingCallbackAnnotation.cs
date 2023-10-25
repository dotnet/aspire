// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel;

public class ManifestPublishingCallbackAnnotation(Action<Utf8JsonWriter>? callback) : IResourceAnnotation
{
    public Action<Utf8JsonWriter>? Callback { get; } = callback;
    public static ManifestPublishingCallbackAnnotation Ignore { get; } = new ManifestPublishingCallbackAnnotation(null);
}
