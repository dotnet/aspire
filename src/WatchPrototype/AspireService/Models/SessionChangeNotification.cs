// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Aspire.Tools.Service;

internal static class NotificationType
{
    public const string ProcessRestarted = "processRestarted";
    public const string SessionTerminated = "sessionTerminated";
    public const string ServiceLogs = "serviceLogs";
}

/// <summary>
/// Implements https://github.com/dotnet/aspire/blob/445d2fc8a6a0b7ce3d8cc42def4d37b02709043b/docs/specs/IDE-execution.md#common-notification-properties.
/// </summary>
internal class SessionNotification
{
    public const string Url = "/notify";

    /// <summary>
    /// One of <see cref="NotificationType"/>.
    /// </summary>
    [Required]
    [JsonPropertyName("notification_type")]
    public required string NotificationType { get; init; }

    /// <summary>
    /// The id of the run session that the notification is related to.
    /// </summary>
    [Required]
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Implements https://github.com/dotnet/aspire/blob/445d2fc8a6a0b7ce3d8cc42def4d37b02709043b/docs/specs/IDE-execution.md#session-terminated-notification.
/// <see cref="SessionNotification.NotificationType"/> is <see cref="NotificationType.SessionTerminated"/>.
/// </summary>
internal sealed class SessionTerminatedNotification : SessionNotification
{
    /// <summary>
    /// The process id of the service process associated with the run session.
    /// </summary>
    [Required]
    [JsonPropertyName("pid")]
    public required int Pid { get; init; }

    /// <summary>
    /// The exit code of the process associated with the run session.
    /// </summary>
    [Required]
    [JsonPropertyName("exit_code")]
    public required int? ExitCode { get; init; }

    public override string ToString()
        => $"pid={Pid}, exit_code={ExitCode}";
}

/// <summary>
/// Implements https://github.com/dotnet/aspire/blob/445d2fc8a6a0b7ce3d8cc42def4d37b02709043b/docs/specs/IDE-execution.md#process-restarted-notification.
/// <see cref="SessionNotification.NotificationType"/> is <see cref="NotificationType.ProcessRestarted"/>.
/// </summary>
internal sealed class ProcessRestartedNotification : SessionNotification
{
    /// <summary>
    /// The process id of the service process associated with the run session.
    /// </summary>
    [Required]
    [JsonPropertyName("pid")]
    public required int PID { get; init; }

    public override string ToString()
        => $"pid={PID}";
}

/// <summary>
/// Implements https://github.com/dotnet/aspire/blob/445d2fc8a6a0b7ce3d8cc42def4d37b02709043b/docs/specs/IDE-execution.md#log-notification
/// <see cref="SessionNotification.NotificationType"/> is <see cref="NotificationType.ServiceLogs"/>.
/// </summary>
internal sealed class ServiceLogsNotification : SessionNotification
{
    /// <summary>
    /// True if the output comes from standard error stream, otherwise false (implying standard output stream).
    /// </summary>
    [Required]
    [JsonPropertyName("is_std_err")]
    public required bool IsStdErr { get; init; }

    /// <summary>
    /// The text written by the service program.
    /// </summary>
    [Required]
    [JsonPropertyName("log_message")]
    public required string LogMessage { get; init; }

    public override string ToString()
        => $"log_message='{LogMessage}', is_std_err={IsStdErr}";
}
