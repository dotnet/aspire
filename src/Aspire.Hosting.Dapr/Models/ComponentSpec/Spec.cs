// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal sealed class Spec
{
    public required string Type { get; init; }
    public required string Version { get; init; } = "v1";
    public string? InitTimeout { get; init; }
    public bool? IgnoreErrors { get; init; }
    public List<MetadataValue> Metadata { get; init; } = new();
}