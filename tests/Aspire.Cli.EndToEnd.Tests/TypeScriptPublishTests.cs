// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI publish flows with a TypeScript AppHost.
/// </summary>
public sealed class TypeScriptPublishTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PublishWithDockerComposeServiceCallbackSucceeds()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        using var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, variant: CliE2ETestHelpers.DockerfileVariant.DotNet, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        var waitingForLanguagePrompt = new CellPatternSearcher()
            .Find("Which language would you like to use?");

        var waitingForTypeScriptSelected = new CellPatternSearcher()
            .Find("> TypeScript (Node.js)");

        var waitingForAppHostCreated = new CellPatternSearcher()
            .Find("Created apphost.ts");

        var waitingForPackageAdded = new CellPatternSearcher()
            .Find("The package Aspire.Hosting.");

        var waitingForRestoreSuccess = new CellPatternSearcher()
            .Find("SDK code restored successfully");

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        sequenceBuilder.EnablePolyglotSupport(counter);

        sequenceBuilder
            .Type("aspire init")
            .Enter()
            .WaitUntil(s => waitingForLanguagePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForTypeScriptSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter()
            .WaitUntil(s => waitingForAppHostCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .DeclineAgentInitPrompt(counter);

        sequenceBuilder
            .Type("aspire add Aspire.Hosting.Docker")
            .Enter()
            .WaitUntil(s => waitingForPackageAdded.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire add Aspire.Hosting.PostgreSQL")
            .Enter()
            .WaitUntil(s => waitingForPackageAdded.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire restore")
            .Enter()
            .WaitUntil(s => waitingForRestoreSuccess.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ExecuteCallback(() =>
        {
            var appHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.ts");
            var newContent = """
                import { createBuilder } from './.modules/aspire.js';

                const builder = await createBuilder();

                await builder.addDockerComposeEnvironment("compose");

                const postgres = await builder.addPostgres("postgres")
                    .publishAsDockerComposeService(async (_, svc) => {
                        await svc.name.set("postgres");
                    });

                await postgres.addDatabase("db");

                await builder.build().run();
                """;

            File.WriteAllText(appHostPath, newContent);
        });

        sequenceBuilder
            .Type("unset ASPIRE_PLAYGROUND")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire publish -o artifacts --non-interactive")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

        sequenceBuilder
            .Type("ls -la artifacts/docker-compose.yaml")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("grep -F \"postgres:\" artifacts/docker-compose.yaml")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();
        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
        await pendingRun;
    }
}
