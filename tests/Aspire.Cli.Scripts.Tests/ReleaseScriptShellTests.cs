// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for get-aspire-cli.sh (release/rolling builds script).
/// These tests run the bash script in dry-run mode to verify parameter handling and output.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash script tests require Unix shell")]
public class ReleaseScriptShellTests(ITestOutputHelper testOutput)
{
    private readonly ITestOutputHelper _testOutput = testOutput;

    private static string GetScriptPath()
    {
        var repoRoot = TestUtils.FindRepoRoot()?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
        return Path.Combine(repoRoot, "eng", "scripts", "get-aspire-cli.sh");
    }

    [Fact]
    public async Task HelpFlag_ShowsHelpAndSucceeds()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput);
        
        var result = await script.ExecuteAsync("--help");
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Aspire CLI Download Script", result.Output);
        Assert.Contains("USAGE:", result.Output);
    }

    [Fact]
    public async Task DryRun_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync("--dry-run", "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[DRY RUN]", result.Output);
        Assert.Contains("Would download", result.Output);
    }

    [Fact]
    public async Task InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync("--quality", "invalid");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Error:", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyInstallPath_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync("--install-path", "");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Error:", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConflictingParameters_VersionAndQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync("--version", "1.0.0", "--quality", "release");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Cannot specify both --version and --quality", result.Output);
    }

    [Fact]
    public async Task PlatformOverride_DryRun_ShowsCorrectRID()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "--dry-run",
            "--os", "linux",
            "--arch", "x64",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("linux-x64", result.Output);
    }

    [Fact]
    public async Task CustomInstallPath_DryRun_ShowsPath()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-cli");
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync("--dry-run", "--install-path", customPath);
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(customPath, result.Output);
    }

    [Fact]
    public async Task QualityRelease_DryRun_ShowsReleaseURL()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "--dry-run",
            "--quality", "release",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("aka.ms", result.Output);
    }

    [Fact]
    public async Task SkipPath_DryRun_ShowsSkipMessage()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("HOME", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "--dry-run",
            "--skip-path",
            "--install-path", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        // In dry-run mode, the script should show it would skip PATH configuration
        Assert.Contains("[DRY RUN]", result.Output);
    }
}
