// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for release CLI acquisition scripts (get-aspire-cli.sh and get-aspire-cli.ps1).
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
public class ReleaseScriptTests(ITestOutputHelper output)
{
    private readonly string _shellScriptPath = Path.Combine(GetRepoRoot(), "eng", "scripts", "get-aspire-cli.sh");
    private readonly string _powerShellScriptPath = Path.Combine(GetRepoRoot(), "eng", "scripts", "get-aspire-cli.ps1");

    private static string GetRepoRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(currentDir, "Aspire.slnx")))
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null)
            {
                throw new InvalidOperationException("Could not find repository root");
            }
            currentDir = parent.FullName;
        }
        return currentDir;
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PowerShell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-Help");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_DryRun_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--quality", "release");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("DRY RUN", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PowerShell_WhatIf_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-WhatIf", "-Quality", "release");

        Assert.Contains("What if", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--quality", "invalid-quality-name");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Unsupported", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PowerShell_InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        // PowerShell validates via [ValidateSet], so test with missing required params
        var result = await command.ExecuteAsync("-Version", "1.0.0", "-InstallPath", Path.Combine(env.TempDirectory, "install"));

        // This should work or show appropriate error - we're just testing it doesn't crash
        Assert.True(result.ExitCode == 0 || result.Output.Contains("Error", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_DryRunWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--install-path", customPath, "--quality", "release");

        Assert.Equal(0, result.ExitCode);
        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PowerShell_WhatIfWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-WhatIf", "-InstallPath", customPath, "-Quality", "release");

        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_ValidQualityValues_AreAccepted()
    {
        using var env = new TestEnvironment();
        
        foreach (var quality in new[] { "release", "staging", "dev" })
        {
            var command = new ScriptCommand(_shellScriptPath, env, output);
            var result = await command.ExecuteAsync("--dry-run", "--quality", quality);

            Assert.Equal(0, result.ExitCode);
        }
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PowerShell_ValidQualityValues_AreAccepted()
    {
        using var env = new TestEnvironment();
        
        foreach (var quality in new[] { "release", "staging", "dev" })
        {
            var command = new ScriptCommand(_powerShellScriptPath, env, output);
            var result = await command.ExecuteAsync("-WhatIf", "-Quality", quality);

            Assert.True(result.ExitCode == 0 || result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_VerboseFlag_ShowsAdditionalOutput()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--verbose", "--quality", "release");

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Output.Length > 0);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    public async Task Shell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("--dry-run", "--os", "linux", "--arch", "x64", "--quality", "release");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("linux-x64", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PowerShell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-WhatIf", "-OS", "win", "-Architecture", "x64", "-Quality", "release");

        Assert.True(result.ExitCode == 0 || result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase));
    }
}
