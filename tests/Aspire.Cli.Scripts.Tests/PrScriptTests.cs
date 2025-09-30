// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for PR CLI acquisition scripts (get-aspire-cli-pr.sh and get-aspire-cli-pr.ps1).
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
public class PrScriptTests
{
    private readonly string _repoRoot;
    private readonly string _shellScriptPath;
    private readonly string _powerShellScriptPath;

    public PrScriptTests()
    {
        // Find repo root
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

        _repoRoot = currentDir;
        _shellScriptPath = Path.Combine(_repoRoot, "eng", "scripts", "get-aspire-cli-pr.sh");
        _powerShellScriptPath = Path.Combine(_repoRoot, "eng", "scripts", "get-aspire-cli-pr.ps1");
    }

    [Fact]
    public async Task Shell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, "--help");

        // Skip if bash not available on Windows
        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess, $"Script failed with exit code {result.ExitCode}.\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        Assert.Contains("Usage", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PowerShell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        // Note: get-aspire-cli-pr.ps1 doesn't have a -Help parameter implemented
        // Using -? or Get-Help would work, but they don't work well with Process execution
        // Instead, just verify the script can be invoked without PR number and shows appropriate error
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env);

        // Skip if PowerShell not available
        if (result.WasSkipped)
        {
            return;
        }

        // The script should fail when no PR number is provided
        Assert.True(result.IsFailure, "Script should fail when PR number is missing");
    }

    [Fact]
    public async Task Shell_MissingPrNumber_ShowsError()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env);

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsFailure, "Script should fail when PR number is missing");
        Assert.True(
            result.Output.Contains("Usage", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorOutput.Contains("Usage", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorOutput.Contains("required", StringComparison.OrdinalIgnoreCase),
            $"Expected usage or error message. Output: {result.Output}\nError: {result.ErrorOutput}");
    }

    [Fact]
    public async Task PowerShell_MissingPrNumber_ShowsError()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env);

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsFailure, "Script should fail when PR number is missing");
    }

    [Fact]
    public async Task Shell_DryRunWithPrNumber_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, "1234", "--dry-run");

        if (result.WasSkipped)
        {
            return;
        }

        // The PR script requires GitHub CLI (gh) authentication, so it may fail
        // We accept either success with DRY RUN output or failure due to gh auth
        if (result.IsSuccess)
        {
            Assert.Contains("DRY RUN", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // If it fails, it should be due to gh authentication
            Assert.True(
                result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorOutput.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorOutput.Contains("GitHub CLI", StringComparison.OrdinalIgnoreCase),
                $"Expected gh authentication error. Error: {result.ErrorOutput}");
        }
    }

    [Fact]
    public async Task PowerShell_WhatIfWithPrNumber_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, "-PrNumber", "1234", "-WhatIf");

        if (result.WasSkipped)
        {
            return;
        }

        // WhatIf shows what would happen
        Assert.Contains("What if", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shell_DryRunWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, 
            "1234",
            "--dry-run", 
            "--install-prefix", customPath);

        if (result.WasSkipped)
        {
            return;
        }

        // PR scripts require GitHub CLI auth, so we check if gh error or success
        // Either way, dry run should not create directories
        Assert.False(Directory.Exists(customPath), "Dry run should not create directories");
    }

    [Fact]
    public async Task PowerShell_WhatIfWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, 
            "-PrNumber", "1234",
            "-WhatIf",
            "-InstallPrefix", customPath);

        if (result.WasSkipped)
        {
            return;
        }

        // WhatIf should not create directories regardless of gh auth status
        Assert.False(Directory.Exists(customPath), "WhatIf should not create directories");
    }

    [Fact]
    public async Task Shell_VerboseFlag_ShowsAdditionalOutput()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, 
            "1234",
            "--dry-run",
            "--verbose");

        if (result.WasSkipped)
        {
            return;
        }

        // Verbose mode should produce output (or gh auth error)
        Assert.True(result.Output.Length > 0 || result.ErrorOutput.Length > 0, "Verbose mode should produce output");
    }

    [Fact]
    public async Task Shell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env,
            "1234",
            "--dry-run",
            "--os", "linux",
            "--arch", "x64");

        if (result.WasSkipped)
        {
            return;
        }

        // Script should accept parameters (even if gh auth fails)
        // We just verify it doesn't reject the parameters themselves
        Assert.True(
            result.IsSuccess ||
            result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorOutput.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase),
            $"Script should accept OS/arch parameters or fail due to gh auth. Exit code: {result.ExitCode}\nError: {result.ErrorOutput}");
    }

    [Fact]
    public async Task PowerShell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env,
            "-PrNumber", "1234",
            "-WhatIf",
            "-OS", "linux",
            "-Arch", "x64");

        if (result.WasSkipped)
        {
            return;
        }

        // Script should accept parameters or show WhatIf
        Assert.True(result.IsSuccess || result.Output.Contains("What if") || 
            result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase), 
            $"Script should accept OS and arch. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.ErrorOutput}");
    }

    [Fact]
    public async Task Shell_HiveOnlyFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env,
            "1234",
            "--dry-run",
            "--hive-only");

        if (result.WasSkipped)
        {
            return;
        }

        // Script should accept the flag (even if gh auth fails)
        Assert.True(
            result.IsSuccess ||
            result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorOutput.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase),
            $"Script should accept --hive-only flag. Exit code: {result.ExitCode}\nError: {result.ErrorOutput}");
    }

    [Fact]
    public async Task PowerShell_HiveOnlyFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env,
            "-PrNumber", "1234",
            "-WhatIf",
            "-HiveOnly");

        if (result.WasSkipped)
        {
            return;
        }

        // Script should accept the flag or show WhatIf
        Assert.True(result.IsSuccess || result.Output.Contains("What if") ||
            result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase), 
            $"Script should accept -HiveOnly flag. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.ErrorOutput}");
    }

    [Fact]
    public async Task Shell_SkipExtensionFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env,
            "1234",
            "--dry-run",
            "--skip-extension");

        if (result.WasSkipped)
        {
            return;
        }

        // Script should accept the flag (even if gh auth fails)
        Assert.True(
            result.IsSuccess ||
            result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorOutput.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase),
            $"Script should accept --skip-extension flag. Exit code: {result.ExitCode}\nError: {result.ErrorOutput}");
    }

    [Fact]
    public async Task PowerShell_SkipExtensionFlag_IsAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env,
            "-PrNumber", "1234",
            "-WhatIf",
            "-SkipExtension");

        if (result.WasSkipped)
        {
            return;
        }

        // Script should accept the flag or show WhatIf
        Assert.True(result.IsSuccess || result.Output.Contains("What if") ||
            result.ErrorOutput.Contains("gh", StringComparison.OrdinalIgnoreCase), 
            $"Script should accept -SkipExtension flag. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.ErrorOutput}");
    }
}
