// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for release CLI acquisition shell scripts (get-aspire-cli.sh).
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
public class ReleaseScriptShellTests(ITestOutputHelper output)
{
    private readonly string _shellScriptPath = Path.Combine(TestUtils.FindRepoRoot()!.FullName, "eng", "scripts", "get-aspire-cli.sh");

    [Fact]
    public async Task Shell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--help");

        result.EnsureSuccessful();
        Assert.Contains("Usage", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shell_DryRun_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--quality", "release");

        result.EnsureSuccessful();
        Assert.Contains("DRY RUN", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shell_InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--quality", "invalid-quality-name");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Unsupported", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shell_DryRunWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--install-path", customPath, "--quality", "release");

        result.EnsureSuccessful();
        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    public async Task Shell_ValidQualityValues_AreAccepted()
    {
        using var env = new TestEnvironment();
        
        foreach (var quality in new[] { "release", "staging", "dev" })
        {
            var command = new ScriptToolCommand(_shellScriptPath, env, output);
            var result = await command.ExecuteAsync("--dry-run", "--quality", quality);

            result.EnsureSuccessful($"Script should accept quality '{quality}'");
        }
    }

    [Fact]
    public async Task Shell_VerboseFlag_ShowsAdditionalOutput()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--verbose", "--quality", "release");

        result.EnsureSuccessful();
        Assert.True(result.Output.Length > 0);
    }

    [Fact]
    public async Task Shell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--os", "linux", "--arch", "x64", "--quality", "release");

        result.EnsureSuccessful();
        Assert.Contains("linux-x64", result.Output, StringComparison.OrdinalIgnoreCase);
    }
}
