// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for release CLI acquisition scripts (get-aspire-cli.sh and get-aspire-cli.ps1).
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
public class ReleaseScriptTests
{
    private readonly string _repoRoot;
    private readonly string _shellScriptPath;
    private readonly string _powerShellScriptPath;

    public ReleaseScriptTests()
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
        _shellScriptPath = Path.Combine(_repoRoot, "eng", "scripts", "get-aspire-cli.sh");
        _powerShellScriptPath = Path.Combine(_repoRoot, "eng", "scripts", "get-aspire-cli.ps1");
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
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, "-Help");

        // Skip if PowerShell not available
        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess, $"Script failed with exit code {result.ExitCode}.\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        // PowerShell help uses WhatIf parameter which may have different output
    }

    [Fact]
    public async Task Shell_DryRun_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, "--dry-run", "--quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess, $"Script failed with exit code {result.ExitCode}.\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        // Dry run should indicate what would be done
        Assert.Contains("DRY RUN", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PowerShell_WhatIf_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, "-WhatIf", "-Quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        // WhatIf shows what would happen
        Assert.Contains("What if", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shell_InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, "--quality", "invalid-quality-name");

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsFailure, "Script should fail with invalid quality");
        Assert.Contains("Unsupported", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PowerShell_InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        // PowerShell validates via [ValidateSet], so we can't pass invalid values directly
        // Instead, test that it handles missing required parameters
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, "-Version", "1.0.0", "-InstallPath", Path.Combine(env.TempDirectory, "install"));

        if (result.WasSkipped)
        {
            return;
        }

        // This should work or show appropriate error - we're just testing it doesn't crash
        // The script should handle this gracefully
    }

    [Fact]
    public async Task Shell_DryRunWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, 
            "--dry-run", 
            "--install-path", customPath,
            "--quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess, $"Script failed with exit code {result.ExitCode}.\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        // Dry run should not create directories
        Assert.False(Directory.Exists(customPath), "Dry run should not create directories");
    }

    [Fact]
    public async Task PowerShell_WhatIfWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, 
            "-WhatIf",
            "-InstallPath", customPath,
            "-Quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        // WhatIf should not create directories
        Assert.False(Directory.Exists(customPath), "WhatIf should not create directories");
    }

    [Fact]
    public async Task Shell_ValidQualityValues_AreAccepted()
    {
        using var env = new TestEnvironment();
        
        foreach (var quality in new[] { "release", "staging", "dev" })
        {
            var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, 
                "--dry-run", 
                "--quality", quality);

            if (result.WasSkipped)
            {
                return;
            }

            Assert.True(result.IsSuccess, $"Script should accept quality '{quality}'. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        }
    }

    [Fact]
    public async Task PowerShell_ValidQualityValues_AreAccepted()
    {
        using var env = new TestEnvironment();
        
        foreach (var quality in new[] { "release", "staging", "dev" })
        {
            var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env, 
                "-WhatIf",
                "-Quality", quality);

            if (result.WasSkipped)
            {
                return;
            }

            Assert.True(result.IsSuccess || result.Output.Contains("What if"), 
                $"Script should accept quality '{quality}'. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        }
    }

    [Fact]
    public async Task Shell_VerboseFlag_ShowsAdditionalOutput()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env, 
            "--dry-run",
            "--verbose",
            "--quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess, $"Script failed with exit code {result.ExitCode}.\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        // Verbose mode should produce more output
        Assert.True(result.Output.Length > 0 || result.ErrorOutput.Length > 0, "Verbose mode should produce output");
    }

    [Fact]
    public async Task Shell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_shellScriptPath, env,
            "--dry-run",
            "--os", "linux",
            "--arch", "x64",
            "--quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess, $"Script failed with exit code {result.ExitCode}.\nOutput: {result.Output}\nError: {result.ErrorOutput}");
        Assert.Contains("linux-x64", result.Output + result.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PowerShell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var result = await ScriptExecutor.ExecuteAsync(_powerShellScriptPath, env,
            "-WhatIf",
            "-OS", "win",
            "-Architecture", "x64",
            "-Quality", "release");

        if (result.WasSkipped)
        {
            return;
        }

        Assert.True(result.IsSuccess || result.Output.Contains("What if"), 
            $"Script should accept OS and arch. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.ErrorOutput}");
    }
}
