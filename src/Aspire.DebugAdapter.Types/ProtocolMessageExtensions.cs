// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.DebugAdapter.Types;

/// <summary>
/// Extension properties for protocol messages.
/// </summary>
public abstract partial class ProtocolMessage
{
    /// <summary>
    /// Gets or sets the raw JSON string of this message as received from the transport.
    /// </summary>
    /// <remarks>
    /// This property is populated by the transport layer during deserialization
    /// and is not serialized back out. It is useful for diagnostic logging of
    /// unknown or unrecognized message types.
    /// </remarks>
    [JsonIgnore]
    public string? RawJson { get; set; }
}
