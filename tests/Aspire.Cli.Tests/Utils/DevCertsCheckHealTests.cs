// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.DotNet;
using Aspire.Cli.Utils.EnvironmentChecker;
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
            new(CertificateTrustLevel.None, "AABB1234", 4)
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
            new(CertificateTrustLevel.Full, "AABB1234", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Empty(actions);
    }

    [Fact]
    public void EvaluateHealActions_MultipleCerts_RecommendsTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AABB1234", 4),
            new(CertificateTrustLevel.None, "CCDD5678", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Single(actions);
        Assert.Equal("trust", actions[0].Name);
    }

    [Fact]
    public void EvaluateHealActions_OldVersionCert_RecommendsTrust()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AABB1234", 1)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Single(actions);
        Assert.Equal("trust", actions[0].Name);
    }

    [Fact]
    public void EvaluateHealActions_PartiallyTrustedCert_ReturnsEmpty()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Partial, "AABB1234", 4)
        };

        var actions = DevCertsCheck.EvaluateHealActions(certInfos, CreateHealActions());

        Assert.Empty(actions);
    }

    [Fact]
    public void EvaluateHealActions_MultipleAllTrusted_ReturnsEmpty()
    {
        var certInfos = new List<CertificateInfo>
        {
            new(CertificateTrustLevel.Full, "AABB1234", 4),
            new(CertificateTrustLevel.Full, "CCDD5678", 4)
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
            CleanResult = (_, _) =>
            {
                cleanCalled = true;
                return 0;
            },
            TrustResult = (_, _) =>
            {
                trustCalled = true;
                return 0;
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
            CleanResult = (_, _) => 1
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("clean", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Failed to clean", result.Message);
    }

    [Fact]
    public async Task HealAsync_TrustAction_CallsCleanThenTrust()
    {
        var callOrder = new List<string>();
        var runner = new TestHealCertificateToolRunner
        {
            CleanResult = (_, _) =>
            {
                callOrder.Add("clean");
                return 0;
            },
            TrustResult = (_, _) =>
            {
                callOrder.Add("trust");
                return 0;
            }
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("trust", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(["clean", "trust"], callOrder);
        Assert.Contains("cleaned and new certificate trusted", result.Message);
    }

    [Fact]
    public async Task HealAsync_TrustAction_CleanFails_DoesNotTrust()
    {
        var trustCalled = false;
        var runner = new TestHealCertificateToolRunner
        {
            CleanResult = (_, _) => 1,
            TrustResult = (_, _) =>
            {
                trustCalled = true;
                return 0;
            }
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("trust", CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(trustCalled);
        Assert.Contains("Failed to clean", result.Message);
    }

    [Fact]
    public async Task HealAsync_TrustAction_TrustFails_ReportsFailure()
    {
        var runner = new TestHealCertificateToolRunner
        {
            CleanResult = (_, _) => 0,
            TrustResult = (_, _) => 1
        };

        var check = CreateCheck(runner);
        var result = await check.HealAsync("trust", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("failed to trust", result.Message);
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

    private static DevCertsCheck CreateCheck(ICertificateToolRunner runner)
    {
        return new DevCertsCheck(NullLogger<DevCertsCheck>.Instance, runner);
    }

    private static List<HealAction> CreateHealActions() =>
    [
        new("clean", "Clean all development certificates", "Cleaning development certificates..."),
        new("trust", "Clean all development certificates and create a new trusted one", "Cleaning and trusting development certificate...")
    ];

    /// <summary>
    /// Minimal ICertificateToolRunner implementation for heal tests.
    /// Only trust and clean methods are functional; check returns a default result.
    /// </summary>
    private sealed class TestHealCertificateToolRunner : ICertificateToolRunner
    {
        public Func<DotNetCliRunnerInvocationOptions, CancellationToken, int>? TrustResult { get; set; }
        public Func<DotNetCliRunnerInvocationOptions, CancellationToken, int>? CleanResult { get; set; }

        public Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(TrustResult?.Invoke(options, cancellationToken) ?? 0);

        public Task<int> CleanHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult(CleanResult?.Invoke(options, cancellationToken) ?? 0);

        public Task<(int ExitCode, CertificateTrustResult? Result)> CheckHttpCertificateMachineReadableAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
            => Task.FromResult<(int, CertificateTrustResult?)>((0, new CertificateTrustResult { HasCertificates = true, TrustLevel = DevCertTrustLevel.Full, Certificates = [] }));
    }
}
