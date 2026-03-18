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
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Patterns for aspire agent --help
        var agentMcpSubcommand = new CellPatternSearcher().Find("mcp");
        var agentInitSubcommand = new CellPatternSearcher().Find("init");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

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

        // Test 4: aspire mcp --help (now shows tools and call subcommands)
        var mcpToolsSubcommand = new CellPatternSearcher().Find("tools");
        var mcpCallSubcommand = new CellPatternSearcher().Find("call");
        sequenceBuilder
            .Type("aspire mcp --help")
            .Enter()
            .WaitUntil(s =>
            {
                var hasTools = mcpToolsSubcommand.Search(s).Count > 0;
                var hasCall = mcpCallSubcommand.Search(s).Count > 0;
                return hasTools && hasCall;
            }, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Test 5: aspire mcp tools --help
        var mcpToolsHelpPattern = new CellPatternSearcher().Find("aspire mcp tools [options]");
        sequenceBuilder
            .Type("aspire mcp tools --help")
            .Enter()
            .WaitUntil(s => mcpToolsHelpPattern.Search(s).Count > 0, TimeSpan.FromSeconds(30))
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
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Use .mcp.json (Claude Code format) for simpler testing
        // This is the same format used by the doctor test that passes
        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        var containerConfigPath = CliE2ETestHelpers.ToContainerPath(configPath, workspace);

        // Patterns for agent init prompts - look for the colon at the end which indicates
        // the prompt is ready for input
        var workspacePathPrompt = new CellPatternSearcher().Find("workspace:");

        // Pattern to detect if no environments are found
        var noEnvironmentsMessage = new CellPatternSearcher().Find("No agent environments");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

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
            .Type($"ls -la {containerConfigPath} && pwd")
            .Enter()
            .WaitUntil(s => fileExistsPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 2: Run aspire agent init - should detect and auto-migrate deprecated config
        // In the new flow, deprecated config migrations are applied silently
        var configurePrompt = new CellPatternSearcher().Find("configure");
        var configComplete = new CellPatternSearcher().Find("omplete");

        sequenceBuilder
            .Type("aspire agent init")
            .Enter()
            .WaitUntil(s => workspacePathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Wait(500) // Small delay to ensure prompt is ready
            .Enter() // Accept default workspace path
            .WaitUntil(s =>
            {
                // Migration happens silently. We'll see either:
                // - The configure prompt (if other environments were detected)
                // - "Configuration complete" (if only deprecated configs were found)
                // - "No agent environments" (if nothing was found)
                var hasConfigure = configurePrompt.Search(s).Count > 0;
                var hasNoEnv = noEnvironmentsMessage.Search(s).Count > 0;
                var hasComplete = configComplete.Search(s).Count > 0;
                return hasConfigure || hasNoEnv || hasComplete;
            }, TimeSpan.FromSeconds(60));

        // If we got the configure prompt, just press Enter to accept defaults
        // If we got complete/no-env, this Enter is harmless
        sequenceBuilder
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Debug: Show the scanner log file to diagnose what the scanner found
        var debugLogPattern = new CellPatternSearcher().Find("Scanning context");
        sequenceBuilder
            .Type("cat /tmp/aspire-deprecated-scan.log 2>/dev/null || echo 'No debug log found'")
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
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

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

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

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
    /// Tests that aspire agent init with a .vscode folder shows the skill pre-selected
    /// and MCP as an opt-in option, and that accepting the defaults (skill only) completes
    /// successfully and creates the skill file.
    /// </summary>
    [Fact]
    public async Task AgentInitCommand_DefaultSelection_InstallsSkillOnly()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Set up .vscode folder so VS Code scanner detects it
        var vscodePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".vscode");

        // Patterns
        var workspacePathPrompt = new CellPatternSearcher().Find("workspace:");
        var configurePrompt = new CellPatternSearcher().Find("configure");
        var skillOption = new CellPatternSearcher().Find("skill");
        var configComplete = new CellPatternSearcher().Find("complete");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Create .vscode folder so the scanner detects VS Code environment
        sequenceBuilder
            .CreateVsCodeFolder(vscodePath);

        // Run aspire agent init and accept defaults (skill is pre-selected)
        sequenceBuilder
            .Type("aspire agent init")
            .Enter()
            .WaitUntil(s => workspacePathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Wait(500)
            .Enter() // Accept default workspace path
            .WaitUntil(s => configurePrompt.Search(s).Count > 0 && skillOption.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Enter() // Accept defaults (skill pre-selected)
            .WaitUntil(s => configComplete.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Verify skill file was created
        var skillFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".github", "skills", "aspire", "SKILL.md");
        sequenceBuilder
            .VerifyFileContains(skillFilePath, "aspire start");

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}

