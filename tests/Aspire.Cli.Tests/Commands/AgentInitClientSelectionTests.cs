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
    [InlineData("copilot", "user", ".agents")]
    [InlineData("opencode", "user", ".agents")]
    [InlineData("claude", "user", ".claude")]
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
            $"agent init --agent {client} --scope {scope} --skills aspire --mcp=false");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        var root = scope == "project" ? workspace.WorkspaceRoot : homeDirectory;
        var expected = Path.Combine(root.FullName, skillFolder, "skills");
        var candidates = new[]
        {
            Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills"),
            Path.Combine(workspace.WorkspaceRoot.FullName, ".claude", "skills"),
            Path.Combine(homeDirectory.FullName, ".agents", "skills"),
            Path.Combine(homeDirectory.FullName, ".claude", "skills")
        };
        Assert.Equal([expected], candidates.Where(Directory.Exists));
        Assert.Equal(
            ["SKILL.md", Path.Combine("references", "app-commands.md")],
            Directory.EnumerateFiles(Path.Combine(expected, "aspire"), "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(Path.Combine(expected, "aspire"), path)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task AgentInit_CopilotAppCliAndEditor_AreOneDetectedSelection()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector
            {
                DetectedClients = [AgentClientKind.CopilotApp, AgentClientKind.CopilotCli, AgentClientKind.VsCode]
            };
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --skills aspire --mcp=false");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "aspire", "SKILL.md")));
        Assert.False(Directory.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".claude")));
        Assert.True(AgentClient.Copilot.Includes(AgentClientKind.CopilotApp));
        Assert.True(AgentClient.Copilot.Includes(AgentClientKind.CopilotCli));
        Assert.True(AgentClient.Copilot.Includes(AgentClientKind.VsCode));
    }

    [Fact]
    public async Task AgentInit_NoDetectedClients_DefaultsToNoneWithoutResolvingBundle()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var original = Directory.GetFileSystemEntries(workspace.WorkspaceRoot.FullName).Order(StringComparer.Ordinal).ToArray();
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(original, Directory.GetFileSystemEntries(workspace.WorkspaceRoot.FullName).Order(StringComparer.Ordinal));
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
        var before = Directory.EnumerateFiles(workspace.WorkspaceRoot.FullName, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: homeDirectory);
            options.AgentEnvironmentDetectorFactory = provider => new AgentEnvironmentDetector(
                [new DeprecatedMcpCommandScanner(provider.GetRequiredService<CliExecutionContext>(), NullLogger<DeprecatedMcpCommandScanner>.Instance)]);
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agent {clients} --scope user --skills {skills} {mcp}");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(original, await File.ReadAllTextAsync(configPath));
        Assert.Equal(before, Directory.EnumerateFiles(workspace.WorkspaceRoot.FullName, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
        Assert.Empty(Directory.GetFileSystemEntries(homeDirectory.FullName));
    }

    [Theory]
    [InlineData("--agent unsupported")]
    [InlineData("--agent \"\"")]
    [InlineData("--scope both")]
    [InlineData("--scope 0")]
    [InlineData("--scope 1")]
    public void AgentInit_InvalidSelection_IsRejectedBeforeExecution(string options)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse($"agent init {options}");

        Assert.Single(result.Errors);
        Assert.Empty(Directory.GetFileSystemEntries(workspace.WorkspaceRoot.FullName));
    }

    [Fact]
    public async Task AgentInit_DuplicateClientsAndSharedDestinations_AreReportedOnce()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interaction;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agent COPILOT,copilot,OpenCode --skills aspire");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        var summary = Assert.Single(interaction.DisplayedMessages, message => message.Message.StartsWith(AgentCommandStrings.InitCommand_InstalledSkillsSummary, StringComparison.Ordinal));
        await Verify(summary.Message);
    }

    [Theory]
    [InlineData("project")]
    [InlineData("user")]
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

        var result = provider.GetRequiredService<RootCommand>().Parse($"agent init --agent all --scope {scope} --skills none --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());

        var selectedScope = scope == "project" ? AgentConfigurationScope.Project : AgentConfigurationScope.User;
        var paths = AgentClient.All.Select(client => client.GetMcpConfigPath(selectedScope, workspace.WorkspaceRoot, executionContext, environment)).Distinct().ToArray();
        var root = selectedScope is AgentConfigurationScope.Project ? workspace.WorkspaceRoot : homeDirectory;
        var contents = new Dictionary<string, JsonElement>();
        foreach (var path in paths)
        {
            contents.Add(Path.GetRelativePath(root.FullName, path).Replace('\\', '/'), JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(path)));
        }

        Assert.Equal(paths.Order(StringComparer.Ordinal),
            Directory.EnumerateFiles(root.FullName, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
        if (selectedScope is AgentConfigurationScope.Project)
        {
            Assert.Empty(Directory.GetFileSystemEntries(homeDirectory.FullName));
        }
        else
        {
            Assert.Equal(["fake-home"], Directory.GetFileSystemEntries(workspace.WorkspaceRoot.FullName).Select(Path.GetFileName));
        }

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

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agent copilot,claude --skills none --mcp");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(pinned, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task AgentInit_MalformedSelectedMcp_FailsWithoutOverwriting()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var path = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        const string original = "{ malformed";
        await File.WriteAllTextAsync(path, original);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("agent init --agent claude --skills none --mcp");
        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("project")]
    [InlineData("user")]
    public async Task AgentInit_Hooks_AreLimitedToSelectedDetectedClientsInUserScope(string scope)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var hooks = new TestTelemetryHookConfigurator();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector
            {
                DetectedClients = [AgentClientKind.CopilotApp, AgentClientKind.ClaudeCode, AgentClientKind.OpenCode]
            };
            options.TelemetryHookConfiguratorFactory = _ => hooks;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse($"agent init --agent claude --scope {scope} --skills aspire");
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(scope == "user" ? [AgentClientKind.ClaudeCode] : [], hooks.ConfiguredClients);
    }

    [Fact]
    public async Task McpInit_LegacyDelegation_UsesClientScopeAndCopiedSkillOptions()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse("mcp init --agent claude --scope project --skills aspire --mcp");
        Assert.Empty(result.Errors);
        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".claude", "skills", "aspire", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json")));
    }
}
