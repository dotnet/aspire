// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting;

internal sealed class LaunchSettings
{
    [JsonPropertyName("profiles")]
    public Dictionary<string, LaunchProfile> Profiles { get; set; } = new Dictionary<string, LaunchProfile>();
}
