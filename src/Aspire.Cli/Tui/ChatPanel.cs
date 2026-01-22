// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Tui;

/// <summary>
/// Represents a message or activity in the chat panel.
/// </summary>
internal abstract record ChatEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}

/// <summary>
/// A user message in the chat.
/// </summary>
internal sealed record UserMessage(string Content) : ChatEntry;

/// <summary>
/// An assistant message in the chat (can be streaming).
/// </summary>
internal sealed record AssistantMessage : ChatEntry
{
    public StringBuilder Content { get; } = new();
    public bool IsComplete { get; set; }
}

/// <summary>
/// A tool execution activity.
/// </summary>
internal sealed record ToolActivity(string ToolName) : ChatEntry
{
    public bool IsComplete { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// A progress/status activity.
/// </summary>
internal sealed record ProgressActivity(string Message) : ChatEntry
{
    public bool IsComplete { get; set; }
    public bool Success { get; set; } = true;
}

/// <summary>
/// An error message.
/// </summary>
internal sealed record ErrorMessage(string Message) : ChatEntry;

/// <summary>
/// Manages chat state and provides renderables for chat content.
/// </summary>
internal sealed class ChatPanel
{
    private readonly List<ChatEntry> _entries = [];
    private readonly int _maxVisibleEntries;
    private int _spinnerFrame;

    private static readonly string[] s_spinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    public ChatPanel(int maxVisibleEntries = 50)
    {
        _maxVisibleEntries = maxVisibleEntries;
    }

    /// <summary>
    /// Adds a user message to the chat.
    /// </summary>
    public void AddUserMessage(string content)
    {
        _entries.Add(new UserMessage(content));
        TrimEntries();
    }

    /// <summary>
    /// Starts a new assistant message (for streaming).
    /// </summary>
    public AssistantMessage StartAssistantMessage()
    {
        var message = new AssistantMessage();
        _entries.Add(message);
        TrimEntries();
        return message;
    }

    /// <summary>
    /// Starts a tool execution activity.
    /// </summary>
    public ToolActivity StartToolActivity(string toolName)
    {
        var activity = new ToolActivity(toolName);
        _entries.Add(activity);
        TrimEntries();
        return activity;
    }

    /// <summary>
    /// Starts a progress activity.
    /// </summary>
    public ProgressActivity StartProgress(string message)
    {
        var activity = new ProgressActivity(message);
        _entries.Add(activity);
        TrimEntries();
        return activity;
    }

    /// <summary>
    /// Adds an error message.
    /// </summary>
    public void AddError(string message)
    {
        _entries.Add(new ErrorMessage(message));
        TrimEntries();
    }

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Advances the spinner animation.
    /// </summary>
    public void Tick()
    {
        _spinnerFrame = (_spinnerFrame + 1) % s_spinnerFrames.Length;
    }

    /// <summary>
    /// Renders the chat panel.
    /// </summary>
    /// <param name="height">The available height for the panel.</param>
    public IRenderable Render(int height)
    {
        var rows = new List<IRenderable>();
        var spinner = s_spinnerFrames[_spinnerFrame];

        // Drop completed activities to reduce noise.
        _entries.RemoveAll(entry => entry switch
        {
            ToolActivity tool => tool.IsComplete,
            ProgressActivity progress => progress.IsComplete,
            _ => false
        });

        var activeActivities = _entries
            .Where(entry => entry switch
            {
                ToolActivity tool => !tool.IsComplete,
                ProgressActivity progress => !progress.IsComplete,
                _ => false
            })
            .ToList();

        // Render recent entries (keep room for input)
        var maxEntries = Math.Max(1, height - 4);
        var visibleEntries = _entries
            .Where(entry => entry is not ToolActivity && entry is not ProgressActivity)
            .TakeLast(maxEntries)
            .ToList();

        foreach (var entry in visibleEntries)
        {
            var rendered = RenderEntry(entry, spinner);
            rows.Add(rendered);
        }

        if (activeActivities.Count > 0)
        {
            var activityMessage = activeActivities[0] switch
            {
                ToolActivity tool => $"Running {tool.ToolName}",
                ProgressActivity progress => progress.Message,
                _ => "Working..."
            };

            if (activeActivities.Count > 1)
            {
                activityMessage += $" (+{activeActivities.Count - 1} more)";
            }

            rows.Add(new Markup($"[grey]{spinner} {activityMessage.EscapeMarkup()}[/]"));
        }

        // If no entries, show placeholder
        if (rows.Count == 0)
        {
            rows.Add(new Markup("[dim]Type a message to get started...[/]"));
        }

        return new Rows(rows);
    }

    /// <summary>
    /// Renders the current input line with optional completion hint.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="completionHint">Optional completion suggestion.</param>
    public static IRenderable RenderInputLine(string input, string? completionHint = null)
    {
        var prompt = "[cyan]>[/] ";
        var inputText = input.EscapeMarkup();

        if (!string.IsNullOrEmpty(completionHint) && completionHint.StartsWith(input, StringComparison.OrdinalIgnoreCase))
        {
            var remaining = completionHint[input.Length..];
            return new Markup($"{prompt}{inputText}[dim]{remaining.EscapeMarkup()}[/]");
        }

        return new Markup($"{prompt}{inputText}[blink]▌[/]");
    }

    private static IRenderable RenderEntry(ChatEntry entry, string spinner)
    {
        return entry switch
        {
            UserMessage user => new Markup($"[grey]You: {user.Content.EscapeMarkup()}[/]"),

            AssistantMessage assistant => RenderAssistantMessage(assistant, spinner),

            ToolActivity tool => RenderToolActivity(tool, spinner),

            ProgressActivity progress => RenderProgressActivity(progress, spinner),

            ErrorMessage error => new Markup($"[red]Error:[/] {error.Message.EscapeMarkup()}"),

            _ => new Markup("[dim](unknown entry)[/]")
        };
    }

    private static IRenderable RenderAssistantMessage(AssistantMessage message, string spinner)
    {
        var content = message.Content.ToString();
        if (string.IsNullOrEmpty(content))
        {
            if (!message.IsComplete)
            {
                return new Markup($"[grey]Assistant: {spinner} thinking...[/]");
            }
            return new Markup("[grey]Assistant: (no response)[/]");
        }

        var suffix = message.IsComplete ? "" : $" {spinner}";
        return new Markup($"[grey]Assistant: {content.EscapeMarkup()}{suffix}[/]");
    }

    private static IRenderable RenderToolActivity(ToolActivity tool, string spinner)
    {
        if (!tool.IsComplete)
        {
            return new Markup($"[grey]  {spinner} Running: {tool.ToolName.EscapeMarkup()}[/]");
        }

        var icon = tool.Success ? "✓" : "✗";
        return new Markup($"[grey]  {icon} {tool.ToolName.EscapeMarkup()}[/]");
    }

    private static IRenderable RenderProgressActivity(ProgressActivity progress, string spinner)
    {
        if (!progress.IsComplete)
        {
            return new Markup($"[grey]{spinner} {progress.Message.EscapeMarkup()}[/]");
        }

        var icon = progress.Success ? "✓" : "✗";
        return new Markup($"[grey]{icon} {progress.Message.EscapeMarkup()}[/]");
    }

    private void TrimEntries()
    {
        while (_entries.Count > _maxVisibleEntries)
        {
            _entries.RemoveAt(0);
        }
    }
}
