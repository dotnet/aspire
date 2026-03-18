// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.DebugAdapter.Types;

[JsonDerivedType(typeof(AspireIdeSessionServerEvent), "aspire/ideSessionServer")]
public partial class EventMessage { }

/// <summary>
/// Custom event sent by the Aspire debug adapter middleware to notify the IDE
/// about the IDE session server connection information.
/// This allows DCP to connect to the Aspire CLI for run session management.
/// </summary>
public sealed class AspireIdeSessionServerEvent : EventMessage
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string? EventName => "aspire/ideSessionServer";

    /// <summary>
    /// The event body containing server connection information.
    /// </summary>
    [JsonPropertyName("body")]
    public new required AspireIdeSessionServerEventBody Body { get; set; }
}

/// <summary>
/// Body of the AspireIdeSessionServerEvent containing server connection details.
/// </summary>
public sealed class AspireIdeSessionServerEventBody
{
    /// <summary>
    /// The port of the IDE session server (e.g., 12345).
    /// </summary>
    [JsonPropertyName("port")]
    public required int Port { get; set; }

    /// <summary>
    /// The bearer token for authentication.
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; set; }

    /// <summary>
    /// The self-signed certificate in base64 DER format.
    /// </summary>
    [JsonPropertyName("certificate")]
    public required string Certificate { get; set; }
}
