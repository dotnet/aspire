// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for get-aspire-cli.ps1 (release/rolling builds script).
/// These tests run the PowerShell script in dry-run mode to verify parameter handling and output.
/// </summary>
[RequiresTools(["pwsh"])]
public class ReleaseScriptPowerShellTests(ITestOutputHelper testOutput)
{
    private readonly ITestOutputHelper _testOutput = testOutput;

    private string GetScriptPath()
    {
        var repoRoot = TestUtils.FindRepoRoot()?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
        return Path.Combine(repoRoot, "eng", "scripts", "get-aspire-cli.ps1");
    }

    [Fact]
    public async Task HelpFlag_ShowsHelpAndSucceeds()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput);
        
        var result = await script.ExecuteAsync("-Help");
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Aspire CLI Download Script", result.Output);
        Assert.Contains("SYNOPSIS", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DryRun_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "cli");
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync("-WhatIf", "-InstallPath", customPath);
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
    }

    [Fact]
    public async Task InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync("-Quality", "invalid");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Cannot validate argument", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyInstallPath_ReturnsError()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory);
        
        var result = await script.ExecuteAsync("-InstallPath", "");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Error:", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlatformOverride_WhatIf_ShowsCorrectRID()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-WhatIf",
            "-OS", "linux",
            "-Architecture", "x64",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("linux-x64", result.Output);
    }

    [Fact]
    public async Task CustomInstallPath_WhatIf_ShowsPath()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-cli");
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync("-WhatIf", "-InstallPath", customPath);
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(customPath, result.Output);
    }

    [Fact]
    public async Task QualityRelease_WhatIf_ShowsReleaseURL()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-WhatIf",
            "-Quality", "release",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("aka.ms", result.Output);
    }

    [Fact]
    public async Task SkipPath_WhatIf_ShowsSkipMessage()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-WhatIf",
            "-SkipPath",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        // In WhatIf mode, the script should show its intentions
        Assert.Contains("What if:", result.Output);
    }
}
