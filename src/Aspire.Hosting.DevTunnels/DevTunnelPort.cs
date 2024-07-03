// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnelPort
{
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("portNumber")]
    public int? PortNumber { get; set; }

    [JsonPropertyName("labels")]
    public string[]? Labels { get; set; }
}
