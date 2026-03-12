// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils.EnvironmentChecker;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Aspire.Cli.Tests.Utils;

public class DevCertsCheckFixRecommendationTests
{
    [Fact]
    public void EvaluateCertificateResults_NoCertificates_RecommendsTrust()
    {
        var results = DevCertsCheck.EvaluateCertificateResults([]);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Warning, result.Status);
        Assert.NotNull(result.Fix);
        Assert.Contains("aspire certs trust", result.Fix);
        Assert.DoesNotContain("aspire certs clean", result.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_SingleUntrustedCert_RecommendsTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.None, "AABB1234", 4)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Warning, result.Status);
        Assert.NotNull(result.Fix);
        Assert.Contains("aspire certs trust", result.Fix);
        Assert.DoesNotContain("aspire certs clean", result.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_SingleFullyTrustedCert_ReportsPass()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 4)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Pass, result.Status);
        Assert.Null(result.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_SomeUntrusted_RecommendsCleanAndTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 4),
            new(CertificateManager.TrustLevel.None, "CCDD5678", 4)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Warning, result.Status);
        Assert.NotNull(result.Fix);
        Assert.Contains("aspire certs clean", result.Fix);
        Assert.Contains("aspire certs trust", result.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_NoneUntrusted_RecommendsCleanAndTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.None, "AABB1234", 4),
            new(CertificateManager.TrustLevel.None, "CCDD5678", 4)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Warning, result.Status);
        Assert.NotNull(result.Fix);
        Assert.Contains("aspire certs clean", result.Fix);
        Assert.Contains("aspire certs trust", result.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_OldVersionCert_RecommendsCleanAndTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 1)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        // Should have two results: pass for trust status, warning for old version
        Assert.Equal(2, results.Count);
        var versionWarning = results.First(r => r.Name == "dev-certs-version");
        Assert.Equal(EnvironmentCheckStatus.Warning, versionWarning.Status);
        Assert.NotNull(versionWarning.Fix);
        Assert.Contains("aspire certs clean", versionWarning.Fix);
        Assert.Contains("aspire certs trust", versionWarning.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_PartiallyTrustedCert_RecommendsSslCertDir()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Partial, "AABB1234", 4)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Warning, result.Status);
        Assert.NotNull(result.Fix);
        // Partial trust fix involves SSL_CERT_DIR, not aspire certs commands
        Assert.DoesNotContain("aspire certs", result.Fix);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleAllTrusted_ReportsPass()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 4),
            new(CertificateManager.TrustLevel.Full, "CCDD5678", 4)
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certInfos);

        var result = Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Pass, result.Status);
    }

    [Fact]
    public void IsSuccessfulTrustResult_SuccessResults_ReturnsTrue()
    {
        Assert.True(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.Succeeded));
        Assert.True(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.ValidCertificatePresent));
        Assert.True(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.ExistingHttpsCertificateTrusted));
        Assert.True(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.NewHttpsCertificateTrusted));
    }

    [Fact]
    public void IsSuccessfulTrustResult_FailureResults_ReturnsFalse()
    {
        Assert.False(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.FailedToTrustTheCertificate));
        Assert.False(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.ErrorCreatingTheCertificate));
        Assert.False(DevCertsCheck.IsSuccessfulTrustResult(EnsureCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore));
    }
}
