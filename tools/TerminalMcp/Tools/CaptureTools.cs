// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TerminalMcp.Tools;

/// <summary>
/// MCP tools for capturing terminal output.
/// </summary>
[McpServerToolType]
public sealed class CaptureTools(TerminalSessionManager sessionManager)
{
    /// <summary>
    /// Captures the current terminal screen as text.
    /// </summary>
    [McpServerTool, Description("Capture the current terminal screen content as plain text.")]
    public CaptureTextResult CaptureTerminalText(
        [Description("The session ID returned by start_terminal")] string sessionId)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new CaptureTextResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        try
        {
            var text = session.CaptureText();

            return new CaptureTextResult
            {
                Success = true,
                SessionId = sessionId,
                Message = "Terminal screen captured.",
                Text = text,
                Width = session.Width,
                Height = session.Height,
                HasExited = session.HasExited,
                ExitCode = session.HasExited ? session.ExitCode : null
            };
        }
        catch (Exception ex)
        {
            return new CaptureTextResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Failed to capture text: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Captures the current terminal screen as an SVG image.
    /// </summary>
    [McpServerTool, Description("Capture the current terminal screen as an SVG image and save to a file.")]
    public CaptureScreenshotResult CaptureTerminalScreenshot(
        [Description("The session ID returned by start_terminal")] string sessionId,
        [Description("File path to save the SVG screenshot (required).")] string savePath)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new CaptureScreenshotResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found."
            };
        }

        if (string.IsNullOrWhiteSpace(savePath))
        {
            return new CaptureScreenshotResult
            {
                Success = false,
                SessionId = sessionId,
                Message = "savePath is required. Please provide a file path to save the screenshot."
            };
        }

        try
        {
            var svg = session.CaptureSvg();

            // Ensure directory exists
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Ensure .svg extension
            if (!savePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                savePath = Path.ChangeExtension(savePath, ".svg");
            }

            // Save SVG
            File.WriteAllText(savePath, svg);

            return new CaptureScreenshotResult
            {
                Success = true,
                SessionId = sessionId,
                Message = $"Screenshot saved to {savePath}",
                SavedPath = savePath,
                Width = session.Width,
                Height = session.Height,
                HasExited = session.HasExited,
                ExitCode = session.HasExited ? session.ExitCode : null
            };
        }
        catch (Exception ex)
        {
            return new CaptureScreenshotResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Failed to capture screenshot: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Waits for specific text to appear on the terminal screen.
    /// </summary>
    [McpServerTool, Description("Wait for specific text to appear on the terminal screen. Useful for waiting for prompts or output.")]
    public async Task<WaitForTextResult> WaitForTerminalText(
        [Description("The session ID returned by start_terminal")] string sessionId,
        [Description("The text to wait for")] string text,
        [Description("Maximum seconds to wait (default: 10)")] int timeoutSeconds = 10,
        CancellationToken ct = default)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session is null)
        {
            return new WaitForTextResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Session '{sessionId}' not found.",
                Found = false
            };
        }

        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(1, Math.Min(timeoutSeconds, 60)));
            var found = await session.WaitForTextAsync(text, timeout, ct).ConfigureAwait(false);

            if (found)
            {
                return new WaitForTextResult
                {
                    Success = true,
                    SessionId = sessionId,
                    Message = $"Text '{text}' found on terminal.",
                    Found = true
                };
            }
            else
            {
                return new WaitForTextResult
                {
                    Success = true,
                    SessionId = sessionId,
                    Message = $"Text '{text}' not found within {timeoutSeconds} seconds.",
                    Found = false,
                    CurrentText = session.CaptureText()
                };
            }
        }
        catch (Exception ex)
        {
            return new WaitForTextResult
            {
                Success = false,
                SessionId = sessionId,
                Message = $"Failed to wait for text: {ex.Message}",
                Found = false
            };
        }
    }
}
