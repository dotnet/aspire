// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnelCreateCommandResponse
{
    [JsonPropertyName("tunnel")]
    public DevTunnel? Tunnel { get; set; }
}
