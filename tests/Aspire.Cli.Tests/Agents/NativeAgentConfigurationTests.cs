// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Agents;

public class NativeAgentConfigurationTests(ITestOutputHelper output) : IDisposable
{
    private readonly AgentConfigurationTestContext _context = new(output);

    [Fact]
    public async Task Copilot_RegistersOneSharedSourcePerScope()
    {
        IAgentClient[] clients = [_context.Copilot];

        var projectResults = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, clients).DefaultTimeout();
        Assert.Equal(Path.Combine(_context.Project.FullName, ".github", "copilot", "settings.json"),
            Assert.Single(projectResults).TargetPath);
        Assert.Empty(_context.Home.EnumerateFileSystemInfos());
        var userResults = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Global, clients).DefaultTimeout();
        var results = projectResults.Concat(userResults).ToArray();

        Assert.Equal(2, results.Length);
        Assert.All(results, result =>
        {
            Assert.Equal(AgentConfigurationStatus.Configured, result.Status);
            Assert.Equal(clients, result.Clients);
        });
        Assert.Equal(Path.Combine(_context.CopilotDirectory, "settings.json"), Assert.Single(userResults).TargetPath);
        var project = await File.ReadAllTextAsync(Path.Combine(_context.Project.FullName, ".github", "copilot", "settings.json")).DefaultTimeout();
        var user = await File.ReadAllTextAsync(Path.Combine(_context.CopilotDirectory, "settings.json")).DefaultTimeout();
        Assert.Equal(project, user);
        Assert.False(Directory.Exists(Path.Combine(_context.Project.FullName, ".vscode")));

        await Verify(project, "json");
    }

    [Fact]
    public async Task Claude_PreservesPinsPreferencesAndExistingBytes()
    {
        var userPath = Path.Combine(_context.ClaudeDirectory, "settings.json");
        const string existing = """
            {
              // Keep this comment on a semantic no-op.
              "permissions": { "deny": ["Bash(git push:*)"] },
              "autoUpdate": false,
              "extraKnownMarketplaces": {
                "aspire-skills": {
                  "source": { "source": "github", "repo": "microsoft/aspire-skills", "ref": "v0.0.3" },
                  "autoUpdate": false
                }
              },
              "enabledPlugins": { "aspire@aspire-skills": true },
            }
            """;
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(existing)).ToArray();
        Directory.CreateDirectory(_context.ClaudeDirectory);
        await File.WriteAllBytesAsync(userPath, bytes).DefaultTimeout();
        File.SetLastWriteTimeUtc(userPath, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(userPath);

        var results = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Global, [_context.ClaudeCode]).DefaultTimeout();

        Assert.Equal(AgentConfigurationStatus.Unchanged, Assert.Single(results).Status);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(userPath).DefaultTimeout());
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(userPath));
        await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [_context.ClaudeCode]).DefaultTimeout();
        var project = await File.ReadAllTextAsync(Path.Combine(_context.Project.FullName, ".claude", "settings.json")).DefaultTimeout();
        await Verify(project, "json");
    }

    [Theory]
    [InlineData("copilot", false, """{"enabledPlugins":{"aspire@aspire-skills":false},"autoUpdate":false}""")]
    [InlineData("claude", false, """{"enabledPlugins":{"aspire@aspire-skills":false},"autoUpdate":false}""")]
    [InlineData("claude", true, """{"enabledPlugins":{"aspire@aspire-skills":false}}""")]
    [InlineData("copilot", false, "{broken")]
    [InlineData("copilot", false, """{"extraKnownMarketplaces":[]}""")]
    [InlineData("copilot", false, """{"extraKnownMarketplaces":{"aspire-skills":null}}""")]
    [InlineData("copilot", false, """{"enabledPlugins":{"aspire@aspire-skills":"true"}}""")]
    [InlineData("copilot", false, """{"extraKnownMarketplaces":{"aspire-skills":{"source":{"source":"directory","path":"./private-marketplace"}}}}""")]
    public async Task Plugins_RejectDisabledConflictingOrMalformedSettingsWithoutWriting(string clientId, bool local, string existing)
    {
        var client = _context.Clients.Single(client => client.Id == clientId);
        var path = local ? Path.Combine(_context.Project.FullName, ".claude", "settings.local.json")
            : client.GetIntegrationConfigPath(AgentConfigurationScope.Global, _context.Project);
        await AgentConfigurationTestContext.WriteAsync(path, existing).DefaultTimeout();

        var results = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [client]).DefaultTimeout();

        Assert.Equal(AgentConfigurationStatus.Failed, Assert.Single(results).Status);
        Assert.Equal(existing, await File.ReadAllTextAsync(path).DefaultTimeout());
        Assert.Equal(local ? [path] : Array.Empty<string>(),
            Directory.EnumerateFiles(_context.Project.FullName, "*", SearchOption.AllDirectories));
        Assert.False(File.Exists(client.GetIntegrationConfigPath(AgentConfigurationScope.Project, _context.Project)));
    }

    [Theory]
    [InlineData(false, false, """{"strictKnownMarketplaces":[{"source":"github","repo":"microsoft/aspire-skills"}]}""")]
    [InlineData(false, true, """{"blockedMarketplaces":[]}""")]
    [InlineData(true, false, """{"blockedMarketplaces":[]}""")]
    public async Task Claude_LeavesMarketplacePolicyEnforcementToTheClient(bool fragment, bool existingTarget, string policy)
    {
        var managed = fragment
            ? Path.Combine(_context.ClaudeManagedDirectory, "managed-settings.d", "team.json")
            : Path.Combine(_context.ClaudeManagedDirectory, "managed-settings.json");
        await AgentConfigurationTestContext.WriteAsync(managed, policy);
        var target = Path.Combine(_context.Project.FullName, ".claude", "settings.json");
        const string existing = """{"model":"preserved"}""";
        if (existingTarget)
        {
            await AgentConfigurationTestContext.WriteAsync(target, existing);
        }
        var timestamp = File.GetLastWriteTimeUtc(target);

        var results = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [_context.ClaudeCode]);

        Assert.Equal(AgentConfigurationStatus.Failed, Assert.Single(results).Status);
        Assert.Equal(policy, await File.ReadAllTextAsync(managed));
        Assert.Equal(existingTarget, File.Exists(target));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(target));
        if (existingTarget)
        {
            Assert.Equal(existing, await File.ReadAllTextAsync(target));
        }
        else
        {
            Assert.Empty(_context.Project.EnumerateFileSystemInfos());
        }
        Assert.Empty(_context.Home.EnumerateFileSystemInfos());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Claude_PreservesTheDestinationSourceRatherThanAnInheritedPin(bool pinnedDestination)
    {
        var source = pinnedDestination
            ? """{"source":"github","repo":"microsoft/aspire-skills","ref":"approved-release"}"""
            : """{"source":"github","repo":"microsoft/aspire-skills"}""";
        var user = Path.Combine(_context.ClaudeDirectory, "settings.json");
        const string inherited = """{"extraKnownMarketplaces":{"aspire-skills":{"source":{"source":"github","repo":"microsoft/aspire-skills","ref":"other-release"}}}}""";
        await AgentConfigurationTestContext.WriteAsync(user, inherited);
        var target = Path.Combine(_context.Project.FullName, ".claude", "settings.json");
        await AgentConfigurationTestContext.WriteAsync(target, $$"""{"extraKnownMarketplaces":{"aspire-skills":{"source": {{source}} } } }""");

        var results = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [_context.ClaudeCode]);

        Assert.Equal(AgentConfigurationStatus.Configured, Assert.Single(results).Status);
        var settings = JsonNode.Parse(await File.ReadAllTextAsync(target))!;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(source), settings["extraKnownMarketplaces"]!["aspire-skills"]!["source"]));
        Assert.True(settings["enabledPlugins"]!["aspire@aspire-skills"]!.GetValue<bool>());
        Assert.Equal(inherited, await File.ReadAllTextAsync(user));
    }

    [Fact]
    public async Task Claude_UsesDocumentedConfigDirectoryOverrideWithoutChangingUserMcp()
    {
        var custom = Path.Combine(_context.Home.FullName, "claude-work");
        _context.SetVariable("CLAUDE_CONFIG_DIR", custom);
        var state = Path.Combine(custom, ".claude.json");
        const string existing = """{"oauthAccount":{"accountUuid":"preserved"},"projects":{},"theme":"dark"}""";
        await AgentConfigurationTestContext.WriteAsync(state, existing).DefaultTimeout();

        var results = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Global, [_context.ClaudeCode]).DefaultTimeout();

        Assert.All(results, result => Assert.Equal(AgentConfigurationStatus.Configured, result.Status));
        Assert.True(File.Exists(Path.Combine(custom, "settings.json")));
        Assert.False(File.Exists(Path.Combine(_context.Home.FullName, ".claude.json")));
        Assert.Equal(existing, await File.ReadAllTextAsync(state).DefaultTimeout());
    }

    [Fact]
    public async Task Copilot_UserOverride_PreservesTheRequestedScope()
    {
        _context.SetVariable("COPILOT_HOME", Path.Combine(_context.Project.FullName, ".github", "copilot"));
        var results = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Global, [_context.Copilot]).DefaultTimeout();
        var repeated = await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Global, [_context.Copilot]).DefaultTimeout();

        Assert.Equal(AgentConfigurationStatus.Configured, Assert.Single(results).Status);
        Assert.Equal(Path.Combine(_context.Project.FullName, ".github", "copilot", "settings.json"), results[0].TargetPath);
        Assert.Equal([results[0].TargetPath], Directory.EnumerateFiles(_context.Project.FullName, "*", SearchOption.AllDirectories));
        Assert.Empty(_context.Home.EnumerateFileSystemInfos());
        Assert.Equal(AgentConfigurationStatus.Unchanged, Assert.Single(repeated).Status);
    }

    public void Dispose() => _context.Dispose();
}
