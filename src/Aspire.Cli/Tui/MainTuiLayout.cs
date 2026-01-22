// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Cli.Backchannel;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Tui;

/// <summary>
/// Main TUI layout combining the chat area with AppHosts sidebar.
/// </summary>
internal sealed class MainTuiLayout : TuiComponent, IInputHandler
{
    private readonly AppHostsPanel _appHostsPanel;
    private readonly ChatPanel _chatPanel;
    private readonly StringBuilder _inputBuffer = new();
    private readonly Action<string>? _onSubmit;
    private string? _completionHint;
    private int _spinnerFrame;
    private readonly Dictionary<string, string> _backgroundStatus = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] s_spinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    public MainTuiLayout(
        IAuxiliaryBackchannelMonitor monitor,
        Action<string>? onSubmit = null,
        Dictionary<string, object?>? props = null)
        : base(props)
    {
        _appHostsPanel = new AppHostsPanel(monitor);
        _chatPanel = new ChatPanel();
        _onSubmit = onSubmit;
    }

    /// <summary>
    /// Gets the chat panel for adding messages and progress.
    /// </summary>
    public ChatPanel Chat => _chatPanel;

    /// <summary>
    /// Gets the AppHosts panel for managing connections.
    /// </summary>
    public AppHostsPanel AppHosts => _appHostsPanel;

    /// <summary>
    /// Updates the background task status lines.
    /// </summary>
    public void UpdateBackgroundStatus(string key, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            if (_backgroundStatus.Remove(key))
            {
                ScheduleRerender();
            }
            return;
        }

        _backgroundStatus[key] = status;
        ScheduleRerender();
    }

    /// <summary>
    /// Gets the current input text.
    /// </summary>
    public string CurrentInput => _inputBuffer.ToString();

    public override void ComponentDidMount()
    {
        _appHostsPanel.Runtime = Runtime;
        base.ComponentDidMount();
    }

    public bool HandleInput(ConsoleKeyInfo keyInfo)
    {
        // Check if AppHosts panel handles it (number keys for quick switch)
        if (_appHostsPanel.HandleInput(keyInfo))
        {
            return true;
        }

        // Handle special keys
        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                if (_inputBuffer.Length > 0)
                {
                    var input = _inputBuffer.ToString();
                    _inputBuffer.Clear();
                    _completionHint = null;
                    _onSubmit?.Invoke(input);
                    ScheduleRerender();
                }
                return true;

            case ConsoleKey.Backspace:
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer.Length--;
                    UpdateCompletionHint();
                    ScheduleRerender();
                }
                return true;

            case ConsoleKey.Escape:
                _inputBuffer.Clear();
                _completionHint = null;
                ScheduleRerender();
                return true;

            case ConsoleKey.Tab:
                // Accept completion hint
                if (!string.IsNullOrEmpty(_completionHint))
                {
                    _inputBuffer.Clear();
                    _inputBuffer.Append(_completionHint);
                    _completionHint = null;
                    ScheduleRerender();
                }
                return true;

            default:
                // Regular character input
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _inputBuffer.Append(keyInfo.KeyChar);
                    UpdateCompletionHint();
                    ScheduleRerender();
                }
                return true;
        }
    }

    public override IRenderable Render()
    {
        _spinnerFrame = (_spinnerFrame + 1) % s_spinnerFrames.Length;
        _chatPanel.Tick();

        var consoleHeight = Console.WindowHeight;
        var consoleWidth = Console.WindowWidth;

        // Build the layout
        var chatContent = _chatPanel.Render(consoleHeight - 6);
        var inputLine = ChatPanel.RenderInputLine(_inputBuffer.ToString(), _completionHint);

        var chatArea = new Rows(
            chatContent,
            new Rule { Style = new Style(Color.Grey) },
            inputLine
        );

        if (consoleWidth > 80)
        {
            var sidebar = new Rows(
                _appHostsPanel.Render(),
                RenderBackgroundPanel()
            );

            var layout = new Columns(
                chatArea,
                sidebar
            );

            return new Rows(
                RenderHeader(),
                RenderStatus(),
                layout,
                RenderFooter()
            );
        }

        return new Rows(
            RenderHeader(),
            RenderStatus(),
            chatArea,
            RenderFooter()
        );
    }

    private static IRenderable RenderHeader()
    {
        var title = new FigletText("Aspire")
            .Color(Color.MediumPurple1);

        return title;
    }

    private IRenderable RenderStatus()
    {
        var connectionCount = _appHostsPanel.ConnectionCount;
        var connectionText = connectionCount switch
        {
            0 => "No AppHosts connected",
            1 => "1 AppHost connected",
            _ => $"{connectionCount} AppHosts connected"
        };

        return new Markup($"[grey]Status: {connectionText}[/]");
    }

    private IRenderable RenderBackgroundPanel()
    {
        if (_backgroundStatus.Count == 0)
        {
            return new Panel(new Markup("[grey]Background: idle[/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Grey),
                Header = new PanelHeader(" Background ", Justify.Center),
                Padding = new Padding(1, 0),
                Width = 24
            };
        }

        var lines = _backgroundStatus
            .Values
            .Select(line => new Markup($"[grey]{line.EscapeMarkup()}[/]"));
        var content = new Rows(lines);

        return new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Header = new PanelHeader(" Background ", Justify.Center),
            Padding = new Padding(1, 0),
            Width = 24
        };
    }

    private IRenderable RenderFooter()
    {
        var connectionCount = _appHostsPanel.ConnectionCount;
        var connectionText = connectionCount switch
        {
            0 => "[dim]No AppHosts connected[/]",
            1 => "[green]1 AppHost connected[/]",
            _ => $"[green]{connectionCount} AppHosts connected[/]"
        };

        return new Markup($"{connectionText}  [dim]|  /help for commands  |  Ctrl+C to exit[/]");
    }

    private void UpdateCompletionHint()
    {
        var input = _inputBuffer.ToString();

        // Check for /switch command completion
        if (input.StartsWith("/switch ", StringComparison.OrdinalIgnoreCase))
        {
            var partial = input[8..];
            var completions = _appHostsPanel.GetCompletions(partial);
            if (completions.Count > 0)
            {
                _completionHint = "/switch " + completions[0];
                return;
            }
        }

        // Check for command completion
        if (input.StartsWith("/"))
        {
            var commands = new[] { "/help", "/clear", "/status", "/switch ", "/quit" };
            var match = commands.FirstOrDefault(c =>
                c.StartsWith(input, StringComparison.OrdinalIgnoreCase) && c != input);
            _completionHint = match;
            return;
        }

        _completionHint = null;
    }
}
