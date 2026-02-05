// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.DebugAdapter.Types;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Default JSON serialization context for Debug Adapter Protocol types.
/// </summary>
/// <remarks>
/// <para>
/// This context is pre-configured with the appropriate settings for Debug Adapter Protocol serialization
/// and can be used directly with <see cref="StreamMessageTransport"/>.
/// </para>
/// <para>
/// The context automatically includes all standard Debug Adapter Protocol types and any custom types
/// defined in the Aspire.DebugAdapter.Types assembly.
/// </para>
/// </remarks>
[JsonSerializable(typeof(ProtocolMessage))]
[JsonSerializable(typeof(RequestMessage))]
[JsonSerializable(typeof(ResponseMessage))]
[JsonSerializable(typeof(EventMessage))]
[JsonSerializable(typeof(AspireDashboardEvent))]
[JsonSerializable(typeof(AspireDashboardEventBody))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    AllowOutOfOrderMetadataProperties = true)]
public partial class DefaultDebugAdapterJsonContext : JsonSerializerContext
{
}
