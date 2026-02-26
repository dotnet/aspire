// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Npm;

/// <summary>
/// Represents the outcome of a provenance verification check.
/// Each value corresponds to a specific gate in the verification process.
/// </summary>
internal enum ProvenanceVerificationOutcome
{
    /// <summary>
    /// All checks passed and the source repository matches the expected value.
    /// </summary>
    Verified,

    /// <summary>
    /// Failed to fetch attestation data from the npm registry (network error or non-success HTTP status).
    /// </summary>
    AttestationFetchFailed,

    /// <summary>
    /// The attestation response could not be parsed as valid JSON.
    /// </summary>
    AttestationParseFailed,

    /// <summary>
    /// No SLSA provenance attestation was found in the registry response.
    /// </summary>
    SlsaProvenanceNotFound,

    /// <summary>
    /// The DSSE envelope payload could not be decoded from the attestation bundle.
    /// </summary>
    PayloadDecodeFailed,

    /// <summary>
    /// The source repository could not be extracted from the provenance statement.
    /// </summary>
    SourceRepositoryNotFound,

    /// <summary>
    /// The attested source repository does not match the expected value.
    /// </summary>
    SourceRepositoryMismatch
}

/// <summary>
/// Represents the deserialized provenance data extracted from an SLSA attestation.
/// </summary>
internal sealed class NpmProvenanceData
{
    /// <summary>
    /// Gets the source repository URL from the attestation (e.g., "https://github.com/microsoft/playwright-cli").
    /// </summary>
    public string? SourceRepository { get; init; }

    /// <summary>
    /// Gets the workflow file path from the attestation (e.g., ".github/workflows/publish.yml").
    /// </summary>
    public string? WorkflowPath { get; init; }

    /// <summary>
    /// Gets the builder ID URI from the attestation (e.g., "https://github.com/actions/runner/github-hosted").
    /// </summary>
    public string? BuilderId { get; init; }

    /// <summary>
    /// Gets the workflow reference (e.g., "refs/tags/v0.1.1").
    /// </summary>
    public string? WorkflowRef { get; init; }
}

/// <summary>
/// Represents the result of a provenance verification check.
/// </summary>
internal sealed class ProvenanceVerificationResult
{
    /// <summary>
    /// Gets the outcome of the verification, indicating which gate passed or failed.
    /// </summary>
    public required ProvenanceVerificationOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the deserialized provenance data, if available. May be partially populated
    /// depending on how far verification progressed before failure.
    /// </summary>
    public NpmProvenanceData? Provenance { get; init; }

    /// <summary>
    /// Gets a value indicating whether the verification succeeded.
    /// </summary>
    public bool IsVerified => Outcome is ProvenanceVerificationOutcome.Verified;
}

/// <summary>
/// Verifies npm package provenance by checking SLSA attestations from the npm registry.
/// </summary>
internal interface INpmProvenanceChecker
{
    /// <summary>
    /// Verifies that the SLSA provenance attestation for a package was built from the expected source repository.
    /// </summary>
    /// <param name="packageName">The npm package name (e.g., "@playwright/cli").</param>
    /// <param name="version">The exact version to verify.</param>
    /// <param name="expectedSourceRepository">The expected source repository URL (e.g., "https://github.com/microsoft/playwright-cli").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ProvenanceVerificationResult"/> indicating the outcome and any extracted provenance data.</returns>
    Task<ProvenanceVerificationResult> VerifyProvenanceAsync(string packageName, string version, string expectedSourceRepository, CancellationToken cancellationToken);
}
