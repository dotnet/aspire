// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.DebugAdapter.Types;

[JsonDerivedType(typeof(AspireRunSessionEvent), "aspire/runSession")]
public partial class EventMessage { }

/// <summary>
/// Custom event sent by the Aspire debug adapter middleware to notify the IDE
/// that a run session should be started. This event is emitted when DCP
/// calls PUT /run_session on the IDE session server.
/// </summary>
public sealed class AspireRunSessionEvent : EventMessage
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string? EventName => "aspire/runSession";

    /// <summary>
    /// The event body containing run session request details.
    /// </summary>
    [JsonPropertyName("body")]
    public new required AspireRunSessionEventBody Body { get; set; }
}

/// <summary>
/// Body of the AspireRunSessionEvent containing run session request details.
/// The launch configurations are passed through as raw JSON so the IDE can
/// handle different configuration types (project, python, etc.) without the
/// middleware needing to understand their schemas.
/// </summary>
public sealed class AspireRunSessionEventBody
{
    /// <summary>
    /// Unique identifier for this run session request from DCP.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; set; }

    /// <summary>
    /// The DCP instance identifier.
    /// </summary>
    [JsonPropertyName("dcpId")]
    public required string DcpId { get; set; }

    /// <summary>
    /// The launch configurations from DCP, passed through as raw JSON.
    /// The IDE is responsible for interpreting different configuration types.
    /// </summary>
    [JsonPropertyName("launch_configurations")]
    public required System.Text.Json.JsonElement[] LaunchConfigurations { get; set; }

    /// <summary>
    /// Environment variables to set.
    /// </summary>
    [JsonPropertyName("env")]
    public Dictionary<string, string?>? Env { get; set; }

    /// <summary>
    /// Command-line arguments.
    /// </summary>
    [JsonPropertyName("args")]
    public string[]? Args { get; set; }

    /// <summary>
    /// Unix domain socket path for DAP bridging (API version >= 2026-02-01).
    /// </summary>
    [JsonPropertyName("debugBridgeSocketPath")]
    public string? DebugBridgeSocketPath { get; set; }

    /// <summary>
    /// Token for authenticating with the debug bridge (API version >= 2026-02-01).
    /// Included when <see cref="DebugBridgeSocketPath"/> is specified.
    /// </summary>
    [JsonPropertyName("bridgeToken")]
    public string? BridgeToken { get; set; }

    /// <summary>
    /// Debug session ID from DCP for the bridge handshake (API version >= 2026-02-01).
    /// This is the session ID that DCP registers with the bridge socket manager.
    /// </summary>
    [JsonPropertyName("debugSessionId")]
    public string? DebugSessionId { get; set; }
}
