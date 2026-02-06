// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils.EnvironmentChecker;

namespace Aspire.Cli.Tests.Utils;

public class DevCertsCheckTests
{
    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_AllTrusted_ReturnsPass()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AAAA1111BBBB2222", 4),
            new(CertificateTrustLevel.Full, "CCCC3333DDDD4444", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Pass, devCertsResult.Status);
        Assert.Contains("trusted", devCertsResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_NoneTrusted_ReturnsWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.None, "AAAA1111BBBB2222", 4),
            new(CertificateTrustLevel.None, "CCCC3333DDDD4444", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Warning, devCertsResult.Status);
        Assert.Contains("none are trusted", devCertsResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_SomeUntrusted_ReturnsWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AAAA1111BBBB2222", 4),
            new(CertificateTrustLevel.None, "CCCC3333DDDD4444", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Warning, devCertsResult.Status);
        Assert.Contains("Multiple HTTPS development certificates found", devCertsResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_SingleCert_Trusted_ReturnsPass()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AAAA1111BBBB2222", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Pass, devCertsResult.Status);
        Assert.Contains("trusted", devCertsResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_SingleCert_Untrusted_ReturnsWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.None, "AAAA1111BBBB2222", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Warning, devCertsResult.Status);
        Assert.Contains("not trusted", devCertsResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_SingleCert_PartiallyTrusted_ReturnsWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Partial, "AAAA1111BBBB2222", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Warning, devCertsResult.Status);
        Assert.Contains("partially trusted", devCertsResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_OldTrustedCert_ReturnsVersionWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AAAA1111BBBB2222", 2),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        Assert.Equal(2, results.Count);
        var versionResult = Assert.Single(results, r => r.Name == "dev-certs-version");
        Assert.Equal(EnvironmentCheckStatus.Warning, versionResult.Status);
        Assert.Contains("older version", versionResult.Message);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_AllTrusted_NoVersionWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AAAA1111BBBB2222", 4),
            new(CertificateTrustLevel.Full, "CCCC3333DDDD4444", 5),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        // Should only have the pass result, no version warning
        var devCertsResult = Assert.Single(results);
        Assert.Equal("dev-certs", devCertsResult.Name);
        Assert.Equal(EnvironmentCheckStatus.Pass, devCertsResult.Status);
    }

    [Fact]
    public void EvaluateCertificateResults_MultipleCerts_AllPartiallyTrusted_ReturnsPass()
    {
        // Partially trusted counts as trusted (not None), so all certs are "trusted"
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Partial, "AAAA1111BBBB2222", 4),
            new(CertificateTrustLevel.Partial, "CCCC3333DDDD4444", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        // Should not have a "Multiple certs" warning since all are trusted
        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.NotEqual(EnvironmentCheckStatus.Warning, devCertsResult.Status);
    }

    [Fact]
    public void EvaluateCertificateResults_ThreeCerts_TwoTrustedOneNot_ReturnsWarning()
    {
        var certs = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AAAA1111BBBB2222", 4),
            new(CertificateTrustLevel.Full, "CCCC3333DDDD4444", 4),
            new(CertificateTrustLevel.None, "EEEE5555FFFF6666", 4),
        };

        var results = DevCertsCheck.EvaluateCertificateResults(certs);

        var devCertsResult = Assert.Single(results, r => r.Name == "dev-certs");
        Assert.Equal(EnvironmentCheckStatus.Warning, devCertsResult.Status);
        Assert.Contains("3 certificates", devCertsResult.Message);
    }
}
