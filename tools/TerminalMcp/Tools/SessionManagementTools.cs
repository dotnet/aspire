// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TerminalMcp.Tools;

/// <summary>
/// MCP tools for managing terminal session lifecycle.
/// </summary>
[McpServerToolType]
public sealed class SessionManagementTools(TerminalSessionManager sessionManager)
{
    /// <summary>
    /// Starts a new bash terminal session.
    /// </summary>
    [McpServerTool, Description("Start a new bash terminal session. Use this on Linux and macOS. Returns the session ID for use with other terminal tools.")]
    public async Task<StartTerminalResult> StartBashTerminal(
        [Description("Optional working directory for the bash session")] string? workingDirectory = null,
        [Description("Terminal width in columns (default: 80)")] int width = 80,
        [Description("Terminal height in rows (default: 24)")] int height = 24,
        CancellationToken ct = default)
    {
        return await StartShellAsync("bash", [], workingDirectory, width, height, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts a new PowerShell (pwsh) terminal session.
    /// </summary>
    [McpServerTool, Description("Start a new PowerShell (pwsh) terminal session. Use this on Windows or when PowerShell is preferred. Returns the session ID for use with other terminal tools.")]
    public async Task<StartTerminalResult> StartPwshTerminal(
        [Description("Optional working directory for the PowerShell session")] string? workingDirectory = null,
        [Description("Terminal width in columns (default: 80)")] int width = 80,
        [Description("Terminal height in rows (default: 24)")] int height = 24,
        CancellationToken ct = default)
    {
        return await StartShellAsync("pwsh", [], workingDirectory, width, height, ct).ConfigureAwait(false);
    }

    private async Task<StartTerminalResult> StartShellAsync(
        string command,
        string[] arguments,
        string? workingDirectory,
        int width,
        int height,
        CancellationToken ct)
    {
        try
        {
            var session = await sessionManager.StartSessionAsync(
                command,
                arguments,
                workingDirectory,
                environment: null,
                width,
                height,
                ct).ConfigureAwait(false);

            return new StartTerminalResult
            {
                Success = true,
                SessionId = session.Id,
                ProcessId = session.ProcessId,
                Message = $"Terminal session started successfully.",
                Command = session.Command,
                Arguments = session.Arguments.ToArray(),
                WorkingDirectory = session.WorkingDirectory,
                Width = session.Width,
                Height = session.Height
            };
        }
        catch (Exception ex)
        {
            return new StartTerminalResult
            {
                Success = false,
                SessionId = null,
                ProcessId = null,
                Message = $"Failed to start terminal: {ex.Message}",
                Command = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                Width = width,
                Height = height
            };
        }
    }

    /// <summary>
    /// Stops a terminal session's process but keeps the session for inspection.
    /// </summary>
    [McpServerTool, Description("Stop a terminal session's process by its ID. The session remains available for inspection. Use remove_session to fully clean up.")]
    public StopTerminalResult StopTerminal(
        [Description("The session ID returned by start_bash_terminal or start_pwsh_terminal")] string sessionId)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new StopTerminalResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        var hadExited = session.HasExited;
        var exitCode = hadExited ? session.ExitCode : (int?)null;

        if (!hadExited)
        {
            sessionManager.StopSession(sessionId);
        }

        return new StopTerminalResult
        {
            Success = true,
            SessionId = sessionId,
            Message = hadExited
                ? $"Process had already exited with code {exitCode}."
                : "Process stopped. Use remove_session to clean up the session.",
            HadAlreadyExited = hadExited,
            ExitCode = exitCode
        };
    }

    /// <summary>
    /// Lists all active terminal sessions.
    /// </summary>
    [McpServerTool, Description("List all active terminal sessions with their status and information.")]
    public ListTerminalsResult ListTerminals()
    {
        var sessions = sessionManager.ListSessions();

        return new ListTerminalsResult
        {
            SessionCount = sessions.Count,
            Sessions = sessions.Select(s => new TerminalSessionInfo
            {
                SessionId = s.Id,
                ProcessId = s.ProcessId,
                Command = s.Command,
                Arguments = s.Arguments.ToArray(),
                WorkingDirectory = s.WorkingDirectory,
                Width = s.Width,
                Height = s.Height,
                StartedAt = s.StartedAt,
                HasExited = s.HasExited,
                ExitCode = s.ExitCode,
                RunningFor = s.HasExited ? null : DateTimeOffset.UtcNow - s.StartedAt
            }).ToArray()
        };
    }

    /// <summary>
    /// Removes a terminal session completely, disposing all resources.
    /// </summary>
    [McpServerTool, Description("Remove a terminal session completely, disposing all resources. Use after stop_terminal or when the process has exited.")]
    public async Task<RemoveSessionResult> RemoveSession(
        [Description("The session ID returned by start_bash_terminal or start_pwsh_terminal")] string sessionId)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new RemoveSessionResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        var wasRunning = !session.HasExited;
        var removed = await sessionManager.RemoveSessionAsync(sessionId).ConfigureAwait(false);

        return new RemoveSessionResult
        {
            Success = removed,
            SessionId = sessionId,
            Message = removed
                ? (wasRunning ? "Session removed (process was still running and has been killed)." : "Session removed.")
                : $"Failed to remove session '{sessionId}'.",
            WasRunning = wasRunning
        };
    }

    /// <summary>
    /// Resizes a terminal session.
    /// </summary>
    [McpServerTool, Description("Resize a terminal session to the specified dimensions.")]
    public async Task<ResizeTerminalResult> ResizeTerminal(
        [Description("The session ID returned by start_bash_terminal or start_pwsh_terminal")] string sessionId,
        [Description("New width in columns")] int width,
        [Description("New height in rows")] int height,
        CancellationToken ct = default)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new ResizeTerminalResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        if (width < 1 || height < 1)
        {
            return new ResizeTerminalResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Invalid dimensions: width and height must be at least 1."
            };
        }

        if (width > 500 || height > 200)
        {
            return new ResizeTerminalResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Dimensions too large: max 500x200."
            };
        }

        try
        {
            var oldWidth = session.Width;
            var oldHeight = session.Height;
            await session.ResizeAsync(width, height, ct).ConfigureAwait(false);

            return new ResizeTerminalResult
            {
                Success = true,
                SessionId = sessionId,
                Message = $"Resized terminal from {oldWidth}x{oldHeight} to {width}x{height}.",
                OldWidth = oldWidth,
                OldHeight = oldHeight,
                NewWidth = width,
                NewHeight = height
            };
        }
        catch (Exception ex)
        {
            return new ResizeTerminalResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Failed to resize: {ex.Message}"
            };
        }
    }
}
