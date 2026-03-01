// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Npm;

namespace Aspire.Cli.Tests.Agents;

public class NpmProvenanceCheckerTests
{
    [Fact]
    public void ParseProvenance_WithValidSlsaProvenance_ReturnsVerifiedWithData()
    {
        var json = BuildAttestationJson("https://github.com/microsoft/playwright-cli");

        var result = NpmProvenanceChecker.ParseProvenance(json);

        Assert.NotNull(result);
        Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Value.Outcome);
        Assert.Equal("https://github.com/microsoft/playwright-cli", result.Value.Provenance.SourceRepository);
        Assert.Equal(".github/workflows/publish.yml", result.Value.Provenance.WorkflowPath);
        Assert.Equal("https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1", result.Value.Provenance.BuildType);
        Assert.Equal("https://github.com/actions/runner/github-hosted", result.Value.Provenance.BuilderId);
        Assert.Equal("refs/tags/v0.1.1", result.Value.Provenance.WorkflowRef);
    }

    [Fact]
    public void ParseProvenance_WithDifferentRepository_ReturnsVerifiedWithThatRepository()
    {
        var json = BuildAttestationJson("https://github.com/attacker/malicious-package");

        var result = NpmProvenanceChecker.ParseProvenance(json);

        Assert.NotNull(result);
        Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Value.Outcome);
        Assert.Equal("https://github.com/attacker/malicious-package", result.Value.Provenance.SourceRepository);
    }

    [Fact]
    public void ParseProvenance_WithNoSlsaPredicate_ReturnsSlsaProvenanceNotFound()
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

        var result = NpmProvenanceChecker.ParseProvenance(json);

        Assert.NotNull(result);
        Assert.Equal(ProvenanceVerificationOutcome.SlsaProvenanceNotFound, result.Value.Outcome);
    }

    [Fact]
    public void ParseProvenance_WithEmptyAttestations_ReturnsSlsaProvenanceNotFound()
    {
        var json = """{"attestations": []}""";

        var result = NpmProvenanceChecker.ParseProvenance(json);

        Assert.NotNull(result);
        Assert.Equal(ProvenanceVerificationOutcome.SlsaProvenanceNotFound, result.Value.Outcome);
    }

    [Fact]
    public void ParseProvenance_WithMalformedJson_ThrowsException()
    {
        Assert.ThrowsAny<JsonException>(() => NpmProvenanceChecker.ParseProvenance("not json"));
    }

    [Fact]
    public void ParseProvenance_WithMissingWorkflowNode_ReturnsSourceRepositoryNotFound()
    {
        var statement = new JsonObject
        {
            ["_type"] = "https://in-toto.io/Statement/v1",
            ["predicateType"] = "https://slsa.dev/provenance/v1",
            ["predicate"] = new JsonObject
            {
                ["buildDefinition"] = new JsonObject
                {
                    ["externalParameters"] = new JsonObject()
                }
            }
        };

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(statement.ToJsonString()));
        var json = $$"""
        {
            "attestations": [
                {
                    "predicateType": "https://slsa.dev/provenance/v1",
                    "bundle": {
                        "dsseEnvelope": {
                            "payload": "{{payload}}"
                        }
                    }
                }
            ]
        }
        """;

        var result = NpmProvenanceChecker.ParseProvenance(json);

        Assert.NotNull(result);
        Assert.Equal(ProvenanceVerificationOutcome.SourceRepositoryNotFound, result.Value.Outcome);
    }

    [Fact]
    public void ParseProvenance_WithMissingPayload_ReturnsPayloadDecodeFailed()
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

        var result = NpmProvenanceChecker.ParseProvenance(json);

        Assert.NotNull(result);
        Assert.Equal(ProvenanceVerificationOutcome.PayloadDecodeFailed, result.Value.Outcome);
    }

    [Fact]
    public async Task VerifyProvenanceAsync_WithMismatchedWorkflowRef_ReturnsWorkflowRefMismatch()
    {
        var json = BuildAttestationJson(
            "https://github.com/microsoft/playwright-cli",
            workflowRef: "refs/tags/v9.9.9");

        var handler = new TestHttpMessageHandler(json);
        var httpClient = new HttpClient(handler);
        var checker = new NpmProvenanceChecker(httpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<NpmProvenanceChecker>.Instance);

        var result = await checker.VerifyProvenanceAsync(
            "@playwright/cli",
            "0.1.1",
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            refInfo => string.Equals(refInfo.Kind, "tags", StringComparison.Ordinal) &&
                       string.Equals(refInfo.Name, "v0.1.1", StringComparison.Ordinal),
            CancellationToken.None);

        Assert.Equal(ProvenanceVerificationOutcome.WorkflowRefMismatch, result.Outcome);
        Assert.Equal("refs/tags/v9.9.9", result.Provenance?.WorkflowRef);
    }

    [Fact]
    public async Task VerifyProvenanceAsync_WithMatchingWorkflowRef_ReturnsVerified()
    {
        var json = BuildAttestationJson(
            "https://github.com/microsoft/playwright-cli",
            workflowRef: "refs/tags/v0.1.1");

        var handler = new TestHttpMessageHandler(json);
        var httpClient = new HttpClient(handler);
        var checker = new NpmProvenanceChecker(httpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<NpmProvenanceChecker>.Instance);

        var result = await checker.VerifyProvenanceAsync(
            "@playwright/cli",
            "0.1.1",
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            refInfo => string.Equals(refInfo.Kind, "tags", StringComparison.Ordinal) &&
                       string.Equals(refInfo.Name, "v0.1.1", StringComparison.Ordinal),
            CancellationToken.None);

        Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Outcome);
    }

    [Fact]
    public async Task VerifyProvenanceAsync_WithNullCallback_SkipsRefValidation()
    {
        var json = BuildAttestationJson(
            "https://github.com/microsoft/playwright-cli",
            workflowRef: "refs/tags/any-format-at-all");

        var handler = new TestHttpMessageHandler(json);
        var httpClient = new HttpClient(handler);
        var checker = new NpmProvenanceChecker(httpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<NpmProvenanceChecker>.Instance);

        var result = await checker.VerifyProvenanceAsync(
            "@playwright/cli",
            "0.1.1",
            "https://github.com/microsoft/playwright-cli",
            ".github/workflows/publish.yml",
            "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1",
            validateWorkflowRef: null,
            CancellationToken.None);

        Assert.Equal(ProvenanceVerificationOutcome.Verified, result.Outcome);
    }

    private static string BuildAttestationJson(string sourceRepository, string workflowPath = ".github/workflows/publish.yml", string buildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1", string workflowRef = "refs/tags/v0.1.1")
    {
        var statement = new JsonObject
        {
            ["_type"] = "https://in-toto.io/Statement/v1",
            ["predicateType"] = "https://slsa.dev/provenance/v1",
            ["predicate"] = new JsonObject
            {
                ["buildDefinition"] = new JsonObject
                {
                    ["buildType"] = buildType,
                    ["externalParameters"] = new JsonObject
                    {
                        ["workflow"] = new JsonObject
                        {
                            ["repository"] = sourceRepository,
                            ["path"] = workflowPath,
                            ["ref"] = workflowRef
                        }
                    }
                },
                ["runDetails"] = new JsonObject
                {
                    ["builder"] = new JsonObject
                    {
                        ["id"] = "https://github.com/actions/runner/github-hosted"
                    }
                }
            }
        };

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(statement.ToJsonString()));

        var attestationResponse = new JsonObject
        {
            ["attestations"] = new JsonArray
            {
                new JsonObject
                {
                    ["predicateType"] = "https://slsa.dev/provenance/v1",
                    ["bundle"] = new JsonObject
                    {
                        ["dsseEnvelope"] = new JsonObject
                        {
                            ["payload"] = payload
                        }
                    }
                }
            }
        };

        return attestationResponse.ToJsonString();
    }

    private sealed class TestHttpMessageHandler(string responseContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
        }
    }

    [Theory]
    [InlineData("refs/tags/v0.1.1", "tags", "v0.1.1")]
    [InlineData("refs/heads/main", "heads", "main")]
    [InlineData("refs/tags/@scope/pkg@1.0.0", "tags", "@scope/pkg@1.0.0")]
    [InlineData("refs/tags/release/1.0.0", "tags", "release/1.0.0")]
    public void WorkflowRefInfo_TryParse_ValidRefs_ParsesCorrectly(string raw, string expectedKind, string expectedName)
    {
        var success = WorkflowRefInfo.TryParse(raw, out var refInfo);

        Assert.True(success);
        Assert.NotNull(refInfo);
        Assert.Equal(raw, refInfo.Raw);
        Assert.Equal(expectedKind, refInfo.Kind);
        Assert.Equal(expectedName, refInfo.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ref")]
    [InlineData("refs/")]
    [InlineData("refs/tags/")]
    public void WorkflowRefInfo_TryParse_InvalidRefs_ReturnsFalse(string? raw)
    {
        var success = WorkflowRefInfo.TryParse(raw, out var refInfo);

        Assert.False(success);
        Assert.Null(refInfo);
    }
}
