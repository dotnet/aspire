// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Extension.EndToEndTests.Infrastructure;
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
            var screenshotPath = Path.Combine(_fixture.ArtifactsDir, "screenshots", "vscode-loaded.png");
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

            await page.ScreenshotAsync(new()
            {
                Path = screenshotPath,
                FullPage = true,
            });

            _output.WriteLine($"Screenshot saved to: {screenshotPath}");

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
            // Save trace for this test
            await _fixture.SaveTraceAsync(nameof(VsCodeLaunchesAndRendersWorkbench));
        }
    }

    [Fact]
    public async Task InstallAspireCliViaTerminal()
    {
        _output.WriteLine($"Connecting to VS Code at {_fixture.Url}");

        var page = await _fixture.CreatePageAsync();

        try
        {
            // Wait for workbench to fully load
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });
            _output.WriteLine("VS Code workbench loaded.");

            // Dismiss any first-run dialogs/overlays by pressing Escape a couple times
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

            // Take a screenshot showing the terminal
            await ScreenshotAsync(page, "terminal-opened.png");

            // Install Aspire CLI using the official aspire.dev install script.
            // See: https://aspire.dev/get-started/install-cli/
            // We write a marker file on completion so we can detect it via docker exec
            // (xterm.js renders to canvas, so we can't read terminal text from the DOM).
            var installCmd = "curl -sSL https://aspire.dev/install.sh | bash -s -- --verbose 2>&1; echo $? > /tmp/aspire-install-exit-code";
            _output.WriteLine($"Running: {installCmd}");
            await page.Keyboard.TypeAsync(installCmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            // Poll via docker exec for the marker file to appear
            _output.WriteLine("Waiting for Aspire CLI install to complete...");
            var completedSuccessfully = false;
            var timeout = TimeSpan.FromMinutes(5);
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(5000);

                var result = await _fixture.Container.ExecAsync("cat /tmp/aspire-install-exit-code 2>/dev/null");
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

            // Take a screenshot of the final state
            await ScreenshotAsync(page, "install-complete.png");

            Assert.True(completedSuccessfully, "Aspire CLI install should complete with exit code 0");

            // Verify the CLI works by running aspire --version inside the container
            _output.WriteLine("Verifying aspire CLI version via docker exec...");
            var versionResult = await _fixture.Container.ExecAsync(
                "export PATH=$HOME/.aspire/bin:$PATH && aspire --version",
                timeout: TimeSpan.FromSeconds(30));

            _output.WriteLine($"aspire --version exit code: {versionResult.ExitCode}");
            _output.WriteLine($"aspire --version output: {versionResult.StdOut.Trim()}");
            Assert.Equal(0, versionResult.ExitCode);
            Assert.NotEmpty(versionResult.StdOut.Trim());

            // Also run it in the VS Code terminal for the screenshot
            await page.Keyboard.TypeAsync("export PATH=\"$HOME/.aspire/bin:$PATH\" && aspire --version", new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");
            await Task.Delay(5000);
            await ScreenshotAsync(page, "aspire-version.png");
        }
        finally
        {
            await _fixture.SaveTraceAsync(nameof(InstallAspireCliViaTerminal));
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
