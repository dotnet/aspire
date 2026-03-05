// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Npm;

namespace Aspire.Cli.Tests.Agents;

public class SigstoreNpmProvenanceCheckerTests
{
    [Fact]
    public void ParseAttestation_WithValidSlsaAttestation_ReturnsBundleAndProvenance()
    {
        var json = BuildAttestationJsonWithBundle("https://github.com/microsoft/playwright-cli");

        var result = SigstoreNpmProvenanceChecker.ParseAttestation(json);

        Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Outcome);
        Assert.NotNull(result.BundleNode);
        Assert.NotNull(result.BundleNode["dsseEnvelope"]);
        Assert.NotNull(result.Provenance);
        Assert.Equal("https://github.com/microsoft/playwright-cli", result.Provenance.SourceRepository);
        Assert.Equal(".github/workflows/publish.yml", result.Provenance.WorkflowPath);
        Assert.Equal("refs/tags/v0.1.1", result.Provenance.WorkflowRef);
    }

    [Fact]
    public void ParseAttestation_WithNoSlsaPredicate_ReturnsSlsaProvenanceNotFound()
    {
        var json = """
        {
            "attestations": [
                {
                    "predicateType": "https://github.com/npm/attestation/tree/main/specs/publish/v0.1",
                    "bundle": {
                        "dsseEnvelope": {
                            "payload": ""
                        }
                    }
                }
            ]
        }
        """;

        var result = SigstoreNpmProvenanceChecker.ParseAttestation(json);

        Assert.Equal(ProvenanceVerificationOutcome.SlsaProvenanceNotFound, result.Outcome);
    }

    [Fact]
    public void ParseAttestation_WithEmptyAttestations_ReturnsSlsaProvenanceNotFound()
    {
        var json = """{"attestations": []}""";

        var result = SigstoreNpmProvenanceChecker.ParseAttestation(json);

        Assert.Equal(ProvenanceVerificationOutcome.SlsaProvenanceNotFound, result.Outcome);
    }

    [Fact]
    public void ParseAttestation_WithInvalidJson_ReturnsAttestationParseFailed()
    {
        var result = SigstoreNpmProvenanceChecker.ParseAttestation("not valid json {{{");

        Assert.Equal(ProvenanceVerificationOutcome.AttestationParseFailed, result.Outcome);
    }

    [Fact]
    public void ParseAttestation_WithMissingPayload_ReturnsPayloadDecodeFailed()
    {
        var json = """
        {
            "attestations": [
                {
                    "predicateType": "https://slsa.dev/provenance/v1",
                    "bundle": {
                        "dsseEnvelope": {}
                    }
                }
            ]
        }
        """;

        var result = SigstoreNpmProvenanceChecker.ParseAttestation(json);

        Assert.Equal(ProvenanceVerificationOutcome.PayloadDecodeFailed, result.Outcome);
        Assert.NotNull(result.BundleNode);
    }

    [Fact]
    public void ParseProvenanceFromStatement_WithValidStatement_ReturnsProvenance()
    {
        var payload = BuildProvenancePayload("https://github.com/microsoft/playwright-cli");
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);

        var provenance = SigstoreNpmProvenanceChecker.ParseProvenanceFromStatement(bytes);

        Assert.NotNull(provenance);
        Assert.Equal("https://github.com/microsoft/playwright-cli", provenance.SourceRepository);
        Assert.Equal(".github/workflows/publish.yml", provenance.WorkflowPath);
        Assert.Equal("refs/tags/v0.1.1", provenance.WorkflowRef);
        Assert.Equal("https://github.com/actions/runner/github-hosted", provenance.BuilderId);
        Assert.Equal("https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1", provenance.BuildType);
    }

    [Fact]
    public void ParseProvenanceFromStatement_WithInvalidJson_ReturnsNull()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("not json");

        var provenance = SigstoreNpmProvenanceChecker.ParseProvenanceFromStatement(bytes);

        Assert.Null(provenance);
    }

    [Fact]
    public void VerifyProvenanceFields_WithAllFieldsMatching_ReturnsVerified()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            WorkflowRef = "refs/tags/v0.1.1",
            BuilderId = "https://github.com/actions/runner/github-hosted"
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            refInfo => refInfo.Kind == "tags");

        Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Outcome);
    }

    [Fact]
    public void VerifyProvenanceFields_WithSourceRepoMismatch_ReturnsSourceRepositoryMismatch()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/evil/repo",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            null);

        Assert.Equal(ProvenanceVerificationOutcome.SourceRepositoryMismatch, result.Outcome);
    }

    [Fact]
    public void VerifyProvenanceFields_WithWorkflowMismatch_ReturnsWorkflowMismatch()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/evil.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            null);

        Assert.Equal(ProvenanceVerificationOutcome.WorkflowMismatch, result.Outcome);
    }

    [Fact]
    public void VerifyProvenanceFields_WithBuildTypeMismatch_ReturnsBuildTypeMismatch()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://evil.example.com/build/v1",
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            null);

        Assert.Equal(ProvenanceVerificationOutcome.BuildTypeMismatch, result.Outcome);
    }

    [Fact]
    public void VerifyProvenanceFields_WithWorkflowRefValidationFailure_ReturnsWorkflowRefMismatch()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            WorkflowRef = "refs/heads/main"
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            refInfo => refInfo.Kind == "tags");

        Assert.Equal(ProvenanceVerificationOutcome.WorkflowRefMismatch, result.Outcome);
    }

    [Theory]
    [InlineData("https://github.com/microsoft/playwright-cli", "microsoft", "playwright-cli")]
    [InlineData("https://github.com/dotnet/aspire", "dotnet", "aspire")]
    [InlineData("https://github.com/owner/repo", "owner", "repo")]
    public void TryParseGitHubOwnerRepo_WithValidUrl_ReturnsTrueAndParsesComponents(string url, string expectedOwner, string expectedRepo)
    {
        var result = SigstoreNpmProvenanceChecker.TryParseGitHubOwnerRepo(url, out var owner, out var repo);

        Assert.True(result);
        Assert.Equal(expectedOwner, owner);
        Assert.Equal(expectedRepo, repo);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("https://github.com/")]
    [InlineData("https://github.com/only-owner")]
    public void TryParseGitHubOwnerRepo_WithInvalidUrl_ReturnsFalse(string url)
    {
        var result = SigstoreNpmProvenanceChecker.TryParseGitHubOwnerRepo(url, out _, out _);

        Assert.False(result);
    }

    private static string BuildAttestationJsonWithBundle(string sourceRepository)
    {
        var payload = BuildProvenancePayload(sourceRepository);
        var payloadBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));

        return $$"""
        {
            "attestations": [
                {
                    "predicateType": "https://slsa.dev/provenance/v1",
                    "bundle": {
                        "mediaType": "application/vnd.dev.sigstore.bundle.v0.3+json",
                        "dsseEnvelope": {
                            "payload": "{{payloadBase64}}",
                            "payloadType": "application/vnd.in-toto+json",
                            "signatures": [
                                {
                                    "sig": "MEUCIQC+fake+signature",
                                    "keyid": ""
                                }
                            ]
                        },
                        "verificationMaterial": {
                            "certificate": {
                                "rawBytes": "MIIFake..."
                            },
                            "tlogEntries": [
                                {
                                    "logIndex": "12345",
                                    "logId": {
                                        "keyId": "fake-key-id"
                                    },
                                    "kindVersion": {
                                        "kind": "dsse",
                                        "version": "0.0.1"
                                    },
                                    "integratedTime": "1700000000",
                                    "inclusionPromise": {
                                        "signedEntryTimestamp": "MEUC..."
                                    },
                                    "canonicalizedBody": "eyJ..."
                                }
                            ]
                        }
                    }
                }
            ]
        }
        """;
    }

    private static string BuildProvenancePayload(string sourceRepository)
    {
        return $$"""
        {
            "_type": "https://in-toto.io/Statement/v1",
            "subject": [
                {
                    "name": "pkg:npm/@playwright/cli@0.1.1",
                    "digest": { "sha512": "abc123" }
                }
            ],
            "predicateType": "https://slsa.dev/provenance/v1",
            "predicate": {
                "buildDefinition": {
                    "buildType": "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
                    "externalParameters": {
                        "workflow": {
                            "ref": "refs/tags/v0.1.1",
                            "repository": "{{sourceRepository}}",
                            "path": ".github/workflows/publish.yml"
                        }
                    }
                },
                "runDetails": {
                    "builder": {
                        "id": "https://github.com/actions/runner/github-hosted"
                    }
                }
            }
        }
        """;
    }
}
