// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for the bash PR script (get-aspire-cli-pr.sh).
/// These tests validate parameter handling without making actual downloads.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash script tests require bash shell")]
[RequiresGHCli]
public class PRScriptShellTests
{
    private readonly ITestOutputHelper _testOutput;

    public PRScriptShellTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Fact]
    public async Task HelpFlag_ShowsUsage()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("--help");

        result.EnsureSuccessful();
        Assert.True(result.Output.Contains("Usage", StringComparison.OrdinalIgnoreCase), "Output should contain 'Usage'");
        Assert.Contains("PR", result.Output);
    }

    [Fact]
    public async Task MissingPRNumber_ReturnsError()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("--dry-run");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task DryRunWithPRNumber_ShowsSteps()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run");

        result.EnsureSuccessful();
        Assert.Contains("12345", result.Output);
    }

    [Fact]
    public async Task CustomInstallPath_IsRecognized()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom");
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--install-path", customPath);

        result.EnsureSuccessful();
        Assert.Contains(customPath, result.Output);
    }

    [Fact]
    public async Task RunIdParameter_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--run-id", "987654321");

        result.EnsureSuccessful();
        Assert.Contains("987654321", result.Output);
    }

    [Fact]
    public async Task OSOverride_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--os", "linux");

        result.EnsureSuccessful();
        Assert.Contains("linux", result.Output);
    }

    [Fact]
    public async Task ArchOverride_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--arch", "x64");

        result.EnsureSuccessful();
        Assert.Contains("x64", result.Output);
    }

    [Fact]
    public async Task HiveOnlyFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--hive-only");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task SkipExtensionFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--skip-extension");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task UseInsidersFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--use-insiders");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task SkipPathFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--skip-path");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task VerboseFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--verbose");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task KeepArchiveFlag_IsRecognized()
    {
        using var env = new TestEnvironment();
        var cmd = new ScriptToolCommand("eng/scripts/get-aspire-cli-pr.sh", env, _testOutput);
        var result = await cmd.ExecuteAsync("12345", "--dry-run", "--keep-archive");

        result.EnsureSuccessful();
    }
}
