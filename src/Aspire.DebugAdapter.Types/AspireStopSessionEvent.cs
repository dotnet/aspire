// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.DebugAdapter.Types;

[JsonDerivedType(typeof(AspireStopSessionEvent), "aspire/stopSession")]
public partial class EventMessage { }

/// <summary>
/// Custom event sent by the Aspire debug adapter middleware to notify the IDE
/// that a run session should be stopped. This event is emitted when DCP
/// calls DELETE /run_session/{id} on the IDE session server.
/// </summary>
public sealed class AspireStopSessionEvent : EventMessage
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string? EventName => "aspire/stopSession";

    /// <summary>
    /// The event body containing stop session request details.
    /// </summary>
    [JsonPropertyName("body")]
    public new required AspireStopSessionEventBody Body { get; set; }
}

/// <summary>
/// Body of the AspireStopSessionEvent.
/// </summary>
public sealed class AspireStopSessionEventBody
{
    /// <summary>
    /// The session ID that should be stopped.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; set; }
}
