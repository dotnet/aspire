// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class McpInitCommandTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("agent init")]
    [InlineData("mcp init")]
    public async Task InitCommands_UseTheSameAgentConfigurationFlow(string commandName)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var selectedRoot = workspace.CreateDirectory("selected workspace");
        var hooks = new TestTelemetryHookConfigurator();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.TelemetryHookConfiguratorFactory = _ => hooks;
            options.AgentEnvironments = TestAgentEnvironmentScanner.CreateEnvironments();
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"{commandName} --workspace-root \"{selectedRoot.FullName}\" --mcp Y --playwright false --dotnet-inspect true --aspire-skills n --agents \"GitHub Copilot,Claude Code\"");
        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        var request = Assert.Single(hooks.Requests);
        Assert.Equal(selectedRoot.FullName, Assert.IsType<DirectoryInfo>(request.WorkspaceRoot).FullName);
        Assert.Equal(new AgentAssetSelection(true, false, true, false), request.Assets);
        Assert.Equal(["GitHub Copilot", "Claude Code"], request.Environments.Select(client => client.DisplayName));
        Assert.Empty(request.Detections);
    }

    [Fact]
    public async Task McpInitCommand_ShowsDeprecationAndPropagatesConfigurationFailure()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var output = new TestOutputTextWriter(outputHelper);
        using var error = new StringWriter();
        var hooks = new TestTelemetryHookConfigurator();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.TelemetryHookConfiguratorFactory = _ => hooks;
            var claude = options.AgentEnvironments.Single(scanner => scanner.DisplayName == "Claude Code");
            claude.GetTargetsCallback = request =>
                [claude.CreateTarget(Path.Combine(Assert.IsType<DirectoryInfo>(request.WorkspaceRoot).FullName, ".mcp.json"),
                    AgentAssetKind.Mcp, AgentConfigurationStatus.Blocked, "Existing MCP configuration is disabled.")];
            options.OutputTextWriter = output;
            options.ErrorTextWriter = error;
            options.DisableAnsi = true;
        });
        using var provider = services.BuildServiceProvider();

        var exitCode = await provider.GetRequiredService<RootCommand>()
            .Parse("mcp init --mcp --aspire-skills n --agents \"Claude Code\"").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.Single(hooks.Requests);
        var logFilePath = provider.GetRequiredService<CliExecutionContext>().LogFilePath;
        await Verify(new
        {
            Output = output.Logs,
            Error = error.ToString().Replace(logFilePath, "<log-file>", StringComparison.Ordinal)
                .Replace(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json"), ".mcp.json", StringComparison.Ordinal)
        });
    }

    [Fact]
    public async Task McpInitCommand_NoClients_DoesNotConfigureOrMigrate()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        const string existing = """{"mcpServers":{"aspire":{"command":"aspire","args":["mcp","start"]}}}""";
        await File.WriteAllTextAsync(configPath, existing);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();

        var exitCode = await provider.GetRequiredService<RootCommand>()
            .Parse("mcp init --mcp --agents none").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(Assert.IsType<TestTelemetryHookConfigurator>(provider.GetRequiredService<ITelemetryHookConfigurator>()).Requests);
        Assert.Equal(existing, await File.ReadAllTextAsync(configPath));
    }
}
