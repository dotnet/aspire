// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end test that validates the <c>aspire restore</c> command by creating a
/// TypeScript AppHost with two integrations and verifying the generated SDK files
/// are produced in the <c>.modules/</c> directory.
/// </summary>
public sealed class TypeScriptCodegenValidationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task RestoreGeneratesSdkFiles()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;

        using var terminal = CliE2ETestHelpers.CreateTestTerminal();
        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        // Patterns for interactive prompts
        var waitingForLanguagePrompt = new CellPatternSearcher()
            .Find("Which language would you like to use?");

        var waitingForTypeScriptSelected = new CellPatternSearcher()
            .Find("> TypeScript (Node.js)");

        var waitingForAppHostCreated = new CellPatternSearcher()
            .Find("Created apphost.ts");

        var waitingForPackageAdded = new CellPatternSearcher()
            .Find("The package Aspire.Hosting.");

        var waitingForRestoreSuccess = new CellPatternSearcher()
            .Find("Polyglot SDK code restored successfully");

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            // Polyglot tests require the bundle because the AppHost server is bundled
            sequenceBuilder.InstallAspireBundleFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireBundleEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Enable polyglot support
        sequenceBuilder.EnablePolyglotSupport(counter);

        // Step 1: Create a TypeScript AppHost
        sequenceBuilder
            .Type("aspire init")
            .Enter()
            .WaitUntil(s => waitingForLanguagePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForTypeScriptSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter()
            .WaitUntil(s => waitingForAppHostCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 2: Add two integrations
        sequenceBuilder
            .Type("aspire add Aspire.Hosting.Redis")
            .Enter()
            .WaitUntil(s => waitingForPackageAdded.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire add Aspire.Hosting.SqlServer")
            .Enter()
            .WaitUntil(s => waitingForPackageAdded.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 3: Run aspire restore and verify success
        sequenceBuilder
            .Type("aspire restore")
            .Enter()
            .WaitUntil(s => waitingForRestoreSuccess.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Step 4: Verify generated SDK files exist
        sequenceBuilder.ExecuteCallback(() =>
        {
            var modulesDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".modules");
            if (!Directory.Exists(modulesDir))
            {
                throw new InvalidOperationException($".modules directory was not created at {modulesDir}");
            }

            var expectedFiles = new[] { "aspire.ts", "base.ts", "transport.ts" };
            foreach (var file in expectedFiles)
            {
                var filePath = Path.Combine(modulesDir, file);
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException($"Expected generated file not found: {filePath}");
                }

                var content = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new InvalidOperationException($"Generated file is empty: {filePath}");
                }
            }

            // Verify aspire.ts contains symbols from both integrations
            var aspireTs = File.ReadAllText(Path.Combine(modulesDir, "aspire.ts"));
            if (!aspireTs.Contains("addRedis"))
            {
                throw new InvalidOperationException("aspire.ts does not contain addRedis from Aspire.Hosting.Redis");
            }
            if (!aspireTs.Contains("addSqlServer"))
            {
                throw new InvalidOperationException("aspire.ts does not contain addSqlServer from Aspire.Hosting.SqlServer");
            }
        });

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();
        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
        await pendingRun;
    }
}
