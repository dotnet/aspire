// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Npm;

/// <summary>
/// Verifies npm package provenance by fetching and parsing SLSA attestations from the npm registry API.
/// </summary>
internal sealed class NpmProvenanceChecker(HttpClient httpClient, ILogger<NpmProvenanceChecker> logger) : INpmProvenanceChecker
{
    internal const string NpmRegistryAttestationsBaseUrl = "https://registry.npmjs.org/-/npm/v1/attestations";
    internal const string SlsaProvenancePredicateType = "https://slsa.dev/provenance/v1";

    /// <inheritdoc />
    public async Task<ProvenanceVerificationResult> VerifyProvenanceAsync(string packageName, string version, string expectedSourceRepository, string expectedWorkflowPath, string expectedBuildType, CancellationToken cancellationToken)
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

        // Gate 2: Parse the attestation JSON and extract provenance data.
        NpmProvenanceData provenance;
        try
        {
            var parseResult = ParseProvenance(json);
            if (parseResult is null)
            {
                return new ProvenanceVerificationResult { Outcome = parseResult?.Outcome ?? ProvenanceVerificationOutcome.SlsaProvenanceNotFound };
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
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Failed to parse attestation response for {Package}@{Version}", packageName, version);
            return new ProvenanceVerificationResult { Outcome = ProvenanceVerificationOutcome.AttestationParseFailed };
        }

        logger.LogDebug("SLSA provenance source repository: {SourceRepository}", provenance.SourceRepository);

        // Gate 3: Verify the source repository matches.
        if (!string.Equals(provenance.SourceRepository, expectedSourceRepository, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Provenance verification failed: expected source repository {Expected} but attestation says {Actual}",
                expectedSourceRepository,
                provenance.SourceRepository);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.SourceRepositoryMismatch,
                Provenance = provenance
            };
        }

        // Gate 4: Verify the workflow path matches.
        if (!string.Equals(provenance.WorkflowPath, expectedWorkflowPath, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Provenance verification failed: expected workflow path {Expected} but attestation says {Actual}",
                expectedWorkflowPath,
                provenance.WorkflowPath);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.WorkflowMismatch,
                Provenance = provenance
            };
        }

        // Gate 5: Verify the build type matches (confirms CI system and OIDC token issuer).
        if (!string.Equals(provenance.BuildType, expectedBuildType, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Provenance verification failed: expected build type {Expected} but attestation says {Actual}",
                expectedBuildType,
                provenance.BuildType);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.BuildTypeMismatch,
                Provenance = provenance
            };
        }

        // Gate 6: Verify the workflow ref corresponds to a version tag matching the package version.
        var expectedRef = $"refs/tags/v{version}";
        if (!string.Equals(provenance.WorkflowRef, expectedRef, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Provenance verification failed: expected workflow ref {Expected} but attestation says {Actual}",
                expectedRef,
                provenance.WorkflowRef);

            return new ProvenanceVerificationResult
            {
                Outcome = ProvenanceVerificationOutcome.WorkflowRefMismatch,
                Provenance = provenance
            };
        }

        return new ProvenanceVerificationResult
        {
            Outcome = ProvenanceVerificationOutcome.Verified,
            Provenance = provenance
        };
    }

    /// <summary>
    /// Parses provenance data from the npm attestation API response.
    /// </summary>
    internal static (NpmProvenanceData Provenance, ProvenanceVerificationOutcome Outcome)? ParseProvenance(string attestationJson)
    {
        var doc = JsonNode.Parse(attestationJson);
        var attestations = doc?["attestations"]?.AsArray();

        if (attestations is null || attestations.Count == 0)
        {
            return (new NpmProvenanceData(), ProvenanceVerificationOutcome.SlsaProvenanceNotFound);
        }

        foreach (var attestation in attestations)
        {
            var predicateType = attestation?["predicateType"]?.GetValue<string>();
            if (!string.Equals(predicateType, SlsaProvenancePredicateType, StringComparison.Ordinal))
            {
                continue;
            }

            // The SLSA provenance is in the DSSE envelope payload, base64-encoded.
            var payload = attestation?["bundle"]?["dsseEnvelope"]?["payload"]?.GetValue<string>();
            if (payload is null)
            {
                return (new NpmProvenanceData(), ProvenanceVerificationOutcome.PayloadDecodeFailed);
            }

            byte[] decodedBytes;
            try
            {
                decodedBytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                return (new NpmProvenanceData(), ProvenanceVerificationOutcome.PayloadDecodeFailed);
            }

            var statement = JsonNode.Parse(decodedBytes);
            var predicate = statement?["predicate"];
            var buildDefinition = predicate?["buildDefinition"];
            var workflow = buildDefinition
                ?["externalParameters"]
                ?["workflow"];

            var repository = workflow?["repository"]?.GetValue<string>();
            var workflowPath = workflow?["path"]?.GetValue<string>();
            var workflowRef = workflow?["ref"]?.GetValue<string>();

            var builderId = predicate
                ?["runDetails"]
                ?["builder"]
                ?["id"]
                ?.GetValue<string>();

            var buildType = buildDefinition?["buildType"]?.GetValue<string>();

            var provenance = new NpmProvenanceData
            {
                SourceRepository = repository,
                WorkflowPath = workflowPath,
                WorkflowRef = workflowRef,
                BuilderId = builderId,
                BuildType = buildType
            };

            if (repository is null)
            {
                return (provenance, ProvenanceVerificationOutcome.SourceRepositoryNotFound);
            }

            return (provenance, ProvenanceVerificationOutcome.Verified);
        }

        return (new NpmProvenanceData(), ProvenanceVerificationOutcome.SlsaProvenanceNotFound);
    }
}
