// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.IdeSessionServer;

/// <summary>
/// JSON serialization context for IDE session server types.
/// Required for AOT compilation support.
/// </summary>
[JsonSerializable(typeof(RunSessionPayload))]
[JsonSerializable(typeof(RunSessionInfo))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ErrorDetail))]
[JsonSerializable(typeof(LaunchConfiguration))]
[JsonSerializable(typeof(ProjectLaunchConfiguration))]
[JsonSerializable(typeof(PythonLaunchConfiguration))]
[JsonSerializable(typeof(EnvVar))]
[JsonSerializable(typeof(EnvVar[]))]
[JsonSerializable(typeof(RunSessionNotification))]
[JsonSerializable(typeof(ProcessRestartedNotification))]
[JsonSerializable(typeof(SessionTerminatedNotification))]
[JsonSerializable(typeof(ServiceLogsNotification))]
[JsonSerializable(typeof(BridgeSessionFailedNotification))]
[JsonSerializable(typeof(TelemetryEnabledResponse))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class SessionJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Response for GET /telemetry/enabled endpoint.
/// </summary>
internal sealed class TelemetryEnabledResponse
{
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; init; }
}
