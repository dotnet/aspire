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

        // Prepare environment
        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
        }

        // Create a TypeScript AppHost project using aspire new --language typescript
        var waitingForProjectCreated = new CellPatternSearcher()
            .Find("Created TypeScript");

        sequenceBuilder
            .Type("aspire new --language typescript --name TsSecrets --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForProjectCreated.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Change into the project directory
        sequenceBuilder
            .Type("cd TsSecrets")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set a secret using --apphost to point to the TS file
        var waitingForSetSuccess = new CellPatternSearcher()
            .Find("set successfully");

        sequenceBuilder
            .Type("aspire secret set MyConfig:ApiKey test-key-123 --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Set another secret
        sequenceBuilder
            .Type("aspire secret set ConnectionStrings:Database Server=localhost --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Get a secret
        var waitingForGetValue = new CellPatternSearcher()
            .Find("test-key-123");

        sequenceBuilder
            .Type("aspire secret get MyConfig:ApiKey --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForGetValue.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // List secrets
        var waitingForListOutput = new CellPatternSearcher()
            .Find("ConnectionStrings:Database");

        sequenceBuilder
            .Type("aspire secret list --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForListOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // List secrets as JSON
        var waitingForJsonOutput = new CellPatternSearcher()
            .Find("MyConfig:ApiKey");

        sequenceBuilder
            .Type("aspire secret list --format json --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForJsonOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Delete a secret
        var waitingForDeleteSuccess = new CellPatternSearcher()
            .Find("deleted successfully");

        sequenceBuilder
            .Type("aspire secret delete MyConfig:ApiKey --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForDeleteSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Verify deletion - should only show remaining secret
        var waitingForRemainingSecret = new CellPatternSearcher()
            .Find("ConnectionStrings:Database");

        sequenceBuilder
            .Type("aspire secret list --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForRemainingSecret.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Get nonexistent key - should show error
        var waitingForNotFound = new CellPatternSearcher()
            .Find("not found");

        sequenceBuilder
            .Type("aspire secret get nope --apphost apphost.ts --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForNotFound.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
        await pendingRun;
    }
}
