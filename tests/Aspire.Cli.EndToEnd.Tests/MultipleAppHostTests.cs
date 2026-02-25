// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Tests that <c>aspire run --detach --format json</c> produces well-formed JSON
/// without human-readable messages polluting stdout.
/// </summary>
public sealed class MultipleAppHostTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DetachFormatJsonProducesValidJson()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // aspire new prompts
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find("Enter the project name (");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find("Enter the output path:");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find("Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find("Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find("Do you want to create a test project?");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create a single project using aspire new
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select Starter App
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("TestApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ClearScreen(counter);

        // Navigate into the project directory
        sequenceBuilder
            .Type("cd TestApp")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // First: launch the apphost with --detach (interactive, no JSON)
        // Just wait for the command to complete (WaitForSuccessPrompt waits for the shell prompt)
        sequenceBuilder
            .Type("aspire run --detach")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ClearScreen(counter);

        // Second: launch again with --detach --format json, redirecting stdout to a file.
        // This tests that the JSON output is well-formed and not polluted by human-readable messages.
        // stderr is left visible in the terminal for debugging (human-readable messages go to stderr
        // when --format json is used, which is exactly what this PR validates).
        sequenceBuilder
            .Type("aspire run --detach --format json > output.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ClearScreen(counter);

        // Validate the JSON output file is well-formed by using python to parse it
        sequenceBuilder
            .Type("python3 -c \"import json; data = json.load(open('output.json')); print('JSON_VALID'); print('appHostPath' in data); print('appHostPid' in data)\"")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Also cat the file so we can see it in the recording
        sequenceBuilder
            .Type("cat output.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Clean up: stop any running instances
        sequenceBuilder
            .Type("aspire stop --all 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
