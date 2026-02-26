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

    private static string BuildAttestationJson(string sourceRepository, string workflowPath = ".github/workflows/publish.yml", string buildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1")
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
                            ["path"] = workflowPath
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
}
