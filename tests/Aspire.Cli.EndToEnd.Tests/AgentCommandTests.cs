// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
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
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

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
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Use .mcp.json (Claude Code format) for simpler testing
        // This is the same format used by the doctor test that passes
        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");

        // Patterns for agent init prompts - look for the colon at the end which indicates
        // the prompt is ready for input
        var workspacePathPrompt = new CellPatternSearcher().Find("workspace:");

        // Patterns for deprecated config detection in agent init
        var deprecatedPrompt = new CellPatternSearcher().Find("Update");

        // Pattern to detect if no environments are found
        var noEnvironmentsMessage = new CellPatternSearcher().Find("No agent environments");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Create deprecated config file using Claude Code format (.mcp.json)
        // This simulates a config that was created by an older version of the CLI
        // Using single-line JSON to avoid any whitespace parsing issues
        sequenceBuilder
            .CreateDeprecatedMcpConfig(configPath);

        // Verify the deprecated config was created
        sequenceBuilder
            .VerifyFileContains(configPath, "\"mcp\"")
            .VerifyFileContains(configPath, "\"start\"");

        // Debug: Show that the file exists and where we are
        var fileExistsPattern = new CellPatternSearcher().Find(".mcp.json");
        sequenceBuilder
            .Type($"ls -la {configPath} && pwd")
            .Enter()
            .WaitUntil(s => fileExistsPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 2: Run aspire agent init - should detect deprecated config
        sequenceBuilder
            .Type("aspire agent init")
            .Enter()
            .WaitUntil(s => workspacePathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Wait(500) // Small delay to ensure prompt is ready
            .Enter() // Accept default workspace path
            .WaitUntil(s =>
            {
                // Either we should see the deprecated config prompt, OR the "no environments" message
                // This helps us diagnose whether the scanner is finding anything
                var hasDeprecated = deprecatedPrompt.Search(s).Count > 0;
                var hasNoEnv = noEnvironmentsMessage.Search(s).Count > 0;
                return hasDeprecated || hasNoEnv;
            }, TimeSpan.FromSeconds(60));

        // Verify we got the deprecated prompt (not "no environments")
        // This will show in the terminal capture if the test fails
        sequenceBuilder
            .Type(" ") // Space to select update
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Debug: Show the scanner log file to diagnose what the scanner found
        var debugLogPath = Path.Combine(Path.GetTempPath(), "aspire-deprecated-scan.log");
        var debugLogPattern = new CellPatternSearcher().Find("Scanning context");
        sequenceBuilder
            .Type($"cat {debugLogPath} 2>/dev/null || echo 'No debug log found'")
            .Enter()
            .WaitUntil(s => debugLogPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 3: Verify config was updated to new format
        // The updated config should contain "agent" and "mcp" but not "start"
        sequenceBuilder
            .VerifyFileContains(configPath, "\"agent\"")
            .VerifyFileContains(configPath, "\"mcp\"")
            .VerifyFileDoesNotContain(configPath, "\"start\"");

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
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

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

    /// <summary>
    /// Tests that aspire init prompts to run agent init after successful initialization,
    /// and chains into agent init when the user accepts.
    /// </summary>
    [Fact]
    public async Task InitCommand_ChainsIntoAgentInit_WhenUserAccepts()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for the agent init confirmation prompt after aspire init succeeds
        var agentInitPrompt = new CellPatternSearcher()
            .Find("configure AI agent environments");

        // Pattern for NuGet.config prompt that may appear during init with PR channel
        var nugetConfigPrompt = new CellPatternSearcher()
            .Find("NuGet.config");

        // Pattern for the workspace path prompt that appears when agent init starts
        var workspacePathPrompt = new CellPatternSearcher()
            .Find("workspace:");

        // Pattern for no environments found (expected in empty workspace)
        var noEnvironmentsMessage = new CellPatternSearcher()
            .Find("No agent environments");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Run aspire init (no solution file → creates single-file AppHost)
        // When using a PR channel, a "NuGet.config" prompt may appear during init.
        sequenceBuilder
            .Type("aspire init")
            .Enter()
            // Wait for the NuGet.config prompt (appears when using PR channel)
            .WaitUntil(s => nugetConfigPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(120))
            .Wait(500)
            .Enter() // Accept default (Yes) for NuGet.config creation
            // Wait for the agent init confirmation prompt
            .WaitUntil(s => agentInitPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(120))
            .Wait(500)
            // Accept the prompt to chain into agent init
            .Type("y")
            .Enter()
            // Wait for agent init's workspace prompt
            .WaitUntil(s => workspacePathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Wait(500)
            // Accept default workspace path
            .Enter()
            // Wait for agent init to complete (may show "No agent environments" in empty workspace)
            .WaitUntil(s => noEnvironmentsMessage.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    /// <summary>
    /// Tests that aspire init completes normally when the user declines
    /// the agent init prompt.
    /// </summary>
    [Fact]
    public async Task InitCommand_SkipsAgentInit_WhenUserDeclines()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for the agent init confirmation prompt after aspire init succeeds
        var agentInitPrompt = new CellPatternSearcher()
            .Find("configure AI agent environments");

        // Pattern for NuGet.config prompt that may appear during init with PR channel
        var nugetConfigPrompt = new CellPatternSearcher()
            .Find("NuGet.config");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Run aspire init (no solution file → creates single-file AppHost)
        // When using a PR channel, a "NuGet.config" prompt may appear during init.
        sequenceBuilder
            .Type("aspire init")
            .Enter()
            // Wait for the NuGet.config prompt (appears when using PR channel)
            .WaitUntil(s => nugetConfigPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(120))
            .Wait(500)
            .Enter() // Accept default (Yes) for NuGet.config creation
            // Wait for the agent init confirmation prompt
            .WaitUntil(s => agentInitPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(120))
            .Wait(500)
            // Decline the prompt (n is the default, just press Enter)
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

