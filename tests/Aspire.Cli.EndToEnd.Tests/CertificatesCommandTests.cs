// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI certificates command, testing
/// certificate clean and trust operations in a Docker container.
/// </summary>
public sealed class CertificatesCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CertificatesTrust_WithUntrustedCert_TrustsCertificate()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for successful trust output
        var trustSuccessPattern = new CellPatternSearcher()
            .Find("trusted successfully");

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

        // Run aspire certs trust — should trust the existing cert
        sequenceBuilder
            .Type("aspire certs trust")
            .Enter()
            .WaitUntil(s => trustSuccessPattern.Search(s).Count > 0, TimeSpan.FromSeconds(60))
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
    public async Task CertificatesClean_RemovesCertificates()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for successful clean
        var cleanedPattern = new CellPatternSearcher()
            .Find("cleaned successfully");

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

        // Run aspire certs clean
        sequenceBuilder
            .Type("aspire certs clean")
            .Enter()
            .WaitUntil(s => cleanedPattern.Search(s).Count > 0, TimeSpan.FromSeconds(60))
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
    public async Task CertificatesTrust_WithNoCert_CreatesAndTrustsCertificate()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for successful trust
        var trustSuccessPattern = new CellPatternSearcher()
            .Find("trusted successfully");

        // Pattern for doctor showing trusted
        var trustedPattern = new CellPatternSearcher()
            .Find("certificate is trusted");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Configure SSL_CERT_DIR so trust detection works properly
        sequenceBuilder.ConfigureSslCertDir(counter);

        // Run aspire certs trust with NO pre-existing cert — should create and trust
        sequenceBuilder
            .Type("aspire certs trust")
            .Enter()
            .WaitUntil(s => trustSuccessPattern.Search(s).Count > 0, TimeSpan.FromSeconds(60))
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
}
