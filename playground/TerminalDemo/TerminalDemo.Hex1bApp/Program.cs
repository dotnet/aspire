// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b;
using Hex1b.Widgets;
using TerminalDemo.Hex1bApp;

// Get the terminal socket path from environment variable
var socketPath = Environment.GetEnvironmentVariable("ASPIRE_TERMINAL_SOCKET");

// Application state
var inputText = "";
var outputText = "Output will appear here...";
var clickCount = 0;
var statusMessage = "Ready";
Hex1bApp? displayApp = null;

Hex1bWidget BuildApp(RootContext ctx)
{
    return ctx.VStack(main =>
    [
        // Menu bar at the top
        main.MenuBar(m =>
        [
            m.Menu("File", m =>
            [
                m.MenuItem("New").OnActivated(_ =>
                {
                    inputText = "";
                    outputText = "New session started";
                    clickCount = 0;
                    statusMessage = "New session";
                    displayApp?.Invalidate();
                }),
                m.Separator(),
                m.MenuItem("Quit").OnActivated(_ => displayApp?.RequestStop())
            ]),
            m.Menu("Edit", m =>
            [
                m.MenuItem("Clear Input").OnActivated(_ =>
                {
                    inputText = "";
                    statusMessage = "Input cleared";
                    displayApp?.Invalidate();
                }),
                m.MenuItem("Clear Output").OnActivated(_ =>
                {
                    outputText = "";
                    statusMessage = "Output cleared";
                    displayApp?.Invalidate();
                })
            ]),
            m.Menu("Help", m =>
            [
                m.MenuItem("About").OnActivated(_ =>
                {
                    outputText = "Aspire Terminal Demo v1.0 - Built with Hex1b";
                    statusMessage = "About";
                    displayApp?.Invalidate();
                })
            ])
        ]),

        // Main content area with border
        main.Border(
            main.VStack(content =>
            [
                content.Text(""),
                content.Text("  Welcome to the Hex1b Terminal Demo!"),
                content.Text(""),
                content.HStack(row =>
                [
                    row.Text("  Enter your name: "),
                    row.TextBox(inputText)
                        .FixedWidth(30)
                        .OnTextChanged(e => inputText = e.NewText)
                ]).FixedHeight(1),
                content.Text(""),
                content.HStack(row =>
                [
                    row.Text("  "),
                    row.Button("Say Hello").OnClick(_ =>
                    {
                        if (!string.IsNullOrWhiteSpace(inputText))
                        {
                            outputText = $"Hello, {inputText}! Welcome to Aspire.";
                            clickCount++;
                            statusMessage = $"Greeted {inputText}";
                        }
                        else
                        {
                            outputText = "Please enter your name first!";
                            statusMessage = "Name required";
                        }
                        displayApp?.Invalidate();
                    }),
                    row.Text(" "),
                    row.Button("Clear").OnClick(_ =>
                    {
                        inputText = "";
                        outputText = "Cleared!";
                        clickCount = 0;
                        statusMessage = "Cleared";
                        displayApp?.Invalidate();
                    }),
                    row.Text(" "),
                    row.Button("Exit").OnClick(_ => displayApp?.RequestStop())
                ]).FixedHeight(1),
                content.Text(""),
                content.Text($"  {outputText}"),
                content.Text(""),
                content.Text($"  Button clicked {clickCount} time(s)")
            ]),
            title: "Aspire Terminal Demo"
        ).Fill(),

        // Status bar at the bottom
        main.InfoBar([
            "Tab", "Navigate",
            "Enter", "Activate",
            "Alt+Letter", "Menu",
            "", statusMessage
        ])
    ]);
}

if (string.IsNullOrEmpty(socketPath))
{
    Console.WriteLine("ASPIRE_TERMINAL_SOCKET environment variable not set.");
    Console.WriteLine("This app is designed to be run with Aspire's WithTerminal() support.");
    Console.WriteLine();
    Console.WriteLine("Running in standalone console mode...");
    Console.WriteLine();

    // Run with standard console presentation
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
    Console.WriteLine($"Connecting to terminal socket: {socketPath}");

    // Run with UDS presentation adapter - connect to the TerminalHost
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
        .WithMouse()
        .WithRenderOptimization()
        .Build();

    await terminal.RunAsync();
}
