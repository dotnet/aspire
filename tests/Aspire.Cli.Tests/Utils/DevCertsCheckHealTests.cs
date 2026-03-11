// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.Utils.EnvironmentChecker;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Utils;

public class DevCertsCheckHealTests
{
    [Fact]
    public void EvaluateHealActions_NoCertificates_RecommendsTrust()
    {
        var actions = DevCertsCheck.EvaluateHealActions([], CreateHealActions());

        Assert.Single(actions);
        Assert.Equal("trust", actions[0].Name);
    }

    [Fact]
    public void EvaluateHealActions_SingleUntrustedCert_RecommendsTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.None, "AABB1234", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Single(actions);
        Assert.Equal("trust", actions[0].Name);
    }

    [Fact]
    public void EvaluateHealActions_SingleFullyTrustedCert_ReturnsEmpty()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Empty(actions);
    }

    [Fact]
    public void EvaluateHealActions_MultipleCerts_SomeUntrusted_RecommendsCleanThenTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 4),
            new(CertificateManager.TrustLevel.None, "CCDD5678", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Equal(2, actions.Count);
        Assert.Equal("clean", actions[0].Name);
        Assert.Equal("trust", actions[1].Name);
    }

    [Fact]
    public void EvaluateHealActions_OldVersionCert_RecommendsCleanThenTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 1)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Equal(2, actions.Count);
        Assert.Equal("clean", actions[0].Name);
        Assert.Equal("trust", actions[1].Name);
    }

    [Fact]
    public void EvaluateHealActions_PartiallyTrustedCert_ReturnsEmpty()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Partial, "AABB1234", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Empty(actions);
    }

    [Fact]
    public void EvaluateHealActions_MultipleAllTrusted_ReturnsEmpty()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateManager.TrustLevel.Full, "AABB1234", 4),
            new(CertificateManager.TrustLevel.Full, "CCDD5678", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Empty(actions);
    }

    [Fact]
    public async Task HealAsync_CleanAction_CallsCleanOnly()
    {
        var cleanCalled = false;
        var trustCalled = false;
        var runner = new TestHealCertificateToolRunner
        {
            CleanResult = () =>
            {
                cleanCalled = true;
                return true;
            },
            TrustResult = () =>
            {
                trustCalled = true;
                return EnsureCertificateResult.ExistingHttpsCertificateTrusted;
            }
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("clean", CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(cleanCalled);
        Assert.False(trustCalled);
        Assert.Contains("cleaned successfully", result.Message);
    }

    [Fact]
    public async Task HealAsync_CleanAction_CleanFails_ReturnsFailure()
    {
        var runner = new TestHealCertificateToolRunner
        {
            CleanResult = () => false
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("clean", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Failed to clean", result.Message);
    }

    [Fact]
    public async Task HealAsync_TrustAction_CallsTrustOnly()
    {
        var cleanCalled = false;
        var trustCalled = false;
        var runner = new TestHealCertificateToolRunner
        {
            CleanResult = () =>
            {
                cleanCalled = true;
                return true;
            },
            TrustResult = () =>
            {
                trustCalled = true;
                return EnsureCertificateResult.ExistingHttpsCertificateTrusted;
            }
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("trust", CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(trustCalled);
        Assert.False(cleanCalled);
        Assert.Contains("trusted successfully", result.Message);
    }

    [Fact]
    public async Task HealAsync_TrustAction_TrustFails_ReportsFailure()
    {
        var runner = new TestHealCertificateToolRunner
        {
            TrustResult = () => EnsureCertificateResult.FailedToTrustTheCertificate
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("trust", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Failed to trust", result.Message);
    }

    [Fact]
    public async Task HealAsync_UnknownAction_ReturnsFailure()
    {
        var runner = new TestHealCertificateToolRunner();
        var check = CreateCheck(runner);

        var result = await check.HealAsync("nonexistent", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unknown action", result.Message);
    }

    [Fact]
    public void HealCommandName_ReturnsCertificates()
    {
        var runner = new TestHealCertificateToolRunner();
        var check = CreateCheck(runner);

        Assert.Equal("certificates", check.HealCommandName);
    }

    [Fact]
    public void HealActions_ContainsCleanAndTrust()
    {
        var runner = new TestHealCertificateToolRunner();
        var check = CreateCheck(runner);

        Assert.Equal(2, check.HealActions.Count);
        Assert.Contains(check.HealActions, a => a.Name == "clean");
        Assert.Contains(check.HealActions, a => a.Name == "trust");
        Assert.All(check.HealActions, a => Assert.False(string.IsNullOrWhiteSpace(a.ProgressDescription)));
    }

    [Fact]
    public async Task EvaluateAsync_DoesNotThrow()
    {
        var runner = new TestHealCertificateToolRunner();
        var check = CreateCheck(runner);

        var actions = await check.EvaluateAsync(CancellationToken.None);

        // The result depends on actual machine cert state, but it should not throw
        Assert.NotNull(actions);
    }

    private static IHealableEnvironmentCheck CreateCheck(ICertificateToolRunner runner)
    {
        return new DevCertsCheck(NullLogger<DevCertsCheck>.Instance, runner);
    }

    private static HealActionCollection CreateHealActions() => new()
    {
        new("clean", "Clean all development certificates", "Cleaning development certificates...", _ => Task.FromResult(new HealResult(true, "Cleaned"))),
        new("trust", "Clean all development certificates and create a new trusted one", "Cleaning and trusting development certificate...", _ => Task.FromResult(new HealResult(true, "Trusted")))
    };

    /// <summary>
    /// Minimal ICertificateToolRunner implementation for heal tests.
    /// Only trust and clean methods are functional; check returns a default result.
    /// </summary>
    private sealed class TestHealCertificateToolRunner : ICertificateToolRunner
    {
        public Func<EnsureCertificateResult>? TrustResult { get; set; }
        public Func<bool>? CleanResult { get; set; }

        public EnsureCertificateResult TrustHttpCertificate()
            => TrustResult?.Invoke() ?? EnsureCertificateResult.ExistingHttpsCertificateTrusted;

        public bool CleanHttpCertificate()
            => CleanResult?.Invoke() ?? true;

        public CertificateTrustResult CheckHttpCertificate()
            => new CertificateTrustResult { HasCertificates = true, TrustLevel = CertificateManager.TrustLevel.Full, Certificates = [] };
    }
}
