// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Npm;
using Sigstore;

namespace Aspire.Cli.Tests.Agents;

public class SigstoreNpmProvenanceCheckerTests
{
    #region ExtractSlsaBundleJson Tests

    [Fact]
    public void ExtractSlsaBundleJson_WithValidSlsaAttestation_ReturnsBundleJson()
    {
        var json = BuildAttestationJsonWithBundle("https://github.com/microsoft/playwright-cli");

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.NotNull(bundleJson);
        var bundleDoc = JsonDocument.Parse(bundleJson);
        Assert.True(bundleDoc.RootElement.TryGetProperty("dsseEnvelope", out _));
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithNoSlsaPredicate_ReturnsNull()
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

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithEmptyAttestations_ReturnsNull()
    {
        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson("""{"attestations": []}""");

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithInvalidJson_ReturnsNull()
    {
        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson("not valid json {{{");

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithMultipleMixedAttestations_FindsSlsaPredicate()
    {
        var json = $$"""
        {
            "attestations": [
                {
                    "predicateType": "https://github.com/npm/attestation/tree/main/specs/publish/v0.1",
                    "bundle": { "wrong": true }
                },
                {
                    "predicateType": "https://slsa.dev/provenance/v1",
                    "bundle": {
                        "dsseEnvelope": { "payload": "dGVzdA==", "payloadType": "application/vnd.in-toto+json" }
                    }
                }
            ]
        }
        """;

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.NotNull(bundleJson);
        var doc = JsonDocument.Parse(bundleJson);
        Assert.True(doc.RootElement.TryGetProperty("dsseEnvelope", out _));
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithNoBundleProperty_ReturnsNull()
    {
        var json = """
        {
            "attestations": [
                {
                    "predicateType": "https://slsa.dev/provenance/v1"
                }
            ]
        }
        """;

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    #endregion

    #region ExtractProvenanceFromResult Tests

    [Fact]
    public void ExtractProvenanceFromResult_WithStatementAndExtensions_ReturnsProvenance()
    {
        var result = BuildVerificationResult(
            sourceRepoUri: "https://github.com/microsoft/playwright-cli",
            sourceRepoRef: "refs/tags/v0.1.1",
            workflowPath: ".github/workflows/publish.yml",
            buildType: "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            builderId: "https://github.com/actions/runner/github-hosted",
            sourceRepoInPredicate: "https://github.com/microsoft/playwright-cli");

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Equal("https://github.com/microsoft/playwright-cli", provenance.SourceRepository);
        Assert.Equal(".github/workflows/publish.yml", provenance.WorkflowPath);
        Assert.Equal("refs/tags/v0.1.1", provenance.WorkflowRef);
        Assert.Equal("https://github.com/actions/runner/github-hosted", provenance.BuilderId);
        Assert.Equal("https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1", provenance.BuildType);
    }

    [Fact]
    public void ExtractProvenanceFromResult_PrefersExtensionsOverPredicate()
    {
        var result = BuildVerificationResult(
            sourceRepoUri: "https://github.com/microsoft/playwright-cli",
            sourceRepoRef: "refs/tags/v0.1.1",
            workflowPath: ".github/workflows/publish.yml",
            buildType: "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            builderId: "https://github.com/actions/runner/github-hosted",
            sourceRepoInPredicate: "https://github.com/evil/repo",
            workflowRefInPredicate: "refs/heads/main");

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        // Certificate extensions should win over predicate values
        Assert.Equal("https://github.com/microsoft/playwright-cli", provenance.SourceRepository);
        Assert.Equal("refs/tags/v0.1.1", provenance.WorkflowRef);
    }

    [Fact]
    public void ExtractProvenanceFromResult_WithNoStatement_ReturnsPartialProvenance()
    {
        var result = new VerificationResult
        {
            SignerIdentity = new VerifiedIdentity
            {
                SubjectAlternativeName = "https://github.com/microsoft/playwright-cli/.github/workflows/publish.yml@refs/tags/v0.1.1",
                Issuer = "https://token.actions.githubusercontent.com",
                Extensions = new FulcioCertificateExtensions
                {
                    SourceRepositoryUri = "https://github.com/microsoft/playwright-cli",
                    SourceRepositoryRef = "refs/tags/v0.1.1"
                }
            },
            Statement = null
        };

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Equal("https://github.com/microsoft/playwright-cli", provenance.SourceRepository);
        Assert.Equal("refs/tags/v0.1.1", provenance.WorkflowRef);
        Assert.Null(provenance.WorkflowPath);
        Assert.Null(provenance.BuildType);
        Assert.Null(provenance.BuilderId);
    }

    [Fact]
    public void ExtractProvenanceFromResult_WithNoExtensions_FallsToPredicate()
    {
        var result = BuildVerificationResult(
            sourceRepoUri: null,
            sourceRepoRef: null,
            workflowPath: ".github/workflows/publish.yml",
            buildType: "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            builderId: "https://github.com/actions/runner/github-hosted",
            sourceRepoInPredicate: "https://github.com/microsoft/playwright-cli",
            workflowRefInPredicate: "refs/tags/v0.1.1",
            includeExtensions: false);

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Equal("https://github.com/microsoft/playwright-cli", provenance.SourceRepository);
        Assert.Equal("refs/tags/v0.1.1", provenance.WorkflowRef);
    }

    [Fact]
    public void ExtractProvenanceFromResult_WithWrongPredicateType_ReturnsNullFields()
    {
        var predicateJson = """
        {
            "_type": "https://in-toto.io/Statement/v1",
            "predicateType": "https://example.com/custom/v1",
            "subject": [],
            "predicate": { "custom": true }
        }
        """;

        var statement = InTotoStatement.Parse(predicateJson);
        var result = new VerificationResult
        {
            SignerIdentity = new VerifiedIdentity
            {
                SubjectAlternativeName = "test",
                Issuer = "test",
                Extensions = new FulcioCertificateExtensions()
            },
            Statement = statement
        };

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Null(provenance.WorkflowPath);
        Assert.Null(provenance.BuildType);
        Assert.Null(provenance.BuilderId);
    }

    #endregion

    #region VerifyProvenanceFields Tests

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

    #endregion

    #region TryParseGitHubOwnerRepo Tests

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

    #endregion

    #region Adversarial Tests - Malformed JSON

    [Fact]
    public void ExtractSlsaBundleJson_WithDeeplyNestedJson_ReturnsNull()
    {
        // Deeply nested JSON should not cause stack overflow
        var depth = 1000;
        var json = new string('{', depth) + "\"attestations\":[]" + new string('}', depth);

        // Should either return null or handle gracefully (no exception)
        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        // The deeply nested JSON either parses (with attestations: []) or fails to parse
        // Either way, no SLSA provenance should be found
        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithTruncatedJson_ReturnsNull()
    {
        var json = """{"attestations": [{"predicateType": "https://slsa.dev/provenance/v1", "bundle": {"dsse""";

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithWrongJsonTypes_ReturnsNull()
    {
        // attestations is a string instead of array
        var json = """{"attestations": "not an array"}""";

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithNullAttestations_ReturnsNull()
    {
        var json = """{"attestations": null}""";

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithEmptyObject_ReturnsNull()
    {
        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson("{}");

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithEmptyString_ReturnsNull()
    {
        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson("");

        Assert.Null(bundleJson);
    }

    #endregion

    #region Adversarial Tests - Provenance Spoofing

    [Theory]
    [InlineData("https://github.com/micr0soft/playwright-cli")] // Homoglyph: zero instead of 'o'
    [InlineData("https://github.com/microsofт/playwright-cli")] // Homoglyph: Turkish dotless t
    [InlineData("https://github.com/microsoft-/playwright-cli")] // Trailing dash
    [InlineData("https://github.com/MICROSOFT/playwright-cli")] // Case should match (OrdinalIgnoreCase)
    public void VerifyProvenanceFields_WithSimilarRepositoryUrls_ChecksCorrectly(string spoofedUrl)
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = spoofedUrl,
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            null);

        // MICROSOFT should match (OrdinalIgnoreCase), all others should fail
        if (string.Equals(spoofedUrl, "https://github.com/microsoft/playwright-cli", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Outcome);
        }
        else
        {
            Assert.Equal(ProvenanceVerificationOutcome.SourceRepositoryMismatch, result.Outcome);
        }
    }

    [Theory]
    [InlineData("../../.github/workflows/evil.yml")]
    [InlineData(".github/workflows/../../../evil.yml")]
    [InlineData(".github/workflows/publish.yml\0evil")]
    [InlineData("")]
    public void VerifyProvenanceFields_WithWorkflowPathManipulation_RejectsInvalid(string spoofedPath)
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = spoofedPath,
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

    [Theory]
    [InlineData("refs/heads/main")] // Branch instead of tag
    [InlineData("refs/tags/v0.1.1/../../heads/main")] // Path traversal in ref
    [InlineData("refs/tags/")] // Empty tag name
    [InlineData("refs/")] // No kind or name
    [InlineData("tags/v0.1.1")] // Missing refs/ prefix
    [InlineData("")] // Empty string
    public void VerifyProvenanceFields_WithRefManipulation_RejectsInvalidRefs(string spoofedRef)
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            WorkflowRef = spoofedRef
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            refInfo => string.Equals(refInfo.Kind, "tags", StringComparison.Ordinal) &&
                       string.Equals(refInfo.Name, "v0.1.1", StringComparison.Ordinal));

        Assert.Equal(ProvenanceVerificationOutcome.WorkflowRefMismatch, result.Outcome);
    }

    [Fact]
    public void VerifyProvenanceFields_WithNullWorkflowRef_ReturnsWorkflowRefMismatch()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            WorkflowRef = null
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            refInfo => refInfo.Kind == "tags");

        Assert.Equal(ProvenanceVerificationOutcome.WorkflowRefMismatch, result.Outcome);
    }

    #endregion

    #region Adversarial Tests - Build Type Spoofing

    [Theory]
    [InlineData("https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1?inject=true")]
    [InlineData("https://evil.com/github-actions-buildtypes/workflow/v1")]
    [InlineData("")]
    public void VerifyProvenanceFields_WithBuildTypeSpoofing_Rejects(string spoofedBuildType)
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = spoofedBuildType,
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
    public void VerifyProvenanceFields_WithNullBuildType_ReturnsBuildTypeMismatch()
    {
        var provenance = new NpmProvenanceData
        {
            SourceRepository = "https://github.com/microsoft/playwright-cli",
            WorkflowPath = ".github/workflows/publish.yml",
            BuildType = null,
        };

        var result = SigstoreNpmProvenanceChecker.VerifyProvenanceFields(
            provenance,
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            null);

        Assert.Equal(ProvenanceVerificationOutcome.BuildTypeMismatch, result.Outcome);
    }

    #endregion

    #region Adversarial Tests - URL Parsing Edge Cases

    [Theory]
    [InlineData("https://github.com.evil.com/microsoft/playwright-cli")] // Subdomain attack
    [InlineData("https://githüb.com/microsoft/playwright-cli")] // Unicode domain
    [InlineData("ftp://github.com/microsoft/playwright-cli")] // Wrong scheme
    public void TryParseGitHubOwnerRepo_WithSuspiciousUrls_HandlesCorrectly(string url)
    {
        var result = SigstoreNpmProvenanceChecker.TryParseGitHubOwnerRepo(url, out var owner, out var repo);

        // These are syntactically valid URLs so TryParseGitHubOwnerRepo will succeed,
        // but the domain mismatch would be caught by Sigstore's certificate identity check
        // (SAN pattern matching against github.com). TryParseGitHubOwnerRepo only extracts
        // the path segments — the security boundary is in the VerificationPolicy.
        if (result)
        {
            // Verify it at least parsed the path segments
            Assert.False(string.IsNullOrEmpty(owner));
            Assert.False(string.IsNullOrEmpty(repo));
        }
    }

    [Theory]
    [InlineData("https://github.com/microsoft/playwright-cli/../evil-repo")]
    [InlineData("https://github.com/microsoft/playwright-cli/extra/segments")]
    public void TryParseGitHubOwnerRepo_WithExtraPathSegments_ExtractsFirstTwo(string url)
    {
        var result = SigstoreNpmProvenanceChecker.TryParseGitHubOwnerRepo(url, out var owner, out _);

        Assert.True(result);
        Assert.Equal("microsoft", owner);
        // URI normalization resolves ".." so the path may differ
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relative/path")]
    public void TryParseGitHubOwnerRepo_WithNonAbsoluteUri_ReturnsFalse(string url)
    {
        var result = SigstoreNpmProvenanceChecker.TryParseGitHubOwnerRepo(url, out _, out _);

        Assert.False(result);
    }

    #endregion

    #region Adversarial Tests - Statement Extraction Edge Cases

    [Fact]
    public void ExtractProvenanceFromResult_WithMissingPredicateFields_ReturnsPartialData()
    {
        var predicateJson = """
        {
            "_type": "https://in-toto.io/Statement/v1",
            "predicateType": "https://slsa.dev/provenance/v1",
            "subject": [],
            "predicate": {
                "buildDefinition": {
                    "buildType": "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1"
                }
            }
        }
        """;

        var statement = InTotoStatement.Parse(predicateJson);
        var result = new VerificationResult
        {
            SignerIdentity = new VerifiedIdentity
            {
                SubjectAlternativeName = "test",
                Issuer = "test",
                Extensions = new FulcioCertificateExtensions()
            },
            Statement = statement
        };

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Equal("https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1", provenance.BuildType);
        Assert.Null(provenance.WorkflowPath);
        Assert.Null(provenance.BuilderId);
    }

    [Fact]
    public void ExtractProvenanceFromResult_WithEmptyPredicate_ReturnsNullFields()
    {
        var predicateJson = """
        {
            "_type": "https://in-toto.io/Statement/v1",
            "predicateType": "https://slsa.dev/provenance/v1",
            "subject": [],
            "predicate": {}
        }
        """;

        var statement = InTotoStatement.Parse(predicateJson);
        var result = new VerificationResult
        {
            SignerIdentity = new VerifiedIdentity
            {
                SubjectAlternativeName = "test",
                Issuer = "test",
                Extensions = new FulcioCertificateExtensions()
            },
            Statement = statement
        };

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Null(provenance.BuildType);
        Assert.Null(provenance.WorkflowPath);
    }

    [Fact]
    public void ExtractProvenanceFromResult_WithNullSignerIdentity_ReturnsProvenanceFromPredicate()
    {
        var result = BuildVerificationResult(
            sourceRepoUri: null,
            sourceRepoRef: null,
            workflowPath: ".github/workflows/publish.yml",
            buildType: "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            builderId: "https://github.com/actions/runner/github-hosted",
            sourceRepoInPredicate: "https://github.com/microsoft/playwright-cli",
            includeIdentity: false);

        var provenance = SigstoreNpmProvenanceChecker.ExtractProvenanceFromResult(result);

        Assert.NotNull(provenance);
        Assert.Equal("https://github.com/microsoft/playwright-cli", provenance.SourceRepository);
    }

    #endregion

    #region Adversarial Tests - Attestation Structure

    [Fact]
    public void ExtractSlsaBundleJson_WithNullPredicateType_ReturnsNull()
    {
        var json = """
        {
            "attestations": [
                {
                    "bundle": { "dsseEnvelope": {} }
                }
            ]
        }
        """;

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    [Fact]
    public void ExtractSlsaBundleJson_WithCaseSensitivePredicateType_ReturnsNull()
    {
        // Predicate type comparison is case-sensitive (Ordinal)
        var json = """
        {
            "attestations": [
                {
                    "predicateType": "HTTPS://SLSA.DEV/PROVENANCE/V1",
                    "bundle": { "dsseEnvelope": {} }
                }
            ]
        }
        """;

        var bundleJson = SigstoreNpmProvenanceChecker.ExtractSlsaBundleJson(json);

        Assert.Null(bundleJson);
    }

    #endregion

    #region WorkflowRefInfo Adversarial Tests

    [Theory]
    [InlineData("refs/tags/v1.0.0", true, "tags", "v1.0.0")]
    [InlineData("refs/heads/main", true, "heads", "main")]
    [InlineData("refs/tags/@scope/pkg@1.0.0", true, "tags", "@scope/pkg@1.0.0")]
    [InlineData("refs/tags/", false, null, null)] // Empty name
    [InlineData("refs/", false, null, null)] // No kind/name
    [InlineData("", false, null, null)] // Empty string
    [InlineData("heads/main", false, null, null)] // Missing refs/ prefix
    [InlineData("refs/tags/v1/../../../etc/passwd", true, "tags", "v1/../../../etc/passwd")] // Path traversal in name (accepted as-is)
    public void WorkflowRefInfo_TryParse_HandlesEdgeCases(string? input, bool expectedSuccess, string? expectedKind, string? expectedName)
    {
        var result = WorkflowRefInfo.TryParse(input, out var refInfo);

        Assert.Equal(expectedSuccess, result);
        if (expectedSuccess)
        {
            Assert.NotNull(refInfo);
            Assert.Equal(expectedKind, refInfo.Kind);
            Assert.Equal(expectedName, refInfo.Name);
        }
        else
        {
            Assert.Null(refInfo);
        }
    }

    #endregion

    #region Test Helpers

    private static VerificationResult BuildVerificationResult(
        string? sourceRepoUri,
        string? sourceRepoRef,
        string? workflowPath,
        string? buildType,
        string? builderId,
        string? sourceRepoInPredicate = null,
        string? workflowRefInPredicate = null,
        bool includeExtensions = true,
        bool includeIdentity = true)
    {
        var predicateJson = BuildSlsaPredicateStatementJson(
            sourceRepoInPredicate ?? sourceRepoUri ?? "https://github.com/test/repo",
            workflowPath ?? ".github/workflows/test.yml",
            workflowRefInPredicate ?? sourceRepoRef ?? "refs/tags/v0.0.1",
            buildType ?? "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            builderId ?? "https://github.com/actions/runner/github-hosted");

        var statement = InTotoStatement.Parse(predicateJson);

        VerifiedIdentity? identity = null;
        if (includeIdentity)
        {
            identity = new VerifiedIdentity
            {
                SubjectAlternativeName = "https://github.com/test/repo/.github/workflows/test.yml@refs/tags/v0.0.1",
                Issuer = "https://token.actions.githubusercontent.com",
                Extensions = includeExtensions ? new FulcioCertificateExtensions
                {
                    SourceRepositoryUri = sourceRepoUri,
                    SourceRepositoryRef = sourceRepoRef
                } : null
            };
        }

        return new VerificationResult
        {
            SignerIdentity = identity,
            Statement = statement
        };
    }

    private static string BuildSlsaPredicateStatementJson(
        string sourceRepository,
        string workflowPath,
        string workflowRef,
        string buildType,
        string builderId)
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
                    "buildType": "{{buildType}}",
                    "externalParameters": {
                        "workflow": {
                            "ref": "{{workflowRef}}",
                            "repository": "{{sourceRepository}}",
                            "path": "{{workflowPath}}"
                        }
                    }
                },
                "runDetails": {
                    "builder": {
                        "id": "{{builderId}}"
                    }
                }
            }
        }
        """;
    }

    private static string BuildAttestationJsonWithBundle(string sourceRepository)
    {
        var payload = BuildSlsaPredicateStatementJson(
            sourceRepository,
            ".github/workflows/publish.yml",
            "refs/tags/v0.1.1",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            "https://github.com/actions/runner/github-hosted");
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

    #endregion
}
