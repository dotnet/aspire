// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Dcp.Model;

internal sealed class RunSessionInfo
{
    [JsonPropertyName("protocols_supported")]
    public required string[] ProtocolsSupported { get; set; }

    [JsonPropertyName("supported_launch_configurations")]
    public string[]? SupportedLaunchConfigurations { get; set; }
}
