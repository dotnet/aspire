// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Agents;

public class AgentEnvironmentDetectorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task DetectAsync_WithNoScanners_ReturnsEmptyArray()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var detector = new AgentEnvironmentDetector([], executionContext);

        var applicators = await detector.DetectAsync(workspace.WorkspaceRoot, workspace.WorkspaceRoot, CancellationToken.None);

        Assert.Empty(applicators);
    }

    [Fact]
    public async Task DetectAsync_WithScanner_RunsScannerWithCorrectContext()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var scanner = new TestAgentEnvironmentScanner();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var detector = new AgentEnvironmentDetector([scanner], executionContext);

        var applicators = await detector.DetectAsync(workspace.WorkspaceRoot, workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(scanner.WasScanned);
        Assert.Equal(workspace.WorkspaceRoot.FullName, scanner.ScanContext?.WorkingDirectory.FullName);
        Assert.Equal(workspace.WorkspaceRoot.FullName, scanner.ScanContext?.RepositoryRoot.FullName);
    }

    [Fact]
    public async Task DetectAsync_WithScannerThatAddsApplicator_ReturnsApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var scanner = new TestAgentEnvironmentScanner
        {
            ApplicatorToAdd = new AgentEnvironmentApplicator(
                "Test Environment",
                _ => Task.CompletedTask)
        };
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var detector = new AgentEnvironmentDetector([scanner], executionContext);

        var applicators = await detector.DetectAsync(workspace.WorkspaceRoot, workspace.WorkspaceRoot, CancellationToken.None);

        Assert.Single(applicators);
        Assert.Equal("Test Environment", applicators[0].Description);
    }

    [Fact]
    public async Task DetectAsync_WithMultipleScanners_RunsAllScanners()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var scanner1 = new TestAgentEnvironmentScanner
        {
            ApplicatorToAdd = new AgentEnvironmentApplicator(
                "Environment 1",
                _ => Task.CompletedTask)
        };
        var scanner2 = new TestAgentEnvironmentScanner
        {
            ApplicatorToAdd = new AgentEnvironmentApplicator(
                "Environment 2",
                _ => Task.CompletedTask)
        };
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var detector = new AgentEnvironmentDetector([scanner1, scanner2], executionContext);

        var applicators = await detector.DetectAsync(workspace.WorkspaceRoot, workspace.WorkspaceRoot, CancellationToken.None);

        Assert.True(scanner1.WasScanned);
        Assert.True(scanner2.WasScanned);
        Assert.Equal(2, applicators.Length);
    }

    private static CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory)
    {
        return new CliExecutionContext(
            workingDirectory: workingDirectory,
            hivesDirectory: workingDirectory,
            cacheDirectory: workingDirectory,
            sdksDirectory: workingDirectory,
            debugMode: false,
            environmentVariables: null);
    }

    private sealed class TestAgentEnvironmentScanner : IAgentEnvironmentScanner
    {
        public bool WasScanned { get; private set; }
        public AgentEnvironmentScanContext? ScanContext { get; private set; }
        public AgentEnvironmentApplicator? ApplicatorToAdd { get; set; }

        public Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
        {
            WasScanned = true;
            ScanContext = context;

            if (ApplicatorToAdd is not null)
            {
                context.AddApplicator(ApplicatorToAdd);
            }

            return Task.CompletedTask;
        }
    }
}
