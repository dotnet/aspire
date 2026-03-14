// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Hex1b.Input;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI with TypeScript polyglot AppHost.
/// Tests creating a TypeScript-based AppHost and adding a Vite application.
/// </summary>
public sealed class TypeScriptPolyglotTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateTypeScriptAppHostWithViteApp()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, variant: CliE2ETestHelpers.DockerfileVariant.Polyglot, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for language selection prompt
        var waitingForLanguageSelectionPrompt = new CellPatternSearcher()
            .Find("Which language would you like to use?");

        // Pattern for TypeScript language selected
        var waitingForTypeScriptSelected = new CellPatternSearcher()
            .Find("> TypeScript (Node.js)");

        // Pattern for waiting for apphost.ts creation success
        var waitingForAppHostCreated = new CellPatternSearcher()
            .Find("Created apphost.ts");

        // Pattern for aspire add completion
        var waitingForPackageAdded = new CellPatternSearcher()
            .Find("The package Aspire.Hosting.");

        // Pattern for aspire run ready
        var waitForCtrlCMessage = new CellPatternSearcher()
            .Find("Press CTRL+C to stop the apphost and exit.");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Step 1: Create TypeScript AppHost using aspire init with interactive language selection
        await auto.TypeAsync("aspire init");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForLanguageSelectionPrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for language selection prompt");
        // Navigate down to "TypeScript (Node.js)" which is the 2nd option
        await auto.DownAsync();
        await auto.WaitUntilAsync(s => waitingForTypeScriptSelected.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(5), description: "waiting for TypeScript option to be selected");
        await auto.EnterAsync(); // select TypeScript
        await auto.WaitUntilAsync(s => waitingForAppHostCreated.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(2), description: "waiting for apphost.ts creation");
        await auto.DeclineAgentInitPromptAsync(counter);

        // Step 2: Create a Vite app using npm create vite
        // Using --template vanilla-ts for a minimal TypeScript Vite app
        // Use -y to skip npm prompts and -- to pass args to create-vite
        // Use --no-interactive to skip vite's interactive prompts (rolldown, install now, etc.)
        await auto.TypeAsync("npm create -y vite@latest viteapp -- --template vanilla-ts --no-interactive");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

        // Step 3: Install Vite app dependencies
        await auto.TypeAsync("cd viteapp && npm install && cd ..");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

        // Step 4: Add Aspire.Hosting.JavaScript package
        // When channel is set (CI) and there's only one channel with one version,
        // the version is auto-selected without prompting.
        await auto.TypeAsync("aspire add Aspire.Hosting.JavaScript");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForPackageAdded.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(2), description: "waiting for JavaScript package to be added");
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 5: Modify apphost.ts to add the Vite app
        var appHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.ts");
        var newContent = """
            // Aspire TypeScript AppHost
            // For more information, see: https://aspire.dev

            import { createBuilder } from './.modules/aspire.js';

            const builder = await createBuilder();

            // Add the Vite frontend application
            const viteApp = await builder.addViteApp("viteapp", "./viteapp");

            await builder.build().run();
            """;

        File.WriteAllText(appHostPath, newContent);

        // Step 6: Run the apphost
        await auto.TypeAsync("aspire run");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitForCtrlCMessage.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(3), description: "waiting for Ctrl+C message from apphost");

        // Step 7: Stop the apphost
        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
