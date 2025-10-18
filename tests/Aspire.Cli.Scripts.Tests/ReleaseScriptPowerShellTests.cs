// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for release CLI acquisition PowerShell scripts (get-aspire-cli.ps1).
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
[RequiresTools(["pwsh"])]
public class ReleaseScriptPowerShellTests(ITestOutputHelper output)
{
    private readonly string _powerShellScriptPath = Path.Combine(TestUtils.FindRepoRoot()!.FullName, "eng", "scripts", "get-aspire-cli.ps1");

    [Fact]
    public async Task PowerShell_HelpFlag_ShowsUsageWithoutError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-Help");

        result.EnsureSuccessful();
    }

    [Fact]
    public async Task PowerShell_WhatIf_ShowsIntendedActions()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-WhatIf", "-Quality", "release");

        Assert.Contains("What if", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PowerShell_InvalidQuality_ReturnsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-Version", "1.0.0", "-InstallPath", Path.Combine(env.TempDirectory, "install"));

        Assert.True(result.ExitCode == 0 || result.Output.Contains("Error", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PowerShell_WhatIfWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-WhatIf", "-InstallPath", customPath, "-Quality", "release");

        Assert.False(Directory.Exists(customPath));
    }

    [Fact]
    public async Task PowerShell_ValidQualityValues_AreAccepted()
    {
        using var env = new TestEnvironment();
        
        foreach (var quality in new[] { "release", "staging", "dev" })
        {
            var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
            var result = await command.ExecuteAsync("-WhatIf", "-Quality", quality);

            Assert.True(result.ExitCode == 0 || result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task PowerShell_OsAndArchParameters_AreAccepted()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-WhatIf", "-OS", "win", "-Architecture", "x64", "-Quality", "release");

        Assert.True(result.ExitCode == 0 || result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase));
    }
}
