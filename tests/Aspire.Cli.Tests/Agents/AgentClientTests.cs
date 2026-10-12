// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Agents;

public class AgentClientTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void Catalog_HasStableClientIdentifiers()
    {
        Assert.Equal(["copilot", "claude", "opencode"], AgentClient.All.Select(client => client.Id));
    }

    [Fact]
    public void ProjectPaths_IgnoreUserOverridesAndShareCompatibilityLocations()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executionContext = workspace.CreateExecutionContext();
        var environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["COPILOT_HOME"] = "custom-copilot",
            ["CLAUDE_CONFIG_DIR"] = "custom-claude",
            ["OPENCODE_CONFIG"] = "custom-opencode.json",
            ["OPENCODE_CONFIG_DIR"] = "custom-opencode",
            ["XDG_CONFIG_HOME"] = "custom-xdg"
        });

        Assert.Equal(
            [Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills"), Path.Combine(workspace.WorkspaceRoot.FullName, ".claude", "skills")],
            AgentClient.All.Select(client => client.GetSkillBaseDirectory(AgentConfigurationScope.Project, workspace.WorkspaceRoot, executionContext, environment)).Distinct());
        Assert.Equal(
            [Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json"), Path.Combine(workspace.WorkspaceRoot.FullName, "opencode.jsonc")],
            AgentClient.All.Select(client => client.GetMcpConfigPath(AgentConfigurationScope.Project, workspace.WorkspaceRoot, executionContext, environment)).Distinct());
    }

    [Fact]
    public void UserPaths_RespectClientOverridesWithoutRelocatingSharedSkills()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executionContext = workspace.CreateExecutionContext();
        var copilotDirectory = Path.Combine(workspace.WorkspaceRoot.FullName, "custom-copilot");
        var claudeDirectory = Path.Combine(workspace.WorkspaceRoot.FullName, "custom-claude");
        var openCodeFile = Path.Combine(workspace.WorkspaceRoot.FullName, "custom-opencode.json");
        var environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["COPILOT_HOME"] = copilotDirectory,
            ["CLAUDE_CONFIG_DIR"] = claudeDirectory,
            ["OPENCODE_CONFIG"] = openCodeFile
        });

        Assert.Equal(
            [Path.Combine(copilotDirectory, "mcp-config.json"), Path.Combine(claudeDirectory, ".claude.json"), openCodeFile],
            AgentClient.All.Select(client => client.GetMcpConfigPath(AgentConfigurationScope.User, workspace.WorkspaceRoot, executionContext, environment)));
        Assert.Equal(
            [Path.Combine(executionContext.HomeDirectory.FullName, ".agents", "skills"), Path.Combine(claudeDirectory, "skills")],
            AgentClient.All.Select(client => client.GetSkillBaseDirectory(AgentConfigurationScope.User, workspace.WorkspaceRoot, executionContext, environment)).Distinct());
    }

    [Theory]
    [InlineData("OPENCODE_CONFIG_DIR", "custom-opencode", "opencode.jsonc")]
    [InlineData("XDG_CONFIG_HOME", "custom-xdg", "opencode", "opencode.jsonc")]
    public void OpenCode_UserConfigDirectories_AreResolvedRelativeToExecutionContext(string variable, string value, params string[] suffix)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executionContext = workspace.CreateExecutionContext();
        var environment = new TestEnvironment(new Dictionary<string, string?> { [variable] = value });
        var expected = Path.Combine([workspace.WorkspaceRoot.FullName, value, .. suffix]);

        Assert.Equal(expected, AgentClient.OpenCode.GetMcpConfigPath(AgentConfigurationScope.User, workspace.WorkspaceRoot, executionContext, environment));
    }
}
