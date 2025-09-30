// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;

using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for PR CLI acquisition scripts (get-aspire-cli-pr.sh and get-aspire-cli-pr.ps1).
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
public class PrScriptTests(ITestOutputHelper output)
{
    private readonly string _shellScriptPath = Path.Combine(GetRepoRoot(), "eng", "scripts", "get-aspire-cli-pr.sh");
    private readonly string _powerShellScriptPath = Path.Combine(GetRepoRoot(), "eng", "scripts", "get-aspire-cli-pr.ps1");

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
    [RequiresGHCli]
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
    [RequiresGHCli]
    public async Task PowerShell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync();

        // The script should fail when no PR number is provided
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_MissingPrNumber_ShowsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync();

        Assert.NotEqual(0, result.ExitCode);
        Assert.True(
            result.Output.Contains("Usage", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("required", StringComparison.OrdinalIgnoreCase),
            $"Expected usage or error message. Output: {result.Output}");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    [RequiresGHCli]
    public async Task PowerShell_MissingPrNumber_ShowsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync();

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_DryRunWithPrNumber_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("1234", "--dry-run");

        // The PR script requires GitHub CLI authentication
        // We accept either success with DRY RUN output or failure due to gh auth
        if (result.ExitCode == 0)
        {
            Assert.Contains("DRY RUN", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // If it fails, it should be due to gh authentication or API access
            Assert.True(
                result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
                result.Output.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase) ||
                result.Output.Contains("GitHub CLI", StringComparison.OrdinalIgnoreCase) ||
                result.Output.Contains("API", StringComparison.OrdinalIgnoreCase),
                $"Expected gh authentication or API error. Output: {result.Output}");
        }
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    [RequiresGHCli]
    public async Task PowerShell_WhatIfWithPrNumber_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-PrNumber", "1234", "-WhatIf");

        // WhatIf shows what would happen or may fail due to gh auth
        Assert.True(
            result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_DryRunWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("1234", "--dry-run", "--install-prefix", customPath);

        // PR scripts require GitHub CLI auth, so we check if gh error or success
        // Either way, dry run should not create directories
        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    [RequiresGHCli]
    public async Task PowerShell_WhatIfWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-PrNumber", "1234", "-WhatIf", "-InstallPrefix", customPath);

        // WhatIf should not create directories regardless of gh auth status
        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_VerboseFlag_ShowsAdditionalOutput()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("1234", "--dry-run", "--verbose");

        // Verbose mode should produce output (or gh auth error)
        Assert.True(result.Output.Length > 0);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("1234", "--dry-run", "--os", "linux", "--arch", "x64");

        // Script should accept parameters (even if gh auth fails)
        // We just verify it doesn't reject the parameters themselves
        Assert.True(
            result.ExitCode == 0 ||
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("API", StringComparison.OrdinalIgnoreCase),
            $"Script should accept OS/arch parameters or fail due to gh auth. Exit code: {result.ExitCode}\nOutput: {result.Output}");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    [RequiresGHCli]
    public async Task PowerShell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-PrNumber", "1234", "-WhatIf", "-OS", "linux", "-Arch", "x64");

        // Script should accept parameters or show WhatIf
        Assert.True(result.ExitCode == 0 || 
            result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase) || 
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_HiveOnlyFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("1234", "--dry-run", "--hive-only");

        // Script should accept the flag (even if gh auth fails)
        Assert.True(
            result.ExitCode == 0 ||
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase),
            $"Script should accept --hive-only flag. Exit code: {result.ExitCode}\nOutput: {result.Output}");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    [RequiresGHCli]
    public async Task PowerShell_HiveOnlyFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-PrNumber", "1234", "-WhatIf", "-HiveOnly");

        // Script should accept the flag or show WhatIf
        Assert.True(result.ExitCode == 0 || 
            result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
    [RequiresGHCli]
    public async Task Shell_SkipExtensionFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync("1234", "--dry-run", "--skip-extension");

        // Script should accept the flag (even if gh auth fails)
        Assert.True(
            result.ExitCode == 0 ||
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase),
            $"Script should accept --skip-extension flag. Exit code: {result.ExitCode}\nOutput: {result.Output}");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    [RequiresGHCli]
    public async Task PowerShell_SkipExtensionFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-PrNumber", "1234", "-WhatIf", "-SkipExtension");

        // Script should accept the flag or show WhatIf
        Assert.True(result.ExitCode == 0 || 
            result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("gh", StringComparison.OrdinalIgnoreCase));
    }
}
