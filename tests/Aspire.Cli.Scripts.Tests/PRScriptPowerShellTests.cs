// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for get-aspire-cli-pr.ps1 (PR builds script).
/// These tests run the PowerShell script in dry-run mode to verify parameter handling and output.
/// </summary>
[RequiresTools(["pwsh"])]
[RequiresGHCli]
public class PRScriptPowerShellTests(ITestOutputHelper testOutput)
{
    private readonly ITestOutputHelper _testOutput = testOutput;

    private static string GetScriptPath()
    {
        var repoRoot = TestUtils.FindRepoRoot()?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
        return Path.Combine(repoRoot, "eng", "scripts", "get-aspire-cli-pr.ps1");
    }

    [Fact]
    public async Task HelpFlag_ShowsHelpAndSucceeds()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput);
        
        var result = await script.ExecuteAsync("-Help");
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Aspire CLI PR Download Script", result.Output);
        Assert.Contains("SYNOPSIS", result.Output, StringComparison.OrdinalIgnoreCase);
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
        
        var result = await script.ExecuteAsync("-PRNumber", "abc");
        
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Cannot process argument", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WhatIf_WithPRNumber_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", "12345",
            "-WhatIf",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
    }

    [Fact]
    public async Task RunIdParameter_WhatIf_AcceptsWorkflowId()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", "12345",
            "-WorkflowRunId", "98765432",
            "-WhatIf",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
    }

    [Fact]
    public async Task PlatformOverride_WhatIf_ShowsCorrectRID()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", "12345",
            "-WhatIf",
            "-OS", "linux",
            "-Architecture", "x64",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("linux-x64", result.Output);
    }

    [Fact]
    public async Task HiveOnly_WhatIf_SkipsCLIDownload()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", "12345",
            "-WhatIf",
            "-HiveOnly",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
    }

    [Fact]
    public async Task SkipPath_WhatIf_ShowsSkipMessage()
    {
        using var env = new TestEnvironment();
        var script = new ScriptToolCommand(GetScriptPath(), _testOutput)
            .WithWorkingDirectory(env.TempDirectory)
            .WithEnvironmentVariable("USERPROFILE", env.MockHome);
        
        var result = await script.ExecuteAsync(
            "-PRNumber", "12345",
            "-WhatIf",
            "-SkipPath",
            "-InstallPath", Path.Combine(env.TempDirectory, "cli"));
        
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("What if:", result.Output);
    }
}
