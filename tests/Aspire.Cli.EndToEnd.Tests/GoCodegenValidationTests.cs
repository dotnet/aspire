// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests that validate <c>aspire restore</c> for Go AppHosts by creating a
/// Go AppHost with multiple integrations and verifying the generated Go SDK files.
/// </summary>
public sealed class GoCodegenValidationTests(ITestOutputHelper output)
{
    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task RestoreGeneratesSdkFiles()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        using var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotGo, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalGoSupportAsync(counter);

        await auto.AspireInitGoAppHostAsync(counter);

        await auto.AspireAddPackageAsync("Aspire.Hosting.Redis", counter);
        await auto.RunCommandAsync("aspire restore --non-interactive", counter, TimeSpan.FromMinutes(3));

        var modulesDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "modules");
        Assert.True(Directory.Exists(modulesDir), $".aspire/modules directory was not created at {modulesDir}");

        // The second integration is added only after `.aspire/modules` is already populated. A
        // restore that treats a non-empty modules directory as up to date would leave the stale
        // SDK in place and still report success, so the SqlServer assertions below are what
        // actually prove the refresh. Adding both integrations before the first restore would
        // generate everything in one pass and never exercise that path.
        var codegenHashPath = Path.Combine(modulesDir, ".codegen-hash");
        var initialCodegenHash = File.Exists(codegenHashPath) ? File.ReadAllText(codegenHashPath) : null;

        // aspire.go is the whole generated SDK in one file, so assert on substrings and keep it out of failure messages.
        var sdkPath = Path.Combine(modulesDir, "aspire.go");
        Assert.True(File.Exists(sdkPath), $"Expected generated file not found: {sdkPath}");
        Assert.False(
            File.ReadAllText(sdkPath).Contains("AddSqlServer", StringComparison.Ordinal),
            "Baseline SDK already exposes AddSqlServer; test cannot verify refresh behavior after adding Aspire.Hosting.SqlServer.");

        await auto.AspireAddPackageAsync("Aspire.Hosting.SqlServer", counter);
        await auto.RunCommandAsync("aspire restore --non-interactive", counter, TimeSpan.FromMinutes(3));

        var expectedFiles = new[] { "aspire.go", "base.go", "transport.go", "go.mod" };

        foreach (var file in expectedFiles)
        {
            var filePath = Path.Combine(modulesDir, file);
            Assert.True(File.Exists(filePath), $"Expected generated file not found: {filePath}");
            Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(filePath)), $"Generated file is empty: {filePath}");
        }

        var sdk = File.ReadAllText(sdkPath);
        Assert.True(sdk.Contains("AddRedis", StringComparison.Ordinal), "aspire.go does not contain AddRedis from Aspire.Hosting.Redis");
        Assert.True(
            sdk.Contains("AddSqlServer", StringComparison.Ordinal),
            "aspire.go does not contain AddSqlServer from Aspire.Hosting.SqlServer; restore did not refresh an already-populated .aspire/modules");

        var restoredCodegenHash = File.Exists(codegenHashPath) ? File.ReadAllText(codegenHashPath) : null;
        Assert.NotEqual(initialCodegenHash, restoredCodegenHash);
    }
}
