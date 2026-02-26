// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Aspire.Cli.Npm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Agents.Playwright;

/// <summary>
/// Orchestrates secure installation of the Playwright CLI with supply chain verification.
/// </summary>
internal sealed class PlaywrightCliInstaller(
    INpmRunner npmRunner,
    INpmProvenanceChecker provenanceChecker,
    IPlaywrightCliRunner playwrightCliRunner,
    IConfiguration configuration,
    ILogger<PlaywrightCliInstaller> logger)
{
    /// <summary>
    /// The npm package name for the Playwright CLI.
    /// </summary>
    internal const string PackageName = "@playwright/cli";

    /// <summary>
    /// The version range to resolve. Updated periodically with Aspire releases.
    /// </summary>
    internal const string VersionRange = "0.1.1";

    /// <summary>
    /// The expected source repository for provenance verification.
    /// </summary>
    internal const string ExpectedSourceRepository = "https://github.com/microsoft/playwright-cli";

    /// <summary>
    /// Configuration key that disables package validation when set to "true".
    /// This is a break-glass mechanism for debugging npm service issues and must never be the default.
    /// </summary>
    internal const string DisablePackageValidationKey = "disablePlaywrightCliPackageValidation";

    /// <summary>
    /// Installs the Playwright CLI with supply chain verification and generates skill files.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if installation succeeded or was skipped (already up-to-date), false on failure.</returns>
    public async Task<bool> InstallAsync(CancellationToken cancellationToken)
    {
        // Step 1: Resolve the target version and integrity hash from the npm registry.
        logger.LogDebug("Resolving {Package}@{Range} from npm registry", PackageName, VersionRange);
        var packageInfo = await npmRunner.ResolvePackageAsync(PackageName, VersionRange, cancellationToken);

        if (packageInfo is null)
        {
            logger.LogWarning("Failed to resolve {Package}@{Range} from npm registry. Is npm installed?", PackageName, VersionRange);
            return false;
        }

        logger.LogDebug("Resolved {Package}@{Version} with integrity {Integrity}", PackageName, packageInfo.Version, packageInfo.Integrity);

        // Step 2: Check if a suitable version is already installed.
        var installedVersion = await playwrightCliRunner.GetVersionAsync(cancellationToken);
        if (installedVersion is not null)
        {
            var comparison = SemVersion.ComparePrecedence(installedVersion, packageInfo.Version);
            if (comparison >= 0)
            {
                logger.LogDebug(
                    "playwright-cli {InstalledVersion} is already installed (target: {TargetVersion}), skipping installation",
                    installedVersion,
                    packageInfo.Version);

                // Still install skills in case they're missing.
                return await playwrightCliRunner.InstallSkillsAsync(cancellationToken);
            }

            logger.LogDebug(
                "Upgrading playwright-cli from {InstalledVersion} to {TargetVersion}",
                installedVersion,
                packageInfo.Version);
        }

        // Check break-glass configuration to bypass package validation.
        var validationDisabled = string.Equals(configuration[DisablePackageValidationKey], "true", StringComparison.OrdinalIgnoreCase);
        if (validationDisabled)
        {
            logger.LogWarning(
                "Package validation is disabled via '{ConfigKey}'. " +
                "Sigstore attestation, provenance, and integrity checks will be skipped. " +
                "This should only be used for debugging npm service issues.",
                DisablePackageValidationKey);
        }

        if (!validationDisabled)
        {
            // Step 3: Verify Sigstore attestation signatures via npm audit signatures.
            // This creates a temporary project to give npm audit signatures a working context.
            logger.LogDebug("Verifying Sigstore attestations for {Package}@{Version}", PackageName, packageInfo.Version);
            var auditPassed = await npmRunner.AuditSignaturesAsync(PackageName, packageInfo.Version.ToString(), cancellationToken);
            if (!auditPassed)
            {
                logger.LogWarning(
                    "Sigstore attestation verification failed for {Package}@{Version}. The package may not have valid attestations.",
                    PackageName,
                    packageInfo.Version);
                return false;
            }

            logger.LogDebug("Sigstore attestation verification passed for {Package}@{Version}", PackageName, packageInfo.Version);

            // Step 4: Verify provenance source repository from SLSA attestation.
            logger.LogDebug("Verifying provenance for {Package}@{Version}", PackageName, packageInfo.Version);
            var provenanceResult = await provenanceChecker.VerifyProvenanceAsync(
                PackageName,
                packageInfo.Version.ToString(),
                ExpectedSourceRepository,
                cancellationToken);

            if (!provenanceResult.IsVerified)
            {
                logger.LogWarning(
                    "Provenance verification failed for {Package}@{Version}: {Outcome}. Expected source repository: {ExpectedRepo}",
                    PackageName,
                    packageInfo.Version,
                    provenanceResult.Outcome,
                    ExpectedSourceRepository);
                return false;
            }

            logger.LogDebug(
                "Provenance verification passed for {Package}@{Version} (source: {SourceRepo})",
                PackageName,
                packageInfo.Version,
                provenanceResult.Provenance?.SourceRepository);
        }

        // Step 5: Download the tarball via npm pack.
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire-playwright-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            logger.LogDebug("Downloading {Package}@{Version} to {TempDir}", PackageName, packageInfo.Version, tempDir);
            var tarballPath = await npmRunner.PackAsync(PackageName, packageInfo.Version.ToString(), tempDir, cancellationToken);

            if (tarballPath is null)
            {
                logger.LogWarning("Failed to download {Package}@{Version}", PackageName, packageInfo.Version);
                return false;
            }

            // Step 6: Verify the downloaded tarball's SHA-512 hash matches the SRI integrity value.
            if (!validationDisabled && !VerifyIntegrity(tarballPath, packageInfo.Integrity))
            {
                logger.LogWarning(
                    "Integrity verification failed for {Package}@{Version}. The downloaded package may have been tampered with.",
                    PackageName,
                    packageInfo.Version);
                return false;
            }

            if (!validationDisabled)
            {
                logger.LogDebug("Integrity verification passed for {TarballPath}", tarballPath);
            }

            // Step 7: Install globally from the verified tarball.
            logger.LogDebug("Installing {Package}@{Version} globally", PackageName, packageInfo.Version);
            var installSuccess = await npmRunner.InstallGlobalAsync(tarballPath, cancellationToken);

            if (!installSuccess)
            {
                logger.LogWarning("Failed to install {Package}@{Version} globally", PackageName, packageInfo.Version);
                return false;
            }

            // Step 8: Generate skill files.
            logger.LogDebug("Generating Playwright CLI skill files");
            return await playwrightCliRunner.InstallSkillsAsync(cancellationToken);
        }
        finally
        {
            // Clean up temporary directory.
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (IOException ex)
            {
                logger.LogDebug(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
            }
        }
    }

    /// <summary>
    /// Verifies that the SHA-512 hash of the file matches the SRI integrity string.
    /// </summary>
    internal static bool VerifyIntegrity(string filePath, string sriIntegrity)
    {
        // SRI format: "sha512-<base64hash>"
        if (!sriIntegrity.StartsWith("sha512-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expectedHash = sriIntegrity["sha512-".Length..];

        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA512.HashData(stream);
        var actualHash = Convert.ToBase64String(hashBytes);

        return string.Equals(expectedHash, actualHash, StringComparison.Ordinal);
    }
}
