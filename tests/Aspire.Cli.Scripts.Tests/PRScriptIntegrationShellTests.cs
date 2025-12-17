// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Integration tests for get-aspire-cli-pr.sh with real GitHub PRs.
/// These tests require GH_TOKEN to be set and gh CLI to be available.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash script tests require Unix shell")]
[RequiresGHCli]
public class PRScriptIntegrationShellTests(RealGitHubPRFixture fixture, ITestOutputHelper testOutput)
    : IClassFixture<RealGitHubPRFixture>
{
    private readonly RealGitHubPRFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutput = testOutput;

    private string GetScriptPath()
    {
        var repoRoot = TestUtils.FindRepoRoot()?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
        return Path.Combine(repoRoot, "eng", "scripts", "get-aspire-cli-pr.sh");
    }

    [Fact]
    public async Task RealPR_DryRun_DownloadsArtifactsSuccessfully()
    {
        using var env = new TestEnvironment();
        
        _testOutput.WriteLine($"Testing with PR #{_fixture.PRNumber} (workflow run: {_fixture.WorkflowRunId})");
        
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            _fixture.PRNumber.ToString(),
            "--dry-run",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[DRY RUN]", result.Output);
        Assert.Contains(_fixture.PRNumber.ToString(), result.Output);
    }

    [Fact]
    public async Task RealPR_WithRunId_DryRun_DownloadsArtifactsSuccessfully()
    {
        using var env = new TestEnvironment();
        
        _testOutput.WriteLine($"Testing with PR #{_fixture.PRNumber} and run ID {_fixture.WorkflowRunId}");
        
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            _fixture.PRNumber.ToString(),
            "--run-id", _fixture.WorkflowRunId.ToString(),
            "--dry-run",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[DRY RUN]", result.Output);
        Assert.Contains(_fixture.WorkflowRunId.ToString(), result.Output);
    }
}
