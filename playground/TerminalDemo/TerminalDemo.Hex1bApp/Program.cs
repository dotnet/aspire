// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b;
using Hex1b.Input;
using Hex1b.Widgets;
using TerminalDemo.Hex1bApp;

// Get the terminal socket path from environment variable
var socketPath = Environment.GetEnvironmentVariable("ASPIRE_TERMINAL_SOCKET");

// Application state
var statusMessage = "Welcome to the Aspire Terminal Demo";
var clickCount = 0;

// Reference to the app for invalidation
Hex1bApp? displayApp = null;

// Build the widget tree
Hex1bWidget BuildApp(RootContext ctx)
{
    return ctx.VStack(main =>
    [
        // Menu bar
        main.MenuBar(m =>
        [
            m.Menu("File", m =>
            [
                m.MenuItem("Quit").OnActivated(_ => displayApp?.RequestStop())
            ]),
            m.Menu("Help", m =>
            [
                m.MenuItem("About").OnActivated(_ =>
                {
                    statusMessage = "Aspire Terminal Demo - A sample TUI app using Hex1b";
                    displayApp?.Invalidate();
                })
            ])
        ]),

        // Main content
        main.Align(Alignment.Center,
            main.Border(
                main.VStack(content =>
                [
                    content.Text(""),
                    content.Text("  Aspire Terminal Demo"),
                    content.Text(""),
                    content.Text("  This is a sample TUI application running"),
                    content.Text("  inside Aspire using the Hex1b library."),
                    content.Text(""),
                    content.HStack(buttons =>
                    [
                        buttons.Text("  "),
                        buttons.Button("Click Me!").OnClick(_ =>
                        {
                            clickCount++;
                            statusMessage = $"Button clicked {clickCount} time{(clickCount == 1 ? "" : "s")}!";
                            displayApp?.Invalidate();
                        }),
                        buttons.Text("  "),
                        buttons.Button("Reset").OnClick(_ =>
                        {
                            clickCount = 0;
                            statusMessage = "Counter reset";
                            displayApp?.Invalidate();
                        }),
                        buttons.Text("  ")
                    ]),
                    content.Text("")
                ]),
                title: "Welcome"
            )
        ).Fill(),

        // Status bar
        main.InfoBar([
            "Tab", "Navigate",
            "Enter", "Activate",
            "Ctrl+Q", "Quit",
            "", statusMessage
        ])
    ]).WithInputBindings(bindings =>
    {
        bindings.Ctrl().Key(Hex1bKey.Q).Action(_ => displayApp?.RequestStop(), "Quit");
    });
}

// Main entry point
if (string.IsNullOrEmpty(socketPath))
{
    // Run with standard console presentation (standalone mode)
    await using var terminal = Hex1bTerminal.CreateBuilder()
        .WithHex1bApp((app, options) =>
        {
            displayApp = app;
            return ctx => BuildApp(ctx);
        })
        .WithMouse()
        .WithRenderOptimization()
        .Build();

    await terminal.RunAsync();
}
else
{
    // Run with UDS presentation adapter - connect to the Aspire TerminalHost
    await using var udsAdapter = new UdsClientPresentationAdapter(socketPath);
    await udsAdapter.ConnectAsync();

    await using var terminal = Hex1bTerminal.CreateBuilder()
        .WithMouse()
        .WithPresentation(udsAdapter)
        .WithHex1bApp((app, options) =>
        {
            displayApp = app;
            return ctx => BuildApp(ctx);
        })
        .WithRenderOptimization()
        .Build();

    await terminal.RunAsync();
}
