// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Semver;

namespace Aspire.Cli.Tests.Agents;

public class AgentEnvironmentScannerTests(ITestOutputHelper output)
{
    [Fact]
    public void EnvironmentMasks_PreserveActualDetectionAndSupportedGroupBits()
    {
        using var context = new AgentConfigurationTestContext(output);
        var discovery = new AgentDiscovery(context.Environments, Array.AsReadOnly<AgentClientDetection>(
        [
            new(AgentEnvironment.CopilotApp, null),
            new(AgentEnvironment.ClaudeCode, null),
            new(AgentEnvironment.VsCode, null)
        ]));
        var detected = AgentEnvironment.CopilotApp | AgentEnvironment.ClaudeCode | AgentEnvironment.VsCode;

        Assert.Equal([0, 1, 2, 4, 8, 16], Enum.GetValues<AgentEnvironment>().Select(value => (int)value));
        Assert.Equal(AgentEnvironment.CopilotCli | AgentEnvironment.CopilotApp | AgentEnvironment.VsCode, AgentClient.Copilot.Environments);
        Assert.Equal(AgentEnvironment.ClaudeCode, AgentClient.ClaudeCode.Environments);
        Assert.Equal(AgentEnvironment.OpenCode, AgentClient.OpenCode.Environments);
        Assert.Equal(detected, discovery.DetectedEnvironments);
        Assert.Equal([AgentEnvironment.CopilotApp, AgentEnvironment.ClaudeCode, AgentEnvironment.VsCode],
            discovery.Detections.Select(detection => detection.Client));
        Assert.True(AgentClient.Copilot.Includes(discovery.DetectedEnvironments));
        Assert.True(AgentClient.ClaudeCode.Includes(discovery.DetectedEnvironments));
        Assert.False(AgentClient.OpenCode.Includes(discovery.DetectedEnvironments));
        Assert.False(AgentClient.Copilot.Includes(AgentEnvironment.None));
        Assert.Throws<NotSupportedException>(() => ((ICollection<AgentClientDetection>)discovery.Detections).Clear());
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
            ["copilot", "claude", "opencode"],
            context.Environments.Select(scanner => scanner.Client.Id));
        Assert.Equal(
            [AgentCommandStrings.Environment_Copilot, AgentCommandStrings.Environment_ClaudeCode, AgentCommandStrings.Environment_OpenCode],
            context.Environments.Select(client => client.DisplayName));
        Assert.Equal(AgentClient.All.Select(client => client.Id), AgentClient.All.Select(client => client.ToString()));
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.CliRunner.Commands);
    }

    [Theory]
    [InlineData("copilot", nameof(AgentEnvironment.CopilotCli))]
    [InlineData("claude", nameof(AgentEnvironment.ClaudeCode))]
    [InlineData("opencode", nameof(AgentEnvironment.OpenCode))]
    public async Task ScanAsync_ReusedInputReturnsIndependentImmutableEvidence(string clientId, string detectedEnvironment)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var version = new SemVersion(1, 0, 0);
        var runner = new TestAgentCliRunner
        {
            GetVersionAsyncCallback = (_, _) => Task.FromResult<SemVersion?>(version)
        };
        var scanner = runner.CreateScanners(workspace.CreateExecutionContext(), TestEnvironment.CreateWindows())
            .Single(scanner => scanner.Client.Id == clientId);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workspace.WorkspaceRoot,
            RepositoryRoot = workspace.WorkspaceRoot
        };

        var first = await scanner.ScanAsync(context, TestContext.Current.CancellationToken).DefaultTimeout();
        version = new SemVersion(2, 0, 0);
        var second = await scanner.ScanAsync(context, TestContext.Current.CancellationToken).DefaultTimeout();

        var environment = Enum.Parse<AgentEnvironment>(detectedEnvironment);
        Assert.Equal(new AgentClientDetection(environment, new SemVersion(1, 0, 0)), Assert.Single(first));
        Assert.Equal(new AgentClientDetection(environment, new SemVersion(2, 0, 0)), Assert.Single(second));
        Assert.Throws<NotSupportedException>(() => ((ICollection<AgentClientDetection>)first).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<AgentClientDetection>)second).Clear());
        Assert.Equal([clientId, clientId], runner.Commands);
        Assert.Empty(workspace.WorkspaceRoot.EnumerateFileSystemInfos());
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    [InlineData("OpenCode")]
    public async Task UndetectedEnvironment_CanConfigureWithoutClientEvidence(string clientName)
    {
        using var context = new AgentConfigurationTestContext(output);
        var client = context.Environments.Single(client => client.DisplayName == clientName);
        var scanContext = new AgentEnvironmentScanContext
        {
            WorkingDirectory = context.Project,
            RepositoryRoot = context.Project
        };
        var detections = new List<AgentClientDetection>();
        foreach (var scanner in context.Environments)
        {
            detections.AddRange(await scanner.ScanAsync(scanContext, CancellationToken.None).DefaultTimeout());
        }
        var probes = context.CliRunner.Commands.ToArray();

        Assert.Empty(detections);
        Assert.Equal(["copilot", "code", "code-insiders", "claude", "opencode"], probes);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Project, [client], detections: detections), CancellationToken.None).DefaultTimeout();

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
