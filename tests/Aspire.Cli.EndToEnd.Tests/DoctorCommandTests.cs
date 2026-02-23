// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
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
        Assert.SkipWhen(OperatingSystem.IsWindows(), "SSL_CERT_DIR is a Linux-specific concept; certificate trust on Windows uses the Windows certificate store.");

        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(DoctorCommand_WithoutSslCertDir_ShowsPartiallyTrusted));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPlatformShell();

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect partial trust warning in aspire doctor output
        var partiallyTrustedPattern = new CellPatternSearcher()
            .Find("partially trusted");

        // Pattern to detect doctor command completion (shows environment check results)
        var doctorCompletePattern = new CellPatternSearcher()
            .Find("dev-certs");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

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
        Assert.SkipWhen(OperatingSystem.IsWindows(), "SSL_CERT_DIR is a Linux-specific concept; certificate trust on Windows uses the Windows certificate store.");

        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(DoctorCommand_WithSslCertDir_ShowsTrusted));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPlatformShell();

        using var terminal = builder.Build();

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

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

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
