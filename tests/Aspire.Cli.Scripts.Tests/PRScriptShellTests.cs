// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for get-aspire-cli-pr.sh (PR builds script).
/// These tests run the bash script in dry-run mode to verify parameter handling and output.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash script tests require Unix shell")]
[RequiresGHCli]
public class PRScriptShellTests(ITestOutputHelper testOutput)
{
    private readonly ITestOutputHelper _testOutput = testOutput;

    private string GetScriptPath()
    {
        var repoRoot = TestUtils.FindRepoRoot()?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
        return Path.Combine(repoRoot, "eng", "scripts", "get-aspire-cli-pr.sh");
    }

    [Fact]
    public async Task HelpFlag_ShowsHelpAndSucceeds()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput);
        
        var result = await script.ExecuteAsync("--help");
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Aspire CLI PR Download Script", result.Output);
        Assert.Contains("USAGE:", result.Output);
    }

    [Fact]
    public async Task MissingPRNumber_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync();
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("required", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidPRNumber_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync("abc");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Error:", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DryRun_WithPRNumber_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "12345",
            "--dry-run",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[DRY RUN]", result.Output);
    }

    [Fact]
    public async Task RunIdParameter_DryRun_AcceptsWorkflowId()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "12345",
            "--run-id", "98765432",
            "--dry-run",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[DRY RUN]", result.Output);
    }

    [Fact]
    public async Task PlatformOverride_DryRun_ShowsCorrectRID()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "12345",
            "--dry-run",
            "--os", "linux",
            "--arch", "x64",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("linux-x64", result.Output);
    }

    [Fact]
    public async Task HiveOnly_DryRun_SkipsCLIDownload()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "12345",
            "--dry-run",
            "--hive-only",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Skipping CLI", result.Output);
    }

    [Fact]
    public async Task SkipPath_DryRun_ShowsSkipMessage()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "12345",
            "--dry-run",
            "--skip-path",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[DRY RUN]", result.Output);
    }
}
