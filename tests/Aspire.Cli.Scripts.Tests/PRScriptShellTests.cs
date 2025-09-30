// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for PR CLI acquisition shell scripts (get-aspire-cli-pr.sh) - basic parameter validation only.
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
[RequiresGHCli]
public class PRScriptShellTests(ITestOutputHelper output)
{
    private readonly string _shellScriptPath = Path.Combine(TestUtils.FindRepoRoot()!.FullName, "eng", "scripts", "get-aspire-cli-pr.sh");

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
    public async Task Shell_MissingPrNumber_ShowsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync();

        Assert.NotEqual(0, result.ExitCode);
        Assert.True(
            result.Output.Contains("Usage", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("required", StringComparison.OrdinalIgnoreCase),
            $"Expected usage or error message. Output: {result.Output}");
    }

    [Fact]
    public async Task Shell_DryRunWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        // Using a fake PR number with dry-run - this will fail at gh cli call but won't create directories
        var result = await command.ExecuteAsync("99999", "--dry-run", "--install-prefix", customPath);

        // Dry run should not create directories regardless of script exit code
        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    public async Task Shell_VerboseFlag_ShowsAdditionalOutput()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        // Using a fake PR number - this will fail but verbose output should be present
        var result = await command.ExecuteAsync("99999", "--dry-run", "--verbose");

        // Verbose mode should produce output
        Assert.True(result.Output.Length > 0);
    }
}
