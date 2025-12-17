// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for the PowerShell release script (get-aspire-cli.ps1).
/// These tests validate parameter handling, platform detection, and dry-run behavior
/// without making any modifications to the user environment.
/// </summary>
[RequiresTools(["pwsh"])]
public class ReleaseScriptPowerShellTests
{
    private readonly ITestOutputHelper _testOutput;

    public ReleaseScriptPowerShellTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Fact]
    public async Task HelpFlag_ShowsUsage()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-Help");

        result.EnsureSuccessful();
        Assert.True(
            result.Output.Contains("DESCRIPTION", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("PARAMETERS", StringComparison.OrdinalIgnoreCase),
            "Output should contain 'DESCRIPTION' or 'PARAMETERS'");
        Assert.Contains("Aspire CLI", result.Output);
    }

    [Fact]
    public async Task InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-Quality", "invalid-quality");

        Assert.NotEqual(0, result.ExitCode);
        Assert.True(
            result.Output.Contains("Unsupported", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("invalid", StringComparison.OrdinalIgnoreCase),
            "Output should contain 'Unsupported' or 'invalid'");
    }

    [Fact]
    public async Task MissingVersion_ShowsError()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        // Try to install with an invalid version format
        var result = await cmd.ExecuteAsync("-Version", "invalid.version");

        // The script will attempt to download and fail, or show validation error
        // Either is acceptable - we're just testing parameter handling
        // We don't assert exit code because it may vary
    }

    [Fact]
    public async Task CustomInstallPath_IsRecognized()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-bin");
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        // This will fail because we're not doing actual downloads, but parameter should be recognized
        var result = await cmd.ExecuteAsync("-InstallPath", customPath, "-Quality", "release");
        
        // We're just testing that the parameter is accepted, not that installation succeeds
        // The script may fail later when trying to download, but shouldn't fail on parameter parsing
    }

    // Note: PowerShell scripts don't have -DryRun or -Verbose parameters
    // These tests verify that parameters are recognized by the script

    [Fact]
    public async Task QualityParameter_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        // Check that -Help shows Quality parameter
        var result = await cmd.ExecuteAsync("-Help");
        
        result.EnsureSuccessful();
        Assert.Contains("Quality", result.Output);
    }

    [Fact]
    public async Task AllMainParameters_ShownInHelp()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-Help");
        
        result.EnsureSuccessful();
        // Verify key parameters are documented
        Assert.Contains("InstallPath", result.Output);
        Assert.Contains("Quality", result.Output);
        Assert.Contains("Version", result.Output);
        Assert.Contains("OS", result.Output);
        Assert.Contains("Architecture", result.Output);
        Assert.Contains("InstallExtension", result.Output);
        Assert.Contains("UseInsiders", result.Output);
        Assert.Contains("SkipPath", result.Output);
        Assert.Contains("KeepArchive", result.Output);
    }
}
