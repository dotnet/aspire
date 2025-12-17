// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for the PowerShell PR script (get-aspire-cli-pr.ps1).
/// These tests validate parameter handling without making actual downloads.
/// </summary>
[RequiresTools(["pwsh"])]
[RequiresGHCli]
public class PRScriptPowerShellTests
{
    private readonly ITestOutputHelper _testOutput;

    public PRScriptPowerShellTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Fact]
    public async Task GetHelp_WithGetHelpCommand_ShowsUsage()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        // Use Get-Help to show script help
        var result = await cmd.ExecuteAsync("-?");

        result.EnsureSuccessful();
        Assert.True(
            result.Output.Contains("SYNOPSIS", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("DESCRIPTION", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("PARAMETERS", StringComparison.OrdinalIgnoreCase),
            "Output should contain help information");
    }

    [Fact]
    public async Task MissingPRNumber_ReturnsError()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task DryRunWithPRNumber_ShowsSteps()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun");

        result.EnsureSuccessful();
        Assert.Contains("12345", result.Output);
    }

    [Fact]
    public async Task CustomInstallPath_IsRecognized()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom");
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-InstallPrefix", customPath);

        result.EnsureSuccessful();
        Assert.Contains(customPath, result.Output);
    }

    [Fact]
    public async Task RunIdParameter_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-WorkflowRunId", "987654321");

        result.EnsureSuccessful();
        Assert.Contains("987654321", result.Output);
    }

    [Fact]
    public async Task OSOverride_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-OSArg", "win");

        result.EnsureSuccessful();
        Assert.Contains("win", result.Output);
    }

    [Fact]
    public async Task ArchOverride_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-ArchArg", "x64");

        result.EnsureSuccessful();
        Assert.Contains("x64", result.Output);
    }

    [Fact]
    public async Task HiveOnlyFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-HiveOnly");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task SkipExtensionInstallFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-SkipExtensionInstall");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task UseInsidersFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-UseInsiders");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task SkipPathFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-SkipPath");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task VerboseFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-Verbose");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task KeepArchiveFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-PRNumber", "12345", "-DryRun", "-KeepArchive");

        result.EnsureSuccessful();
    }
}
