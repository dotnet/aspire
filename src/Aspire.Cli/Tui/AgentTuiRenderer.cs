// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Cli.Agent;
using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Tui;

/// <summary>
/// Main TUI renderer for the Aspire agent experience.
/// </summary>
internal sealed class AgentTuiRenderer : IAgentTuiRenderer
{
    private readonly IAnsiConsole _console;
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private static readonly TimeSpan s_healthPollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_buildHintInterval = TimeSpan.FromSeconds(30);

    public AgentTuiRenderer(IAnsiConsole console, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor)
    {
        _console = console;
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
    }

    public async Task RunAsync(IAgentSession session, CancellationToken cancellationToken)
    {
        using var runtime = new TuiRuntime(_console);

        MainTuiLayout? layout = null;
        layout = new MainTuiLayout(_auxiliaryBackchannelMonitor, input =>
        {
            var activeLayout = layout ?? throw new InvalidOperationException("TUI layout not initialized.");
            if (HandleSpecialCommand(input, session, activeLayout))
            {
                return;
            }

            activeLayout.Chat.AddUserMessage(input);
            runtime.ScheduleRerender();

            _ = ProcessMessageAsync(session, activeLayout, input, runtime, cancellationToken);
        });

        _auxiliaryBackchannelMonitor.ConnectionsChanged += (_, _) => runtime.ScheduleRerender();

        layout.Chat.AddUserMessage("Welcome! Type a message or /help for commands.");
        runtime.ScheduleRerender();

        using var healthMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var buildHintCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = RunHealthMonitorAsync(layout, healthMonitorCts.Token);
        _ = RunBuildHelperAsync(layout, buildHintCts.Token);

        await runtime.RunAsync(layout, cancellationToken);
    }

    private static bool HandleSpecialCommand(string command, IAgentSession session, MainTuiLayout layout)
    {
        if (!command.StartsWith('/'))
        {
            return false;
        }

        switch (command.ToLowerInvariant())
        {
            case "/help":
                layout.Chat.AddUserMessage("Commands: /help, /clear, /status, /switch <name>, /stop, /quit");
                return true;

            case "/clear":
                layout.Chat.Clear();
                return true;

            case "/status":
                layout.Chat.AddUserMessage(GetStatusMessage(session.Context));
                return true;

            case "/quit":
            case "/exit":
                Environment.Exit(0);
                return true;

            default:
                if (command.StartsWith("/switch ", StringComparison.OrdinalIgnoreCase))
                {
                    var name = command[8..].Trim();
                    if (!layout.AppHosts.SelectByName(name))
                    {
                        layout.Chat.AddError($"No AppHost matches '{name}'.");
                    }
                    return true;
                }
                if (command.Equals("/stop", StringComparison.OrdinalIgnoreCase))
                {
                    _ = StopSelectedAppHostAsync(layout);
                    return true;
                }
                return false;
        }
    }

    private static async Task StopSelectedAppHostAsync(MainTuiLayout layout)
    {
        var connection = layout.AppHosts.SelectedConnection;
        if (connection is null)
        {
            layout.Chat.AddError("No AppHost selected.");
            return;
        }

        try
        {
            var name = connection.AppHostInfo?.AppHostPath ?? "(unknown)";
            layout.Chat.StartProgress($"Stopping {name}...");
            await connection.StopAppHostAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            layout.Chat.AddError($"Failed to stop AppHost: {ex.Message}");
        }
    }

    private static string GetStatusMessage(AgentContext context)
    {
        if (context.IsOfflineMode)
        {
            return $"Working directory: {context.WorkingDirectory.FullName} (offline)";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "Working directory: {0} | AppHost: {1} | Resources: {2}",
            context.WorkingDirectory.FullName,
            context.AppHostProject!.FullName,
            context.Resources.Count);
    }

    private static async Task ProcessMessageAsync(IAgentSession session, MainTuiLayout layout, string prompt, TuiRuntime runtime, CancellationToken cancellationToken)
    {
        var responseBuilder = new StringBuilder();
        var assistantMessage = layout.Chat.StartAssistantMessage();
        ToolActivity? currentTool = null;
        runtime.ScheduleRerender();

        try
        {
            await session.SendMessageAsync(prompt, evt =>
            {
                switch (evt)
                {
                    case AssistantTurnStartEvent:
                        break;

                    case AssistantMessageDeltaEvent delta:
                        assistantMessage.Content.Append(delta.Content);
                        runtime.ScheduleRerender();
                        break;

                    case AssistantMessageEvent msg:
                        if (!string.IsNullOrEmpty(msg.Content))
                        {
                            assistantMessage.Content.Clear();
                            assistantMessage.Content.Append(msg.Content);
                        }
                        assistantMessage.IsComplete = true;
                        runtime.ScheduleRerender();
                        break;

                    case ToolExecutionStartEvent toolStart:
                        currentTool = layout.Chat.StartToolActivity(toolStart.ToolName);
                        currentTool.IsComplete = false;
                        runtime.ScheduleRerender();
                        break;

                    case ToolExecutionCompleteEvent toolEnd:
                        if (currentTool is not null && string.Equals(currentTool.ToolName, toolEnd.ToolName, StringComparison.OrdinalIgnoreCase))
                        {
                            currentTool.IsComplete = true;
                            currentTool.Success = toolEnd.Success;
                        }
                        else
                        {
                            var toolComplete = layout.Chat.StartToolActivity(toolEnd.ToolName);
                            toolComplete.IsComplete = true;
                            toolComplete.Success = toolEnd.Success;
                        }
                        runtime.ScheduleRerender();
                        break;

                    case SessionErrorEvent error:
                        layout.Chat.AddError(error.Message);
                        assistantMessage.IsComplete = true;
                        runtime.ScheduleRerender();
                        break;

                    case SessionIdleEvent:
                        assistantMessage.IsComplete = true;
                        runtime.ScheduleRerender();
                        break;
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            layout.Chat.AddError("Cancelled");
            assistantMessage.IsComplete = true;
            runtime.ScheduleRerender();
        }
    }

    private static async Task RunHealthMonitorAsync(MainTuiLayout layout, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var connection = layout.AppHosts.SelectedConnection;
            if (connection?.Rpc is not null)
            {
                try
                {
                    var unhealthyResources = new List<string>();

                    await foreach (var snapshot in connection.WatchResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (!string.IsNullOrEmpty(snapshot.State) &&
                            snapshot.State.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                        {
                            unhealthyResources.Add($"{snapshot.Name} ({snapshot.State})");
                        }
                    }

                    if (unhealthyResources.Count > 0)
                    {
                        var summary = $"Health: {unhealthyResources.Count} unhealthy";
                        var details = string.Join(", ", unhealthyResources.Take(3));
                        layout.UpdateBackgroundStatus("health", $"{summary} - {details}");
                    }
                    else
                    {
                        layout.UpdateBackgroundStatus("health", "Health: healthy");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    layout.UpdateBackgroundStatus("health", $"Health: error ({ex.Message})");
                }
            }
            else
            {
                layout.UpdateBackgroundStatus("health", null);
            }

            await Task.Delay(s_healthPollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task RunBuildHelperAsync(MainTuiLayout layout, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (layout.AppHosts.SelectedConnection is not null)
            {
                layout.UpdateBackgroundStatus("build", "Build helper: ready to build (type 'build app' or /help)");
            }
            else
            {
                layout.UpdateBackgroundStatus("build", null);
            }

            await Task.Delay(s_buildHintInterval, cancellationToken).ConfigureAwait(false);
        }
    }
}
