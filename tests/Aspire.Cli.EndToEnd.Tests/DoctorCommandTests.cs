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

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect partial trust warning in aspire doctor output
        var partiallyTrustedPattern = new CellPatternSearcher()
            .Find("partially trusted");

        // Pattern to detect doctor command completion (shows environment check results)
        var doctorCompletePattern = new CellPatternSearcher()
            .Find("dev-certs");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate and trust dev certs inside the container (Docker images don't have them by default)
        sequenceBuilder
            .Type("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Unset SSL_CERT_DIR to trigger partial trust detection on Linux
        sequenceBuilder
            .ClearSslCertDir(counter)
            .Type("aspire doctor")
            .Enter()
            .WaitUntil(s =>
            {
                // Wait for doctor to complete and show partial trust warning
                var hasDevCerts = doctorCompletePattern.Search(s).Count > 0;
                var hasPartiallyTrusted = partiallyTrustedPattern.Search(s).Count > 0;
                return hasDevCerts && hasPartiallyTrusted;
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task DoctorCommand_WithSslCertDir_ShowsTrusted()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, workspace: workspace);

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
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Generate and trust dev certs inside the container (Docker images don't have them by default)
        sequenceBuilder
            .Type("dotnet dev-certs https --trust 2>/dev/null || dotnet dev-certs https")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set SSL_CERT_DIR to include dev-certs trust path for full trust
        sequenceBuilder
            .ConfigureSslCertDir(counter)
            .Type("aspire doctor")
            .Enter()
            .WaitUntil(s =>
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
            }, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
