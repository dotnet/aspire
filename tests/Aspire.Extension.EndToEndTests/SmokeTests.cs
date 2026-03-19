// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Extension.EndToEndTests.Infrastructure;
using Aspire.Extension.EndToEndTests.Infrastructure.Hex1b;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Extension.EndToEndTests;

/// <summary>
/// Smoke tests that verify VS Code can be launched in a browser via Docker
/// and automated with Playwright.
/// </summary>
public sealed class SmokeTests : IClassFixture<VsCodeWebFixture>, IAsyncDisposable
{
    private readonly VsCodeWebFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SmokeTests(VsCodeWebFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task VsCodeLaunchesAndRendersWorkbench()
    {
        _output.WriteLine($"Connecting to VS Code at {_fixture.Url}");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for VS Code workbench to fully load
            _output.WriteLine("Waiting for VS Code workbench to render...");
            await page.WaitForSelectorAsync(".monaco-workbench", new()
            {
                Timeout = 120_000, // VS Code can take a while to load, especially first time
            });

            _output.WriteLine("VS Code workbench rendered successfully!");

            // Take a screenshot as proof
            await ScreenshotAsync(page, "vscode-loaded.png");

            // Verify key VS Code UI elements are present
            var workbench = await page.QuerySelectorAsync(".monaco-workbench");
            Assert.NotNull(workbench);

            // Check for the activity bar (sidebar icons)
            var activityBar = await page.QuerySelectorAsync(".activitybar");
            if (activityBar is not null)
            {
                _output.WriteLine("Activity bar found.");
            }

            // Check for the editor area
            var editor = await page.QuerySelectorAsync(".editor");
            if (editor is not null)
            {
                _output.WriteLine("Editor area found.");
            }

            // Check for the status bar
            var statusBar = await page.QuerySelectorAsync(".statusbar");
            if (statusBar is not null)
            {
                _output.WriteLine("Status bar found.");
            }
        }
        finally
        {
            await _fixture.SaveTraceAsync(nameof(VsCodeLaunchesAndRendersWorkbench));
        }
    }

    [Fact]
    public async Task Hex1bTerminalCreatesSocketViaMountedVolume()
    {
        _output.WriteLine("Verifying hex1b tool is installed and socket mount works");
        _output.WriteLine($"Socket mount path: {_fixture.Container.SocketMountPath}");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for VS Code workbench to fully load
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });

            // Dismiss any first-run dialogs
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(500);
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(1000);

            // Open the integrated terminal
            _output.WriteLine("Opening integrated terminal...");
            await page.Keyboard.PressAsync("Control+Backquote");
            await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 30_000 });
            await Task.Delay(2000);

            // Verify hex1b is available
            var hexResult = await _fixture.Container.ExecAsync("hex1b --version");
            _output.WriteLine($"hex1b --version: {hexResult.StdOut.Trim()} (exit: {hexResult.ExitCode})");
            Assert.Equal(0, hexResult.ExitCode);

            // Maximize the terminal panel so it fills the entire editor area
            await MaximizeTerminalPanelAsync(page);

            // Start hex1b terminal with attach in the VS Code terminal.
            // This spawns a headless host process (with diagnostics socket) and attaches a TUI.
            var cmd = "hex1b terminal start --attach -- /bin/bash";
            _output.WriteLine($"Typing: {cmd}");
            await page.Keyboard.TypeAsync(cmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            // Wait for the diagnostics socket to appear in the mounted directory
            var socketPath = await _fixture.Container.WaitForSocketAsync(
                timeout: TimeSpan.FromSeconds(60));

            _output.WriteLine($"Diagnostics socket found at: {socketPath}");
            Assert.True(File.Exists(socketPath), $"Socket file should exist at {socketPath}");

            await ScreenshotAsync(page, "hex1b-terminal-started.png");
            _output.WriteLine("✓ hex1b terminal started, diagnostics socket is accessible via volume mount");

            // Linger for 5 seconds so the final result is visible in the video
            await Task.Delay(5000);
        }
        finally
        {
            await _fixture.SaveTraceAsync(nameof(Hex1bTerminalCreatesSocketViaMountedVolume));
        }
    }

    [Fact]
    public async Task RemoteTerminalSessionCanSendAndReceiveText()
    {
        _output.WriteLine("Testing DiagnosticsWorkloadAdapter + RemoteTerminalSession round-trip");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for VS Code and open terminal
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(500);
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(1000);

            _output.WriteLine("Opening integrated terminal...");
            await page.Keyboard.PressAsync("Control+Backquote");
            await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 30_000 });
            await Task.Delay(2000);

            // Kill any existing hex1b processes from previous tests and clean up stale sockets
            await _fixture.Container.ExecAsync("pkill -f 'hex1b terminal' 2>/dev/null; rm -f /root/.hex1b/sockets/*.socket");
            await Task.Delay(1000);

            // Maximize the terminal panel so it fills the entire editor area
            await MaximizeTerminalPanelAsync(page);

            // Start hex1b with attach in VS Code terminal
            var cmd = "hex1b terminal start --attach -- /bin/bash";
            _output.WriteLine($"Typing: {cmd}");
            await page.Keyboard.TypeAsync(cmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            // Wait for socket
            var socketPath = await _fixture.Container.WaitForSocketAsync(timeout: TimeSpan.FromSeconds(60));
            _output.WriteLine($"Socket found: {socketPath}");

            // Connect our remote terminal session
            await using var session = await RemoteTerminalSession.ConnectAsync(socketPath);
            _output.WriteLine("Connected to remote terminal session");

            // Give the shell prompt a moment to render
            await Task.Delay(1000);
            var initialText = session.GetScreenText();
            _output.WriteLine($"Initial screen (first 200 chars): {initialText[..Math.Min(200, initialText.Length)]}");

            // Send a command and verify we see the output
            var echoMarker = $"HEX1B_ECHO_{Guid.NewGuid():N}";
            _output.WriteLine($"Sending: echo {echoMarker}");
            await session.SendTextAsync($"echo {echoMarker}\r");

            // Wait longer with periodic screen dumps for debugging
            var found = false;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(500);
                var screenNow = session.GetScreenText();
                if (screenNow.Contains(echoMarker, StringComparison.Ordinal))
                {
                    found = true;
                    _output.WriteLine($"Marker found after {(i + 1) * 500}ms");
                    break;
                }
                if (i % 4 == 0)
                {
                    var nonEmpty = session.GetNonEmptyLines();
                    _output.WriteLine($"  Poll {i}: {nonEmpty.Count} non-empty lines: {string.Join(" | ", nonEmpty.Take(5))}");
                }
            }

            var finalText = session.GetScreenText();
            _output.WriteLine($"Final screen contains marker: {found}");
            _output.WriteLine($"Final screen (first 500 chars):\n{finalText[..Math.Min(500, finalText.Length)]}");

            await ScreenshotAsync(page, "remote-terminal-echo.png");

            Assert.True(found, $"Expected to find '{echoMarker}' in terminal screen text");
            _output.WriteLine("✓ Remote terminal round-trip verified: send input → receive output");

            // Linger for 5 seconds so the final result is visible in the video
            await Task.Delay(5000);
        }
        finally
        {
            await _fixture.SaveTraceAsync(nameof(RemoteTerminalSessionCanSendAndReceiveText));
        }
    }

    [Theory]
    [InlineData("release", "latest stable GA")]
    [InlineData("dev", "daily development build from main")]
    [InlineData("staging", "prerelease from release branch")]
    public async Task InstallAspireCliViaTerminal(string quality, string description)
    {
        _output.WriteLine($"Installing Aspire CLI ({description}) with --quality {quality}");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for workbench to fully load
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });
            _output.WriteLine("VS Code workbench loaded.");

            // Clean up any previous install and hex1b processes
            _output.WriteLine("Cleaning up previous state...");
            await _fixture.Container.ExecAsync("rm -rf $HOME/.aspire /tmp/aspire-install-*");
            await _fixture.Container.ExecAsync("pkill -f 'hex1b terminal' 2>/dev/null; rm -f /root/.hex1b/sockets/*.socket");
            await Task.Delay(1000);

            // Dismiss any first-run dialogs
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(500);
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(1000);

            // Open the integrated terminal
            _output.WriteLine("Opening integrated terminal...");
            await page.Keyboard.PressAsync("Control+Backquote");
            await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 30_000 });
            await Task.Delay(2000);

            await ScreenshotAsync(page, $"terminal-opened-{quality}.png");

            // Maximize the terminal panel so it fills the entire editor area
            await MaximizeTerminalPanelAsync(page);

            // Start hex1b with asciinema recording
            var recordPath = $"/tmp/aspire-install-{quality}.cast";
            var hex1bCmd = $"hex1b terminal start --attach --record {recordPath} -- /bin/bash";
            _output.WriteLine($"Starting hex1b: {hex1bCmd}");
            await page.Keyboard.TypeAsync(hex1bCmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            // Wait for socket and connect
            var socketPath = await _fixture.Container.WaitForSocketAsync(timeout: TimeSpan.FromSeconds(60));
            _output.WriteLine($"Socket found: {socketPath}");

            await using var session = await RemoteTerminalSession.ConnectAsync(socketPath);
            _output.WriteLine("Remote terminal session connected");

            // Wait for shell prompt to appear
            var promptFound = await session.WaitForTextAsync("~#", timeout: TimeSpan.FromSeconds(10));
            _output.WriteLine($"Shell prompt ready: {promptFound}");

            // Install Aspire CLI using the official aspire.dev install script
            var installCmd = $"curl -sSL https://aspire.dev/install.sh | bash -s -- --quality {quality} --verbose";
            _output.WriteLine($"Running: {installCmd}");
            await session.SendTextAsync(installCmd + "\r");

            // Wait for install to complete — look for the success message or a new prompt
            // The install script prints "Successfully installed" on success.
            _output.WriteLine($"Waiting for Aspire CLI ({quality}) install to complete...");
            var installDone = await session.WaitForAnyTextAsync(
                ["Successfully installed", "Installation failed", "Error:"],
                timeout: TimeSpan.FromMinutes(5),
                includeScrollback: true);

            await ScreenshotAsync(page, $"install-complete-{quality}.png");

            var screenAfterInstall = session.GetScreenText();
            _output.WriteLine($"Install result signal: {installDone ?? "TIMEOUT"}");

            Assert.NotNull(installDone);
            Assert.Equal("Successfully installed", installDone);

            // Verify the CLI works
            _output.WriteLine("Verifying aspire CLI version...");
            await session.SendTextAsync("export PATH=\"$HOME/.aspire/bin:$PATH\" && aspire --version\r");
            await Task.Delay(3000);

            var versionScreen = session.GetScreenText();
            var versionLines = session.GetNonEmptyLines();
            _output.WriteLine($"Screen after aspire --version ({versionLines.Count} non-empty lines):");
            foreach (var line in versionLines.TakeLast(5))
            {
                _output.WriteLine($"  {line}");
            }

            await ScreenshotAsync(page, $"aspire-version-{quality}.png");

            // Also verify via docker exec for a clean version string
            var versionResult = await _fixture.Container.ExecAsync(
                "export PATH=$HOME/.aspire/bin:$PATH && aspire --version",
                timeout: TimeSpan.FromSeconds(30));

            var version = versionResult.StdOut.Trim();
            _output.WriteLine($"aspire --version: {version}");
            Assert.Equal(0, versionResult.ExitCode);
            Assert.NotEmpty(version);

            _output.WriteLine($"✓ Aspire CLI ({quality}) installed and verified: {version}");

            // Linger for 5 seconds so the final result is visible in the video
            await Task.Delay(5000);

            // Copy asciinema recording from container
            var artifactCastPath = Path.Combine(_fixture.ArtifactsDir, "recordings", $"aspire-install-{quality}.cast");
            await _fixture.Container.CopyFromContainerAsync(recordPath, artifactCastPath);
            if (File.Exists(artifactCastPath))
            {
                _output.WriteLine($"Asciinema recording saved: {artifactCastPath}");
            }
        }
        finally
        {
            await _fixture.SaveTraceAsync($"InstallAspireCliViaTerminal_{quality}");
        }
    }

    [Fact]
    public async Task AspireNewCreatesProjectInteractively()
    {
        _output.WriteLine("Testing interactive 'aspire new' project creation");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for VS Code and open terminal
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(500);
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(1000);

            _output.WriteLine("Opening integrated terminal...");
            await page.Keyboard.PressAsync("Control+Backquote");
            await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 30_000 });
            await Task.Delay(2000);

            // Clean up previous state
            await _fixture.Container.ExecAsync("rm -rf $HOME/.aspire /tmp/aspire-new-* /root/MyAspireApp");
            await _fixture.Container.ExecAsync("pkill -f 'hex1b terminal' 2>/dev/null; rm -f /root/.hex1b/sockets/*.socket");
            await Task.Delay(1000);

            // Maximize the terminal panel so it fills the entire editor area
            await MaximizeTerminalPanelAsync(page);

            // Start hex1b with asciinema recording
            var recordPath = "/tmp/aspire-new-interactive.cast";
            var hex1bCmd = $"hex1b terminal start --attach --record {recordPath} -- /bin/bash";
            _output.WriteLine($"Starting hex1b: {hex1bCmd}");
            await page.Keyboard.TypeAsync(hex1bCmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            // Connect remote terminal session
            var socketPath = await _fixture.Container.WaitForSocketAsync(timeout: TimeSpan.FromSeconds(60));
            await using var session = await RemoteTerminalSession.ConnectAsync(socketPath);
            _output.WriteLine("Remote terminal session connected");

            // Wait for shell prompt
            await session.WaitForTextAsync("~#", timeout: TimeSpan.FromSeconds(10));

            // Verify the session works with a quick echo test
            var marker = $"READY_{Guid.NewGuid():N}";
            await session.SendTextAsync($"echo {marker}\r");
            var echoOk = await session.WaitForTextAsync(marker, timeout: TimeSpan.FromSeconds(10));
            Assert.True(echoOk, "Remote terminal session echo test failed — input not working");
            await session.WaitForTextAsync("~#", timeout: TimeSpan.FromSeconds(5));

            // Step 1: Install Aspire CLI (release quality)
            _output.WriteLine("Installing Aspire CLI (release)...");
            await session.SendTextAsync("curl -sSL https://aspire.dev/install.sh | bash -s -- --quality release --verbose\r");

            var installDone = await session.WaitForAnyTextAsync(
                ["Successfully installed", "Installation failed"],
                timeout: TimeSpan.FromMinutes(5),
                includeScrollback: true);
            Assert.Equal("Successfully installed", installDone);
            _output.WriteLine("Aspire CLI installed");

            // Add aspire to PATH for this session
            await session.SendTextAsync("export PATH=\"$HOME/.aspire/bin:$PATH\"\r");
            await Task.Delay(1000);

            await ScreenshotAsync(page, "aspire-new-cli-installed.png");

            // Step 2: Run 'aspire new' fully interactively (no template argument)
            _output.WriteLine("Running: aspire new");
            await session.SendTextAsync("aspire new\r");

            // Wait for template selection prompt
            _output.WriteLine("Waiting for template selection prompt...");
            var templatePrompt = await session.WaitForTextAsync("Select a template", timeout: TimeSpan.FromSeconds(60));
            if (!templatePrompt)
            {
                DumpScreen(session, "Template prompt NOT found");
            }
            Assert.True(templatePrompt, "Expected 'Select a template' prompt");
            _output.WriteLine("Template selection prompt appeared");

            await ScreenshotAsync(page, "aspire-new-template-prompt.png");
            DumpScreen(session, "After template prompt");

            // Select the first template (highlighted by default) by pressing Enter
            _output.WriteLine("Selecting default template (Enter)...");
            await session.SendEnterAsync();

            // Wait for project name prompt
            _output.WriteLine("Waiting for project name prompt...");
            var namePrompt = await session.WaitForTextAsync("project name", timeout: TimeSpan.FromSeconds(15));
            Assert.True(namePrompt, "Expected project name prompt");
            _output.WriteLine("Project name prompt appeared");

            await ScreenshotAsync(page, "aspire-new-name-prompt.png");
            DumpScreen(session, "After name prompt");

            // Type a project name and press Enter
            var projectName = "MyAspireApp";
            _output.WriteLine($"Entering project name: {projectName}");
            await session.SendTextAsync(projectName);
            await Task.Delay(500);
            await session.SendEnterAsync();

            // Wait for output path prompt
            _output.WriteLine("Waiting for output path prompt...");
            var pathPrompt = await session.WaitForTextAsync("output path", timeout: TimeSpan.FromSeconds(15));
            Assert.True(pathPrompt, "Expected output path prompt");
            _output.WriteLine("Output path prompt appeared");

            await ScreenshotAsync(page, "aspire-new-path-prompt.png");

            // Accept the default output path
            _output.WriteLine("Accepting default output path (Enter)...");
            await session.SendEnterAsync();

            // Handle any additional template-specific prompts (e.g., "Use *.dev.localhost URLs",
            // "Use Redis Cache") by accepting defaults, then wait for the success message.
            // The success message format is "Created <type> project at <path>".
            _output.WriteLine("Answering any additional prompts and waiting for project creation...");
            string? created = null;
            var createDeadline = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            var promptsAnswered = 0;

            while (DateTime.UtcNow < createDeadline)
            {
                await Task.Delay(2000);

                var fullText = session.GetFullText();

                // Check for the success message — aspire new prints
                // "Project created successfully in <path>" or "Created <type> project at <path>"
                if (fullText.Contains("created successfully", StringComparison.OrdinalIgnoreCase) ||
                    fullText.Contains("project at", StringComparison.Ordinal))
                {
                    created = "Created";
                    break;
                }

                // Check for Spectre.Console selection prompts and accept defaults
                var screenText = session.GetScreenText();
                if (screenText.Contains("Type to search", StringComparison.Ordinal))
                {
                    promptsAnswered++;
                    _output.WriteLine($"  Additional prompt #{promptsAnswered} detected — pressing Enter");
                    DumpScreen(session, $"Additional prompt #{promptsAnswered}");
                    await session.SendEnterAsync();
                    continue;
                }
            }

            await ScreenshotAsync(page, "aspire-new-complete.png");
            DumpScreen(session, "After project creation");

            Assert.NotNull(created);
            Assert.Equal("Created", created);
            _output.WriteLine("Project creation completed successfully");

            // Verify project files exist inside the container
            _output.WriteLine("Verifying project files...");
            var lsResult = await _fixture.Container.ExecAsync(
                $"ls -la /root/{projectName}/",
                timeout: TimeSpan.FromSeconds(10));

            _output.WriteLine($"Project directory contents:\n{lsResult.StdOut}");
            Assert.Equal(0, lsResult.ExitCode);

            // Check for AppHost project (common across all templates)
            var findResult = await _fixture.Container.ExecAsync(
                $"find /root/{projectName} -name '*.csproj' -o -name '*.ts' -o -name 'package.json' | head -20",
                timeout: TimeSpan.FromSeconds(10));

            _output.WriteLine($"Project files found:\n{findResult.StdOut}");
            Assert.NotEmpty(findResult.StdOut.Trim());

            _output.WriteLine($"✓ Interactive 'aspire new' created project '{projectName}' successfully");

            // Linger for 5 seconds so the final result is visible in the video
            await Task.Delay(5000);

            // Copy asciinema recording
            var artifactCastPath = Path.Combine(_fixture.ArtifactsDir, "recordings", "aspire-new-interactive.cast");
            await _fixture.Container.CopyFromContainerAsync(recordPath, artifactCastPath);
            if (File.Exists(artifactCastPath))
            {
                _output.WriteLine($"Asciinema recording saved: {artifactCastPath}");
            }
        }
        finally
        {
            await _fixture.SaveTraceAsync(nameof(AspireNewCreatesProjectInteractively));
        }
    }

    /// <summary>
    /// Maximizes the VS Code terminal panel so it fills the entire editor area.
    /// Uses the command palette to run "View: Toggle Maximized Panel".
    /// </summary>
    private async Task MaximizeTerminalPanelAsync(IPage page)
    {
        _output.WriteLine("Maximizing terminal panel...");

        // Open the command palette
        await page.Keyboard.PressAsync("Control+Shift+KeyP");
        await Task.Delay(500);

        // Type the command to maximize the panel
        await page.Keyboard.TypeAsync("View: Toggle Maximized Panel", new() { Delay = 30 });
        await Task.Delay(500);

        // Execute it
        await page.Keyboard.PressAsync("Enter");
        await Task.Delay(1000);

        _output.WriteLine("Terminal panel maximized");
    }

    private void DumpScreen(RemoteTerminalSession session, string label)
    {
        var lines = session.GetNonEmptyLines();
        _output.WriteLine($"--- {label} ({lines.Count} non-empty lines) ---");
        foreach (var line in lines)
        {
            _output.WriteLine($"  | {line}");
        }
        _output.WriteLine("---");
    }

    private async Task ScreenshotAsync(IPage page, string filename)
    {
        var path = Path.Combine(_fixture.ArtifactsDir, "screenshots", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
        _output.WriteLine($"Screenshot: {path}");
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup is handled by the fixture
        await ValueTask.CompletedTask;
    }
}
