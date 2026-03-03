// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Sigstore;

namespace Aspire.Cli.Npm;

/// <summary>
/// The parsed result of an npm attestation response, containing both the Sigstore bundle
/// and the provenance data extracted from the DSSE envelope in a single pass.
/// </summary>
internal sealed class NpmAttestationParseResult
{
    /// <summary>
    /// Gets the outcome of the parse operation.
    /// </summary>
    public required ProvenanceVerificationOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the raw Sigstore bundle JSON node for deserialization by the Sigstore library.
    /// </summary>
    public JsonNode? BundleNode { get; init; }

    /// <summary>
    /// Gets the provenance data extracted from the DSSE envelope payload.
    /// </summary>
    public NpmProvenanceData? Provenance { get; init; }
}

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
        var json = await FetchAttestationJsonAsync(packageName, version, cancellationToken).ConfigureAwait(false);
        if (json is null)
        {
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationFetchFailed };
        }

        var attestation = ParseAttestation(json);
        if (attestation.Outcome is not ProvenanceVerificationOutcome.Verified)
        {
            return new ProvenanceVerificationResult { Outcome = attestation.Outcome, Provenance = attestation.Provenance };
        }

        var sigstoreFailure = await VerifySigstoreBundleAsync(
            attestation.BundleNode!, expectedSourceRepository, sriIntegrity,
            packageName, version, cancellationToken).ConfigureAwait(false);
        if (sigstoreFailure is not null)
        {
            return sigstoreFailure;
        }

        return VerifyProvenanceFields(
            attestation.Provenance!, expectedSourceRepository, expectedWorkflowPath,
            expectedBuildType, validateWorkflowRef);
    }

    /// <summary>
    /// Fetches the attestation JSON from the npm registry for the given package and version.
    /// </summary>
    private async Task<string?> FetchAttestationJsonAsync(
        string packageName, string version, CancellationToken cancellationToken)
    {
        try
        {
            var encodedPackage = Uri.EscapeDataString(packageName);
            var url = $"{NpmRegistryAttestationsBaseUrl}/{encodedPackage}@{version}";

            logger.LogDebug("Fetching attestations from {Url}", url);
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Failed to fetch attestations: HTTP {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(ex, "Failed to fetch attestations for {Package}@{Version}", packageName, version);
            return null;
        }
    }

    /// <summary>
    /// Parses the npm attestation JSON in a single pass, extracting both the Sigstore bundle
    /// node and the provenance data from the SLSA provenance attestation's DSSE envelope.
    /// </summary>
    internal static NpmAttestationParseResult ParseAttestation(string attestationJson)
    {
        JsonNode? doc;
        try
        {
            doc = JsonNode.Parse(attestationJson);
        }
        catch (JsonException)
        {
            return new NpmAttestationParseResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }

        var attestations = doc?["attestations"]?.AsArray();
        if (attestations is null || attestations.Count == 0)
        {
            return new NpmAttestationParseResult { Outcome = ProvenanceVerificationOutcome.SlsaProvenanceNotFound };
        }

        foreach (var attestation in attestations)
        {
            var predicateType = attestation?["predicateType"]?.GetValue<string>();
            if (!string.Equals(predicateType, SlsaProvenancePredicateType, StringComparison.Ordinal))
            {
                continue;
            }

            var bundleNode = attestation?["bundle"];
            if (bundleNode is null)
            {
                return new NpmAttestationParseResult { Outcome = ProvenanceVerificationOutcome.SlsaProvenanceNotFound };
            }

            var payload = bundleNode["dsseEnvelope"]?["payload"]?.GetValue<string>();
            if (payload is null)
            {
                return new NpmAttestationParseResult
                {
                    Outcome = ProvenanceVerificationOutcome.PayloadDecodeFailed,
                    BundleNode = bundleNode
                };
            }

            byte[] decodedBytes;
            try
            {
                decodedBytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                return new NpmAttestationParseResult
                {
                    Outcome = ProvenanceVerificationOutcome.PayloadDecodeFailed,
                    BundleNode = bundleNode
                };
            }

            var provenance = ParseProvenanceFromStatement(decodedBytes);
            if (provenance is null)
            {
                return new NpmAttestationParseResult
                {
                    Outcome = ProvenanceVerificationOutcome.AttestationParseFailed,
                    BundleNode = bundleNode
                };
            }

            var outcome = provenance.SourceRepository is null
                ? ProvenanceVerificationOutcome.SourceRepositoryNotFound
                : ProvenanceVerificationOutcome.Verified;

            return new NpmAttestationParseResult
            {
                Outcome = outcome,
                BundleNode = bundleNode,
                Provenance = provenance
            };
        }

        return new NpmAttestationParseResult { Outcome = ProvenanceVerificationOutcome.SlsaProvenanceNotFound };
    }

    /// <summary>
    /// Extracts provenance fields from a decoded in-toto statement.
    /// </summary>
    internal static NpmProvenanceData? ParseProvenanceFromStatement(byte[] statementBytes)
    {
        try
        {
            var statement = JsonNode.Parse(statementBytes);
            var predicate = statement?["predicate"];
            var buildDefinition = predicate?["buildDefinition"];
            var workflow = buildDefinition?["externalParameters"]?["workflow"];

            return new NpmProvenanceData
            {
                SourceRepository = workflow?["repository"]?.GetValue<string>(),
                WorkflowPath = workflow?["path"]?.GetValue<string>(),
                WorkflowRef = workflow?["ref"]?.GetValue<string>(),
                BuilderId = predicate?["runDetails"]?["builder"]?["id"]?.GetValue<string>(),
                BuildType = buildDefinition?["buildType"]?.GetValue<string>()
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Cryptographically verifies the Sigstore bundle using the Sigstore library.
    /// Checks the Fulcio certificate chain, Rekor transparency log inclusion, and OIDC identity.
    /// </summary>
    /// <returns><c>null</c> if verification succeeded; otherwise a failure result.</returns>
    private async Task<ProvenanceVerificationResult?> VerifySigstoreBundleAsync(
        JsonNode bundleNode,
        string expectedSourceRepository,
        string? sriIntegrity,
        string packageName,
        string version,
        CancellationToken cancellationToken)
    {
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

        if (!TryParseGitHubOwnerRepo(expectedSourceRepository, out var owner, out var repo))
        {
            logger.LogWarning("Could not parse GitHub owner/repo from expected source repository: {ExpectedSourceRepository}", expectedSourceRepository);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.SourceRepositoryMismatch };
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
                var hashBase64 = sriIntegrity["sha512-".Length..];
                var digestBytes = Convert.FromBase64String(hashBase64);

                (success, result) = await verifier.TryVerifyDigestAsync(
                    digestBytes, HashAlgorithmType.Sha512, bundle, policy, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var payloadBase64 = bundleNode["dsseEnvelope"]?["payload"]?.GetValue<string>();
                if (payloadBase64 is null)
                {
                    logger.LogDebug("No DSSE payload found in bundle for {Package}@{Version}", packageName, version);
                    return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.PayloadDecodeFailed };
                }

                var payloadBytes = Convert.FromBase64String(payloadBase64);
                (success, result) = await verifier.TryVerifyAsync(
                    payloadBytes, bundle, policy, cancellationToken).ConfigureAwait(false);
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

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sigstore verification threw an exception for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }
    }

    /// <summary>
    /// Verifies that the extracted provenance fields match the expected values.
    /// Checks source repository, workflow path, build type, and workflow ref in order.
    /// </summary>
    internal static ProvenanceVerificationResult VerifyProvenanceFields(
        NpmProvenanceData provenance,
        string expectedSourceRepository,
        string expectedWorkflowPath,
        string expectedBuildType,
        Func<WorkflowRefInfo, bool>? validateWorkflowRef)
    {
        if (!string.Equals(provenance.SourceRepository, expectedSourceRepository, StringComparison.OrdinalIgnoreCase))
        {
            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.SourceRepositoryMismatch,
                Provenance = provenance
            };
        }

        if (!string.Equals(provenance.WorkflowPath, expectedWorkflowPath, StringComparison.Ordinal))
        {
            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.WorkflowMismatch,
                Provenance = provenance
            };
        }

        if (!string.Equals(provenance.BuildType, expectedBuildType, StringComparison.Ordinal))
        {
            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.BuildTypeMismatch,
                Provenance = provenance
            };
        }

        if (validateWorkflowRef is not null)
        {
            if (!WorkflowRefInfo.TryParse(provenance.WorkflowRef, out var refInfo) || refInfo is null)
            {
                return new ProvenanceVerificationResult
                {
                    Outcome = ProvenanceVerificationOutcome.WorkflowRefMismatch,
                    Provenance = provenance
                };
            }

            if (!validateWorkflowRef(refInfo))
            {
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
