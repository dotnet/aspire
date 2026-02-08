// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
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
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateTypeScriptAppHostWithViteApp));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

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
            .Find("The package Aspire.Hosting.JavaScript::");

        // In CI, aspire add shows a version selection prompt (but aspire new does not when channel is set)
        var waitingForAddVersionSelectionPrompt = new CellPatternSearcher()
            .Find("Select a version of Aspire.Hosting.JavaScript");

        // Pattern to confirm PR version is selected
        var waitingForPrVersionSelected = new CellPatternSearcher()
            .Find($"> pr-{prNumber}");

        // Pattern to confirm specific version with short SHA is selected (e.g., "> 9.3.0-dev.g1234567")
        var shortSha = commitSha[..7]; // First 7 characters of commit SHA
        var waitingForShaVersionSelected = new CellPatternSearcher()
            .Find($"g{shortSha}");

        // Pattern for aspire run ready
        var waitForCtrlCMessage = new CellPatternSearcher()
            .Find("Press CTRL+C to stop the apphost and exit.");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            // Polyglot tests require the bundle (not just CLI) because the AppHost server
            // is bundled and cannot be obtained via NuGet packages in SDK-based fallback mode
            sequenceBuilder.InstallAspireBundleFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireBundleEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Enable polyglot support feature flag
        sequenceBuilder.EnablePolyglotSupport(counter);

        // Step 1: Create TypeScript AppHost using aspire init with interactive language selection
        sequenceBuilder
            .Type("aspire init")
            .Enter()
            .WaitUntil(s => waitingForLanguageSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            // Navigate down to "TypeScript (Node.js)" which is the 2nd option
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForTypeScriptSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter() // select TypeScript
            .WaitUntil(s => waitingForAppHostCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 2: Create a Vite app using npm create vite
        // Using --template vanilla-ts for a minimal TypeScript Vite app
        // Use -y to skip npm prompts and -- to pass args to create-vite
        // Use --no-interactive to skip vite's interactive prompts (rolldown, install now, etc.)
        sequenceBuilder
            .Type("npm create -y vite@latest viteapp -- --template vanilla-ts --no-interactive")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

        // Step 3: Install Vite app dependencies
        sequenceBuilder
            .Type("cd viteapp && npm install && cd ..")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

        // Step 4: Add Aspire.Hosting.JavaScript package (with -d for debug logging)
        sequenceBuilder
            .Type("aspire add Aspire.Hosting.JavaScript -d")
            .Enter();

        // In CI, aspire add shows a version selection prompt (unlike aspire new which auto-selects when channel is set)
        if (isCI)
        {
            // First prompt: Select the PR channel (pr-XXXXX)
            // The list is: 1) version from NuGet.config (default), 2) daily, 3) pr-{N}, 4) stable
            // Navigate down to pr-{N} and select it
            sequenceBuilder
                .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .WaitUntil(s => waitingForPrVersionSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                .Enter() // select PR channel
                .WaitUntil(s => waitingForShaVersionSelected.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter();
        }

        sequenceBuilder
            .WaitUntil(s => waitingForPackageAdded.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 5: Modify apphost.ts to add the Vite app
        sequenceBuilder.ExecuteCallback(() =>
        {
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
        });

        // Step 6: Run the apphost
        sequenceBuilder
            .Type("aspire run")
            .Enter()
            .WaitUntil(s => waitForCtrlCMessage.Search(s).Count > 0, TimeSpan.FromMinutes(3));

        // Step 7: Stop the apphost
        sequenceBuilder
            .Ctrl().Key(Hex1b.Input.Hex1bKey.C)
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
