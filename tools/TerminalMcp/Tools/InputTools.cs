// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TerminalMcp.Tools;

/// <summary>
/// MCP tools for sending input to terminal sessions.
/// </summary>
[McpServerToolType]
public sealed class InputTools(TerminalSessionManager sessionManager)
{
    /// <summary>
    /// Sends text input to a terminal session.
    /// </summary>
    [McpServerTool, Description("Send text input to a terminal session. Use \\n for newline, \\t for tab.")]
    public async Task<SendInputResult> SendTerminalInput(
        [Description("The session ID returned by start_terminal")] string sessionId,
        [Description("The text to send. Use \\n for newline, \\t for tab.")] string text,
        CancellationToken ct = default)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new SendInputResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        if (session.HasExited)
        {
            return new SendInputResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session process has exited with code {session.ExitCode}."
            };
        }

        try
        {
            // Unescape common escape sequences
            var unescaped = text
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\e", "\x1b")
                .Replace("\\\\", "\\");

            await session.SendInputAsync(unescaped, ct).ConfigureAwait(false);

            return new SendInputResult
            {
                Success = true,
                SessionId = sessionId,
                Message = $"Sent {unescaped.Length} character(s) to terminal.",
                CharactersSent = unescaped.Length
            };
        }
        catch (Exception ex)
        {
            return new SendInputResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Failed to send input: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sends a special key to a terminal session.
    /// </summary>
    [McpServerTool, Description("Send a special key to a terminal session. Supported keys: Enter, Tab, Escape, Up, Down, Left, Right, Backspace, Delete, Home, End, PageUp, PageDown, F1-F12.")]
    public async Task<SendInputResult> SendTerminalKey(
        [Description("The session ID returned by start_terminal")] string sessionId,
        [Description("The key to send (Enter, Tab, Escape, Up, Down, Left, Right, Backspace, Delete, Home, End, PageUp, PageDown, F1-F12)")] string key,
        [Description("Optional modifiers: Ctrl, Alt, Shift")] string[]? modifiers = null,
        CancellationToken ct = default)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new SendInputResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        if (session.HasExited)
        {
            return new SendInputResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session process has exited with code {session.ExitCode}."
            };
        }

        try
        {
            await session.SendKeyAsync(key, modifiers, ct).ConfigureAwait(false);

            var modifierStr = modifiers?.Length > 0 ? $"{string.Join("+", modifiers)}+" : "";
            return new SendInputResult
            {
                Success = true,
                SessionId = sessionId,
                Message = $"Sent key {modifierStr}{key} to terminal.",
                CharactersSent = 1
            };
        }
        catch (Exception ex)
        {
            return new SendInputResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Failed to send key: {ex.Message}"
            };
        }
    }
}
