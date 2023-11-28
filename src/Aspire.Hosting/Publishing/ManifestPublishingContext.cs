// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.Publishing;

public sealed class ManifestPublishingContext(Utf8JsonWriter writer)
{
    public Utf8JsonWriter Writer { get; } = writer;
}
