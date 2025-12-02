// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Tests for PR CLI acquisition PowerShell scripts (get-aspire-cli-pr.ps1) - basic parameter validation only.
/// All tests use isolated temporary environments - no user directory modifications.
/// </summary>
[RequiresTools(["pwsh"])]
[RequiresGHCli]
public class PRScriptPowerShellTests(ITestOutputHelper output)
{
    private readonly string _powerShellScriptPath = Path.Combine(TestUtils.FindRepoRoot()!.FullName, "eng", "scripts", "get-aspire-cli-pr.ps1");

    [Fact]
    public async Task PowerShell_MissingPrNumber_ShowsError()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync();

        // The script should fail when no PR number is provided
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task PowerShell_WhatIfWithCustomPath_DoesNotCreateDirectory()
    {
        using var env = new TestEnvironment();
        var customPath = Path.Combine(env.TempDirectory, "custom-install");
        
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        // Using a fake PR number - this will fail at gh cli call but won't create directories
        var result = await command.ExecuteAsync("-PrNumber", "99999", "-WhatIf", "-InstallPrefix", customPath);

        // WhatIf should not create directories regardless of gh auth status
        Assert.False(Directory.Exists(customPath));
    }
}
