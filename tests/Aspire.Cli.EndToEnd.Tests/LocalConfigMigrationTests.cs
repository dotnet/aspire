// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Hex1b.Input;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests verifying that legacy .aspire/settings.json files with relative paths
/// are correctly migrated to aspire.config.json with adjusted paths.
/// </summary>
/// <remarks>
/// When .aspire/settings.json stores appHostPath relative to the .aspire/ directory
/// (e.g., "../src/apphost.ts"), the migration to aspire.config.json must re-base the path
/// to be relative to the config file's own directory (e.g., "src/apphost.ts").
/// </remarks>
public sealed class LocalConfigMigrationTests(ITestOutputHelper output)
{
    /// <summary>
    /// Verifies that migrating a legacy .aspire/settings.json with a "../src/apphost.ts" path
    /// produces an aspire.config.json with the correct "src/apphost.ts" relative path.
    /// </summary>
    /// <remarks>
    /// This scenario reproduces the bug where FromLegacy() copied appHostPath verbatim,
    /// without adjusting from .aspire/-relative to project-root-relative.
    /// </remarks>
    [Fact]
    public async Task LegacySettingsMigration_AdjustsRelativeAppHostPath()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(
            repoRoot, installMode, output,
            variant: CliE2ETestHelpers.DockerfileVariant.Polyglot,
            mountDockerSocket: true,
            workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Step 1: Create a valid TypeScript AppHost using aspire init.
        // This produces apphost.ts plus .modules/, aspire.config.json, etc.
        await auto.TypeAsync("aspire init");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Which language would you like to use?", timeout: TimeSpan.FromSeconds(30));
        await auto.DownAsync();
        await auto.WaitUntilTextAsync("> TypeScript (Node.js)", timeout: TimeSpan.FromSeconds(5));
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.ts", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        // Step 2: Rearrange files to simulate a legacy project where the apphost
        // lives in a subdirectory (src/) with a .aspire/settings.json pointing to it.
        // Move apphost.ts into src/
        await auto.TypeAsync("mkdir -p src && mv apphost.ts src/apphost.ts");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Remove the aspire.config.json created by aspire init
        await auto.TypeAsync("rm -f aspire.config.json");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 3: Write a legacy .aspire/settings.json with the path relative to .aspire/
        // (i.e., "../src/apphost.ts" which is how the preview CLI stored it).
        var legacySettingsJson = """{"appHostPath":"../src/apphost.ts","language":"typescript/nodejs","sdkVersion":"13.2.0","channel":"staging"}""";
        await auto.TypeAsync($"mkdir -p .aspire && echo '{legacySettingsJson}' > .aspire/settings.json");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 4: Run aspire run to trigger the migration from .aspire/settings.json
        // to aspire.config.json. The migration happens in LoadConfiguration() which
        // is called before the actual build/run step.
        await auto.TypeAsync("aspire run");
        await auto.EnterAsync();

        // Wait for either the Ctrl+C message (success) or a reasonable timeout.
        // The key thing is that the migration happens before the build step,
        // so aspire.config.json will be created regardless of run outcome.
        await auto.WaitUntilAsync(s =>
        {
            // Success: apphost started
            if (s.ContainsText("Press CTRL+C to stop the apphost and exit."))
            {
                return true;
            }
            // The run proceeded far enough to create aspire.config.json
            // (even if it fails during build, the migration already happened)
            if (s.ContainsText("ERR:"))
            {
                return true;
            }
            return false;
        }, timeout: TimeSpan.FromMinutes(3), description: "aspire run started or failed after migration");

        // Stop the apphost if it's running (Ctrl+C), or just wait for the prompt
        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForAnyPromptAsync(counter, timeout: TimeSpan.FromSeconds(30));

        // Step 5: Verify aspire.config.json was created with the corrected path.
        // The path should be "src/apphost.ts" (relative to repo root),
        // NOT "../src/apphost.ts" (which was the legacy .aspire/-relative path).
        // Use grep with the exact JSON key-value to avoid matching stale terminal
        // output from the earlier echo of legacy settings.json content.
        await auto.TypeAsync("grep '\"path\": \"src/apphost.ts\"' aspire.config.json");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Also verify on the host side via bind mount
        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.config.json");
        if (File.Exists(configPath))
        {
            var content = File.ReadAllText(configPath);
            if (content.Contains("\"../src/apphost.ts\""))
            {
                throw new InvalidOperationException(
                    $"Host-side aspire.config.json still contains uncorrected path '../src/apphost.ts'. Content:\n{content}");
            }
            if (!content.Contains("\"src/apphost.ts\""))
            {
                throw new InvalidOperationException(
                    $"Host-side aspire.config.json does not contain expected path 'src/apphost.ts'. Content:\n{content}");
            }
        }

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
