// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.CodingAgent;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.CodingAgent;

public class CodingAgentConfiguratorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ConfigureWorkspaceAsync_ThrowsArgumentNullException_WhenWorkspacePathIsNull()
    {
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            configurator.ConfigureWorkspaceAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ConfigureWorkspaceAsync_ReturnsFalse_WhenWorkspaceDoesNotExist()
    {
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);
        var nonExistentDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        var result = await configurator.ConfigureWorkspaceAsync(nonExistentDir, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ConfigureWorkspaceAsync_CreatesGitHubDirectory_WhenItDoesNotExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);

        var result = await configurator.ConfigureWorkspaceAsync(workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".github")));
    }

    [Fact]
    public async Task ConfigureWorkspaceAsync_CreatesCopilotInstructionsFile_WhenItDoesNotExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);

        var result = await configurator.ConfigureWorkspaceAsync(workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(result);
        var copilotInstructionsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".github", "copilot-instructions.md");
        Assert.True(File.Exists(copilotInstructionsPath));
        
        var content = await File.ReadAllTextAsync(copilotInstructionsPath);
        Assert.Contains("Aspire", content);
        Assert.Contains("AppHost", content);
    }

    [Fact]
    public async Task ConfigureWorkspaceAsync_DoesNotOverwriteExistingCopilotInstructions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);

        // Create existing copilot instructions
        var githubDir = Directory.CreateDirectory(Path.Combine(workspace.WorkspaceRoot.FullName, ".github"));
        var copilotInstructionsPath = Path.Combine(githubDir.FullName, "copilot-instructions.md");
        var existingContent = "# Existing Instructions\nDo not modify this!";
        await File.WriteAllTextAsync(copilotInstructionsPath, existingContent);

        var result = await configurator.ConfigureWorkspaceAsync(workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(result);
        var content = await File.ReadAllTextAsync(copilotInstructionsPath);
        Assert.Equal(existingContent, content);
    }

    [Fact]
    public async Task ConfigureWorkspaceAsync_HandlesMcpSettingsFile_WhenItExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);

        // Create MCP settings directory and file
        var mcpDir = Directory.CreateDirectory(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp"));
        var mcpSettingsPath = Path.Combine(mcpDir.FullName, "settings.json");
        var mcpSettings = @"{
  ""mcpServers"": {
    ""other-server"": {
      ""command"": ""node"",
      ""args"": [""other-server""]
    }
  }
}";
        await File.WriteAllTextAsync(mcpSettingsPath, mcpSettings);

        var result = await configurator.ConfigureWorkspaceAsync(workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(result);
        // The MCP file should still exist and be unchanged
        Assert.True(File.Exists(mcpSettingsPath));
        var content = await File.ReadAllTextAsync(mcpSettingsPath);
        Assert.Contains("other-server", content);
    }

    [Fact]
    public async Task ConfigureWorkspaceAsync_SucceedsWhenNoMcpSettingsFile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new Git.TestLogger<CodingAgentConfigurator>(outputHelper);
        var configurator = new CodingAgentConfigurator(logger);

        var result = await configurator.ConfigureWorkspaceAsync(workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(result);
        // Should succeed even without MCP settings
        var mcpSettingsPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp", "settings.json");
        Assert.False(File.Exists(mcpSettingsPath));
    }
}
