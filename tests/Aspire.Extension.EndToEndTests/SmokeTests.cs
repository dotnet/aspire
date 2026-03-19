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
                timeout: TimeSpan.FromMinutes(5));

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
