// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Npm;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class SigstoreNpmVerifierIntegrationTests
{
    [Fact]
    public async Task VerifyPlaywrightCli_WithRealRegistry_Succeeds()
    {
        // Download tarball first
        var tempDir = Path.Combine(Path.GetTempPath(), $"sigstore-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "npm",
                Arguments = $"pack @playwright/cli@0.1.1 --pack-destination {tempDir}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            var proc = System.Diagnostics.Process.Start(psi)!;
            await proc.WaitForExitAsync();
            
            var tarball = Directory.GetFiles(tempDir, "*.tgz").FirstOrDefault();
            Assert.NotNull(tarball);

            var httpClient = new HttpClient();
            var logger = NullLogger<SigstoreNpmVerifier>.Instance;
            var verifier = new SigstoreNpmVerifier(httpClient, logger);

            var result = await verifier.VerifyAsync(
                tarball,
                "@playwright/cli",
                "0.1.1",
                "https://github.com/microsoft/playwright-cli",
                ".github/workflows/publish.yml",
                "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
                CancellationToken.None);

            Assert.True(result.IsVerified, $"Verification failed: {result.FailureReason}");
            Assert.Contains("microsoft/playwright-cli", result.SignerIdentity!);
            Assert.Equal("https://github.com/microsoft/playwright-cli", result.SourceRepository);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
