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
    public async Task DryRun_ShowsDownloadAndInstallSteps()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release");

        result.EnsureSuccessful();
        // Verify key steps are shown in dry-run mode
        Assert.True(result.Output.Contains("download", StringComparison.OrdinalIgnoreCase), "Output should contain 'download'");
        Assert.True(result.Output.Contains("install", StringComparison.OrdinalIgnoreCase), "Output should contain 'install'");
    }

    [Fact]
    public async Task DryRunWithCustomPath_ShowsCustomInstallPath()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-bin");
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync(
            "-DryRun",
            "-Quality", "release",
            "-InstallPath", customPath);

        result.EnsureSuccessful();
        Assert.Contains(customPath, result.Output);
    }

    [Fact]
    public async Task KeepArchiveFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-KeepArchive");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task VerboseFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-Verbose");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task QualityDev_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "dev");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task QualityStaging_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "staging");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task QualityRelease_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task OSOverride_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-OS", "win");

        result.EnsureSuccessful();
        Assert.Contains("win", result.Output);
    }

    [Fact]
    public async Task ArchitectureOverride_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-Architecture", "x64");

        result.EnsureSuccessful();
        Assert.Contains("x64", result.Output);
    }

    [Fact]
    public async Task SkipPathFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-SkipPath");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task InstallExtensionFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-InstallExtension");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task UseInsidersFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Quality", "release", "-InstallExtension", "-UseInsiders");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task VersionParameter_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli.ps1", env, _testOutput);
        var result = await cmd.ExecuteAsync("-DryRun", "-Version", "9.5.0-preview.1.25366.3");

        result.EnsureSuccessful();
        Assert.Contains("9.5.0-preview.1.25366.3", result.Output);
    }
}
