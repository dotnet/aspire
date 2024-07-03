// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnel
{
    // NOTE: Other properties exist, but I'm only grabbing what I am
    //       interested in for the purposes of .NET Aspire.

    [JsonPropertyName("tunnelId")]
    public string? TunnelId { get; set; }
}
