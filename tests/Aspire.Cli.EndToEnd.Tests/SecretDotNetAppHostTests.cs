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

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
        }

        // Create an Empty AppHost project interactively
        var waitingForTemplatePrompt = new CellPatternSearcher()
            .Find("> Starter App");

        var waitingForEmptySelected = new CellPatternSearcher()
            .Find("> Empty AppHost");

        var waitingForNamePrompt = new CellPatternSearcher()
            .Find("Enter the project name");

        var waitingForOutputPrompt = new CellPatternSearcher()
            .Find("Enter the output path");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find("localhost");

        sequenceBuilder
            .Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplatePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForEmptySelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter() // select Empty AppHost
            .Enter() // select C#
            .WaitUntil(s => waitingForNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("TestSecrets")
            .Enter()
            .WaitUntil(s => waitingForOutputPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitForSuccessPrompt(counter);

        // cd into the project
        sequenceBuilder
            .Type("cd TestSecrets")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set secrets
        var waitingForSetSuccess = new CellPatternSearcher()
            .Find("set successfully");

        sequenceBuilder
            .Type("aspire secret set Azure:Location eastus2")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire secret set Parameters:db-password s3cret")
            .Enter()
            .WaitUntil(s => waitingForSetSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Get
        var waitingForGetValue = new CellPatternSearcher()
            .Find("eastus2");

        sequenceBuilder
            .Type("aspire secret get Azure:Location")
            .Enter()
            .WaitUntil(s => waitingForGetValue.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // List
        var waitingForListOutput = new CellPatternSearcher()
            .Find("db-password");

        sequenceBuilder
            .Type("aspire secret list")
            .Enter()
            .WaitUntil(s => waitingForListOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Delete
        var waitingForDeleteSuccess = new CellPatternSearcher()
            .Find("deleted successfully");

        sequenceBuilder
            .Type("aspire secret delete Azure:Location")
            .Enter()
            .WaitUntil(s => waitingForDeleteSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Verify deletion
        sequenceBuilder
            .Type("aspire secret list")
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
