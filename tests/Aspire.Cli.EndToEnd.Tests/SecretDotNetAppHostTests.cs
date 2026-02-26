// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Tests aspire secret CRUD operations on a .NET AppHost.
/// </summary>
public sealed class SecretDotNetAppHostTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SecretCrudOnDotNetAppHost()
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

        // Create a .NET AppHost project
        var waitingForProjectCreated = new CellPatternSearcher()
            .Find("Project created successfully");

        sequenceBuilder
            .Type("aspire new aspire-apphost-singlefile --name TestSecrets --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForProjectCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Change into the project directory
        sequenceBuilder
            .Type("cd TestSecrets")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set a secret
        var waitingForSetSuccess = new CellPatternSearcher()
            .Find("set successfully");

        sequenceBuilder
            .Type("aspire secret set Azure:Location eastus2 --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Set another secret
        sequenceBuilder
            .Type("aspire secret set Parameters:db-password s3cret --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Get a secret
        var waitingForGetValue = new CellPatternSearcher()
            .Find("eastus2");

        sequenceBuilder
            .Type("aspire secret get Azure:Location --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForGetValue.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // List secrets
        var waitingForListOutput = new CellPatternSearcher()
            .Find("Parameters:db-password");

        sequenceBuilder
            .Type("aspire secret list --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForListOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // List secrets as JSON
        var waitingForJsonOutput = new CellPatternSearcher()
            .Find("Azure:Location");

        sequenceBuilder
            .Type("aspire secret list --format json --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForJsonOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Delete a secret
        var waitingForDeleteSuccess = new CellPatternSearcher()
            .Find("deleted successfully");

        sequenceBuilder
            .Type("aspire secret delete Azure:Location --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForDeleteSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Verify deletion - list should only show remaining secret
        var waitingForRemainingSecret = new CellPatternSearcher()
            .Find("db-password");

        sequenceBuilder
            .Type("aspire secret list --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForRemainingSecret.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
        await pendingRun;
    }
}
