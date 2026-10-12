// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Resources;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Agents;

public class AgentEnvironmentScannerTests(ITestOutputHelper output)
{
    [Fact]
    public void EnvironmentMasks_PreserveActualDetectionAndSupportedGroupBits()
    {
        using var context = new AgentConfigurationTestContext(output);
        var scan = new AgentEnvironmentScanContext(context.Project, context.Project);
        var detected = AgentEnvironment.CopilotApp | AgentEnvironment.ClaudeCode | AgentEnvironment.VsCode;

        scan.AddDetectedEnvironment(detected);
        scan.AddDetectedEnvironment(AgentEnvironment.ClaudeCode);

        Assert.Equal([0, 1, 2, 4, 8, 16], Enum.GetValues<AgentEnvironment>().Select(value => (int)value));
        Assert.Equal(AgentEnvironment.CopilotCli | AgentEnvironment.CopilotApp | AgentEnvironment.VsCode, AgentClient.Copilot.Environments);
        Assert.Equal(AgentEnvironment.ClaudeCode, AgentClient.ClaudeCode.Environments);
        Assert.Equal(AgentEnvironment.OpenCode, AgentClient.OpenCode.Environments);
        Assert.Equal(detected, scan.DetectedEnvironments);
        Assert.Equal([AgentEnvironment.CopilotApp, AgentEnvironment.ClaudeCode, AgentEnvironment.VsCode],
            scan.DetectedClients.Select(detection => detection.Client));
        Assert.True(AgentClient.Copilot.Includes(scan.DetectedEnvironments));
        Assert.True(AgentClient.ClaudeCode.Includes(scan.DetectedEnvironments));
        Assert.False(AgentClient.OpenCode.Includes(scan.DetectedEnvironments));
        Assert.False(AgentClient.Copilot.Includes(AgentEnvironment.None));
        Assert.Throws<NotSupportedException>(() => ((ICollection<AgentClientDetection>)scan.DetectedClients).Clear());
        Assert.Empty(context.CliRunner.Commands);
    }

    [Fact]
    public void EnvironmentMetadata_DoesNotReadConfigurationOrProbeClients()
    {
        using var context = new AgentConfigurationTestContext(output);
        context.SetVariable("COPILOT_HOME", "\0invalid");
        context.SetVariable("CLAUDE_CONFIG_DIR", "\0invalid");
        context.SetVariable("OPENCODE_CONFIG", "\0invalid");
        context.SetVariable("VSCODE_APPDATA", "\0invalid");

        Assert.Equal(
            AgentClient.All,
            context.Environments.Select(scanner => scanner.Client));
        Assert.Equal(
            [AgentCommandStrings.Environment_Copilot, AgentCommandStrings.Environment_ClaudeCode, AgentCommandStrings.Environment_OpenCode],
            context.Environments.Select(client => client.DisplayName));
        Assert.Equal(
            [AgentCommandStrings.Agent_CopilotPlatforms, AgentCommandStrings.Agent_ClaudePlatforms, AgentCommandStrings.Agent_OpenCodePlatforms],
            context.Environments.Select(client => client.Description));
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.CliRunner.Commands);
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    [InlineData("OpenCode")]
    public async Task UndetectedEnvironment_CanConfigureWithoutClientEvidence(string clientName)
    {
        using var context = new AgentConfigurationTestContext(output);
        var client = context.Environments.Single(client => client.DisplayName == clientName);
        var scanContext = new AgentEnvironmentScanContext(context.Project, context.Project);
        foreach (var scanner in context.Environments)
        {
            await scanner.ScanAsync(scanContext, CancellationToken.None).DefaultTimeout();
        }
        var probes = context.CliRunner.Commands.ToArray();

        Assert.Empty(scanContext.DetectedClients);
        Assert.Equal(["copilot", "code", "code-insiders", "claude", "opencode"], probes);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Project, [client], detections: scanContext.DetectedClients), CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.Single(result.Targets);
        Assert.All(result.Targets, target =>
        {
            Assert.Equal([client], target.Environments);
            Assert.Equal(AgentConfigurationStatus.Configured, target.Status);
            Assert.True(File.Exists(target.TargetPath));
        });
        Assert.Equal(probes, context.CliRunner.Commands);
        Assert.Equal(0, context.HookInstaller.Calls);
    }
}
