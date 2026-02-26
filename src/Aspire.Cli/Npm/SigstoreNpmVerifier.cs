// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sigstore.Common;
using Sigstore.Verification;

namespace Aspire.Cli.Npm;

/// <summary>
/// Verifies npm package integrity and provenance using built-in Sigstore verification.
/// This eliminates the dependency on <c>npm audit signatures</c> by performing native
/// Sigstore bundle verification in .NET, collapsing two trust paths into one.
/// </summary>
internal sealed class SigstoreNpmVerifier(
    HttpClient httpClient,
    ILogger<SigstoreNpmVerifier> logger)
{
    private const string NpmRegistryAttestationsUrl = "https://registry.npmjs.org/-/npm/v1/attestations";
    private const string SlsaProvenancePredicateType = "https://slsa.dev/provenance/v1";

    /// <summary>
    /// Verifies an npm package tarball using built-in Sigstore verification.
    /// Downloads the attestation bundle from the npm registry, verifies the Sigstore
    /// signature chain (certificate, transparency log, SCT), then confirms the DSSE
    /// payload's subject digest matches the tarball's SHA-512 hash.
    /// </summary>
    /// <param name="tarballPath">Path to the downloaded tarball.</param>
    /// <param name="packageName">The npm package name (e.g., "@playwright/cli").</param>
    /// <param name="version">The package version.</param>
    /// <param name="expectedSourceRepository">Expected GitHub source repository URL.</param>
    /// <param name="expectedWorkflowPath">Expected workflow file path.</param>
    /// <param name="expectedBuildType">Expected SLSA build type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure with details.</returns>
    public async Task<SigstoreVerificationResult> VerifyAsync(
        string tarballPath,
        string packageName,
        string version,
        string expectedSourceRepository,
        string expectedWorkflowPath,
        string expectedBuildType,
        CancellationToken cancellationToken)
    {
        // Step 1: Compute the SHA-512 hash of the tarball.
        logger.LogDebug("Computing SHA-512 hash of {TarballPath}", tarballPath);
        var tarballHash = await ComputeSha512HexAsync(tarballPath, cancellationToken);
        logger.LogDebug("Tarball SHA-512: {Hash}", tarballHash);

        // Step 2: Download the attestation bundles from the npm registry.
        logger.LogDebug("Fetching attestations for {Package}@{Version}", packageName, version);
        var attestationBundles = await FetchAttestationBundlesAsync(packageName, version, cancellationToken);

        if (attestationBundles is null)
        {
            return SigstoreVerificationResult.Failure("Failed to fetch attestation bundles from npm registry.");
        }

        // Step 3: Find the SLSA provenance attestation.
        SigstoreBundle? provenanceBundle = null;
        foreach (var (predicateType, bundleJson) in attestationBundles)
        {
            if (string.Equals(predicateType, SlsaProvenancePredicateType, StringComparison.OrdinalIgnoreCase))
            {
                provenanceBundle = SigstoreBundle.Deserialize(bundleJson);
                break;
            }
        }

        if (provenanceBundle is null)
        {
            return SigstoreVerificationResult.Failure("No SLSA provenance attestation found in npm registry.");
        }

        logger.LogDebug("Found SLSA provenance bundle: {MediaType}", provenanceBundle.MediaType);

        // Step 4: Verify the Sigstore bundle cryptographically.
        // This verifies the certificate chain, transparency log inclusion, SCTs, and DSSE signature.
        var sourceRepo = expectedSourceRepository.Replace("https://github.com/", "");
        var policy = new VerificationPolicy
        {
            CertificateIdentity = CertificateIdentity.ForGitHubActions(sourceRepo)
        };

        var verifier = new SigstoreVerifier();

        // For DSSE bundles, the artifact stream is not used — the signature is over the PAE-encoded payload.
        using var emptyStream = new MemoryStream();
        var (success, verificationResult) = await verifier.TryVerifyAsync(emptyStream, provenanceBundle, policy, cancellationToken);

        if (!success)
        {
            return SigstoreVerificationResult.Failure(
                $"Sigstore verification failed: {verificationResult?.FailureReason}");
        }

        logger.LogDebug(
            "Sigstore verification passed. Signer: {Signer}, Issuer: {Issuer}",
            verificationResult!.SignerIdentity?.SubjectAlternativeName,
            verificationResult.SignerIdentity?.Issuer);

        // Step 5: Extract and verify the DSSE payload — confirm the subject digest matches.
        if (provenanceBundle.DsseEnvelope is null)
        {
            return SigstoreVerificationResult.Failure("Provenance bundle does not contain a DSSE envelope.");
        }

        var payloadJson = Encoding.UTF8.GetString(provenanceBundle.DsseEnvelope.Payload);
        using var payload = JsonDocument.Parse(payloadJson);

        // Check subject digest
        var subjects = payload.RootElement.GetProperty("subject");
        var digestMatched = false;
        foreach (var subject in subjects.EnumerateArray())
        {
            if (subject.TryGetProperty("digest", out var digest) &&
                digest.TryGetProperty("sha512", out var sha512))
            {
                var attestedHash = sha512.GetString();
                if (string.Equals(attestedHash, tarballHash, StringComparison.OrdinalIgnoreCase))
                {
                    digestMatched = true;
                    break;
                }

                logger.LogDebug("Subject digest mismatch: attested={Attested}, actual={Actual}", attestedHash, tarballHash);
            }
        }

        if (!digestMatched)
        {
            return SigstoreVerificationResult.Failure(
                "Tarball SHA-512 does not match any subject digest in the SLSA provenance attestation.");
        }

        logger.LogDebug("Tarball digest matches SLSA provenance subject.");

        // Step 6: Verify provenance metadata (source repo, workflow, build type).
        var predicate = payload.RootElement.GetProperty("predicate");
        var buildDef = predicate.GetProperty("buildDefinition");

        // Verify build type
        var buildType = buildDef.GetProperty("buildType").GetString();
        if (!string.Equals(buildType, expectedBuildType, StringComparison.OrdinalIgnoreCase))
        {
            return SigstoreVerificationResult.Failure(
                $"Build type mismatch: expected '{expectedBuildType}', got '{buildType}'.");
        }

        // Verify source repository and workflow path
        var workflow = buildDef.GetProperty("externalParameters").GetProperty("workflow");
        var repoUrl = workflow.GetProperty("repository").GetString();
        if (!string.Equals(repoUrl, expectedSourceRepository, StringComparison.OrdinalIgnoreCase))
        {
            return SigstoreVerificationResult.Failure(
                $"Source repository mismatch: expected '{expectedSourceRepository}', got '{repoUrl}'.");
        }

        var workflowPath = workflow.GetProperty("path").GetString();
        if (!string.Equals(workflowPath, expectedWorkflowPath, StringComparison.OrdinalIgnoreCase))
        {
            return SigstoreVerificationResult.Failure(
                $"Workflow path mismatch: expected '{expectedWorkflowPath}', got '{workflowPath}'.");
        }

        logger.LogDebug("Provenance metadata verified: repo={Repo}, workflow={Workflow}, buildType={BuildType}",
            repoUrl, workflowPath, buildType);

        return SigstoreVerificationResult.Verified(
            verificationResult.SignerIdentity?.SubjectAlternativeName,
            verificationResult.SignerIdentity?.Issuer,
            repoUrl,
            workflowPath);
    }

    private async Task<List<(string PredicateType, string BundleJson)>?> FetchAttestationBundlesAsync(
        string packageName, string version, CancellationToken cancellationToken)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(packageName);
            var url = $"{NpmRegistryAttestationsUrl}/{encodedName}@{version}";

            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch attestations: HTTP {StatusCode}", response.StatusCode);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            var results = new List<(string, string)>();
            foreach (var attestation in doc.RootElement.GetProperty("attestations").EnumerateArray())
            {
                var predicateType = attestation.GetProperty("predicateType").GetString() ?? "";
                var bundle = attestation.GetProperty("bundle");
                results.Add((predicateType, bundle.GetRawText()));
            }

            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Error fetching attestation bundles");
            return null;
        }
    }

    private static async Task<string> ComputeSha512HexAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA512.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hashBytes);
    }
}

/// <summary>
/// Result of built-in Sigstore verification for an npm package.
/// </summary>
internal sealed class SigstoreVerificationResult
{
    /// <summary>
    /// Whether verification succeeded.
    /// </summary>
    public bool IsVerified { get; private init; }

    /// <summary>
    /// The failure reason, if verification failed.
    /// </summary>
    public string? FailureReason { get; private init; }

    /// <summary>
    /// The verified signer identity (SAN from the certificate).
    /// </summary>
    public string? SignerIdentity { get; private init; }

    /// <summary>
    /// The OIDC issuer from the certificate.
    /// </summary>
    public string? Issuer { get; private init; }

    /// <summary>
    /// The verified source repository from the SLSA provenance.
    /// </summary>
    public string? SourceRepository { get; private init; }

    /// <summary>
    /// The verified workflow path from the SLSA provenance.
    /// </summary>
    public string? WorkflowPath { get; private init; }

    internal static SigstoreVerificationResult Verified(
        string? signerIdentity, string? issuer, string? sourceRepository, string? workflowPath)
        => new()
        {
            IsVerified = true,
            SignerIdentity = signerIdentity,
            Issuer = issuer,
            SourceRepository = sourceRepository,
            WorkflowPath = workflowPath
        };

    internal static SigstoreVerificationResult Failure(string reason)
        => new() { IsVerified = false, FailureReason = reason };
}
