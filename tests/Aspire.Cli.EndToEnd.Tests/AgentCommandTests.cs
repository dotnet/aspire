// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI agent commands, testing the new `aspire agent`
/// command structure and backward compatibility with `aspire mcp` commands.
/// </summary>
public sealed class AgentCommandTests(ITestOutputHelper output)
{
    /// <summary>
    /// Tests that all agent command help outputs are correct, including:
    /// - aspire agent --help (shows subcommands: mcp, init)
    /// - aspire agent mcp --help (shows MCP server description)
    /// - aspire agent init --help (shows init description)
    /// - aspire mcp --help (legacy, still works)
    /// - aspire mcp start --help (legacy, still works)
    /// </summary>
    [Fact]
    public async Task AgentCommands_AllHelpOutputs_AreCorrect()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(AgentCommands_AllHelpOutputs_AreCorrect));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Patterns for aspire agent --help
        var agentMcpSubcommand = new CellPatternSearcher().Find("mcp");
        var agentInitSubcommand = new CellPatternSearcher().Find("init");

        // Pattern for legacy aspire mcp --help (should still work)
        var legacyMcpStart = new CellPatternSearcher().Find("start");
        var legacyMcpInit = new CellPatternSearcher().Find("init");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Test 1: aspire agent --help
        sequenceBuilder
            .Type("aspire agent --help")
            .Enter()
            .WaitUntil(s =>
            {
                var hasMcp = agentMcpSubcommand.Search(s).Count > 0;
                var hasInit = agentInitSubcommand.Search(s).Count > 0;
                return hasMcp && hasInit;
            }, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Test 2: aspire agent mcp --help
        // Using a more specific pattern that won't match later outputs
        var mcpHelpPattern = new CellPatternSearcher().Find("aspire agent mcp [options]");
        sequenceBuilder
            .Type("aspire agent mcp --help")
            .Enter()
            .WaitUntil(s => mcpHelpPattern.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Test 3: aspire agent init --help
        var initHelpPattern = new CellPatternSearcher().Find("aspire agent init [options]");
        sequenceBuilder
            .Type("aspire agent init --help")
            .Enter()
            .WaitUntil(s => initHelpPattern.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Test 4: aspire mcp --help (legacy, should still work)
        sequenceBuilder
            .Type("aspire mcp --help")
            .Enter()
            .WaitUntil(s =>
            {
                var hasStart = legacyMcpStart.Search(s).Count > 0;
                var hasInit = legacyMcpInit.Search(s).Count > 0;
                return hasStart && hasInit;
            }, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Test 5: aspire mcp start --help (legacy, should still work)
        var legacyMcpStartPattern = new CellPatternSearcher().Find("aspire mcp start [options]");
        sequenceBuilder
            .Type("aspire mcp start --help")
            .Enter()
            .WaitUntil(s => legacyMcpStartPattern.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    /// <summary>
    /// Tests that deprecated MCP configs are detected and can be migrated
    /// to the new agent mcp format during aspire agent init.
    /// </summary>
    [Fact]
    public async Task AgentInitCommand_MigratesDeprecatedConfig()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(AgentInitCommand_MigratesDeprecatedConfig));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var vscodePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".vscode");
        var vscodeConfigPath = Path.Combine(vscodePath, "mcp.json");

        // Patterns for agent init prompts
        var workspacePathPrompt = new CellPatternSearcher().Find("workspace path");

        // Patterns for deprecated config detection in agent init
        var deprecatedPrompt = new CellPatternSearcher().Find("Update");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Create .vscode folder with deprecated config file directly
        // This simulates a config that was created by an older version of the CLI
        // Using single-line JSON to avoid any whitespace parsing issues
        var deprecatedConfig = """{"servers":{"aspire":{"type":"stdio","command":"aspire","args":["mcp","start"]}}}""";

        sequenceBuilder
            .CreateVsCodeFolder(vscodePath)
            .ExecuteCallback(() => File.WriteAllText(vscodeConfigPath, deprecatedConfig));

        // Verify the deprecated config was created
        sequenceBuilder
            .VerifyFileContains(vscodeConfigPath, "\"mcp\"")
            .VerifyFileContains(vscodeConfigPath, "\"start\"");

        // Step 2: Run aspire agent init - should detect deprecated config
        sequenceBuilder
            .Type("aspire agent init")
            .Enter()
            .WaitUntil(s => workspacePathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // Accept default workspace path
            .WaitUntil(s => deprecatedPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Type(" ") // Space to select update
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 3: Verify config was updated to new format
        sequenceBuilder
            .VerifyFileContains(vscodeConfigPath, "\"agent\"")
            .VerifyFileContains(vscodeConfigPath, "\"mcp\"")
            .VerifyFileDoesNotContain(vscodeConfigPath, "\"start\"");

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    /// <summary>
    /// Tests that aspire doctor warns about deprecated agent configs.
    /// </summary>
    [Fact]
    public async Task DoctorCommand_DetectsDeprecatedAgentConfig()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(DoctorCommand_DetectsDeprecatedAgentConfig));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");

        // Pattern to detect deprecated config warning in doctor output
        var deprecatedWarning = new CellPatternSearcher().Find("deprecated");

        // Pattern to detect fix suggestion
        var fixSuggestion = new CellPatternSearcher().Find("aspire agent init");

        // Pattern to detect doctor completion
        var doctorComplete = new CellPatternSearcher().Find("dev-certs");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create deprecated config file
        sequenceBuilder
            .CreateDeprecatedMcpConfig(configPath)
            .Type("aspire doctor")
            .Enter()
            .WaitUntil(s =>
            {
                var hasComplete = doctorComplete.Search(s).Count > 0;
                var hasDeprecated = deprecatedWarning.Search(s).Count > 0;
                var hasFix = fixSuggestion.Search(s).Count > 0;
                return hasComplete && hasDeprecated && hasFix;
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
