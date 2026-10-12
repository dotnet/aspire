// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Agents;
using Aspire.Cli.Commands;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Commands;

public class AgentInitClientSelectionTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("copilot", "project", ".agents")]
    [InlineData("opencode", "project", ".agents")]
    [InlineData("claude", "project", ".claude")]
    [InlineData("copilot", "global", ".agents")]
    [InlineData("opencode", "global", ".agents")]
    [InlineData("claude", "global", ".claude")]
    public async Task AgentInit_ExplicitUndetectedClient_InstallsCopiedSkillsOnlyInSelectedScope(string client, string scope, string skillFolder)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents {client} --scope {scope} --skills aspire --mcp=false");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        var root = scope == "project" ? workspace.WorkspaceRoot : homeDirectory;
        var expected = Path.Combine(root.FullName, skillFolder, "skills");
        var candidates = new[]
        {
            Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills"),
            Path.Combine(workspace.WorkspaceRoot.FullName, ".claude", "skills"),
            Path.Combine(homeDirectory.FullName, ".agents", "skills"),
            Path.Combine(homeDirectory.FullName, ".claude", "skills"),
            Path.Combine(workspace.WorkspaceRoot.FullName, ".github", "skills"),
            Path.Combine(workspace.WorkspaceRoot.FullName, ".opencode", "skills"),
            Path.Combine(workspace.WorkspaceRoot.FullName, ".opencode", "skill"),
            Path.Combine(homeDirectory.FullName, ".github", "skills"),
            Path.Combine(homeDirectory.FullName, ".opencode", "skills"),
            Path.Combine(homeDirectory.FullName, ".opencode", "skill")
        };
        Assert.Equal([expected], candidates.Where(Directory.Exists));
        Assert.Equal(
            ["SKILL.md", Path.Combine("references", "app-commands.md")],
            Directory.EnumerateFiles(Path.Combine(expected, "aspire"), "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(Path.Combine(expected, "aspire"), path)).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("project")]
    [InlineData("global")]
    public async Task AgentInit_UniversalClients_LeaveExistingAgentSpecificSkillsUntouched(string scope)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        const string original = "existing agent-specific skill";
        var existingPaths = new List<string>();
        foreach (var root in new[] { workspace.WorkspaceRoot, homeDirectory })
        {
            foreach (var folder in new[] { ".github", ".opencode" })
            {
                var directory = Path.Combine(root.FullName, folder, "skills", "aspire");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "SKILL.md");
                await File.WriteAllTextAsync(path, original);
                existingPaths.Add(path);
            }
        }

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents copilot,opencode --scope {scope} --skills aspire --mcp=false");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        var selectedRoot = scope == "project" ? workspace.WorkspaceRoot : homeDirectory;
        Assert.True(File.Exists(Path.Combine(selectedRoot.FullName, ".agents", "skills", "aspire", "SKILL.md")));
        foreach (var path in existingPaths)
        {
            Assert.Equal(original, await File.ReadAllTextAsync(path));
            Assert.Equal([path], Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*", SearchOption.AllDirectories));
        }
    }

    [Fact]
    public async Task AgentInit_CopilotAppCliAndEditor_AreOneDetectedSelection()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector
            {
                DetectedEnvironments = [AgentEnvironment.CopilotApp, AgentEnvironment.CopilotCli, AgentEnvironment.VsCode]
            };
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --skills aspire --mcp=false");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "aspire", "SKILL.md")));
        Assert.False(Directory.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".claude")));
        Assert.True(AgentClient.Copilot.Includes(AgentEnvironment.CopilotApp));
        Assert.True(AgentClient.Copilot.Includes(AgentEnvironment.CopilotCli));
        Assert.True(AgentClient.Copilot.Includes(AgentEnvironment.VsCode));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AgentInit_DefaultsToUniversalClientsAndDetectedAdditionalClients(bool detectClaude)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var hooks = new TestTelemetryHookConfigurator();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector
            {
                DetectedEnvironments = detectClaude ? [AgentEnvironment.ClaudeCode] : []
            };
            options.TelemetryHookConfiguratorFactory = _ => hooks;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --scope global --skills aspire --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var environment = provider.GetRequiredService<IEnvironment>();
        var defaults = AgentClient.All.Where(client => client.UsesUniversalSkills || detectClaude && client == AgentClient.ClaudeCode).ToArray();
        Assert.Equal(
            defaults.Select(client => client.GetMcpConfigPath(AgentConfigurationScope.Global, workspace.WorkspaceRoot, executionContext, environment)).Order(StringComparer.Ordinal),
            AgentClient.All.Select(client => client.GetMcpConfigPath(AgentConfigurationScope.Global, workspace.WorkspaceRoot, executionContext, environment)).Where(File.Exists).Order(StringComparer.Ordinal));
        Assert.True(File.Exists(Path.Combine(homeDirectory.FullName, ".agents", "skills", "aspire", "SKILL.md")));
        Assert.Equal(detectClaude, File.Exists(Path.Combine(homeDirectory.FullName, ".claude", "skills", "aspire", "SKILL.md")));
        Assert.False(Directory.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".agents")));
        Assert.Equal(detectClaude ? [AgentEnvironment.ClaudeCode] : [], hooks.ConfiguredEnvironments);
    }

    [Theory]
    [InlineData("none", "all", "--mcp")]
    [InlineData("all", "none", "--mcp=false")]
    public async Task AgentInit_NoneOrAllOff_LeavesSkillsMcpAndHooksUnchanged(string clients, string skills, string mcp)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        const string original = """{"mcpServers":{"aspire":{"command":"aspire","args":["mcp","start"]}}}""";
        await File.WriteAllTextAsync(configPath, original);
        var before = GetFilesWithoutCliLogs(workspace.WorkspaceRoot);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
            options.AgentEnvironmentDetectorFactory = provider => new AgentEnvironmentDetector(
                [new DeprecatedMcpCommandScanner(provider.GetRequiredService<CliExecutionContext>(), NullLogger<DeprecatedMcpCommandScanner>.Instance)]);
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents {clients} --scope global --skills {skills} {mcp}");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(original, await File.ReadAllTextAsync(configPath));
        Assert.Equal(before, GetFilesWithoutCliLogs(workspace.WorkspaceRoot));
        Assert.Empty(Directory.GetFileSystemEntries(homeDirectory.FullName));
    }

    [Theory]
    [InlineData("--agents unsupported")]
    [InlineData("--agents copilot,unsupported")]
    [InlineData("--agents copilot,none")]
    [InlineData("--agents")]
    [InlineData("--agents \"\"")]
    [InlineData("--scope both")]
    [InlineData("--scope 0")]
    [InlineData("--scope 1")]
    [InlineData("--scope user")]
    public void AgentInit_InvalidSelection_IsRejectedBeforeExecution(string options)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var before = GetFilesWithoutCliLogs(workspace.WorkspaceRoot);
        var result = command.Parse($"agent init {options}");

        Assert.Single(result.Errors);
        Assert.Equal(before, GetFilesWithoutCliLogs(workspace.WorkspaceRoot));
    }

    [Theory]
    [InlineData("agent init")]
    [InlineData("mcp init")]
    [InlineData("init")]
    [InlineData("new aspire-empty")]
    public void AgentSelection_SingularOption_IsRejectedBeforeExecution(string command)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var before = GetFilesWithoutCliLogs(workspace.WorkspaceRoot);
        var result = provider.GetRequiredService<RootCommand>().Parse($"{command} --agent copilot");

        Assert.NotEmpty(result.Errors);
        Assert.Equal(before, GetFilesWithoutCliLogs(workspace.WorkspaceRoot));
    }

    [Fact]
    public async Task AgentInit_InteractiveEmptySelection_DoesNotConfigureDefaultClients()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var hooks = new TestTelemetryHookConfigurator();
        var interaction = new TestInteractionService
        {
            PromptForSelectionsCallback = (_, choices, _, _) =>
            {
                Assert.Equal(["copilot", "opencode", "claude"], choices.Cast<AgentClient>().Select(client => client.Id));
                return [];
            },
            PromptForSelectionCallback = (_, _, _, _) => throw new InvalidOperationException("Scope must not be prompted for an empty agent selection.")
        };
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
            options.CliHostEnvironmentFactory = _ => TestHelpers.CreateInteractiveHostEnvironment();
            options.InteractionServiceFactory = _ => interaction;
            options.TelemetryHookConfiguratorFactory = _ => hooks;
        });
        using var provider = services.BuildServiceProvider();
        var before = GetFilesWithoutCliLogs(workspace.WorkspaceRoot);

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --skills all --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(before, GetFilesWithoutCliLogs(workspace.WorkspaceRoot));
        Assert.Empty(homeDirectory.GetFileSystemInfos());
        Assert.Empty(hooks.ConfiguredEnvironments);
    }

    [Fact]
    public async Task AgentInit_DuplicateClientsAndSharedDestinations_AreReportedOnce()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService
        {
            PromptForSelectionsCallback = (_, choices, _, _) => choices.Cast<object>()
                .OfType<SkillDefinition>()
                .Where(skill => skill.HasName("aspire")).Cast<object>().ToList()
        };
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interaction;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agents COPILOT,copilot,OpenCode --skills aspire");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        var summary = Assert.Single(interaction.DisplayedMessages, message => message.Message.StartsWith(AgentCommandStrings.InitCommand_InstalledSkillsSummary, StringComparison.Ordinal));
        await Verify(summary.Message);
    }

    [Theory]
    [InlineData("project")]
    [InlineData("global")]
    public async Task AgentInit_ExplicitMcp_UsesSelectedScopeAndSharedProjectFile(string scope)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var executionContext = TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
        var environment = new TestEnvironment();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => executionContext;
        });
        services.AddSingleton<IEnvironment>(environment);
        using var provider = services.BuildServiceProvider();
        var before = GetFilesWithoutCliLogs(workspace.WorkspaceRoot);

        var result = provider.GetRequiredService<RootCommand>().Parse($"agent init --agents all --scope {scope} --skills none --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        var selectedScope = scope == "project" ? AgentConfigurationScope.Project : AgentConfigurationScope.Global;
        var paths = AgentClient.All.Select(client => client.GetMcpConfigPath(selectedScope, workspace.WorkspaceRoot, executionContext, environment)).Distinct().ToArray();
        var root = selectedScope is AgentConfigurationScope.Project ? workspace.WorkspaceRoot : homeDirectory;
        var contents = new Dictionary<string, JsonElement>();
        foreach (var path in paths)
        {
            contents.Add(Path.GetRelativePath(root.FullName, path).Replace('\\', '/'), JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(path)));
        }

        Assert.Equal(before.Concat(paths).Order(StringComparer.Ordinal), GetFilesWithoutCliLogs(workspace.WorkspaceRoot));

        await Verify(JsonSerializer.Serialize(contents), "json").UseParameters(scope);
    }

    [Fact]
    public async Task AgentInit_ExistingPinnedMcp_IsPreserved()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var path = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        const string pinned = """{ "mcpServers": { "aspire": { "command": "custom-aspire", "args": ["--pinned"] } }, "preference": true }""";
        await File.WriteAllTextAsync(path, pinned);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agents copilot,claude --skills none --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(pinned, await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("copilot", "project", true)]
    [InlineData("claude", "project", false)]
    [InlineData("copilot", "global", false)]
    public async Task AgentInit_LegacyMcpRepairs_AreLimitedToSelectedClientAndScope(string client, string scope, bool shouldRepair)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var configDirectory = workspace.CreateDirectory(".vscode");
        var configPath = Path.Combine(configDirectory.FullName, "mcp.json");
        const string original = """{"servers":{"aspire":{"command":"aspire","args":["mcp","start"]}}}""";
        await File.WriteAllTextAsync(configPath, original);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AgentEnvironmentDetectorFactory = provider => new AgentEnvironmentDetector(
                [new DeprecatedMcpCommandScanner(provider.GetRequiredService<CliExecutionContext>(), NullLogger<DeprecatedMcpCommandScanner>.Instance)]);
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse($"agent init --agents {client} --scope {scope} --skills none --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        Assert.Equal(shouldRepair ? ["agent", "mcp"] : ["mcp", "start"],
            config.RootElement.GetProperty("servers").GetProperty("aspire").GetProperty("args").EnumerateArray().Select(argument => argument.GetString()));
        if (!shouldRepair)
        {
            Assert.Equal(original, await File.ReadAllTextAsync(configPath));
        }
    }

    [Fact]
    public async Task AgentInit_PinnedGlobalMcpOverrides_ArePreserved()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var copilot = workspace.CreateDirectory("custom-copilot");
        var claude = workspace.CreateDirectory("custom-claude");
        var openCode = workspace.CreateDirectory("custom-opencode");
        var environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["COPILOT_HOME"] = copilot.FullName,
            ["CLAUDE_CONFIG_DIR"] = claude.FullName,
            ["OPENCODE_CONFIG"] = Path.Combine(openCode.FullName, "custom.json")
        });
        var executionContext = TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot);
        var originals = new Dictionary<string, string>();
        foreach (var client in AgentClient.All)
        {
            var path = client.GetMcpConfigPath(AgentConfigurationScope.Global, workspace.WorkspaceRoot, executionContext, environment);
            var content = client == AgentClient.OpenCode
                ? """{ "mcp": { "aspire": { "command": ["pinned-aspire"] } }, "preference": true }"""
                : """{ "mcpServers": { "aspire": { "command": "pinned-aspire" } }, "preference": true }""";
            originals.Add(path, content);
            await File.WriteAllTextAsync(path, content);
        }

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        services.AddSingleton<IEnvironment>(environment);
        using var provider = services.BuildServiceProvider();
        var before = GetFilesWithoutCliLogs(workspace.WorkspaceRoot);
        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agents all --scope global --skills none --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        foreach (var (path, content) in originals)
        {
            Assert.Equal(content, await File.ReadAllTextAsync(path));
        }
        Assert.Equal(before, GetFilesWithoutCliLogs(workspace.WorkspaceRoot));
    }

    [Fact]
    public async Task AgentInit_MalformedSelectedMcp_FailsWithoutOverwriting()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var path = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        const string original = "{ malformed";
        await File.WriteAllTextAsync(path, original);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agents claude --skills none --mcp");
        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("project")]
    [InlineData("global")]
    public async Task AgentInit_Hooks_AreLimitedToSelectedDetectedEnvironmentsInGlobalScope(string scope)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var hooks = new TestTelemetryHookConfigurator();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector
            {
                DetectedEnvironments = [AgentEnvironment.CopilotApp, AgentEnvironment.ClaudeCode, AgentEnvironment.OpenCode]
            };
            options.TelemetryHookConfiguratorFactory = _ => hooks;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse($"agent init --agents claude --scope {scope} --skills aspire");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(scope == "global" ? [AgentEnvironment.ClaudeCode] : [], hooks.ConfiguredEnvironments);
    }

    [Fact]
    public async Task McpInit_LegacyDelegation_UsesClientScopeAndCopiedSkillOptions()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("mcp init --agents claude --scope project --skills aspire --mcp");
        Assert.Empty(result.Errors);
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".claude", "skills", "aspire", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json")));
    }

    private static string[] GetFilesWithoutCliLogs(DirectoryInfo workspaceRoot)
    {
        // Agent no-op assertions still allow the CLI's normal diagnostic log writes.
        var logDirectory = Path.Combine(".aspire", "logs") + Path.DirectorySeparatorChar;
        return [.. Directory.EnumerateFiles(workspaceRoot.FullName, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(workspaceRoot.FullName, path).StartsWith(logDirectory, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
    }
}
