// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Nats;

internal sealed class NuiConnectionConfig(string name)
{
    [JsonPropertyName("name")]
    public string Name => name;
    [JsonPropertyName("hosts")]
    public required string[] Hosts { get; init; }
    [JsonPropertyName("subscriptions")]
    public object[]? Subscriptions { get; set; }
    [JsonPropertyName("auth")]
    public NuiConnectionAuth[]? Auth { get; set; }
}

internal sealed class NuiConnectionAuth
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }
    [JsonPropertyName("creds")]
    public string? Creds { get; set; }
    [JsonPropertyName("jwt")]
    public string? Jwt { get; set; }
    [JsonPropertyName("n_key_seed")]
    public string? NKeySeed { get; set; }
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}
