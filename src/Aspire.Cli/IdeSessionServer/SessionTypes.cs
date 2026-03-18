// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.IdeSessionServer;

/// <summary>
/// Environment variable name/value pair.
/// </summary>
internal sealed class EnvVar
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

/// <summary>
/// Launch configuration - stored as raw JSON to pass through to the IDE unchanged.
/// The IDE is responsible for handling different configuration types (project, python, etc.).
/// </summary>
internal class LaunchConfiguration
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    /// <summary>
    /// Extension data captures all other properties as raw JSON.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>
/// Project launch configuration for C# projects.
/// </summary>
internal sealed class ProjectLaunchConfiguration : LaunchConfiguration
{
    [JsonPropertyName("project_path")]
    public required string ProjectPath { get; init; }

    [JsonPropertyName("launch_profile")]
    public string? LaunchProfile { get; init; }

    [JsonPropertyName("disable_launch_profile")]
    public bool? DisableLaunchProfile { get; init; }
}

/// <summary>
/// Python launch configuration.
/// </summary>
internal sealed class PythonLaunchConfiguration : LaunchConfiguration
{
    [JsonPropertyName("project_path")]
    public string? ProjectPath { get; init; }

    [JsonPropertyName("program_path")]
    public string? ProgramPath { get; init; }

    [JsonPropertyName("module")]
    public string? Module { get; init; }

    [JsonPropertyName("interpreter_path")]
    public string? InterpreterPath { get; init; }
}

/// <summary>
/// Request payload for PUT /run_session.
/// </summary>
internal sealed class RunSessionPayload
{
    [JsonPropertyName("launch_configurations")]
    public required LaunchConfiguration[] LaunchConfigurations { get; init; }

    [JsonPropertyName("env")]
    public EnvVar[]? Env { get; init; }

    [JsonPropertyName("args")]
    public string[]? Args { get; init; }

    /// <summary>
    /// Unix domain socket path for DAP bridging (API version >= 2026-02-01).
    /// </summary>
    [JsonPropertyName("debug_bridge_socket_path")]
    public string? DebugBridgeSocketPath { get; init; }

    /// <summary>
    /// Session ID for the debug bridge handshake (API version >= 2026-02-01).
    /// </summary>
    [JsonPropertyName("debug_session_id")]
    public string? DebugSessionId { get; init; }
}

/// <summary>
/// Response for GET /info endpoint.
/// </summary>
internal sealed class RunSessionInfo
{
    [JsonPropertyName("protocols_supported")]
    public required string[] ProtocolsSupported { get; init; }

    [JsonPropertyName("supported_launch_configurations")]
    public required string[] SupportedLaunchConfigurations { get; init; }
}

/// <summary>
/// Error detail for error responses.
/// </summary>
internal sealed class ErrorDetail
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    public ErrorDetail[]? Details { get; init; }
}

/// <summary>
/// Error response body.
/// </summary>
internal sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public required ErrorDetail Error { get; init; }
}

/// <summary>
/// Base notification type.
/// </summary>
internal class RunSessionNotification
{
    [JsonPropertyName("notification_type")]
    public required string NotificationType { get; init; }

    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Notification when a process is restarted.
/// </summary>
internal sealed class ProcessRestartedNotification : RunSessionNotification
{
    [JsonPropertyName("pid")]
    public int? Pid { get; init; }
}

/// <summary>
/// Notification when a session is terminated.
/// </summary>
internal sealed class SessionTerminatedNotification : RunSessionNotification
{
    [JsonPropertyName("exit_code")]
    public required int ExitCode { get; init; }
}

/// <summary>
/// Notification for service logs.
/// </summary>
internal sealed class ServiceLogsNotification : RunSessionNotification
{
    [JsonPropertyName("is_std_err")]
    public required bool IsStdErr { get; init; }

    [JsonPropertyName("log_message")]
    public required string LogMessage { get; init; }
}

/// <summary>
/// Notification for bridge session failure.
/// </summary>
internal sealed class BridgeSessionFailedNotification : RunSessionNotification
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }
}
