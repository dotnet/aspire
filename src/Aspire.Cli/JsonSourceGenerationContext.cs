// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CliSettings))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(NuGetPackage))]
[JsonSerializable(typeof(List<NuGetPackage>))]
[JsonSerializable(typeof(DiskCacheItem))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}

/// <summary>
/// Represents a cached item stored on disk.
/// </summary>
internal sealed class DiskCacheItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? Expiration { get; set; }
}
