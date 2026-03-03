// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Npm;

namespace Aspire.Cli.Tests.Agents;

public class SigstoreNpmProvenanceCheckerTests
{
    [Fact]
    public void FindSlsaProvenanceBundle_WithValidSlsaAttestation_ReturnsBundle()
    {
        var json = BuildAttestationJsonWithBundle("https://github.com/microsoft/playwright-cli");

        var bundle = SigstoreNpmProvenanceChecker.FindSlsaProvenanceBundle(json);

        Assert.NotNull(bundle);
        Assert.NotNull(bundle["dsseEnvelope"]);
    }

    [Fact]
    public void FindSlsaProvenanceBundle_WithNoSlsaPredicate_ReturnsNull()
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

        var bundle = SigstoreNpmProvenanceChecker.FindSlsaProvenanceBundle(json);

        Assert.Null(bundle);
    }

    [Fact]
    public void FindSlsaProvenanceBundle_WithEmptyAttestations_ReturnsNull()
    {
        var json = """{"attestations": []}""";

        var bundle = SigstoreNpmProvenanceChecker.FindSlsaProvenanceBundle(json);

        Assert.Null(bundle);
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
