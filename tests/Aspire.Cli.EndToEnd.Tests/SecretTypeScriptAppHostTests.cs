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
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var isCI = CliE2ETestHelpers.IsRunningInCI;

        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        // Patterns for project creation
        var waitingForLanguagePrompt = new CellPatternSearcher()
            .Find("> C#");

        var waitingForTypeScriptSelected = new CellPatternSearcher()
            .Find("> TypeScript");

        var waitingForAppHostCreated = new CellPatternSearcher()
            .Find("Created TypeScript");

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
        }

        // Enable polyglot support
        sequenceBuilder.EnablePolyglotSupport(counter);

        // Create TypeScript AppHost via aspire init
        sequenceBuilder
            .Type("aspire init")
            .Enter()
            .WaitUntil(s => waitingForLanguagePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForTypeScriptSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter()
            .WaitUntil(s => waitingForAppHostCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Set secrets using --apphost
        var waitingForSetSuccess = new CellPatternSearcher()
            .Find("set successfully");

        sequenceBuilder
            .Type("aspire secret set MyConfig:ApiKey test-key-123 --apphost apphost.ts")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire secret set ConnectionStrings:Db Server=localhost --apphost apphost.ts")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Get
        var waitingForGetValue = new CellPatternSearcher()
            .Find("test-key-123");

        sequenceBuilder
            .Type("aspire secret get MyConfig:ApiKey --apphost apphost.ts")
            .Enter()
            .WaitUntil(s => waitingForGetValue.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // List
        var waitingForListOutput = new CellPatternSearcher()
            .Find("ConnectionStrings:Db");

        sequenceBuilder
            .Type("aspire secret list --apphost apphost.ts")
            .Enter()
            .WaitUntil(s => waitingForListOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Delete
        var waitingForDeleteSuccess = new CellPatternSearcher()
            .Find("deleted successfully");

        sequenceBuilder
            .Type("aspire secret delete MyConfig:ApiKey --apphost apphost.ts")
            .Enter()
            .WaitUntil(s => waitingForDeleteSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Verify deletion
        sequenceBuilder
            .Type("aspire secret list --apphost apphost.ts")
            .Enter()
            .WaitUntil(s => waitingForListOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();
        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
        await pendingRun;
    }
}
