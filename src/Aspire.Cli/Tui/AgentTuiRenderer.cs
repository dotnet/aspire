// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Cli.Agent;
using Spectre.Console;

namespace Aspire.Cli.Tui;

/// <summary>
/// Main TUI renderer for the Aspire agent experience.
/// </summary>
internal sealed class AgentTuiRenderer : IAgentTuiRenderer
{
    private readonly IAnsiConsole _console;
    private static readonly Style s_aspireStyle = new(Color.MediumPurple1);

    public AgentTuiRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public async Task RunAsync(IAgentSession session, CancellationToken cancellationToken)
    {
        RenderWelcome(session.Context);

        while (!cancellationToken.IsCancellationRequested)
        {
            var prompt = await GetUserInputAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                continue;
            }

            // Handle special commands
            if (prompt.StartsWith('/'))
            {
                if (HandleSpecialCommand(prompt, session))
                {
                    continue;
                }
                if (prompt.Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
                    prompt.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            await ProcessMessageAsync(session, prompt, cancellationToken);
        }

        RenderGoodbye();
    }

    private void RenderWelcome(AgentContext context)
    {
        _console.Clear();

        // Aspire logo/header
        var header = new FigletText("Aspire Agent")
            .Color(Color.MediumPurple1);

        _console.Write(header);
        _console.WriteLine();

        // Status panel
        var modeText = context.IsOfflineMode
            ? "[yellow]Offline Mode[/] - No AppHost detected"
            : $"[green]Online Mode[/] - AppHost: {context.AppHostProject?.Name}";

        var panel = new Panel(new Markup(modeText))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = s_aspireStyle,
            Header = new PanelHeader(" Status ", Justify.Center)
        };

        _console.Write(panel);
        _console.WriteLine();

        // Help text
        _console.MarkupLine("[dim]Type your message and press Enter. Use [/][cyan]/help[/][dim] for commands, [/][cyan]/quit[/][dim] to exit.[/]");
        _console.WriteLine();
    }

    private async Task<string> GetUserInputAsync(CancellationToken cancellationToken)
    {
        _console.Markup("[cyan]>[/] ");

        var input = new StringBuilder();
        var line = await Task.Run(() =>
        {
            try
            {
                return Console.ReadLine();
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }, cancellationToken);

        return line ?? string.Empty;
    }

    private bool HandleSpecialCommand(string command, IAgentSession session)
    {
        switch (command.ToLowerInvariant())
        {
            case "/help":
                RenderHelp();
                return true;

            case "/clear":
                _console.Clear();
                RenderWelcome(session.Context);
                return true;

            case "/status":
                RenderStatus(session.Context);
                return true;

            default:
                return false;
        }
    }

    private void RenderHelp()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.MediumPurple1)
            .AddColumn("Command")
            .AddColumn("Description");

        table.AddRow("[cyan]/help[/]", "Show this help message");
        table.AddRow("[cyan]/clear[/]", "Clear the screen");
        table.AddRow("[cyan]/status[/]", "Show current status");
        table.AddRow("[cyan]/quit[/]", "Exit the agent");

        _console.WriteLine();
        _console.Write(table);
        _console.WriteLine();
    }

    private void RenderStatus(AgentContext context)
    {
        _console.WriteLine();

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("[dim]Working Directory:[/]", context.WorkingDirectory.FullName);
        grid.AddRow("[dim]Mode:[/]", context.IsOfflineMode ? "[yellow]Offline[/]" : "[green]Online[/]");

        if (!context.IsOfflineMode)
        {
            grid.AddRow("[dim]AppHost:[/]", context.AppHostProject!.FullName);
            grid.AddRow("[dim]Resources:[/]", context.Resources.Count.ToString(CultureInfo.InvariantCulture));
        }

        _console.Write(new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = s_aspireStyle,
            Header = new PanelHeader(" Status ", Justify.Center)
        });

        _console.WriteLine();
    }

    private async Task ProcessMessageAsync(IAgentSession session, string prompt, CancellationToken cancellationToken)
    {
        _console.WriteLine();

        var responseBuilder = new StringBuilder();
        var isStreaming = false;

        try
        {
            await session.SendMessageAsync(prompt, evt =>
            {
                switch (evt)
                {
                    case AssistantTurnStartEvent:
                        // Starting response
                        break;

                    case AssistantMessageDeltaEvent delta:
                        if (!isStreaming)
                        {
                            _console.Markup("[mediumpurple1]Assistant:[/] ");
                            isStreaming = true;
                        }
                        _console.Write(new Text(delta.Content));
                        responseBuilder.Append(delta.Content);
                        break;

                    case AssistantMessageEvent msg:
                        if (!isStreaming && !string.IsNullOrEmpty(msg.Content))
                        {
                            _console.MarkupLine($"[mediumpurple1]Assistant:[/] {msg.Content.EscapeMarkup()}");
                        }
                        break;

                    case ToolExecutionStartEvent toolStart:
                        if (isStreaming)
                        {
                            _console.WriteLine();
                            isStreaming = false;
                        }
                        _console.MarkupLine($"[yellow]⚙ Running:[/] [dim]{toolStart.ToolName.EscapeMarkup()}[/]");
                        break;

                    case ToolExecutionCompleteEvent toolEnd:
                        var icon = toolEnd.Success ? "[green]✓[/]" : "[red]✗[/]";
                        _console.MarkupLine($"  {icon} [dim]{toolEnd.ToolName.EscapeMarkup()} completed[/]");
                        break;

                    case SessionErrorEvent error:
                        if (isStreaming)
                        {
                            _console.WriteLine();
                            isStreaming = false;
                        }
                        _console.MarkupLine($"[red]Error:[/] {error.Message.EscapeMarkup()}");
                        break;

                    case SessionIdleEvent:
                        if (isStreaming)
                        {
                            _console.WriteLine();
                        }
                        break;
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (isStreaming)
            {
                _console.WriteLine();
            }
            _console.MarkupLine("[dim]Cancelled[/]");
        }

        _console.WriteLine();
    }

    private void RenderGoodbye()
    {
        _console.WriteLine();
        _console.MarkupLine("[mediumpurple1]Goodbye![/]");
    }
}
