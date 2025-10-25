// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Scripts.Tests.Common;
using Aspire.Templates.Tests;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Integration tests that download and install from real PRs.
/// These tests require gh cli authentication and network access.
/// </summary>
[SkipOnPlatform(TestPlatforms.Windows, "Bash scripts are not tested on Windows")]
[RequiresGHCli]
public class PRScriptIntegrationShellTests(ITestOutputHelper output, RealGitHubPRFixture fixture) : IClassFixture<RealGitHubPRFixture>
{
    private readonly string _shellScriptPath = Path.Combine(TestUtils.FindRepoRoot()!.FullName, "eng", "scripts", "get-aspire-cli-pr.sh");

    [Fact]
    public async Task Shell_DryRunWithRealPR_ShowsDownloadAndInstallSteps()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync(fixture.PRNumber.ToString()!, "--dry-run");

        // With RequiresGHCli, we expect the script to succeed in dry-run mode
        result.EnsureSuccessful($"Script should succeed with dry-run for PR {fixture.PRNumber}");
        
        // Verify key steps are shown in dry-run output
        Assert.Contains("DRY RUN", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("download", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("install", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shell_RealPR_CanListArtifacts()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_shellScriptPath, env, output);
        var result = await command.ExecuteAsync(fixture.PRNumber.ToString()!, "--dry-run", "--verbose");

        // With RequiresGHCli and a real PR, we expect success
        result.EnsureSuccessful($"Script should succeed with real PR {fixture.PRNumber}");
        
        // Verify verbose output shows artifact information
        Assert.True(result.Output.Length > 100, "Verbose output should contain substantial information");
    }
}

/// <summary>
/// Integration tests for PowerShell PR scripts with real PRs.
/// These tests require gh cli authentication and network access.
/// </summary>
[RequiresTools(["pwsh"])]
[RequiresGHCli]
public class PRScriptIntegrationPowerShellTests(ITestOutputHelper output, RealGitHubPRFixture fixture) : IClassFixture<RealGitHubPRFixture>
{
    private readonly string _powerShellScriptPath = Path.Combine(TestUtils.FindRepoRoot()!.FullName, "eng", "scripts", "get-aspire-cli-pr.ps1");

    [Fact]
    public async Task PowerShell_WhatIfWithRealPR_ShowsDownloadAndInstallSteps()
    {
        using var env = new TestEnvironment();
        var command = new ScriptToolCommand(_powerShellScriptPath, env, output);
        var result = await command.ExecuteAsync("-PrNumber", fixture.PRNumber.ToString()!, "-WhatIf");

        // With RequiresGHCli, we expect the script to show what it would do
        Assert.True(
            result.ExitCode == 0 || result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase),
            $"Script should show WhatIf output for PR {fixture.PRNumber}. Output: {result.Output}");
        
        // Verify key information is present
        Assert.True(
            result.Output.Contains("download", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("install", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("What if", StringComparison.OrdinalIgnoreCase),
            "Output should mention download, install, or show WhatIf");
    }
}
