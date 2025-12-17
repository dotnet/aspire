// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Integration tests for get-aspire-cli-pr.ps1 with real GitHub PRs.
/// These tests require GH_TOKEN to be set and gh CLI to be available.
/// </summary>
[RequiresTools(["pwsh"])]
[RequiresGHCli]
public class PRScriptIntegrationPowerShellTests(RealGitHubPRFixture fixture, ITestOutputHelper testOutput)
    : IClassFixture<RealGitHubPRFixture>
{
    private readonly RealGitHubPRFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutput = testOutput;

    private string GetScriptPath()
    {
        var repoRoot = TestUtils.FindRepoRoot()?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
        return Path.Combine(repoRoot, "eng", "scripts", "get-aspire-cli-pr.ps1");
    }

    [Fact]
    public async Task RealPR_WhatIf_DownloadsArtifactsSuccessfully()
    {
        using var env = new TestEnvironment();
        
        _testOutput.WriteLine($"Testing with PR #{_fixture.PRNumber} (workflow run: {_fixture.WorkflowRunId})");
        
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", _fixture.PRNumber.ToString(),
            "-WhatIf",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
        Assert.Contains(_fixture.PRNumber.ToString(), result.Output);
    }

    [Fact]
    public async Task RealPR_WithRunId_WhatIf_DownloadsArtifactsSuccessfully()
    {
        using var env = new TestEnvironment();
        
        _testOutput.WriteLine($"Testing with PR #{_fixture.PRNumber} and run ID {_fixture.WorkflowRunId}");
        
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", _fixture.PRNumber.ToString(),
            "-WorkflowRunId", _fixture.WorkflowRunId.ToString(),
            "-WhatIf",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
        Assert.Contains(_fixture.WorkflowRunId.ToString(), result.Output);
    }
}
