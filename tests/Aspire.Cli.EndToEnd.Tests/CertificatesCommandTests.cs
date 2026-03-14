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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Generate dev certs WITHOUT trust (creates untrusted cert)
        await auto.TypeAsync("dotnet dev-certs https 2>/dev/null || true");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Configure SSL_CERT_DIR so trust detection works properly on Linux
        await auto.TypeAsync("export SSL_CERT_DIR=\"/etc/ssl/certs:$HOME/.aspnet/dev-certs/trust\"");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Run aspire certs trust — should trust the existing cert
        await auto.TypeAsync("aspire certs trust");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => trustSuccessPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "trust success output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify doctor now shows the certificate as trusted
        await auto.TypeAsync("aspire doctor");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => trustedPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "certificate is trusted output");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Generate dev certs first
        await auto.TypeAsync("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("export SSL_CERT_DIR=\"/etc/ssl/certs:$HOME/.aspnet/dev-certs/trust\"");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Run aspire certs clean
        await auto.TypeAsync("aspire certs clean");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => cleanedPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "cleaned successfully output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify doctor now shows no certificate
        await auto.TypeAsync("aspire doctor");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => noCertPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "no HTTPS development certificate output");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Configure SSL_CERT_DIR so trust detection works properly
        await auto.TypeAsync("export SSL_CERT_DIR=\"/etc/ssl/certs:$HOME/.aspnet/dev-certs/trust\"");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Run aspire certs trust with NO pre-existing cert — should create and trust
        await auto.TypeAsync("aspire certs trust");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => trustSuccessPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "trust success output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify doctor now shows the certificate as trusted
        await auto.TypeAsync("aspire doctor");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => trustedPattern.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "certificate is trusted output");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
