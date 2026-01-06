// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace TerminalMcp.Tools;

/// <summary>
/// Result from starting a terminal session.
/// </summary>
public sealed class StartTerminalResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID, if successful.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string? SessionId { get; init; }

    /// <summary>
    /// The process ID, if successful.
    /// </summary>
    [JsonPropertyName("processId")]
    public required int? ProcessId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// The command that was executed.
    /// </summary>
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    /// <summary>
    /// The arguments passed to the command.
    /// </summary>
    [JsonPropertyName("arguments")]
    public required string[] Arguments { get; init; }

    /// <summary>
    /// The working directory for the session.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public required string? WorkingDirectory { get; init; }

    /// <summary>
    /// The terminal width in columns.
    /// </summary>
    [JsonPropertyName("width")]
    public required int Width { get; init; }

    /// <summary>
    /// The terminal height in rows.
    /// </summary>
    [JsonPropertyName("height")]
    public required int Height { get; init; }
}

/// <summary>
/// Result from stopping a terminal session.
/// </summary>
public sealed class StopTerminalResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Whether the process had already exited before stopping.
    /// </summary>
    [JsonPropertyName("hadAlreadyExited")]
    public bool HadAlreadyExited { get; init; }

    /// <summary>
    /// The exit code if the process had exited.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }
}

/// <summary>
/// Result from removing a session.
/// </summary>
public sealed class RemoveSessionResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Whether the process was still running when removed.
    /// </summary>
    [JsonPropertyName("wasRunning")]
    public bool WasRunning { get; init; }
}

/// <summary>
/// Result from sending input to a terminal.
/// </summary>
public sealed class SendInputResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// The number of characters sent.
    /// </summary>
    [JsonPropertyName("charactersSent")]
    public int CharactersSent { get; init; }
}

/// <summary>
/// Result from resizing a terminal.
/// </summary>
public sealed class ResizeTerminalResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// The previous width.
    /// </summary>
    [JsonPropertyName("oldWidth")]
    public int OldWidth { get; init; }

    /// <summary>
    /// The previous height.
    /// </summary>
    [JsonPropertyName("oldHeight")]
    public int OldHeight { get; init; }

    /// <summary>
    /// The new width.
    /// </summary>
    [JsonPropertyName("newWidth")]
    public int NewWidth { get; init; }

    /// <summary>
    /// The new height.
    /// </summary>
    [JsonPropertyName("newHeight")]
    public int NewHeight { get; init; }
}

/// <summary>
/// Result from capturing terminal text.
/// </summary>
public sealed class CaptureTextResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// The captured text content.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// The terminal width.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <summary>
    /// The terminal height.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    [JsonPropertyName("hasExited")]
    public bool HasExited { get; init; }

    /// <summary>
    /// The exit code if the process has exited.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }
}

/// <summary>
/// Result from capturing a terminal screenshot.
/// </summary>
public sealed class CaptureScreenshotResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// The path where the screenshot was saved.
    /// </summary>
    [JsonPropertyName("savedPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SavedPath { get; init; }

    /// <summary>
    /// The terminal width.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <summary>
    /// The terminal height.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    [JsonPropertyName("hasExited")]
    public bool HasExited { get; init; }

    /// <summary>
    /// The exit code if the process has exited.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }
}

/// <summary>
/// Result from waiting for text.
/// </summary>
public sealed class WaitForTextResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Whether the text was found.
    /// </summary>
    [JsonPropertyName("found")]
    public required bool Found { get; init; }

    /// <summary>
    /// The current terminal text if the text was not found.
    /// </summary>
    [JsonPropertyName("currentText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CurrentText { get; init; }
}

/// <summary>
/// Result from listing terminals.
/// </summary>
public sealed class ListTerminalsResult
{
    /// <summary>
    /// The number of active sessions.
    /// </summary>
    [JsonPropertyName("sessionCount")]
    public required int SessionCount { get; init; }

    /// <summary>
    /// Information about each session.
    /// </summary>
    [JsonPropertyName("sessions")]
    public required TerminalSessionInfo[] Sessions { get; init; }
}

/// <summary>
/// Information about a terminal session.
/// </summary>
public sealed class TerminalSessionInfo
{
    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>
    /// The process ID.
    /// </summary>
    [JsonPropertyName("processId")]
    public required int ProcessId { get; init; }

    /// <summary>
    /// The command being executed.
    /// </summary>
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    /// <summary>
    /// The command arguments.
    /// </summary>
    [JsonPropertyName("arguments")]
    public required string[] Arguments { get; init; }

    /// <summary>
    /// The working directory.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public required string? WorkingDirectory { get; init; }

    /// <summary>
    /// The terminal width in columns.
    /// </summary>
    [JsonPropertyName("width")]
    public required int Width { get; init; }

    /// <summary>
    /// The terminal height in rows.
    /// </summary>
    [JsonPropertyName("height")]
    public required int Height { get; init; }

    /// <summary>
    /// When the session was started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    [JsonPropertyName("hasExited")]
    public required bool HasExited { get; init; }

    /// <summary>
    /// The exit code if the process has exited.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public required int? ExitCode { get; init; }

    /// <summary>
    /// How long the session has been running.
    /// </summary>
    [JsonPropertyName("runningFor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? RunningFor { get; init; }
}
