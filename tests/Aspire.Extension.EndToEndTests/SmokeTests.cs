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
        _output.WriteLine($"Connecting to VS Code at {_fixture.Url}");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for workbench to fully load
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });
            _output.WriteLine("VS Code workbench loaded.");

            // Clean up any previous install so this test is independent.
            // The container is shared across Theory cases via the class fixture.
            _output.WriteLine("Cleaning up any previous Aspire CLI install...");
            await _fixture.Container.ExecAsync("rm -rf $HOME/.aspire /tmp/aspire-install-*");

            // Dismiss any first-run dialogs/overlays by pressing Escape
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(500);
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(1000);

            // Open the integrated terminal via Ctrl+`
            _output.WriteLine("Opening integrated terminal...");
            await page.Keyboard.PressAsync("Control+Backquote");

            // Wait for the terminal to appear
            await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 30_000 });
            _output.WriteLine("Terminal panel appeared.");

            // Give terminal a moment to initialize the shell
            await Task.Delay(2000);

            await ScreenshotAsync(page, $"terminal-opened-{quality}.png");

            // Install Aspire CLI using the official aspire.dev install script.
            // See: https://aspire.dev/get-started/install-cli/
            // We write a marker file on completion so we can detect it via docker exec
            // (xterm.js renders to canvas, so terminal text can't be read from the DOM).
            var markerFile = $"/tmp/aspire-install-{quality}-exit-code";
            var installCmd = $"curl -sSL https://aspire.dev/install.sh | bash -s -- --quality {quality} --verbose 2>&1; echo $? > {markerFile}";
            _output.WriteLine($"Running: {installCmd}");
            await page.Keyboard.TypeAsync(installCmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            // Poll via docker exec for the marker file to appear.
            // The script downloads ~50-80 MB, so give it generous time.
            _output.WriteLine($"Waiting for Aspire CLI ({quality}) install to complete...");
            var completedSuccessfully = false;
            var timeout = TimeSpan.FromMinutes(5);
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(5000);

                var result = await _fixture.Container.ExecAsync($"cat {markerFile} 2>/dev/null");
                if (result.ExitCode == 0 && result.StdOut.Trim().Length > 0)
                {
                    var exitCode = result.StdOut.Trim();
                    _output.WriteLine($"Install script completed with exit code: {exitCode}");

                    if (exitCode == "0")
                    {
                        completedSuccessfully = true;
                    }

                    break;
                }

                var elapsed = DateTime.UtcNow - start;
                _output.WriteLine($"  Still installing... ({elapsed.TotalSeconds:F0}s)");
            }

            await ScreenshotAsync(page, $"install-complete-{quality}.png");

            Assert.True(completedSuccessfully, $"Aspire CLI install (--quality {quality}) should complete with exit code 0");

            // Verify the CLI works by running aspire --version inside the container
            _output.WriteLine("Verifying aspire CLI version via docker exec...");
            var versionResult = await _fixture.Container.ExecAsync(
                "export PATH=$HOME/.aspire/bin:$PATH && aspire --version",
                timeout: TimeSpan.FromSeconds(30));

            var version = versionResult.StdOut.Trim();
            _output.WriteLine($"aspire --version exit code: {versionResult.ExitCode}");
            _output.WriteLine($"aspire --version output: {version}");
            Assert.Equal(0, versionResult.ExitCode);
            Assert.NotEmpty(version);

            // Also run it in the VS Code terminal for the screenshot
            await page.Keyboard.TypeAsync("export PATH=\"$HOME/.aspire/bin:$PATH\" && aspire --version", new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");
            await Task.Delay(5000);
            await ScreenshotAsync(page, $"aspire-version-{quality}.png");

            _output.WriteLine($"✓ Aspire CLI ({quality}) installed and verified: {version}");
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
