// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Tests aspire secret CRUD operations on a TypeScript (polyglot) AppHost.
/// </summary>
public sealed class SecretTypeScriptAppHostTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SecretCrudOnTypeScriptAppHost()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, variant: CliE2ETestHelpers.DockerfileVariant.Polyglot, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        // Patterns for project creation
        var waitingForLanguagePrompt = new CellPatternSearcher()
            .Find("> C#");

        var waitingForTypeScriptSelected = new CellPatternSearcher()
            .Find("> TypeScript (Node.js)");

        var waitingForAppHostCreated = new CellPatternSearcher()
            .Find("Created apphost.ts");

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create TypeScript AppHost via aspire init
        await auto.TypeAsync("aspire init");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForLanguagePrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for language prompt");
        await auto.DownAsync();
        await auto.WaitUntilAsync(s => waitingForTypeScriptSelected.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(5), description: "waiting for TypeScript selected");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForAppHostCreated.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(2), description: "waiting for apphost.ts created");
        await auto.DeclineAgentInitPromptAsync(counter);

        // Set secrets using --apphost
        var waitingForSetSuccess = new CellPatternSearcher()
            .Find("set successfully");

        await auto.TypeAsync("aspire secret set MyConfig:ApiKey test-key-123 --apphost apphost.ts");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForSetSuccess.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for secret set success");
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("aspire secret set ConnectionStrings:Db Server=localhost --apphost apphost.ts");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForSetSuccess.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for connection string set success");
        await auto.WaitForSuccessPromptAsync(counter);

        // Get
        var waitingForGetValue = new CellPatternSearcher()
            .Find("test-key-123");

        await auto.TypeAsync("aspire secret get MyConfig:ApiKey --apphost apphost.ts");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForGetValue.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for secret get value");
        await auto.WaitForSuccessPromptAsync(counter);

        // List
        var waitingForListOutput = new CellPatternSearcher()
            .Find("ConnectionStrings:Db");

        await auto.TypeAsync("aspire secret list --apphost apphost.ts");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForListOutput.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for secret list output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Delete
        var waitingForDeleteSuccess = new CellPatternSearcher()
            .Find("deleted successfully");

        await auto.TypeAsync("aspire secret delete MyConfig:ApiKey --apphost apphost.ts");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForDeleteSuccess.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for secret delete success");
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify deletion
        await auto.TypeAsync("aspire secret list --apphost apphost.ts");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitingForListOutput.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for secret list after deletion");
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
