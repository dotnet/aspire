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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Test 1: aspire agent --help
        await auto.TypeAsync("aspire agent --help");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            var hasMcp = agentMcpSubcommand.Search(s).Count > 0;
            var hasInit = agentInitSubcommand.Search(s).Count > 0;
            return hasMcp && hasInit;
        }, timeout: TimeSpan.FromSeconds(30), description: "agent help showing mcp and init subcommands");
        await auto.WaitForSuccessPromptAsync(counter);

        // Test 2: aspire agent mcp --help
        // Using a more specific pattern that won't match later outputs
        var mcpHelpPattern = new CellPatternSearcher().Find("aspire agent mcp [options]");
        await auto.TypeAsync("aspire agent mcp --help");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => mcpHelpPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "agent mcp help output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Test 3: aspire agent init --help
        var initHelpPattern = new CellPatternSearcher().Find("aspire agent init [options]");
        await auto.TypeAsync("aspire agent init --help");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => initHelpPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "agent init help output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Test 4: aspire mcp --help (now shows tools and call subcommands)
        var mcpToolsSubcommand = new CellPatternSearcher().Find("tools");
        var mcpCallSubcommand = new CellPatternSearcher().Find("call");
        await auto.TypeAsync("aspire mcp --help");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            var hasTools = mcpToolsSubcommand.Search(s).Count > 0;
            var hasCall = mcpCallSubcommand.Search(s).Count > 0;
            return hasTools && hasCall;
        }, timeout: TimeSpan.FromSeconds(30), description: "mcp help showing tools and call subcommands");
        await auto.WaitForSuccessPromptAsync(counter);

        // Test 5: aspire mcp tools --help
        var mcpToolsHelpPattern = new CellPatternSearcher().Find("aspire mcp tools [options]");
        await auto.TypeAsync("aspire mcp tools --help");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => mcpToolsHelpPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "mcp tools help output");
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Step 1: Create deprecated config file using Claude Code format (.mcp.json)
        // This simulates a config that was created by an older version of the CLI
        // Using single-line JSON to avoid any whitespace parsing issues
        File.WriteAllText(configPath, """{"mcpServers":{"aspire":{"command":"aspire","args":["mcp","start"]}}}""");

        // Verify the deprecated config was created
        var fileContent = File.ReadAllText(configPath);
        if (!fileContent.Contains("\"mcp\""))
        {
            throw new InvalidOperationException($"Expected file '{configPath}' to contain '\"mcp\"' but it did not. Content: {fileContent}");
        }
        fileContent = File.ReadAllText(configPath);
        if (!fileContent.Contains("\"start\""))
        {
            throw new InvalidOperationException($"Expected file '{configPath}' to contain '\"start\"' but it did not. Content: {fileContent}");
        }

        // Debug: Show that the file exists and where we are
        var fileExistsPattern = new CellPatternSearcher().Find(".mcp.json");
        await auto.TypeAsync($"ls -la {containerConfigPath} && pwd");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => fileExistsPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(10), description: "file exists in container");
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 2: Run aspire agent init - should detect and auto-migrate deprecated config
        // In the new flow, deprecated config migrations are applied silently
        var configurePrompt = new CellPatternSearcher().Find("configure");
        var configComplete = new CellPatternSearcher().Find("omplete");

        await auto.TypeAsync("aspire agent init");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => workspacePathPrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "workspace path prompt");
        await auto.WaitAsync(500); // Small delay to ensure prompt is ready
        await auto.EnterAsync(); // Accept default workspace path
        await auto.WaitUntilAsync(s =>
        {
            // Migration happens silently. We'll see either:
            // - The configure prompt (if other environments were detected)
            // - "Configuration complete" (if only deprecated configs were found)
            // - "No agent environments" (if nothing was found)
            var hasConfigure = configurePrompt.Search(s).Count > 0;
            var hasNoEnv = noEnvironmentsMessage.Search(s).Count > 0;
            var hasComplete = configComplete.Search(s).Count > 0;
            return hasConfigure || hasNoEnv || hasComplete;
        }, timeout: TimeSpan.FromSeconds(60), description: "configure prompt, completion, or no environments message");

        // If we got the configure prompt, just press Enter to accept defaults
        // If we got complete/no-env, this Enter is harmless
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Debug: Show the scanner log file to diagnose what the scanner found
        var debugLogPattern = new CellPatternSearcher().Find("Scanning context");
        await auto.TypeAsync("cat /tmp/aspire-deprecated-scan.log 2>/dev/null || echo 'No debug log found'");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => debugLogPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(10), description: "scanner debug log output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 3: Verify config was updated to new format
        // The updated config should contain "agent" and "mcp" but not "start"
        fileContent = File.ReadAllText(configPath);
        if (!fileContent.Contains("\"agent\""))
        {
            throw new InvalidOperationException($"Expected file '{configPath}' to contain '\"agent\"' but it did not. Content: {fileContent}");
        }
        fileContent = File.ReadAllText(configPath);
        if (!fileContent.Contains("\"mcp\""))
        {
            throw new InvalidOperationException($"Expected file '{configPath}' to contain '\"mcp\"' but it did not. Content: {fileContent}");
        }
        fileContent = File.ReadAllText(configPath);
        if (fileContent.Contains("\"start\""))
        {
            throw new InvalidOperationException($"Expected file '{configPath}' to NOT contain '\"start\"' but it did. Content: {fileContent}");
        }

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create deprecated config file
        File.WriteAllText(configPath, """{"mcpServers":{"aspire":{"command":"aspire","args":["mcp","start"]}}}""");
        await auto.TypeAsync("aspire doctor");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            var hasComplete = doctorComplete.Search(s).Count > 0;
            var hasDeprecated = deprecatedWarning.Search(s).Count > 0;
            var hasFix = fixSuggestion.Search(s).Count > 0;
            return hasComplete && hasDeprecated && hasFix;
        }, timeout: TimeSpan.FromSeconds(60), description: "doctor output with deprecated warning and fix suggestion");
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create .vscode folder so the scanner detects VS Code environment
        Directory.CreateDirectory(vscodePath);

        // Run aspire agent init and accept defaults (skill is pre-selected)
        await auto.TypeAsync("aspire agent init");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => workspacePathPrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "workspace path prompt");
        await auto.WaitAsync(500);
        await auto.EnterAsync(); // Accept default workspace path
        await auto.WaitUntilAsync(s => configurePrompt.Search(s).Count > 0 && skillOption.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "configure prompt with skill option");
        await auto.EnterAsync(); // Accept defaults (skill pre-selected)
        await auto.WaitUntilAsync(s => configComplete.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "configuration complete");
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify skill file was created
        var skillFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".github", "skills", "aspire", "SKILL.md");
        var fileContent = File.ReadAllText(skillFilePath);
        if (!fileContent.Contains("aspire start"))
        {
            throw new InvalidOperationException($"Expected file '{skillFilePath}' to contain 'aspire start' but it did not. Content: {fileContent}");
        }

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}

