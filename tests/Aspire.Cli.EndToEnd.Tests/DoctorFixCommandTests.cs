// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI doctor fix command, testing
/// automated certificate repair in a Docker container.
/// </summary>
public sealed class DoctorFixCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DoctorFixCertificates_WithUntrustedCert_FixesCertificate()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for successful fix output
        var fixAppliedPattern = new CellPatternSearcher()
            .Find("Fix applied");

        var summaryPattern = new CellPatternSearcher()
            .Find("Summary:");

        // Pattern for doctor showing trusted after fix
        var trustedPattern = new CellPatternSearcher()
            .Find("certificate is trusted");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate dev certs WITHOUT trust (creates untrusted cert)
        sequenceBuilder
            .Type("dotnet dev-certs https 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Configure SSL_CERT_DIR so trust detection works properly on Linux
        sequenceBuilder.ConfigureSslCertDir(counter);

        // Run aspire doctor fix certificates — should detect untrusted cert and fix it
        sequenceBuilder
            .Type("aspire doctor fix certificates")
            .Enter()
            .WaitUntil(s =>
            {
                var hasFixApplied = fixAppliedPattern.Search(s).Count > 0;
                var hasSummary = summaryPattern.Search(s).Count > 0;
                return hasFixApplied && hasSummary;
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        // Verify doctor now shows the certificate as trusted
        sequenceBuilder
            .Type("aspire doctor")
            .Enter()
            .WaitUntil(s => trustedPattern.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task DoctorFixCertificates_WithTrustedCert_ReportsNoIssues()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for "no issues found" output
        var noIssuesPattern = new CellPatternSearcher()
            .Find("No issues found");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate AND trust dev certs
        sequenceBuilder
            .Type("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Configure SSL_CERT_DIR so trust detection works properly
        sequenceBuilder.ConfigureSslCertDir(counter);

        // Run aspire doctor fix certificates — should detect cert is already trusted
        sequenceBuilder
            .Type("aspire doctor fix certificates")
            .Enter()
            .WaitUntil(s => noIssuesPattern.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task DoctorFixAll_WithUntrustedCert_FixesCertificate()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for successful fix
        var fixAppliedPattern = new CellPatternSearcher()
            .Find("Fix applied");

        var summaryPattern = new CellPatternSearcher()
            .Find("Summary:");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate dev certs WITHOUT trust
        sequenceBuilder
            .Type("dotnet dev-certs https 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ConfigureSslCertDir(counter);

        // Run aspire doctor fix --all — should evaluate and fix certificate issues
        sequenceBuilder
            .Type("aspire doctor fix --all")
            .Enter()
            .WaitUntil(s =>
            {
                var hasFixApplied = fixAppliedPattern.Search(s).Count > 0;
                var hasSummary = summaryPattern.Search(s).Count > 0;
                return hasFixApplied && hasSummary;
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task DoctorFixCertificatesClean_RemovesCertificates()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for successful clean
        var cleanedPattern = new CellPatternSearcher()
            .Find("cleaned successfully");

        var summaryPattern = new CellPatternSearcher()
            .Find("Summary:");

        // Pattern to verify doctor shows no cert after clean
        var noCertPattern = new CellPatternSearcher()
            .Find("No HTTPS development certificate");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate dev certs first
        sequenceBuilder
            .Type("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ConfigureSslCertDir(counter);

        // Run explicit clean sub-command
        sequenceBuilder
            .Type("aspire doctor fix certificates clean")
            .Enter()
            .WaitUntil(s =>
            {
                var hasCleaned = cleanedPattern.Search(s).Count > 0;
                var hasSummary = summaryPattern.Search(s).Count > 0;
                return hasCleaned && hasSummary;
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        // Verify doctor now shows no certificate
        sequenceBuilder
            .Type("aspire doctor")
            .Enter()
            .WaitUntil(s => noCertPattern.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task DoctorFixCertificates_JsonFormat_OutputsStructuredJson()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for JSON output structure
        var jsonFixesPattern = new CellPatternSearcher()
            .Find("\"fixes\":");

        var jsonSummaryPattern = new CellPatternSearcher()
            .Find("\"summary\":");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate dev certs WITHOUT trust to ensure there's something to fix
        sequenceBuilder
            .Type("dotnet dev-certs https 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ConfigureSslCertDir(counter);

        // Run fix with JSON format
        sequenceBuilder
            .Type("aspire doctor fix certificates --format json")
            .Enter()
            .WaitUntil(s =>
            {
                var hasFixes = jsonFixesPattern.Search(s).Count > 0;
                var hasSummary = jsonSummaryPattern.Search(s).Count > 0;
                return hasFixes && hasSummary;
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
