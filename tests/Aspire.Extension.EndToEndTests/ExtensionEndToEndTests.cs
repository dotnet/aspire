// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Extension.EndToEndTests.Infrastructure;
using Aspire.Extension.EndToEndTests.Infrastructure.Hex1b;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Extension.EndToEndTests;

/// <summary>
/// End-to-end tests for the Aspire VS Code extension running in a browser
/// via Docker + Playwright + Hex1b terminal automation.
/// </summary>
public sealed class ExtensionEndToEndTests : IClassFixture<VsCodeWebFixture>, IAsyncDisposable
{
    private readonly VsCodeWebFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ExtensionEndToEndTests(VsCodeWebFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ExtensionShowsResourcesFromRunningAppHost()
    {
        var testDir = _fixture.CreateTestOutputDir(nameof(ExtensionShowsResourcesFromRunningAppHost));

        // Skip if no locally-built artifacts are available
        var artifacts = _fixture.Artifacts;
        if (artifacts is null)
        {
            _output.WriteLine("SKIP: No locally-built artifacts detected. " +
                AspireBuildArtifacts.DescribeMissing(FindRepoRoot()));
            return;
        }

        _output.WriteLine("=== Full Integration Test: Extension + AppHost Resources ===");
        _output.WriteLine($"  CLI:      {artifacts.CliPublishDirectory}");
        _output.WriteLine($"  VSIX:     {artifacts.VsixPath}");
        _output.WriteLine($"  Packages: {artifacts.PackagesDirectory}");

        var projectName = "MyAspireApp";
        var envVars = "PATH=~/.aspire/bin:$PATH ASPIRE_PLAYGROUND=true BUILT_NUGETS_PATH=/opt/aspire/packages " +
                      "TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false";

        var page = await _fixture.CreatePageAsync(testDir);

        try
        {
            // ===== Phase 1: Launch VS Code and set up terminal =====
            _output.WriteLine("--- Phase 1: Opening VS Code and terminal ---");
            await page.WaitForSelectorAsync(".monaco-workbench", new() { Timeout = 120_000 });
            await page.Keyboard.PressAsync("Escape");
            await page.Keyboard.PressAsync("Escape");

            await page.Keyboard.PressAsync("Control+Backquote");
            await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 60_000 });
            // Brief pause to let the terminal shell initialize before typing
            await Task.Delay(1000);

            // Clean up previous state
            await _fixture.Container.ExecAsync($"rm -rf $HOME/.aspire /tmp/integration-* /root/{projectName}");
            await _fixture.Container.ExecAsync("pkill -f 'hex1b terminal' 2>/dev/null");
            _fixture.Container.ResetHex1bPorts();

            await MaximizeTerminalPanelAsync(page);

            // Start hex1b with asciinema recording
            var recordPath = "/tmp/integration-test.cast";
            var (containerPort, wsUri) = _fixture.Container.AllocateHex1bPort();
            var hex1bCmd = $"hex1b terminal start --passthru --port {containerPort} --bind 0.0.0.0 --record {recordPath} -- /bin/bash";
            _output.WriteLine($"Starting hex1b: {hex1bCmd}");

            // Click on the terminal area to ensure it has focus before typing the hex1b command
            var terminalArea = await page.WaitForSelectorAsync(".terminal-wrapper", new() { Timeout = 5000 });
            if (terminalArea is not null)
            {
                await terminalArea.ClickAsync();
            }
            await page.Keyboard.TypeAsync(hex1bCmd, new() { Delay = 20 });
            await page.Keyboard.PressAsync("Enter");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _fixture.Container.WaitForHex1bAsync(wsUri, timeout: TimeSpan.FromSeconds(60));
            _output.WriteLine($"WaitForHex1b: {sw.Elapsed.TotalSeconds:F1}s");

            // Verify hex1b is actually running inside the container before connecting
            var psResult = await _fixture.Container.ExecAsync("pgrep -a hex1b || echo 'NO_HEX1B_RUNNING'");
            _output.WriteLine($"hex1b process check: {psResult.StdOut.Trim()} ({sw.Elapsed.TotalSeconds:F1}s)");

            await using var session = await RemoteTerminalSession.ConnectAsync(wsUri, log: msg => _output.WriteLine($"  [{sw.Elapsed.TotalSeconds:F1}s] {msg}"));
            _output.WriteLine($"Remote terminal session connected ({sw.Elapsed.TotalSeconds:F1}s)");

            // Set up the PROMPT_COMMAND trick: every command completion shows [N OK] $ or [N ERR:code] $
            var counter = await session.SetupPromptAsync();
            _output.WriteLine($"Prompt trick installed, counter at {counter.Value} ({sw.Elapsed.TotalSeconds:F1}s)");

            // ===== Phase 2: Install CLI and create project interactively =====
            _output.WriteLine("--- Phase 2: Installing local CLI ---");
            await session.SendTextAsync("mkdir -p ~/.aspire/bin && cp /opt/aspire-cli/aspire ~/.aspire/bin/aspire && chmod +x ~/.aspire/bin/aspire\r");
            await session.WaitForSuccessPromptFailFastAsync(counter);

            await session.SendTextAsync($"export {envVars}\r");
            await session.WaitForSuccessPromptAsync(counter);

            // Verify CLI version
            await session.SendTextAsync("aspire --version\r");
            await session.WaitForSuccessPromptAsync(counter, timeout: TimeSpan.FromSeconds(30));

            var versionResult = await _fixture.Container.ExecAsync(
                "export PATH=$HOME/.aspire/bin:$PATH && aspire --version",
                timeout: TimeSpan.FromSeconds(30));
            var version = versionResult.StdOut.Trim();
            _output.WriteLine($"aspire --version: {version}");
            Assert.Equal(0, versionResult.ExitCode);
            Assert.NotEmpty(version);

            await ScreenshotAsync(page, testDir, "01-cli-installed.png");
            _output.WriteLine($"✓ CLI installed: {version}");

            // Run aspire new interactively
            _output.WriteLine("--- Phase 2b: Creating project with aspire new ---");
            await session.SendTextAsync("aspire new\r");

            var templatePrompt = await session.WaitForTextAsync("Select a template", timeout: TimeSpan.FromSeconds(60));
            if (!templatePrompt)
            {
                DumpScreen(session, "Template prompt NOT found");
            }
            Assert.True(templatePrompt, "Expected 'Select a template' prompt");
            _output.WriteLine("Template selection prompt appeared");

            await session.SendEnterAsync();

            var namePrompt = await session.WaitForTextAsync("project name", timeout: TimeSpan.FromSeconds(15));
            Assert.True(namePrompt, "Expected project name prompt");

            await session.SendTextAsync(projectName);
            await session.SendEnterAsync();

            var pathPrompt = await session.WaitForTextAsync("output path", timeout: TimeSpan.FromSeconds(15));
            Assert.True(pathPrompt, "Expected output path prompt");

            await session.SendEnterAsync();

            // Handle additional template-specific prompts and wait for success
            _output.WriteLine("Answering prompts and waiting for project creation...");
            string? created = null;
            var createDeadline = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            var promptsAnswered = 0;

            while (DateTime.UtcNow < createDeadline)
            {
                await Task.Delay(500);

                var fullText = session.GetFullText();

                if (fullText.Contains("created successfully", StringComparison.OrdinalIgnoreCase) ||
                    fullText.Contains("project at", StringComparison.Ordinal))
                {
                    created = "Created";
                    break;
                }

                var screenText = session.GetScreenText();
                if (screenText.Contains("Type to search", StringComparison.Ordinal))
                {
                    promptsAnswered++;
                    _output.WriteLine($"  Additional prompt #{promptsAnswered} — pressing Enter");
                    await session.SendEnterAsync();
                    continue;
                }
            }

            Assert.NotNull(created);
            _output.WriteLine("✓ Project created successfully");
            await ScreenshotAsync(page, testDir, "02-project-created.png");

            // Handle the "Would you like to configure AI agent environments?" prompt
            // Wait for either the [y/n] prompt or the success prompt (older CLI without agent init)
            _output.WriteLine("Checking for AI agent environments prompt...");
            var agentDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            var agentPromptHandled = false;
            while (DateTime.UtcNow < agentDeadline)
            {
                await Task.Delay(200);
                var screenNow = session.GetScreenText();

                if (screenNow.Contains("[y/n]", StringComparison.Ordinal) ||
                    screenNow.Contains("AI agent", StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine("AI agent environments prompt detected — declining with 'n'");
                    await session.SendTextAsync("n\r");
                    agentPromptHandled = true;
                    break;
                }

                if (screenNow.Contains($"[{counter.Value} OK]", StringComparison.Ordinal))
                {
                    _output.WriteLine("Already at success prompt — no AI agent prompt");
                    break;
                }
            }

            // Wait for the aspire new command's success prompt
            if (agentPromptHandled)
            {
                // After declining agent init, aspire new exits → success prompt appears
                await session.WaitForSuccessPromptAsync(counter, timeout: TimeSpan.FromSeconds(15));
            }
            else
            {
                // Counter should already be past this prompt
                counter.Increment();
            }
            _output.WriteLine("Shell prompt ready");

            // ===== Phase 3: Patch SDK, restore, and add folder to workspace =====
            _output.WriteLine("--- Phase 3: Patching SDK and restoring ---");

            // Patch SDK version to use dev packages (backchannel sockets require 13.2.0+)
            var patchResult = await _fixture.Container.ExecAsync(
                $"find /root/{projectName} -name '*.csproj' -exec sed -i 's|Aspire.AppHost.Sdk/[0-9.]*\"|Aspire.AppHost.Sdk/{version}\"|g' {{}} \\; && " +
                $"head -1 /root/{projectName}/{projectName}.AppHost/{projectName}.AppHost.csproj",
                timeout: TimeSpan.FromSeconds(10));
            _output.WriteLine($"Patched AppHost SDK → {patchResult.StdOut.Trim()}");

            // Copy NuGet config and restore
            await session.SendTextAsync($"cp /opt/aspire/nuget.config /root/{projectName}/nuget.config\r");
            await session.WaitForSuccessPromptAsync(counter);

            _output.WriteLine("Running dotnet restore...");
            await session.SendTextAsync($"cd /root/{projectName} && dotnet restore\r");
            var restoreSuccess = await session.WaitForAnyPromptAsync(counter, timeout: TimeSpan.FromMinutes(5));
            if (!restoreSuccess)
            {
                DumpScreen(session, "Restore may have failed");
                _output.WriteLine("WARNING: dotnet restore may have failed — continuing anyway");
            }

            await ScreenshotAsync(page, testDir, "03-restored.png");
            _output.WriteLine("✓ NuGet restore complete");

            // Add the project folder to the VS Code workspace using `code -a`.
            // This keeps the current session alive (no page reload) while activating
            // the extension's workspaceContains:**/*.csproj trigger.
            _output.WriteLine("Adding project folder to workspace with 'code -a'...");
            await session.SendTextAsync($"code -a /root/{projectName}\r");
            await session.WaitForSuccessPromptAsync(counter);

            // Wait for the workspace trust dialog to appear (indicates VS Code processed the folder)
            _output.WriteLine("Checking for workspace trust dialog...");
            try
            {
                var trustYesButton = await page.WaitForSelectorAsync(
                    "a.monaco-button.monaco-text-button:has-text('Yes')",
                    new() { Timeout = 10_000 });
                if (trustYesButton is not null)
                {
                    await trustYesButton.ClickAsync();
                    _output.WriteLine("Dismissed workspace trust dialog (clicked Yes)");
                }
            }
            catch (TimeoutException)
            {
                _output.WriteLine("No workspace trust dialog appeared — may already be trusted");
            }

            await ScreenshotAsync(page, testDir, "04-folder-added.png");

            // Handle the "Set default apphost?" notification from the Aspire extension
            _output.WriteLine("Checking for default apphost notification...");
            var setDefaultYes = await page.QuerySelectorAsync(".notification-list-item a.monaco-button:has-text('Yes')");
            if (setDefaultYes is not null)
            {
                await setDefaultYes.ClickAsync();
                _output.WriteLine("Accepted default apphost notification (clicked Yes)");
            }
            else
            {
                _output.WriteLine("No default apphost notification found");
            }

            // ===== Phase 4: Run aspire start =====
            _output.WriteLine("--- Phase 4: Starting AppHost with aspire start ---");
            await session.SendTextAsync($"cd /root/{projectName} && aspire start\r");

            // aspire start detaches the app host, so wait for its success message
            var startedOk = await session.WaitForAnyTextAsync(
                ["AppHost started successfully", "Dashboard"],
                timeout: TimeSpan.FromMinutes(5),
                includeScrollback: true);

            if (startedOk is null)
            {
                DumpScreen(session, "aspire start - did not succeed");
                Assert.Fail("aspire start did not report success within 5 minutes");
            }
            _output.WriteLine($"✓ AppHost started — signal: {startedOk}");

            // Wait for the aspire start command to fully return to the prompt
            await session.WaitForSuccessPromptAsync(counter, timeout: TimeSpan.FromSeconds(30));

            await ScreenshotAsync(page, testDir, "05-aspire-started.png");

            // ===== Phase 5: Wait for resources to be discoverable =====
            _output.WriteLine("--- Phase 5: Waiting for resources via backchannel ---");

            var backchannelCheck = await _fixture.Container.ExecAsync(
                "ls -la /root/.aspire/cli/backchannels/ 2>/dev/null",
                timeout: TimeSpan.FromSeconds(5));
            _output.WriteLine($"Backchannel sockets:\n{backchannelCheck.StdOut.Trim()}");

            var resourcesDiscovered = false;
            for (var attempt = 0; attempt < 30; attempt++)
            {
                await Task.Delay(2000);
                var aspPsResult = await _fixture.Container.ExecAsync(
                    "export PATH=$HOME/.aspire/bin:$PATH && aspire ps --format json --resources 2>/dev/null",
                    timeout: TimeSpan.FromSeconds(15));
                var psOutput = aspPsResult.StdOut.Trim();

                if (psOutput.Contains("\"name\":", StringComparison.Ordinal))
                {
                    resourcesDiscovered = true;
                    _output.WriteLine($"✓ Resources discovered (attempt {attempt + 1})");

                    foreach (var line in psOutput.Split('\n'))
                    {
                        if (line.Contains("\"displayName\":", StringComparison.Ordinal))
                        {
                            _output.WriteLine($"  Resource: {line.Trim()}");
                        }
                    }
                    break;
                }
                _output.WriteLine($"  Attempt {attempt + 1}: resources not yet available");
            }

            if (!resourcesDiscovered)
            {
                var procResult = await _fixture.Container.ExecAsync(
                    "ps aux | grep -i apphost | head -5",
                    timeout: TimeSpan.FromSeconds(5));
                _output.WriteLine($"AppHost processes:\n{procResult.StdOut}");

                var logResult = await _fixture.Container.ExecAsync(
                    "cat $(ls -t /root/.aspire/logs/cli_*detach* 2>/dev/null | head -1) 2>/dev/null | tail -30",
                    timeout: TimeSpan.FromSeconds(10));
                _output.WriteLine($"AppHost detach log:\n{logResult.StdOut}");

                Assert.Fail("Resources not discoverable via aspire ps --resources after 60 seconds");
            }

            // ===== Phase 6: Verify extension shows resources =====
            _output.WriteLine("--- Phase 6: Verifying extension tree view ---");

            // Dismiss any lingering dialogs before interacting with the sidebar
            var lateTrustDialog = await page.QuerySelectorAsync(".monaco-dialog-box a.monaco-button:has-text('Yes')");
            if (lateTrustDialog is not null)
            {
                await lateTrustDialog.ClickAsync();
                _output.WriteLine("Dismissed late workspace trust dialog");
            }

            // Un-maximize the terminal so we can see the activity bar and sidebar
            await MaximizeTerminalPanelAsync(page);

            await ScreenshotAsync(page, testDir, "06-pre-aspire-click.png");

            // Click the Aspire icon in the activity bar to open the panel
            _output.WriteLine("Looking for Aspire activity bar icon...");
            var aspireIcon = await page.QuerySelectorAsync("[aria-label*='Aspire' i]")
                          ?? await page.QuerySelectorAsync("[id*='aspire' i]")
                          ?? await page.QuerySelectorAsync("[id*='aspire-panel']");

            if (aspireIcon is not null)
            {
                await aspireIcon.ClickAsync();
                _output.WriteLine("Clicked Aspire activity bar icon");
            }
            else
            {
                _output.WriteLine("Aspire icon not found — using command palette");
                await page.Keyboard.PressAsync("Control+Shift+KeyP");
                await page.WaitForSelectorAsync(".quick-input-widget", new() { Timeout = 5000 });
                await page.Keyboard.TypeAsync("Aspire: Focus on Running AppHosts View", new() { Delay = 30 });
                await Task.Delay(500);
                await page.Keyboard.PressAsync("Enter");
            }

            // Wait for the sidebar to render (give VS Code time to open the panel)
            await Task.Delay(2000);
            await ScreenshotAsync(page, testDir, "06-aspire-panel.png");

            // Click Refresh to trigger immediate re-discovery
            _output.WriteLine("Clicking Refresh to trigger re-discovery...");
            var refreshButton = await page.QuerySelectorAsync("[aria-label='Refresh']")
                             ?? await page.QuerySelectorAsync("a[title='Refresh']");
            if (refreshButton is not null)
            {
                await refreshButton.ClickAsync();
                _output.WriteLine("Clicked Refresh button");
            }

            // Retry loop: wait for tree items to appear
            _output.WriteLine("Waiting for tree items to appear...");
            IReadOnlyList<Microsoft.Playwright.IElementHandle> treeItems = [];
            for (var attempt = 0; attempt < 30; attempt++)
            {
                await Task.Delay(2000);
                treeItems = await page.QuerySelectorAllAsync("[role='treeitem']");
                _output.WriteLine($"  Attempt {attempt + 1}: {treeItems.Count} tree items");
                if (treeItems.Count > 0)
                {
                    break;
                }

                if (attempt % 3 == 2)
                {
                    refreshButton = await page.QuerySelectorAsync("[aria-label='Refresh']")
                                 ?? await page.QuerySelectorAsync("a[title='Refresh']");
                    if (refreshButton is not null)
                    {
                        await refreshButton.ClickAsync();
                        _output.WriteLine("  Re-clicked Refresh");
                    }
                }
            }

            await ScreenshotAsync(page, testDir, "07-resources.png");

            // Log all tree item labels
            var allLabels = new List<string>();
            foreach (var item in treeItems)
            {
                var label = await item.GetAttributeAsync("aria-label") ?? await item.InnerTextAsync();
                allLabels.Add(label);
                _output.WriteLine($"  Tree item: '{label}'");
            }
            _output.WriteLine($"All tree item labels: [{string.Join(", ", allLabels)}]");

            // Assert resources are visible
            if (treeItems.Count == 0)
            {
                await ScreenshotAsync(page, testDir, "07-no-resources-debug.png");

                await page.Keyboard.PressAsync("Control+Shift+KeyP");
                await Task.Delay(500);
                await page.Keyboard.TypeAsync("Output: Show Output Channel...", new() { Delay = 30 });
                await Task.Delay(500);
                await page.Keyboard.PressAsync("Enter");
                await Task.Delay(500);
                await page.Keyboard.TypeAsync("Aspire", new() { Delay = 30 });
                await Task.Delay(500);
                await page.Keyboard.PressAsync("Enter");
                await Task.Delay(2000);
                await ScreenshotAsync(page, testDir, "07-aspire-output-channel.png");

                Assert.Fail("No tree items found in the extension panel after 60s of retries");
            }

            _output.WriteLine($"✓ Extension shows {treeItems.Count} resource(s) in the tree view");

            // Linger for 5 seconds to capture final state in video
            await Task.Delay(5000);

            await ScreenshotAsync(page, testDir, "08-final.png");
            _output.WriteLine("=== Full Integration Test Complete ===");

            // Copy asciinema recording
            var castPath = Path.Combine(testDir, "terminal-recording.cast");
            await _fixture.Container.CopyFromContainerAsync(recordPath, castPath);
            if (File.Exists(castPath))
            {
                _output.WriteLine($"Asciinema recording: {castPath}");
            }
        }
        finally
        {
            try
            {
                await _fixture.Container.ExecAsync(
                    "export PATH=$HOME/.aspire/bin:$PATH && aspire stop 2>/dev/null || true",
                    timeout: TimeSpan.FromSeconds(15));
            }
            catch
            {
                // Best effort cleanup
            }

            await _fixture.SaveTraceAsync(testDir, nameof(ExtensionShowsResourcesFromRunningAppHost));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Aspire.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }

    /// <summary>
    /// Maximizes the VS Code terminal panel so it fills the entire editor area.
    /// </summary>
    private async Task MaximizeTerminalPanelAsync(IPage page)
    {
        _output.WriteLine("Maximizing terminal panel...");
        await page.Keyboard.PressAsync("Control+Shift+KeyP");
        await page.WaitForSelectorAsync(".quick-input-widget", new() { Timeout = 5000 });
        await page.Keyboard.TypeAsync("View: Toggle Maximized Panel", new() { Delay = 30 });
        await Task.Delay(500);
        await page.Keyboard.PressAsync("Enter");
        // Wait for the command palette to close before returning
        try
        {
            await page.WaitForSelectorAsync(".quick-input-widget", new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
        }
        catch (TimeoutException)
        {
            // If it doesn't close, press Escape to force-close it
            _output.WriteLine("Command palette didn't close, pressing Escape");
            await page.Keyboard.PressAsync("Escape");
            await Task.Delay(300);
        }
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

    private static async Task ScreenshotAsync(IPage page, string testDir, string filename)
    {
        var path = Path.Combine(testDir, filename);
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
    }

    public async ValueTask DisposeAsync()
    {
        await ValueTask.CompletedTask;
    }
}
