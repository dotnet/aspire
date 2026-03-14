// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI doctor command, specifically testing
/// certificate trust level detection on Linux.
/// </summary>
public sealed class DoctorCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DoctorCommand_WithoutSslCertDir_ShowsPartiallyTrusted()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect partial trust warning in aspire doctor output
        var partiallyTrustedPattern = new CellPatternSearcher()
            .Find("partially trusted");

        // Pattern to detect doctor command completion (shows environment check results)
        var doctorCompletePattern = new CellPatternSearcher()
            .Find("dev-certs");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Generate and trust dev certs inside the container (Docker images don't have them by default)
        await auto.TypeAsync("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Unset SSL_CERT_DIR to trigger partial trust detection on Linux
        await auto.TypeAsync("unset SSL_CERT_DIR");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("aspire doctor");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            // Wait for doctor to complete and show partial trust warning
            var hasDevCerts = doctorCompletePattern.Search(s).Count > 0;
            var hasPartiallyTrusted = partiallyTrustedPattern.Search(s).Count > 0;
            return hasDevCerts && hasPartiallyTrusted;
        }, timeout: TimeSpan.FromSeconds(60), description: "doctor to complete with partial trust warning");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    public async Task DoctorCommand_WithSslCertDir_ShowsTrusted()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect fully trusted certificate
        var trustedPattern = new CellPatternSearcher()
            .Find("certificate is trusted");

        // Pattern to detect partial trust (should NOT appear)
        var partiallyTrustedPattern = new CellPatternSearcher()
            .Find("partially trusted");

        // Pattern to detect doctor command completion
        var doctorCompletePattern = new CellPatternSearcher()
            .Find("dev-certs");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Generate and trust dev certs inside the container (Docker images don't have them by default)
        await auto.TypeAsync("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Set SSL_CERT_DIR to include dev-certs trust path for full trust
        await auto.TypeAsync("export SSL_CERT_DIR=\"/etc/ssl/certs:$HOME/.aspnet/dev-certs/trust\"");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("aspire doctor");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            // Wait for doctor to complete
            var hasDevCerts = doctorCompletePattern.Search(s).Count > 0;
            if (!hasDevCerts)
            {
                return false;
            }

            // Verify we see "trusted" but NOT "partially trusted"
            var hasTrusted = trustedPattern.Search(s).Count > 0;
            var hasPartiallyTrusted = partiallyTrustedPattern.Search(s).Count > 0;

            // Fail if we see partial trust when SSL_CERT_DIR is configured
            if (hasPartiallyTrusted)
            {
                throw new InvalidOperationException(
                    "Unexpected 'partially trusted' message when SSL_CERT_DIR is configured!");
            }

            return hasTrusted;
        }, timeout: TimeSpan.FromSeconds(60), description: "doctor to complete with trusted certificate");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
