// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel;

public class ManifestPublishingCallbackAnnotation(Func<Utf8JsonWriter, CancellationToken, Task> callback) : IDistributedApplicationComponentAnnotation
{
    public Func<Utf8JsonWriter, CancellationToken, Task> Callback { get; } = callback;
}
