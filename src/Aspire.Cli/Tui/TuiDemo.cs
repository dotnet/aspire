// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Tui;

/// <summary>
/// Demo runner for testing the TUI components.
/// Run with: dotnet run --project src/Aspire.Cli -- tui-demo
/// </summary>
internal static class TuiDemo
{
    public static async Task RunAsync(IAnsiConsole console, IAuxiliaryBackchannelMonitor monitor, CancellationToken cancellationToken)
    {
        using var runtime = new TuiRuntime(console);

        MainTuiLayout? layout = null;
        layout = new MainTuiLayout(monitor, onSubmit: input =>
        {
            // Handle submitted input
            if (input.StartsWith("/quit", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }
            else if (input.StartsWith("/switch ", StringComparison.OrdinalIgnoreCase))
            {
                var name = input[8..].Trim();
                layout!.AppHosts.SelectByName(name);
            }
            else
            {
                // Add as user message and fake assistant response
                layout!.Chat.AddUserMessage(input);
                var progress = layout.Chat.StartProgress("Processing...");
                
                _ = Task.Delay(1000).ContinueWith(_ =>
                {
                    progress.IsComplete = true;
                    var assistant = layout!.Chat.StartAssistantMessage();
                    assistant.Content.Append(CultureInfo.InvariantCulture, $"You said: {input}");
                    assistant.IsComplete = true;
                    runtime.ScheduleRerender();
                }, TaskScheduler.Default);
            }
        });

        // Add a welcome message
        layout.Chat.AddUserMessage("Welcome! Type a message or /quit to exit.");

        await runtime.RunAsync(layout, cancellationToken);
    }
}
