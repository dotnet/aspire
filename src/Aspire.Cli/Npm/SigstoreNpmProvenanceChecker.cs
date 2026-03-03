// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Sigstore;

namespace Aspire.Cli.Npm;

/// <summary>
/// Verifies npm package provenance by cryptographically verifying Sigstore bundles
/// from the npm registry attestations API using the Sigstore .NET library.
/// </summary>
internal sealed class SigstoreNpmProvenanceChecker(HttpClient httpClient, ILogger<SigstoreNpmProvenanceChecker> logger) : INpmProvenanceChecker
{
    internal const string NpmRegistryAttestationsBaseUrl = "https://registry.npmjs.org/-/npm/v1/attestations";
    internal const string SlsaProvenancePredicateType = "https://slsa.dev/provenance/v1";

    /// <inheritdoc />
    public async Task<ProvenanceVerificationResult> VerifyProvenanceAsync(
        string packageName,
        string version,
        string expectedSourceRepository,
        string expectedWorkflowPath,
        string expectedBuildType,
        Func<WorkflowRefInfo, bool>? validateWorkflowRef,
        CancellationToken cancellationToken,
        string? sriIntegrity = null)
    {
        // Gate 1: Fetch attestations from the npm registry.
        string json;
        try
        {
            var encodedPackage = Uri.EscapeDataString(packageName);
            var url = $"{NpmRegistryAttestationsBaseUrl}/{encodedPackage}@{version}";

            logger.LogDebug("Fetching attestations from {Url}", url);
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Failed to fetch attestations: HTTP {StatusCode}", response.StatusCode);
                return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationFetchFailed };
            }

            json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(ex, "Failed to fetch attestations for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationFetchFailed };
        }

        // Gate 2: Find the SLSA provenance attestation and extract its Sigstore bundle.
        JsonNode? bundleNode;
        try
        {
            bundleNode = FindSlsaProvenanceBundle(json);
            if (bundleNode is null)
            {
                return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.SlsaProvenanceNotFound };
            }
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
        {
            logger.LogDebug(ex, "Failed to parse attestation response for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }

        // Gate 3: Cryptographically verify the Sigstore bundle using the Sigstore library.
        // This verifies the Fulcio certificate chain, Rekor transparency log inclusion, and OIDC identity.
        var bundleJson = bundleNode.ToJsonString();
        SigstoreBundle bundle;
        try
        {
            bundle = SigstoreBundle.Deserialize(bundleJson);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to deserialize Sigstore bundle for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }

        // Extract the owner and repo from the expected source repository URL.
        // Expected format: "https://github.com/{owner}/{repo}"
        if (!TryParseGitHubOwnerRepo(expectedSourceRepository, out var owner, out var repo))
        {
            logger.LogWarning("Could not parse GitHub owner/repo from expected source repository: {ExpectedSourceRepository}", expectedSourceRepository);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.SourceRepositoryNotFound };
        }

        var verifier = new SigstoreVerifier();
        var policy = new VerificationPolicy
        {
            CertificateIdentity = CertificateIdentity.ForGitHubActions(owner, repo)
        };

        try
        {
            bool success;
            VerificationResult? result;

            if (sriIntegrity is not null && sriIntegrity.StartsWith("sha512-", StringComparison.OrdinalIgnoreCase))
            {
                // Verify the bundle against the tarball's SHA-512 digest from the SRI integrity string.
                var hashBase64 = sriIntegrity["sha512-".Length..];
                var digestBytes = Convert.FromBase64String(hashBase64);

                (success, result) = await verifier.TryVerifyDigestAsync(
                    digestBytes, HashAlgorithmType.Sha512, bundle, policy).ConfigureAwait(false);
            }
            else
            {
                // No integrity hash available — verify using the DSSE envelope payload bytes.
                // The DSSE payload is the in-toto statement that was signed.
                var payloadNode = bundleNode["dsseEnvelope"]?["payload"]?.GetValue<string>();
                if (payloadNode is null)
                {
                    logger.LogDebug("No DSSE payload found in bundle for {Package}@{Version}", packageName, version);
                    return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.PayloadDecodeFailed };
                }

                var payloadBytes = Convert.FromBase64String(payloadNode);
                (success, result) = await verifier.TryVerifyAsync(
                    payloadBytes, bundle, policy).ConfigureAwait(false);
            }

            if (!success)
            {
                logger.LogWarning(
                    "Sigstore verification failed for {Package}@{Version}: {FailureReason}",
                    packageName, version, result?.FailureReason);
                return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
            }

            logger.LogDebug(
                "Sigstore verification passed for {Package}@{Version}. Signed by: {Signer}",
                packageName, version, result?.SignerIdentity?.SubjectAlternativeName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sigstore verification threw an exception for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }

        // Gate 4: Parse the DSSE envelope payload for provenance data and apply field-level checks.
        NpmProvenanceData provenance;
        try
        {
            var parseResult = NpmProvenanceChecker.ParseProvenance(json);
            if (parseResult is null)
            {
                return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.SlsaProvenanceNotFound };
            }

            provenance = parseResult.Value.Provenance;
            if (parseResult.Value.Outcome is not ProvenanceVerificationOutcome.Verified)
            {
                return new ProvenanceVerificationResult
                {
                    Outcome = parseResult.Value.Outcome,
                    Provenance = provenance
                };
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogDebug(ex, "Failed to parse provenance data from attestation for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }

        logger.LogDebug("SLSA provenance source repository: {SourceRepository}", provenance.SourceRepository);

        // Gate 5: Verify the source repository matches.
        if (!string.Equals(provenance.SourceRepository, expectedSourceRepository, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Provenance verification failed: expected source repository {Expected} but attestation says {Actual}",
                expectedSourceRepository, provenance.SourceRepository);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.SourceRepositoryMismatch,
                Provenance = provenance
            };
        }

        // Gate 6: Verify the workflow path matches.
        if (!string.Equals(provenance.WorkflowPath, expectedWorkflowPath, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Provenance verification failed: expected workflow path {Expected} but attestation says {Actual}",
                expectedWorkflowPath, provenance.WorkflowPath);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.WorkflowMismatch,
                Provenance = provenance
            };
        }

        // Gate 7: Verify the build type matches.
        if (!string.Equals(provenance.BuildType, expectedBuildType, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Provenance verification failed: expected build type {Expected} but attestation says {Actual}",
                expectedBuildType, provenance.BuildType);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.BuildTypeMismatch,
                Provenance = provenance
            };
        }

        // Gate 8: Verify the workflow ref using the caller-provided validation callback.
        if (validateWorkflowRef is not null)
        {
            if (!WorkflowRefInfo.TryParse(provenance.WorkflowRef, out var refInfo) || refInfo is null)
            {
                logger.LogWarning(
                    "Provenance verification failed: could not parse workflow ref {WorkflowRef}",
                    provenance.WorkflowRef);

                return new ProvenanceVerificationResult
                {
                    Outcome = ProvenanceVerificationOutcome.WorkflowRefMismatch,
                    Provenance = provenance
                };
            }

            if (!validateWorkflowRef(refInfo))
            {
                logger.LogWarning(
                    "Provenance verification failed: workflow ref {WorkflowRef} did not pass validation",
                    provenance.WorkflowRef);

                return new ProvenanceVerificationResult
                {
                    Outcome = ProvenanceVerificationOutcome.WorkflowRefMismatch,
                    Provenance = provenance
                };
            }
        }

        return new ProvenanceVerificationResult
        {
            Outcome = ProvenanceVerificationOutcome.Verified,
            Provenance = provenance
        };
    }

    /// <summary>
    /// Finds the Sigstore bundle JSON node for the SLSA provenance attestation.
    /// </summary>
    internal static JsonNode? FindSlsaProvenanceBundle(string attestationJson)
    {
        var doc = JsonNode.Parse(attestationJson);
        var attestations = doc?["attestations"]?.AsArray();

        if (attestations is null || attestations.Count == 0)
        {
            return null;
        }

        foreach (var attestation in attestations)
        {
            var predicateType = attestation?["predicateType"]?.GetValue<string>();
            if (string.Equals(predicateType, SlsaProvenancePredicateType, StringComparison.Ordinal))
            {
                return attestation?["bundle"];
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a GitHub repository URL into owner and repo components.
    /// </summary>
    internal static bool TryParseGitHubOwnerRepo(string repositoryUrl, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
        {
            return false;
        }

        owner = segments[0];
        repo = segments[1];
        return true;
    }
}
