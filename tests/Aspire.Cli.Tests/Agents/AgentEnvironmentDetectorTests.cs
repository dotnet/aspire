// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.TestServices;
using Microsoft.AspNetCore.InternalTesting;
using Semver;

namespace Aspire.Cli.Tests.Agents;

public class AgentEnvironmentDetectorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task DetectAsync_WithNoScanners_ReturnsNone()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var detector = new AgentEnvironmentDetector([]);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workspace.WorkspaceRoot,
            RepositoryRoot = workspace.WorkspaceRoot,
        };

        var discovery = await detector.DetectAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Empty(discovery.Environments);
        Assert.Empty(discovery.Detections);
    }

    [Fact]
    public async Task DetectAsync_WithScanner_RunsScannerWithCorrectContext()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var scanner = new TestAgentEnvironmentScanner(AgentClient.ClaudeCode);
        var detector = new AgentEnvironmentDetector([scanner]);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workspace.WorkspaceRoot,
            RepositoryRoot = workspace.WorkspaceRoot,
        };

        var discovery = await detector.DetectAsync(context, CancellationToken.None).DefaultTimeout();

        var call = Assert.Single(scanner.Calls);
        Assert.Equal(workspace.WorkspaceRoot.FullName, call.WorkingDirectory.FullName);
        Assert.Equal(workspace.WorkspaceRoot.FullName, call.WorkspaceRoot?.FullName);
        Assert.Empty(discovery.Environments);
        Assert.Empty(discovery.Detections);
    }

    [Fact]
    public async Task DetectAsync_WithScannerThatDetectsEnvironment_ReturnsEnvironment()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var detection = new AgentClientDetection(AgentEnvironment.ClaudeCode, SemVersion.Parse("2.1.0", SemVersionStyles.Strict));
        var scanner = new TestAgentEnvironmentScanner(AgentClient.ClaudeCode, detection);
        var detector = new AgentEnvironmentDetector([scanner]);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workspace.WorkspaceRoot,
            RepositoryRoot = workspace.WorkspaceRoot,
        };

        var discovery = await detector.DetectAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Equal([scanner], discovery.Environments);
        Assert.Equal([detection], discovery.Detections);
        Assert.True(Assert.IsAssignableFrom<ICollection<AgentClientDetection>>(discovery.Detections).IsReadOnly);
    }

    [Fact]
    public async Task DetectAsync_WithMultipleScanners_RunsAllScanners()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var copilotCli = new AgentClientDetection(AgentEnvironment.CopilotCli, null);
        var copilotApp = new AgentClientDetection(AgentEnvironment.CopilotApp, null);
        var openCode = new AgentClientDetection(AgentEnvironment.OpenCode, null);
        var scanner1 = new TestAgentEnvironmentScanner(AgentClient.Copilot, copilotCli, copilotApp);
        var scanner2 = new TestAgentEnvironmentScanner(AgentClient.OpenCode, openCode, copilotCli);
        var detector = new AgentEnvironmentDetector([scanner1, scanner2]);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workspace.WorkspaceRoot,
            RepositoryRoot = workspace.WorkspaceRoot,
        };

        var discovery = await detector.DetectAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Single(scanner1.Calls);
        Assert.Single(scanner2.Calls);
        Assert.Equal([scanner1, scanner2], discovery.Environments);
        Assert.Equal([copilotCli, copilotApp, openCode], discovery.Detections);
    }
}
